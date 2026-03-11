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
  - [RedirectToLogin.razor](#redirecttologinrazor)
  - [NavMenu.razor](#navmenurazor)
  - [Claims.razor](#claimsrazor)
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
│   └── launchSettings.json           # Startprofiel (HTTP poort 5000)
├── wwwroot/
├── Auth/
│   ├── AuthEndpointExtensions.cs     # IEndpointRouteBuilder extensie: MapAuthEndpoints()
│   ├── AuthServiceExtensions.cs      # IServiceCollection extensie: AddKeycloakAuthentication()
│   ├── ConfigureKeycloakOptions.cs   # Vult OpenIdConnectOptions via IConfigureNamedOptions
│   └── KeycloakOptions.cs            # Sterk-getypeerde configuratieklasse
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor             # Toont inloggen/uitloggen op basis van auth-status
│   ├── Pages/
│   │   └── Claims.razor              # Beveiligde pagina met claims-overzicht
│   ├── RedirectToLogin.razor         # Stuurt niet-geauthenticeerde gebruikers door naar /login
│   └── Routes.razor                  # Bevat AuthorizeRouteView voor beveiligde routes
├── appsettings.json                  # Keycloak-verbindingsgegevens
├── appsettings.Development.json      # Development-specifieke instellingen
└── Program.cs                        # Registratie en middleware-pipeline
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
    "Authority": "http://<keycloak-host>:8080/realms/homelab",
    "ClientId": "blazor-web-app",
    "ClientSecret": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "RequireHttpsMetadata": false
  }
}
```

| Veld                   | Toelichting                                                                 |
|------------------------|-----------------------------------------------------------------------------|
| `Authority`            | OIDC-discovery URL inclusief realm-pad                                      |
| `ClientId`             | Client ID zoals aangemaakt in Keycloak                                      |
| `ClientSecret`         | Client secret van het tabblad **Credentials** in Keycloak                  |
| `RequireHttpsMetadata` | `false` voor lokale HTTP-ontwikkeling, `true` in productie                  |

> **Nooit het client secret in versiebeheer opslaan.** Gebruik voor lokale ontwikkeling `dotnet user-secrets`:
>
> ```bash
> dotnet user-secrets set "Keycloak:ClientSecret" "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
> ```

---

## Architectuur

De authenticatie-implementatie bestaat uit vier klassen in de `Auth/`-map. Elke klasse heeft één verantwoordelijkheid.

### KeycloakOptions

`Auth/KeycloakOptions.cs`

Sterk-getypeerde POCO-klasse die de `Keycloak`-sectie uit `appsettings.json` representeert. Gebonden via de Options-pattern.

```csharp
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority            { get; init; } = string.Empty;
    public string ClientId             { get; init; } = string.Empty;
    public string ClientSecret         { get; init; } = string.Empty;
    public bool   RequireHttpsMetadata { get; init; } = false;
}
```

---

### ConfigureKeycloakOptions

`Auth/ConfigureKeycloakOptions.cs`

Implementeert `IConfigureNamedOptions<OpenIdConnectOptions>`. Het framework roept deze klasse aan wanneer `OpenIdConnectOptions` voor het eerst nodig is. `KeycloakOptions` wordt via de constructor geïnjecteerd — geen `BuildServiceProvider()` nodig.

Geconfigureerde instellingen:

| Instelling                          | Waarde / Toelichting                                          |
|-------------------------------------|---------------------------------------------------------------|
| `ResponseType`                      | `code` — Authorization Code Flow                             |
| `SaveTokens`                        | `true` — access en refresh token beschikbaar via `GetTokenAsync` |
| `GetClaimsFromUserInfoEndpoint`     | `true` — profiel- en emailclaims worden opgehaald            |
| Scopes                              | `openid`, `profile`, `email`                                 |
| `ClaimActions.MapJsonKey`           | Mapt `realm_access` naar de claim `roles`                    |
| `NameClaimType`                     | `preferred_username`                                         |
| `RoleClaimType`                     | `roles`                                                      |

---

### AuthServiceExtensions

`Auth/AuthServiceExtensions.cs`

Extension method op `IServiceCollection`. Bundelt alle service-registraties die bij authenticatie horen in één aanroep: `builder.Services.AddKeycloakAuthentication()`.

Registreert:

- `KeycloakOptions` via `AddOptions<T>().BindConfiguration()` met `ValidateOnStart()`
- `ConfigureKeycloakOptions` als `IConfigureOptions<OpenIdConnectOptions>`
- `AddAuthentication()` met Cookie als default scheme en OpenID Connect als challenge scheme
- Cookie-opties: `HttpOnly = true`, `SameSite = Lax`
- `AddOpenIdConnect()` zonder lambda — opties komen uit `ConfigureKeycloakOptions`
- `AddAuthorization()`
- `AddHttpContextAccessor()`
- `AddCascadingAuthenticationState()`

---

### AuthEndpointExtensions

`Auth/AuthEndpointExtensions.cs`

Extension method op `IEndpointRouteBuilder`. Registreert twee minimale API-endpoints via `app.MapAuthEndpoints()`.

**Belangrijk:** `MapAuthEndpoints()` moet in `Program.cs` vóór `MapRazorComponents()` worden aangeroepen. Blazor's router claimt routes via catch-all matching; door de endpoints eerder te registreren worden `/login` en `/logout` door ASP.NET Core's endpoint routing afgehandeld voordat Blazor er aan te pas komt.

| Endpoint   | Method | Authenticatie    | Toelichting                                                      |
|------------|--------|------------------|------------------------------------------------------------------|
| `/login`   | GET    | `AllowAnonymous` | Roept `ChallengeAsync` aan met de `returnUrl` als `RedirectUri` |
| `/logout`  | GET    | Verplicht        | Tekent uit bij zowel cookie- als OIDC-scheme                    |

Beide endpoints hebben `.DisableAntiforgery()` omdat ze HTTP-redirects schrijven en geen formulierdata verwerken.

---

## Blazor-integratie

### Routes.razor

`Components/Routes.razor`

Gebruikt `AuthorizeRouteView` in plaats van de standaard `RouteView`. Dit zorgt dat het `[Authorize]`-attribuut op pagina's automatisch wordt gerespecteerd.

Bij niet-geautoriseerde toegang:
- Niet ingelogd → `<RedirectToLogin />` component
- Ingelogd maar geen toegang → melding op de pagina

---

### RedirectToLogin.razor

`Components/RedirectToLogin.razor`

Eenvoudige component die bij `OnInitialized` de gebruiker doorstuurt naar `/login` met de huidige URL als `returnUrl`. Gebruikt `forceLoad: true` zodat de Blazor-router wordt omzeild en de browser een echte HTTP-request stuurt naar het `/login` endpoint.

```csharp
Navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
```

---

### NavMenu.razor

`Components/Layout/NavMenu.razor`

Gebruikt `<AuthorizeView>` om conditioneel inlog- en uitlogknoppen te tonen.

- `<Authorized>`: toont de gebruikersnaam en een uitlogknop
- `<NotAuthorized>`: toont een inlogknop

Beide knoppen gebruiken `NavigationManager.NavigateTo(..., forceLoad: true)` om een echte HTTP-navigatie te forceren. Een gewone `<a href="/login">` zou door de Blazor-router worden onderschept en het endpoint nooit bereiken.

---

### Claims.razor

`Components/Pages/Claims.razor`

Beveiligde pagina (`@attribute [Authorize]`, `@rendermode InteractiveServer`) bereikbaar op `/claims`. Toont een overzicht van alle claims die de applicatie van Keycloak heeft ontvangen:

- Gebruikerskaart met naam, authenticatiestatus en authenticatietype
- Tabel met alle claims gesorteerd op type, inclusief de `roles`-claim
- Optioneel het ruwe access token, met een link naar [jwt.io](https://jwt.io) voor decodering

---

## Stroom

```
Gebruiker klikt Inloggen
        │
        ▼
NavigationManager.NavigateTo("/login?returnUrl=...", forceLoad: true)
        │
        ▼
GET /login endpoint (AuthEndpointExtensions)
        │
        ▼
ChallengeAsync → HTTP 302 redirect naar Keycloak loginpagina
        │
        ▼
Gebruiker logt in op Keycloak
        │
        ▼
Keycloak redirect naar https://app/signin-oidc?code=...
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
