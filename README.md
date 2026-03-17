# Keycloak Authenticatie — Blazor Web App + API

Deze README beschrijft de implementatie van Keycloak OIDC-authenticatie in de Blazor Web App en de bijbehorende Minimal API. De applicatie gebruikt de Authorization Code Flow via de ASP.NET Core OpenID Connect-middleware voor de Blazor-frontend en JWT Bearer-authenticatie voor de API.

---

## Inhoudsopgave

- [Vereisten](#vereisten)
- [Projectstructuur](#projectstructuur)
- [NuGet-pakketten](#nuget-pakketten)
- [Configuratie](#configuratie)
- [Keycloak.Auth.Blazor](#keycloakauthblazor)
  - [KeycloakOptions](#keycloakoptions)
  - [KeycloakAuthBlazorExtensions](#keycloakauthblazorextensions)
  - [Internal/ConfigureKeycloakOptions](#internalconfigurekeycloakoptions)
  - [Services/TokenProvider](#servicestokenprovider)
  - [Services/TokenService](#servicestokenservice)
  - [Services/BearerTokenHandler](#servicesbearertokenhandler)
- [Keycloak.Auth.Api](#keycloakauthapi)
  - [KeycloakOptions (API)](#keycloakoptions-api)
  - [KeycloakAuthApiExtensions](#keycloakauthapextensions)
  - [Internal/ConfigureJwtBearerOptions](#internalconfigurejwtbeareroptions)
- [Blazor-integratie](#blazor-integratie)
  - [Routes.razor](#routesrazor)
  - [RedirectToNotLoggedIn.razor](#redirecttonotloggedinrazor)
  - [AccessDenied.razor](#accessdeniedrazor)
  - [NavMenu.razor](#navmenurazor)
  - [Todo.razor](#todorazor)
- [Services — Blazor App](#services--blazor-app)
  - [TodoApiClient](#todoapiclient)
- [Sessiebeheer](#sessiebeheer)
- [Docker](#docker)
- [CI/CD](#cicd)
- [Stroom](#stroom)

---

## Vereisten

- .NET 10
- Docker met Docker Compose
- Een draaiende Keycloak-instantie (zie `Keycloak_Installatiegids.md`)
- Een geconfigureerde Keycloak realm en client (zie installatiegids)

---

## Projectstructuur

```
solution/
├── .github/
│   └── workflows/
│       └── build-images.yml                    # Gecombineerde CI/CD pipeline (matrix)
│
├── Keycloak.Auth.Blazor/                       # Class library — Blazor OIDC authenticatie
│   ├── Keycloak.Auth.Blazor.csproj             # SDK: Microsoft.NET.Sdk + FrameworkReference
│   ├── KeycloakOptions.cs                      # Sterk-getypeerde configuratie
│   ├── KeycloakAuthBlazorExtensions.cs         # AddKeycloakBlazorAuth() + MapKeycloakAuthEndpoints()
│   ├── Internal/
│   │   └── ConfigureKeycloakOptions.cs         # Vult OpenIdConnectOptions (internal)
│   └── Services/
│       ├── TokenProvider.cs                    # Houdt tokens bij per Blazor circuit
│       ├── TokenService.cs                     # Voert token refresh uit bij Keycloak
│       └── BearerTokenHandler.cs               # Laadt tokens, valideert, voegt Bearer header toe
│
├── Keycloak.Auth.Api/                          # Class library — API JWT authenticatie
│   ├── Keycloak.Auth.Api.csproj                # SDK: Microsoft.NET.Sdk + FrameworkReference
│   ├── KeycloakOptions.cs                      # Sterk-getypeerde configuratie + RoleClaimType
│   ├── KeycloakAuthApiExtensions.cs            # AddKeycloakApiAuth()
│   └── Internal/
│       └── ConfigureJwtBearerOptions.cs        # Vult JwtBearerOptions (internal)
│
├── BlazorWebAppWithKeycloak/                   # Blazor Web App (frontend)
│   ├── Components/
│   │   ├── Layout/
│   │   │   └── NavMenu.razor                   # Login/logout navigatie
│   │   ├── Pages/
│   │   │   ├── AccessDenied.razor              # /niet-aangemeld
│   │   │   ├── Home.razor                      # /
│   │   │   ├── NotFound.razor                  # 404 pagina
│   │   │   ├── Profiel.razor                   # /profiel — claims en sessiegegevens
│   │   │   └── Todo.razor                      # /todos — persoonlijke takenlijst
│   │   ├── RedirectToNotLoggedIn.razor         # Navigeert naar /niet-aangemeld
│   │   └── Routes.razor                        # AuthorizeRouteView
│   ├── Services/
│   │   └── TodoApiClient.cs                    # Typed HttpClient voor Todo endpoints
│   ├── BlazorWebAppWithKeycloak.csproj         # Verwijst naar Keycloak.Auth.Blazor
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                              # AddKeycloakBlazorAuth() + MapKeycloakAuthEndpoints()
│
├── BlazorWebAppWithKeycloak.API/               # Minimal API (backend)
│   ├── Data/
│   │   └── TodoDbContext.cs                    # EF Core context voor SQLite
│   ├── Extentions/
│   │   └── TodoEndpointExtensions.cs           # MapTodoEndpoints()
│   ├── Models/
│   │   ├── TodoItem.cs                         # EF Core entiteit + Priority enum
│   │   └── TodoDtos.cs                         # Request/response DTOs
│   ├── BlazorWebAppWithKeycloak.API.csproj     # Verwijst naar Keycloak.Auth.Api
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                              # AddKeycloakApiAuth()
│
├── docker-compose.yml
├── .env                                        # Secrets en image-namen (in .gitignore)
└── .env.example                                # Voorbeeld zonder gevoelige waarden
```

---

## NuGet-pakketten

De authenticatie-pakketten zitten in de class libraries — de applicatieprojecten hebben geen directe NuGet-referentie meer nodig.

**Keycloak.Auth.Blazor:**
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.5" />
```

**Keycloak.Auth.Api:**
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.5" />
```

**BlazorWebAppWithKeycloak:**
```xml
<ProjectReference Include="..\Keycloak.Auth.Blazor\Keycloak.Auth.Blazor.csproj" />
```

**BlazorWebAppWithKeycloak.API:**
```xml
<ProjectReference Include="..\Keycloak.Auth.Api\Keycloak.Auth.Api.csproj" />
```

---

## Configuratie

### Blazor Web App — `appsettings.json`

```json
{
  "Keycloak": {
    "Authority": "https://<keycloak-domein>/realms/<realm>",
    "ClientId": "<client-id>",
    "ClientSecret": "",
    "RequireHttpsMetadata": true
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:5114"
  }
}
```

| Veld                   | Toelichting                                                                |
|------------------------|----------------------------------------------------------------------------|
| `Authority`            | Publieke OIDC URL — de URL die de browser gebruikt voor redirects          |
| `MetadataAddress`      | *(Optioneel)* Interne URL voor server-to-server metadata ophalen in Docker |
| `ClientId`             | Client ID zoals aangemaakt in Keycloak                                     |
| `ClientSecret`         | Stel in via user-secrets of omgevingsvariabele — nooit in versiebeheer    |
| `RequireHttpsMetadata` | `false` voor HTTP-ontwikkeling, `true` in productie                        |
| `ApiSettings:BaseUrl`  | Basis-URL van de API                                                       |

### API — `appsettings.json`

```json
{
  "Keycloak": {
    "Authority": "https://<keycloak-domein>/realms/<realm>",
    "ClientId": "<client-id>",
    "RequireHttpsMetadata": true
  }
}
```

De API heeft geen `ClientSecret` — hij valideert tokens alleen, geeft ze niet uit.

### Secret beheer

```bash
# Development
dotnet user-secrets set "Keycloak:ClientSecret" "jouw-secret"

# Productie / Docker
export Keycloak__ClientSecret="jouw-secret"
```

---

## Architectuur — Blazor Web App

### KeycloakOptions

`Auth/KeycloakOptions.cs`

Sterk-getypeerde POCO-klasse met `[Required]` en `[Url]` validatie via de Options-pattern.

```csharp
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required, Url]
    public string  Authority            { get; init; } = string.Empty;
    public string? MetadataAddress      { get; init; }
    [Required]
    public string  ClientId             { get; init; } = string.Empty;
    [Required]
    public string  ClientSecret         { get; init; } = string.Empty;
    public bool    RequireHttpsMetadata { get; init; } = false;
}
```

---

### ConfigureKeycloakOptions

`Auth/ConfigureKeycloakOptions.cs`

Implementeert `IConfigureNamedOptions<OpenIdConnectOptions>`.

| Instelling                      | Waarde / Toelichting                                                                |
|---------------------------------|-------------------------------------------------------------------------------------|
| `ResponseType`                  | `code` — Authorization Code Flow                                                    |
| `SaveTokens`                    | `true` — tokens worden opgeslagen in cookie, beschikbaar via `GetTokenAsync`         |
| `GetClaimsFromUserInfoEndpoint` | `true` — profiel- en emailclaims worden opgehaald                                   |
| Scopes                          | `openid`, `profile`, `email`                                                        |
| `MetadataAddress`               | Interne Docker URL; alleen ingesteld als `KeycloakOptions.MetadataAddress` gevuld is|
| `NameClaimType`                 | `preferred_username`                                                                |
| `RoleClaimType`                 | `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`                      |
| `PushedAuthorizationBehavior`   | `Disable` — PAR vereist expliciete configuratie in Keycloak                         |

---

### AuthServiceExtensions

`Auth/AuthServiceExtensions.cs`

Bundelt alle registraties in `builder.Services.AddKeycloakAuthentication()`.

- Cookie: `HttpOnly = true`, `SameSite = Lax`, `SecurePolicy = None`
- Correlation- en nonce-cookies: `SameSite = Unspecified`, `SecurePolicy = None`

> **SameSite = Unspecified:** Keycloak draait op een ander IP dan de Blazor-app. Met `Lax` blokkeert de browser de correlation cookie bij de terugkeer van Keycloak. `Unspecified` stuurt geen `SameSite`-attribuut, waardoor de cookie altijd wordt doorgestuurd.

---

### AuthEndpointExtensions

`Auth/AuthEndpointExtensions.cs`

Registreert `/login` en `/logout` via `app.MapAuthEndpoints()`. Moet vóór `MapRazorComponents()` worden aangeroepen.

| Endpoint  | Authenticatie    | Toelichting                                                       |
|-----------|------------------|-------------------------------------------------------------------|
| `/login`  | `AllowAnonymous` | Roept `ChallengeAsync` aan; valideert `returnUrl` tegen open-redirect |
| `/logout` | Verplicht        | Verwijdert de lokale cookie — Keycloak SSO-sessie blijft actief   |

---

## Architectuur — API

### KeycloakOptions (API)

`Auth/KeycloakOptions.cs`

Vergelijkbaar met de Blazor variant, maar zonder `ClientSecret`. Bevat de gedeelde `RoleClaimType` constante als centrale bron voor beide klassen:

```csharp
public const string RoleClaimType =
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
```

---

### ConfigureJwtBearerOptions

`Auth/ConfigureJwtBearerOptions.cs`

Valideert inkomende JWT-tokens op issuer, audience, handtekening (via JWKS) en levensduur.

| Instelling        | Waarde / Toelichting                                              |
|-------------------|-------------------------------------------------------------------|
| `Authority`       | Publieke Keycloak URL voor issuer-validatie en JWKS               |
| `MetadataAddress` | Interne Docker URL voor metadata (optioneel)                      |
| `ValidAudiences`  | `[ClientId, "account"]` — zie noot hieronder                      |
| `RoleClaimType`   | `KeycloakOptions.RoleClaimType` — gedeelde constante              |

> **ValidAudiences bevat `"account"`:** Keycloak voegt standaard alleen `account` toe als audience. Voeg een audience-mapper toe in Keycloak (`blazor-web-app-dedicated → Audience → Included Client Audience: blazor-web-app`) om ook `blazor-web-app` op te nemen. Daarna kan `"account"` worden verwijderd.

---

### AuthServiceExtensions (API)

`Auth/AuthServiceExtensions.cs`

Registreert JWT Bearer-authenticatie via `builder.Services.AddKeycloakJwtAuthentication()`.

- Policy `"UserRole"` vereist de `user` client-rol
- Policy `"AdminRole"` vereist de `admin` client-rol
- `RoleClaimType` via `KeycloakOptions.RoleClaimType` (geen duplicatie)

---

### HelloEndpointExtensions

`Extensions/HelloEndpointExtensions.cs`

Extension method op `IEndpointRouteBuilder`. Registreert alle Hello World-endpoints via `app.MapHelloEndpoints()` in `Program.cs`. Dit patroon is consistent met `MapAuthEndpoints()` in de Blazor app en houdt `Program.cs` overzichtelijk naarmate het aantal endpoints groeit.

| Endpoint     | Authenticatie | Toelichting                            |
|--------------|---------------|----------------------------------------|
| `GET /api/hello` | Policy `UserRole` | Retourneert gebruikersnaam en tijdstip |
| `GET /api/admin` | Policy `AdminRole` | Retourneert gebruikersnaam en tijdstip — alleen voor admins |

---

## Blazor-integratie

### Routes.razor

Gebruikt `AuthorizeRouteView`. Niet ingelogd → `<RedirectToNotLoggedIn />`, ingelogd maar geen toegang → melding.

### RedirectToNotLoggedIn.razor

Navigeert naar `/niet-aangemeld?returnUrl=<huidige-url>`. De `returnUrl` wordt bewaard zodat de gebruiker na inloggen terugkeert naar de originele pagina.

### AccessDenied.razor

Route: `/niet-aangemeld`. Leest `returnUrl` via `[SupplyParameterFromQuery]` en geeft die door aan de loginknop.

### NavMenu.razor

Toont via `<AuthorizeView>` conditioneel een inlog- of uitlogknop. Gebruikt `forceLoad: true` zodat de browser echte HTTP-requests stuurt naar de auth-endpoints.

### Todo.razor

Route: `/todos`. Persoonlijke takenlijst — elke gebruiker ziet en beheert alleen zijn eigen items. Functionaliteit: filteren (Alle/Open/Afgerond), aanmaken, bewerken, afgerond toggle, verwijderen met bevestiging en markering van verlopen items. Bij een 401 wordt de gebruiker automatisch naar `/login?returnUrl=/todos` doorgestuurd.

### Profiel.razor

Route: `/profiel`. Toont inloggegevens en claims ontvangen van Keycloak: gebruikerskaart (naam, e-mail, rollen), sessietijden (inlogmoment, cookie- en tokenvervaldatums) en de volledige claimstabel. Geen `@rendermode InteractiveServer` — statische pre-render is voldoende.

---

## Services

### TokenProvider

`Services/TokenProvider.cs`

Scoped service die de tokens van de ingelogde gebruiker bijhoudt per Blazor circuit. Gevuld tijdens de pre-render HTTP-request via `LaadVanuitHttpContextAsync()` — op dat moment is `HttpContext` nog beschikbaar. Na een succesvolle refresh bijgewerkt via `SlaTokensOp()`. Bij een verlopen Keycloak-sessie (`invalid_grant`) worden de tokens gewist via `WisTokens()`.

`IsGeladen` is een berekende property op basis van de aanwezigheid van tokens — nooit een vlag die vroegtijdig gezet kan worden.

### TokenService

`Services/TokenService.cs`

Scoped service die de token refresh uitvoert. Eén publieke methode: `GetGeldigTokenAsync()`.

| Situatie | Gedrag |
|---|---|
| Tokens nog niet geladen | Geeft `null` terug — geen refresh geprobeerd |
| Token nog geldig | Geeft `AccessToken` direct terug |
| Token verlopen of binnen 30s | Refresh via Keycloak token endpoint |
| Refresh mislukt (`invalid_grant`) | Wist tokens via `TokenProvider.WisTokens()`, geeft `null` terug |

Bij een mislukte refresh wordt de volledige Keycloak error response gelogd (`error_description`) voor directe diagnose.

### BearerTokenHandler

`Services/BearerTokenHandler.cs`

Scoped `DelegatingHandler` die bij elke uitgaande API-request drie stappen uitvoert:

1. **Tokens laden** — als `HttpContext` beschikbaar is én tokens nog niet geladen zijn, laadt hij ze uit de cookie via `TokenProvider.LaadVanuitHttpContextAsync()`
2. **Token valideren/verversen** — roept `TokenService.GetGeldigTokenAsync()` aan
3. **Header toevoegen** — zet `Authorization: Bearer <token>`, of geeft `401` terug als er geen geldig token is

Werkt in beide Blazor-fasen: pre-render (HttpContext beschikbaar) en circuit/SignalR (tokens al in `TokenProvider`).

### HelloWorldApiClient

`Services/HelloWorldApiClient.cs`

Typed `HttpClient` voor de Hello World en Admin endpoints.

| Methode | Endpoint | Vereiste rol |
|---------|----------|--------------|
| `GetHelloAsync()` | `GET /api/hello` | `user` |
| `GetAdminAsync()` | `GET /api/admin` | `admin` |

### TodoApiClient

`Services/TodoApiClient.cs`

Typed `HttpClient` voor alle todo-endpoints. Bevat ook de gedeelde DTOs (`TodoResponse`, `TodoAanmakenRequest`, `TodoBijwerkenRequest`) en de `Priority` enum.

| Methode | Endpoint | Omschrijving |
|---------|----------|-------------|
| `GetAlleAsync()` | `GET /api/todos` | Alle eigen items |
| `GetAsync(id)` | `GET /api/todos/{id}` | Één item |
| `AanmakenAsync(request)` | `POST /api/todos` | Nieuw item |
| `BijwerkenAsync(id, request)` | `PUT /api/todos/{id}` | Item bijwerken |
| `ToggleAfgerondAsync(id)` | `PATCH /api/todos/{id}/afgerond` | Afgerond toggle |
| `VerwijderenAsync(id)` | `DELETE /api/todos/{id}` | Item verwijderen |

Geregistreerd in `Program.cs`:

```csharp
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BearerTokenHandler>();

builder.Services
    .AddHttpClient<HelloWorldApiClient>(...)
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services
    .AddHttpClient<TodoApiClient>(...)
    .AddHttpMessageHandler<BearerTokenHandler>();
```

### WeatherForecast

`Services/WeatherForecast.cs`

Immutable `record` model. Staat in een apart bestand in plaats van als private nested class in de Razor component.

---

## Sessiebeheer

### Token levenscyclus

```
Pre-render (HTTP-request, HttpContext beschikbaar)
  └─ BearerTokenHandler.SendAsync()
       └─ TokenProvider.LaadVanuitHttpContextAsync()
            └─ Leest access_token, refresh_token, expires_at uit cookie

Circuit-fase (SignalR, HttpContext = null)
  └─ BearerTokenHandler.SendAsync()
       ├─ Token nog geldig → Bearer header toevoegen
       └─ Token verlopen →
            └─ TokenService.VervangTokenAsync()
                 ├─ POST /token refresh_token → Keycloak
                 ├─ Succes → TokenProvider.SlaTokensOp() → Bearer header
                 └─ invalid_grant / Session not active →
                      └─ TokenProvider.WisTokens()
                           └─ Pagina stuurt door naar /login?returnUrl=...
```

### Keycloak sessie verlopen (`Session not active`)

Dit is geen code-fout maar een Keycloak-sessievervalling. Oorzaken:

| Oorzaak | Oplossing |
|---|---|
| Keycloak herstart (development) | Uitloggen en opnieuw inloggen — of persistente Keycloak-opslag configureren |
| SSO Session Idle timeout | Zet **Realm Settings → Sessions → SSO Session Idle** gelijk aan `ExpireTimeSpan` (8 uur) |
| Keycloak dev mode (in-memory) | Sessies gaan verloren bij herstart; gebruik `start` in plaats van `start-dev` voor persistentie |

De applicatie handelt dit af door de tokens te wissen en de gebruiker door te sturen naar `/login` met een `returnUrl`, zodat hij na het inloggen terugkeert op de juiste pagina.

---

## Docker

### Services

| Service    | Image                          | Poort         |
|------------|--------------------------------|---------------|
| `blazor` | `${BLAZOR_IMAGE}` (via `.env`) | `5000 → 8080` |
| `api`    | `${API_IMAGE}` (via `.env`)    | `5001 → 8080` |

### Volumes

| Volume               | Gemount in       | Inhoud                                        |
|----------------------|------------------|-----------------------------------------------|
| `dataprotection-keys` | `/app/keys`     | ASP.NET Core Data Protection sleutels (Blazor) |
| `todo-data`          | `/app/data`      | SQLite database `todo.db` (API)               |

De database-locatie wordt via de omgevingsvariabele `ConnectionStrings__TodoDb` doorgegeven aan de API container, zodat `appsettings.json` niet gewijzigd hoeft te worden per omgeving:

```yaml
- ConnectionStrings__TodoDb=Data Source=/app/data/todo.db
```

> **Belangrijk:** verwijder het `todo-data` volume niet met `docker compose down -v` tenzij je de todolijsten van alle gebruikers wilt wissen. Gebruik `docker compose down` (zonder `-v`) om alleen de containers te stoppen.

### URL-configuratie per service

De app draait achter een reverse proxy op `https://<app-domein>`. Keycloak is gehost op `https://<keycloak-domein>`. Beide services communiceren direct met de hosted Keycloak zonder interne MetadataAddress.

**Blazor:**
```yaml
- ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
- Keycloak__Authority=${KEYCLOAK_AUTHORITY}
- Keycloak__ClientId=${KEYCLOAK_CLIENT_ID}
- Keycloak__RequireHttpsMetadata=true
```

**API:**
```yaml
- Keycloak__Authority=${KEYCLOAK_AUTHORITY}
- Keycloak__ClientId=${KEYCLOAK_CLIENT_ID}
- Keycloak__RequireHttpsMetadata=true
- ConnectionStrings__TodoDb=Data Source=/app/data/todo.db
```

### `.env` bestand

Image-namen en secrets staan in `.env` naast `docker-compose.yml`. Gebruik `.env.example` als sjabloon:

```env
KEYCLOAK_CLIENT_SECRET=jouw-client-secret
BLAZOR_IMAGE=<jouw-registry>/demo:latest
API_IMAGE=<jouw-registry>/demo-api:latest
```

> Voeg `.env` toe aan `.gitignore` — het bevat secrets en omgevingsspecifieke image-namen.

### Opstarten

```bash
cp .env.example .env
# Vul .env in met de juiste waarden
docker compose up -d
docker compose logs -f blazor
docker compose logs -f api
```

> Gebruik `docker compose down` om containers te stoppen — de volumes (`dataprotection-keys` en `todo-data`) blijven dan behouden. Gebruik `docker compose down -v` **alleen** als je ook alle data wilt wissen.

---

## Reverse proxy

De Blazor app draait achter een reverse proxy (Nginx Proxy Manager) die TLS termineert. De proxy stuurt `X-Forwarded-For` en `X-Forwarded-Proto` headers mee.

### ForwardedHeaders middleware

In `Program.cs` wordt `UseForwardedHeaders()` ingeschakeld **alleen buiten Development**. Dit zorgt dat ASP.NET Core de forwarded headers verwerkt en `https://<app-domein>` als basis-URL gebruikt voor OIDC redirect URIs:

```csharp
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}
```

Zonder dit gebruikt de OIDC-handler `http://localhost:5000/signin-oidc` als `redirect_uri` — wat niet overeenkomt met de ingestelde redirect URI in Keycloak.

Lokaal (Development) worden forwarded headers niet verwerkt zodat `http://localhost:5000` correct blijft werken.

---

## CI/CD

Eén workflow met matrix strategy bouwt beide projecten:

```yaml
# .github/workflows/build-images.yml
strategy:
  matrix:
    include:
      - project: BlazorWebAppWithKeycloak
        name: APP
      - project: BlazorWebAppWithKeycloak.API
        name: API
```

Elke job voert uit: checkout → .NET setup → GitVersion (nbgv) → `dotnet publish /t:PublishContainer`.

### Benodigde GitHub Secrets en Variables

Stel deze in via **GitHub → Repository → Settings → Secrets and variables → Actions**.

**Secrets** (versleuteld, voor gevoelige waarden):

| Secret | Waarde | Toelichting |
|--------|--------|-------------|
| `CONTAINER_REGISTRY` | `<registry-hostname>` | Hostname van de container registry |
| `DOCKER_USER` | `<gebruikersnaam>` | Gebruikersnaam voor authenticatie bij de registry |
| `DOCKER_PASSWORD` | `<wachtwoord>` | Wachtwoord of access token voor de registry |

**Variables** (zichtbaar, voor niet-gevoelige waarden):

| Variable | Waarde | Toelichting |
|----------|--------|-------------|
| `BLAZOR_REPOSITORY` | `<gebruiker>/demo` | Repository-pad voor de Blazor Web App image |
| `API_REPOSITORY` | `<gebruiker>/demo-api` | Repository-pad voor de API image |

`ContainerRegistry` en `ContainerRepository` worden via de workflow doorgegeven aan `dotnet publish` — geen van beide staan in de `.csproj` bestanden.

---

## Stroom

### Inloggen

```
Gebruiker klikt Inloggen
        │
        ▼
/login?returnUrl=... → ChallengeAsync → Keycloak loginpagina
        │
        ▼
Gebruiker logt in → /signin-oidc?code=...
        │
        ▼
OIDC-middleware wisselt code in voor tokens (backchannel)
        │
        ▼
Cookie aangemaakt (access_token, refresh_token, expires_at opgeslagen)
        │
        ▼
Redirect naar returnUrl
```

### Niet-ingelogde gebruiker bezoekt beveiligde pagina

```
/todos of /profiel
        │
        ▼
AuthorizeView: niet ingelogd
        │
        ▼
<RedirectToNotLoggedIn /> → /niet-aangemeld?returnUrl=...
        │
        ▼
Gebruiker klikt Inloggen → na login terug naar originele pagina
```

### API aanroepen vanuit Blazor

```
Todo.razor → TodoApiClient.GetAlleAsync()
        │
        ▼
BearerTokenHandler.SendAsync()
        ├─ Stap 1: TokenProvider.LaadVanuitHttpContextAsync()  (alleen pre-render)
        ├─ Stap 2: TokenService.GetGeldigTokenAsync()
        │           ├─ Token geldig → direct teruggeven
        │           └─ Token verlopen → refresh via Keycloak
        └─ Stap 3: Authorization: Bearer <token> header toevoegen
        │
        ▼
GET /api/todos met Authorization: Bearer <token>
        │
        ▼
API valideert token (issuer, audience, handtekening, rol)
        │
        ├─ 200 OK → response tonen
        └─ 401 Unauthorized → NavigateTo("/login?returnUrl=...")
```

### Uitloggen

```
/logout → SignOutAsync(Cookie) → lokale sessie beëindigd
Keycloak SSO-sessie blijft actief
```
