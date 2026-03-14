# Keycloak Authenticatie — Blazor Web App

Deze README beschrijft de implementatie van Keycloak OIDC-authenticatie in de Blazor Web App. De applicatie gebruikt de Authorization Code Flow via de ASP.NET Core OpenID Connect-middleware, geconfigureerd via de Options-pattern.

---

## Inhoudsopgave

- [Vereisten](#vereisten)
- [Projectstructuur](#projectstructuur)
- [NuGet-pakket](#nuget-pakket)
- [Configuratie](#configuratie)
- [Architectuur](#architectuur)
  - [KeycloakOptions](#keycloakoptions)
  - [ConfigureKeycloakOptions](#configurekeycloakoptions)
  - [AuthServiceExtensions](#authserviceextensions)
  - [AuthEndpointExtensions](#authendpointextensions)
- [Blazor-integratie](#blazor-integratie)
  - [Routes.razor](#routesrazor)
  - [RedirectToNotLoggedIn.razor](#redirecttonotloggedinrazor)
  - [AccessDenied.razor](#accessdeniedrazor)
  - [NavMenu.razor](#navmenurazor)
  - [Claims.razor](#claimsrazor)
  - [Weather.razor](#weatherrazor)
  - [Counter.razor](#counterrazor)
- [Docker](#docker)
- [Data Protection](#data-protection)
- [Stroom](#stroom)

---

## Vereisten

- .NET 10
- Een draaiende Keycloak-instantie (zie `Keycloak_Installatiegids.md`)
- Een geconfigureerde Keycloak realm en client (zie installatiegids stap 2 en 3)

---

## Projectstructuur

De authenticatielogica is gegroepeerd in de map `Auth/`:

```
BlazorWebAppWithKeycloak/
├── Properties/
│   └── launchSettings.json               # Startprofiel (HTTP poort 5000)
├── wwwroot/
├── Auth/
│   ├── AuthEndpointExtensions.cs         # IEndpointRouteBuilder extensie: MapAuthEndpoints()
│   ├── AuthServiceExtensions.cs          # IServiceCollection extensie: AddKeycloakAuthentication()
│   ├── ConfigureKeycloakOptions.cs       # Vult OpenIdConnectOptions via IConfigureNamedOptions
│   └── KeycloakOptions.cs                # Sterk-getypeerde configuratieklasse
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor                 # Toont inloggen/uitloggen op basis van auth-status
│   ├── Pages/
│   │   ├── AccessDenied.razor            # Pagina voor niet-ingelogde gebruikers (/niet-aangemeld)
│   │   ├── Claims.razor                  # Pagina met claims-overzicht (vereist login)
│   │   ├── Counter.razor                 # Teller (knop vereist admin-rol)
│   │   └── Weather.razor                 # Weerpagina (vereist login)
│   ├── RedirectToNotLoggedIn.razor       # Stuurt niet-geauthenticeerde gebruikers naar /niet-aangemeld
│   └── Routes.razor                      # Bevat AuthorizeRouteView voor beveiligde routes
├── appsettings.json                      # Keycloak-verbindingsgegevens
├── appsettings.Development.json          # Development-specifieke instellingen
└── Program.cs                            # Registratie en middleware-pipeline
```

---

## NuGet-pakket

De implementatie vereist één extra pakket bovenop de standaard Blazor Web App template:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.4" />
```

---

## Configuratie

De verbindingsgegevens staan in `appsettings.json` onder de sectie `Keycloak`:

```json
{
  "Keycloak": {
    "Authority": "http://<keycloak-host>:8082/realms/homelab",
    "ClientId": "blazor-web-app",
    "ClientSecret": "",
    "RequireHttpsMetadata": false
  }
}
```

| Veld                   | Toelichting                                                                 |
|------------------------|-----------------------------------------------------------------------------|
| `Authority`            | Publieke OIDC URL — de URL die de browser gebruikt voor redirects           |
| `MetadataAddress`      | *(Optioneel)* Interne URL voor server-to-server metadata ophalen in Docker  |
| `ClientId`             | Client ID zoals aangemaakt in Keycloak                                      |
| `ClientSecret`         | Client secret van het tabblad **Credentials** in Keycloak                  |
| `RequireHttpsMetadata` | `false` voor lokale HTTP-ontwikkeling, `true` in productie                  |

> **Nooit het client secret in versiebeheer opslaan.** Gebruik voor lokale ontwikkeling `dotnet user-secrets`:
>
> ```bash
> dotnet user-secrets set "Keycloak:ClientSecret" "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
> ```
>
> In productie via omgevingsvariabele:
>
> ```bash
> export Keycloak__ClientSecret="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
> ```

### Development-configuratie

Voor lokaal draaien via Visual Studio gebruik je `appsettings.Development.json`. Dit bestand overschrijft `appsettings.json` in de Development-omgeving:

```json
{
  "Keycloak": {
    "Authority": "http://192.168.x.x:8082/realms/homelab",
    "ClientId": "blazor-web-app",
    "ClientSecret": "",
    "RequireHttpsMetadata": false
  }
}
```

---

## Architectuur

De authenticatie-implementatie bestaat uit vier klassen in de `Auth/`-map. Elke klasse heeft één verantwoordelijkheid.

### KeycloakOptions

`Auth/KeycloakOptions.cs`

Sterk-getypeerde POCO-klasse die de `Keycloak`-sectie uit `appsettings.json` representeert. Gebonden via de Options-pattern met `[Required]` en `[Url]` validatie zodat de applicatie bij een ongeldige configuratie direct faalt bij opstarten.

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

De optionele `MetadataAddress` is nodig in Docker-omgevingen waar de Blazor-container Keycloak bereikt via een interne hostnaam, terwijl `Authority` de publieke hostnaam bevat die de browser gebruikt.

---

### ConfigureKeycloakOptions

`Auth/ConfigureKeycloakOptions.cs`

Implementeert `IConfigureNamedOptions<OpenIdConnectOptions>`. Het framework roept deze klasse aan wanneer `OpenIdConnectOptions` voor het eerst nodig is. `KeycloakOptions` wordt via de constructor geïnjecteerd — geen `BuildServiceProvider()` nodig.

Geconfigureerde instellingen:

| Instelling                      | Waarde / Toelichting                                                                    |
|---------------------------------|-----------------------------------------------------------------------------------------|
| `ResponseType`                  | `code` — Authorization Code Flow                                                        |
| `SaveTokens`                    | `true` — access en refresh token beschikbaar via `GetTokenAsync`                        |
| `GetClaimsFromUserInfoEndpoint` | `true` — profiel- en emailclaims worden opgehaald                                       |
| Scopes                          | `openid`, `profile`, `email`                                                            |
| `MetadataAddress`               | Interne URL voor metadata ophalen; alleen ingesteld als `KeycloakOptions.MetadataAddress` gevuld is |
| `NameClaimType`                 | `preferred_username`                                                                    |
| `RoleClaimType`                 | `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`                          |
| `PushedAuthorizationBehavior`   | `Disable` — PAR uitgeschakeld totdat dit expliciet in Keycloak is geconfigureerd        |

> **RoleClaimType:** ASP.NET Core mapt Keycloak-rollen automatisch naar het lange Microsoft schema-URI. Door `RoleClaimType` hierop in te stellen werken `[Authorize(Roles="admin")]` en `<AuthorizeView Roles="admin">` direct zonder extra claim mapping.

---

### AuthServiceExtensions

`Auth/AuthServiceExtensions.cs`

Extension method op `IServiceCollection`. Bundelt alle service-registraties in één aanroep: `builder.Services.AddKeycloakAuthentication()`.

Registreert:

- `KeycloakOptions` via `AddOptions<T>().BindConfiguration()` met `ValidateDataAnnotations()` en `ValidateOnStart()`
- `ConfigureKeycloakOptions` als `IConfigureOptions<OpenIdConnectOptions>`
- `AddAuthentication()` met Cookie als default scheme en OpenID Connect als challenge scheme
- Cookie-opties: `HttpOnly = true`, `SameSite = Lax`, `SecurePolicy = None`
- Correlation- en nonce-cookies: `SameSite = Unspecified`, `SecurePolicy = None`
- `AddAuthorization()`
- `AddHttpContextAccessor()`
- `AddCascadingAuthenticationState()`

> **SameSite = Unspecified op correlation-cookies:** Keycloak draait op een ander host/IP dan de Blazor-app. Dit is een cross-site redirect. Met `SameSite=Lax` blokkeert de browser de correlation cookie bij de terugkeer van Keycloak. `Unspecified` stuurt geen `SameSite`-attribuut mee, waardoor de cookie altijd wordt doorgestuurd.

---

### AuthEndpointExtensions

`Auth/AuthEndpointExtensions.cs`

Extension method op `IEndpointRouteBuilder`. Registreert twee minimale API-endpoints via `app.MapAuthEndpoints()`.

**Belangrijk:** `MapAuthEndpoints()` moet in `Program.cs` vóór `MapRazorComponents()` worden aangeroepen zodat `/login` en `/logout` niet door de Blazor-router worden onderschept.

| Endpoint  | Method | Authenticatie    | Toelichting                                                       |
|-----------|--------|------------------|-------------------------------------------------------------------|
| `/login`  | GET    | `AllowAnonymous` | Roept `ChallengeAsync` aan; valideert `returnUrl` tegen open-redirect |
| `/logout` | GET    | Verplicht        | Verwijdert de lokale cookie — Keycloak SSO-sessie blijft actief   |

Beide endpoints hebben `.DisableAntiforgery()` omdat ze HTTP-redirects schrijven en geen formulierdata verwerken. Het `/login` endpoint valideert de `returnUrl` om open-redirect aanvallen te voorkomen.

---

## Blazor-integratie

### Routes.razor

`Components/Routes.razor`

Gebruikt `AuthorizeRouteView` in plaats van de standaard `RouteView`. Bij niet-geautoriseerde toegang:

- Niet ingelogd → `<RedirectToNotLoggedIn />` component
- Ingelogd maar geen toegang → melding op de pagina

---

### RedirectToNotLoggedIn.razor

`Components/RedirectToNotLoggedIn.razor`

Navigeert de gebruiker naar `/niet-aangemeld` met `forceLoad: false`. Vervangt de voormalige `RedirectToLogin.razor` die direct naar Keycloak stuurde.

---

### AccessDenied.razor

`Components/Pages/AccessDenied.razor` — route: `/niet-aangemeld`

Toont een pagina met een duidelijke melding en een knop om in te loggen. Wordt getoond in plaats van een automatische redirect naar Keycloak, zodat de gebruiker bewust kiest om in te loggen.

---

### NavMenu.razor

`Components/Layout/NavMenu.razor`

Gebruikt `<AuthorizeView>` om conditioneel inlog- en uitlogknoppen te tonen.

- `<Authorized>`: toont de gebruikersnaam en een uitlogknop
- `<NotAuthorized>`: toont een inlogknop

Beide knoppen gebruiken `NavigationManager.NavigateTo(..., forceLoad: true)` om een echte HTTP-navigatie te forceren.

---

### Claims.razor

`Components/Pages/Claims.razor` — route: `/claims`

Toont een overzicht van alle claims ontvangen van Keycloak. Gebruikt `<AuthorizeView>` met `<RedirectToNotLoggedIn />` in de `<NotAuthorized>`-tak. Het access token wordt asynchroon opgehaald via `OnInitializedAsync` met `await`.

---

### Weather.razor

`Components/Pages/Weather.razor` — route: `/weather`

Alleen zichtbaar voor ingelogde gebruikers. Gebruikt `<AuthorizeView>` in plaats van `@attribute [Authorize]` om te voorkomen dat de server direct een OIDC-challenge stuurt en de gebruiker omleidt naar Keycloak zonder tussenkomst van de Blazor-router.

---

### Counter.razor

`Components/Pages/Counter.razor` — route: `/counter`

De **Click me**-knop is alleen bedienbaar door gebruikers met de `admin` client-rol. Gebruikers zonder deze rol zien de knop uitgeschakeld met een toelichting.

```razor
<AuthorizeView Roles="admin">
    <Authorized>
        <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>
    </Authorized>
    <NotAuthorized>
        <button class="btn btn-primary" disabled>Click me</button>
        <p class="text-muted">U heeft de admin rol nodig om de teller te verhogen.</p>
    </NotAuthorized>
</AuthorizeView>
```

---

## Docker

De applicatie wordt als container uitgerold naast een Keycloak-container via `docker-compose.yml`.

### URL-splitsing

In Docker communiceren containers intern via servicenamen, maar de browser gebruikt het host-IP. Dit vereist een splitsing:

| Instelling        | Waarde                                          | Gebruik                                  |
|-------------------|-------------------------------------------------|------------------------------------------|
| `Authority`       | `http://192.168.x.x:8082/realms/homelab`        | Browser redirects + issuer-validatie     |
| `MetadataAddress` | `http://keycloak:8082/realms/homelab/.well-known/openid-configuration` | Server haalt OIDC-metadata intern op |
| `KC_HOSTNAME`     | `192.168.x.x`                                   | Keycloak genereert publieke URLs         |

### Data Protection

ASP.NET Core Data Protection-keys worden persistent opgeslagen in een Docker volume zodat antiforgery tokens en correlation cookies container-herstarts overleven:

```csharp
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("BlazorWebAppWithKeycloak");
```

---

## Stroom

### Inloggen

```
Gebruiker klikt Inloggen
        │
        ▼
NavigationManager.NavigateTo("/login?returnUrl=...", forceLoad: true)
        │
        ▼
GET /login endpoint — returnUrl gevalideerd tegen open-redirect
        │
        ▼
ChallengeAsync → HTTP 302 redirect naar Keycloak loginpagina
        │
        ▼
Gebruiker logt in op Keycloak
        │
        ▼
Keycloak redirect naar http://app/signin-oidc?code=...
        │
        ▼
OIDC-middleware wisselt code in voor tokens (backchannel)
        │
        ▼
Claims worden opgehaald via UserInfo-endpoint
        │
        ▼
Cookie wordt aangemaakt, redirect naar returnUrl
        │
        ▼
Gebruiker is ingelogd — AuthorizeView toont beveiligde inhoud
```

### Niet-ingelogde gebruiker bezoekt beveiligde pagina

```
Gebruiker bezoekt /weather of /claims
        │
        ▼
AuthorizeView detecteert: niet ingelogd
        │
        ▼
<RedirectToNotLoggedIn /> navigeert naar /niet-aangemeld
        │
        ▼
AccessDenied.razor toont melding + inlogknop
```

### Uitloggen

```
Gebruiker klikt Uitloggen
        │
        ▼
NavigationManager.NavigateTo("/logout", forceLoad: true)
        │
        ▼
GET /logout endpoint
        │
        ▼
SignOutAsync(Cookie) → lokale applicatiesessie beëindigd
        │
        ▼
Redirect naar /
        │
        ▼
Gebruiker is uitgelogd uit de applicatie
Keycloak SSO-sessie blijft actief
```