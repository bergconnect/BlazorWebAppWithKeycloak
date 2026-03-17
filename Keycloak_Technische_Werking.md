# Keycloak Integratie — Technische Werking

Dit document beschrijft stap voor stap hoe de Keycloak-authenticatie werkt in deze applicatie: van het eerste inlogverzoek tot en met het uitvoeren van een beveiligde API-aanroep. Het is bedoeld als technische naslaggids bij het lezen van de broncode.

---

## Inhoudsopgave

- [Overzicht](#overzicht)
- [Betrokken componenten](#betrokken-componenten)
- [1. Inloggen — Authorization Code Flow](#1-inloggen--authorization-code-flow)
- [2. Blazor opstart — tokens laden](#2-blazor-opstart--tokens-laden)
- [3. API aanroepen — BearerTokenHandler](#3-api-aanroepen--bearertokenhandler)
- [4. Token refresh](#4-token-refresh)
- [5. Sessie verlopen — Session not active](#5-sessie-verlopen--session-not-active)
- [6. Uitloggen](#6-uitloggen)
- [7. JWT validatie in de API](#7-jwt-validatie-in-de-api)
- [8. Rollen en autorisatie](#8-rollen-en-autorisatie)
- [9. Cookies](#9-cookies)
- [10. Keycloak configuratie die de werking beïnvloedt](#10-keycloak-configuratie-die-de-werking-beïnvloedt)

---

## Overzicht

De applicatie bestaat uit twee services:

- **Blazor Web App** — de frontend, draait op de server (InteractiveServer rendermode)
- **API** — een Minimal API die data levert (todo-items)

Keycloak fungeert als Identity Provider (IdP). De Blazor app authenticeert gebruikers via Keycloak en gebruikt de verkregen tokens om namens de gebruiker de API aan te roepen.

```
Browser ──── HTTPS ────► Blazor Web App ──── Bearer token ────► API
                               │
                               │ Authorization Code Flow
                               ▼
                           Keycloak IdP
```

---

## Betrokken componenten

| Component | Bestand | Verantwoordelijkheid |
|---|---|---|
| `ConfigureKeycloakOptions` | `Auth/ConfigureKeycloakOptions.cs` | Configureert de OIDC-middleware |
| `AuthServiceExtensions` | `Auth/AuthServiceExtensions.cs` | Registreert authenticatie en cookie-instellingen |
| `AuthEndpointExtensions` | `Auth/AuthEndpointExtensions.cs` | `/login` en `/logout` endpoints |
| `TokenProvider` | `Services/TokenProvider.cs` | Houdt tokens in memory per Blazor circuit |
| `TokenService` | `Services/TokenService.cs` | Voert token refresh uit bij Keycloak |
| `BearerTokenHandler` | `Services/BearerTokenHandler.cs` | Voegt Bearer token toe aan API-requests |
| `ConfigureJwtBearerOptions` | `API/Auth/ConfigureJwtBearerOptions.cs` | Configureert JWT-validatie in de API |

---

## 1. Inloggen — Authorization Code Flow

### Stap 1 — Gebruiker klikt op Inloggen

De gebruiker klikt op de inlogknop in `NavMenu.razor`. Dit stuurt de browser naar het `/login` endpoint:

```
GET /login?returnUrl=%2Ftodos
```

### Stap 2 — Challenge naar Keycloak

Het `/login` endpoint in `AuthEndpointExtensions.cs` roept `ChallengeAsync` aan. De OIDC-middleware bouwt een redirect-URL naar Keycloak:

```
https://<keycloak>/realms/<realm>/protocol/openid-connect/auth
  ?client_id=blazor-web-app
  &redirect_uri=http://localhost:5000/signin-oidc
  &response_type=code
  &scope=openid profile email
  &code_challenge=<PKCE challenge>
  &code_challenge_method=S256
  &response_mode=form_post
  &nonce=<random>
  &state=<encrypted state>
```

Twee tijdelijke cookies worden gezet:
- `.AspNetCore.OpenIdConnect.Nonce.*` — voor nonce-validatie
- `.AspNetCore.Correlation.*` — voor state-validatie (CSRF-bescherming)

> **PAR uitgeschakeld:** ASP.NET Core 9+ stuurt standaard Pushed Authorization Requests. Omdat Keycloak dit per client vereist en het hier niet is geconfigureerd, staat `PushedAuthorizationBehavior.Disable` in `ConfigureKeycloakOptions`.

### Stap 3 — Gebruiker logt in bij Keycloak

De browser toont de Keycloak-loginpagina. Na een succesvolle login stuurt Keycloak een `form_post` naar `/signin-oidc`:

```
POST /signin-oidc
  code=<authorization code>
  state=<state>
```

### Stap 4 — Code uitwisselen voor tokens (backchannel)

De OIDC-middleware valideert de `state` en wisselt de code in via een backchannel-request naar Keycloak:

```
POST https://<keycloak>/realms/<realm>/protocol/openid-connect/token
  grant_type=authorization_code
  code=<code>
  redirect_uri=http://localhost:5000/signin-oidc
  client_id=blazor-web-app
  client_secret=<secret>
  code_verifier=<PKCE verifier>
```

Keycloak antwoordt met:
```json
{
  "access_token":  "<JWT>",
  "refresh_token": "<opaque token>",
  "expires_in":    300,
  "refresh_expires_in": 28800,
  "token_type":    "Bearer"
}
```

### Stap 5 — Claims ophalen via UserInfo

Omdat `GetClaimsFromUserInfoEndpoint = true`, doet de middleware nog een extra backchannel-request:

```
GET https://<keycloak>/realms/<realm>/protocol/openid-connect/userinfo
Authorization: Bearer <access_token>
```

Dit vult de claims `preferred_username`, `email`, `given_name`, `family_name` aan.

### Stap 6 — `OnTokenResponseReceived` event

`ConfigureKeycloakOptions` heeft een `OnTokenResponseReceived` event dat `refresh_expires_in` (in seconden) omzet naar een absolute `DateTimeOffset` en opslaat als `refresh_expires_at`. Dit maakt het mogelijk de vervaldatum van het refresh token te tonen in de UI.

### Stap 7 — Authenticatiecookie aanmaken

De OIDC-middleware slaat de tokens op in de authenticatiecookie (omdat `SaveTokens = true`). De cookie bevat:

| Token | Omschrijving |
|---|---|
| `access_token` | Het JWT waarmee de API aangesproken wordt |
| `refresh_token` | Opaque token om een nieuw access token op te halen |
| `expires_at` | ISO 8601 tijdstip waarop het access token verloopt |
| `refresh_expires_at` | ISO 8601 tijdstip waarop het refresh token verloopt |
| `id_token` | JWT met gebruikersinformatie |

De cookie-instellingen (`AuthServiceExtensions.cs`):

| Instelling | Waarde | Reden |
|---|---|---|
| `HttpOnly` | `true` | Niet toegankelijk via JavaScript |
| `SameSite` | `Lax` | Bescherming tegen CSRF |
| `SecurePolicy` | `None` (dev) / `Always` (prod) | HTTPS vereist in productie |
| `ExpireTimeSpan` | 8 uur | Sessieduur |
| `SlidingExpiration` | `true` | Verlengt bij elke actieve request |

### Stap 8 — Redirect naar returnUrl

De gebruiker wordt teruggestuurd naar de originele pagina, bijvoorbeeld `/todos`.

---

## 2. Blazor opstart — tokens laden

Blazor Server werkt in twee fasen die elk hun eigen context hebben.

### Fase 1 — Pre-render (HTTP-request)

Bij het eerste laden van een pagina voert Blazor een pre-render uit als gewone HTTP-request. Op dit moment is `HttpContext` beschikbaar.

`BearerTokenHandler` controleert bij elke uitgaande API-request of de `TokenProvider` al geladen is. In de pre-render fase is dit niet het geval, dus worden de tokens geladen:

```csharp
var httpContext = httpContextAccessor.HttpContext;
if (httpContext is not null && !tokenProvider.IsGeladen)
    await tokenProvider.LaadVanuitHttpContextAsync(httpContext);
```

`TokenProvider.LaadVanuitHttpContextAsync()` leest de tokens uit de cookie:

```csharp
AccessToken  = await httpContext.GetTokenAsync("access_token");
RefreshToken = await httpContext.GetTokenAsync("refresh_token");
ExpiresAt    = await httpContext.GetTokenAsync("expires_at");
```

Tokens worden **alleen** opgeslagen als ze daadwerkelijk aanwezig zijn. `IsGeladen` is een berekende property:

```csharp
public bool IsGeladen => AccessToken is not null || RefreshToken is not null;
```

### Fase 2 — Circuit (SignalR)

Na de pre-render schakelt Blazor over op een SignalR-verbinding. `HttpContext` is nu `null`. De tokens zitten echter al in `TokenProvider` (in memory, per circuit), dus alle verdere API-aanroepen werken zonder `HttpContext`.

---

## 3. API aanroepen — BearerTokenHandler

`BearerTokenHandler` zit als middleware in de `HttpClient`-pipeline. Bij elke uitgaande request voert hij drie stappen uit:

```
Stap 1 — Tokens laden (alleen als HttpContext beschikbaar en nog niet geladen)
     ↓
Stap 2 — Geldig token ophalen via TokenService
     ↓
Stap 3 — Authorization: Bearer header toevoegen
```

Als er geen geldig token is, wordt `401 Unauthorized` teruggegeven. `Todo.razor` vangt dit op en stuurt de gebruiker naar `/login?returnUrl=/todos`.

---

## 4. Token refresh

Een access token is standaard **5 minuten** geldig. `TokenService` vernieuwt het automatisch.

### Wanneer wordt refresh gestart?

`TokenProvider.IsTokenVerlopenOfBijna()` geeft `true` terug als het token binnen **30 seconden** verloopt of al verlopen is. De buffer voorkomt dat een token verlopen is tegen de tijd dat de API-request aankomt.

```csharp
return expiry < DateTimeOffset.UtcNow.AddSeconds(30);
```

### Hoe werkt de refresh?

`TokenService` haalt de Keycloak token endpoint op via de OIDC discovery metadata en stuurt een refresh request:

```
POST https://<keycloak>/realms/<realm>/protocol/openid-connect/token
  grant_type=refresh_token
  client_id=blazor-web-app
  client_secret=<secret>
  refresh_token=<refresh_token>
```

Bij succes worden de nieuwe tokens opgeslagen in `TokenProvider` via `SlaTokensOp()`. De cookie wordt **niet** bijgewerkt vanuit de circuit-fase (geen `HttpContext`), maar de in-memory tokens zijn direct beschikbaar voor verdere requests in het huidige circuit.

Na een browser-refresh worden de nieuwe tokens alsnog via de cookie ingeladen.

### Tijdlijn van een typische sessie

```
T+0:00   Inloggen → access_token geldig tot T+5:00
T+4:30   BearerTokenHandler: token verloopt binnen 30s → refresh
T+4:30   Nieuw access_token geldig tot T+9:30
T+9:00   Volgende refresh
...
T+8:00   Cookie sessie verloopt (ExpireTimeSpan = 8 uur)
```

---

## 5. Sessie verlopen — Session not active

Als de Keycloak SSO-sessie niet meer actief is, antwoordt Keycloak op een refresh-poging met:

```json
{
  "error": "invalid_grant",
  "error_description": "Session not active"
}
```

`TokenService` detecteert dit en roept `TokenProvider.WisTokens()` aan:

```csharp
if (fout.Contains("invalid_grant") || fout.Contains("Session not active"))
    tokenProvider.WisTokens();
```

Na het wissen is `IsGeladen = false`. De volgende API-aanroep geeft `401` terug, waarna de pagina de gebruiker doorstuurt naar `/login`.

### Wanneer treedt dit op?

| Situatie | Oorzaak |
|---|---|
| Keycloak herstart in development | Sessies worden in dev mode in RAM bewaard en gaan verloren bij herstart |
| SSO Session Idle verstreken | Geen activiteit binnen de ingestelde idle timeout |
| Gebruiker uitgelogd via een andere browser/tab | Keycloak-sessie is beëindigd |

---

## 6. Uitloggen

Het `/logout` endpoint roept `SignOutAsync` aan op beide schemes:

```csharp
await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
```

**Cookie scheme** — verwijdert de lokale authenticatiecookie. De gebruiker is direct uitgelogd uit de Blazor app.

**OpenIdConnect scheme** — stuurt een logout-request naar Keycloak's end session endpoint. Hierdoor wordt de Keycloak SSO-sessie ook beëindigd. Zonder dit zou de gebruiker bij het volgende inloggen automatisch opnieuw worden ingelogd (SSO), omdat de Keycloak-sessie nog actief is.

> De `TokenProvider` (in memory) wordt niet expliciet gewist bij uitloggen — het Blazor circuit wordt na het uitloggen beëindigd, waardoor de scoped `TokenProvider` instantie automatisch wordt opgeruimd.

---

## 7. JWT validatie in de API

De API valideert elk inkomend Bearer token via `ConfigureJwtBearerOptions`. Er is geen verbinding met Keycloak nodig per request — de validatie gebeurt lokaal met de publieke sleutels (JWKS).

### Validatie-instellingen

| Controle | Waarde | Omschrijving |
|---|---|---|
| `ValidateIssuer` | `true` | Issuer moet overeenkomen met de Keycloak realm URL |
| `ValidateAudience` | `true` | Audience moet `blazor-web-app` of `account` zijn |
| `ValidateLifetime` | `true` | Token mag niet verlopen zijn |
| `ClockSkew` | 30 seconden | Maximale klokafwijking tussen servers |
| `NameClaimType` | `preferred_username` | Gebruikersnaam uit het token |
| `RoleClaimType` | Microsoft schema URI | Rollen worden hier gelezen |

### JWKS ophalen

Bij de eerste request haalt de API de publieke sleutels op van Keycloak:

```
GET https://<keycloak>/realms/<realm>/protocol/openid-connect/certs
```

De sleutels worden gecached. Bij een `kid` (key ID) die niet in de cache zit, worden de sleutels opnieuw opgehaald — dit ondersteunt automatische key rotation in Keycloak.

### Audience validatie

Keycloak voegt standaard alleen `"account"` als audience toe. Daarom zijn beide waarden als `ValidAudiences` geconfigureerd:

```csharp
ValidAudiences = [_keycloak.ClientId, "account"]
```

Om dit te corrigeren kan in Keycloak een audience mapper worden toegevoegd (`blazor-web-app-dedicated → Audience`), waarna `"account"` uit de lijst kan worden verwijderd.

---

## 8. Rollen en autorisatie

### Rollen in Keycloak

Rollen zijn geconfigureerd als **client-rollen** onder de `blazor-web-app` client. Een gebruiker heeft de `user` rol of de `admin` rol (of beide).

### Rollen in het JWT-token

Standaard worden client-rollen niet automatisch als platte claim meegestuurd. Een **User Client Role mapper** in Keycloak zorgt dat de rollen als `roles` claim in het access token verschijnen:

```json
{
  "preferred_username": "jan.de.vries",
  "roles": ["user", "admin"]
}
```

### RoleClaimType

ASP.NET Core leest rollen standaard uit een Microsoft-specifiek claim type (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`). Zowel de Blazor app als de API configureren dit expliciet zodat `context.User.IsInRole("admin")` en `RequireRole("admin")` correct werken:

```csharp
options.TokenValidationParameters.RoleClaimType =
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
```

De OIDC-middleware mapt de `roles` claim automatisch naar dit type.

### Autorisatie in de API

De API gebruikt twee policies:

```csharp
options.AddPolicy("UserRole", policy => policy.RequireRole("user"));
options.AddPolicy("AdminRole", policy => policy.RequireRole("admin"));  // voor toekomstig gebruik
```

Todo-endpoints vereisen de `UserRole` policy. Gebruikers zien en beheren alleen hun eigen items — gefilterd op `preferred_username` uit het JWT.

### Autorisatie in de Blazor app

De Blazor app gebruikt `<AuthorizeView>` in Razor components om UI-elementen conditioneel te tonen op basis van de ingelogde status. `Routes.razor` gebruikt `AuthorizeRouteView` om niet-ingelogde gebruikers door te sturen naar `/niet-aangemeld`.

---

## 9. Cookies

De applicatie gebruikt drie typen cookies:

### Authenticatiecookie (`.AspNetCore.Cookies`)

De hoofdcookie die de gebruikerssessie bijhoudt. Bevat de tokens (versleuteld via ASP.NET Core Data Protection) en de claims.

| Eigenschap | Waarde |
|---|---|
| Naam | `.AspNetCore.Cookies` |
| Inhoud | Versleuteld: tokens + claims principal |
| HttpOnly | Ja |
| SameSite | Lax |
| Levensduur | 8 uur (sliding) |
| Opslag | Data Protection keys (persistent via volume in Docker) |

### Correlation cookie (`.AspNetCore.Correlation.*`)

Tijdelijke cookie voor CSRF-bescherming tijdens het OIDC-login proces. Gekoppeld aan het `state` parameter.

| Eigenschap | Waarde |
|---|---|
| SameSite | Unspecified (development) / Lax (productie) |
| Levensduur | 15 minuten |
| Pad | `/signin-oidc` |

> **Waarom `Unspecified` in development?** Keycloak draait op een ander IP/poort dan de Blazor app. Bij een cross-origin redirect (Keycloak → `/signin-oidc`) blokkeert de browser cookies met `SameSite=Lax`. Met `Unspecified` wordt het `SameSite` attribuut niet meegezonden, waardoor de browser de cookie altijd meestuurt.

### Nonce cookie (`.AspNetCore.OpenIdConnect.Nonce.*`)

Tijdelijke cookie voor nonce-validatie. Voorkomt replay-aanvallen op het ID-token.

| Eigenschap | Waarde |
|---|---|
| SameSite | Unspecified (development) / Lax (productie) |
| Levensduur | 15 minuten |
| Pad | `/signin-oidc` |

---

## 10. Keycloak configuratie die de werking beïnvloedt

### Sessie-instellingen (Realm Settings → Sessions)

| Instelling | Aanbevolen | Effect op applicatie |
|---|---|---|
| SSO Session Idle | 8 uur | Refresh token wordt ongeldig na inactiviteit → `Session not active` |
| SSO Session Max | 10 uur | Harde grens ongeacht activiteit |
| Access Token Lifespan | 5 minuten | Hoe vaak `TokenService` een refresh uitvoert |

> `SSO Session Idle` moet **≥** `ExpireTimeSpan` van de cookie (8 uur) zijn. Anders verloopt de Keycloak-sessie terwijl de ASP.NET-cookie nog geldig is, wat resulteert in `Session not active` fouten.

### Client-instellingen (Clients → blazor-web-app)

| Instelling | Waarde | Effect |
|---|---|---|
| Client authentication | ON | Confidential client — client secret vereist bij token exchange |
| Standard flow | ON | Authorization Code Flow actief |
| Direct access grants | OFF | Password grant uitgeschakeld (veiliger) |
| Valid redirect URIs | `/signin-oidc` | Keycloak accepteert alleen deze callback URL |
| Valid post logout URIs | `/signout-callback-oidc` | Keycloak redirect na uitloggen |

### Mappers (blazor-web-app-dedicated scope)

| Mapper | Type | Effect |
|---|---|---|
| `client-roles` | User Client Role | Zet client-rollen als `roles` claim in het token |
| `audience` | Audience | Voegt `blazor-web-app` toe als audience — vereist voor API-validatie |

### Development mode (`start-dev`)

Keycloak in `start-dev` mode bewaart sessies in RAM. Na een herstart zijn alle sessies verlopen. De applicatie handelt dit correct af via `Session not active` detectie, maar de gebruiker moet opnieuw inloggen. Gebruik `start` (met een database) voor persistente sessies.
