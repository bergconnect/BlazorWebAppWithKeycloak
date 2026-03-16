# Keycloak Authenticatie — Blazor Web App + API

Deze README beschrijft de implementatie van Keycloak OIDC-authenticatie in de Blazor Web App en de bijbehorende Minimal API. De applicatie gebruikt de Authorization Code Flow via de ASP.NET Core OpenID Connect-middleware voor de Blazor-frontend en JWT Bearer-authenticatie voor de API.

---

## Inhoudsopgave

- [Vereisten](#vereisten)
- [Projectstructuur](#projectstructuur)
- [NuGet-pakketten](#nuget-pakketten)
- [Configuratie](#configuratie)
- [Architectuur — Blazor Web App](#architectuur--blazor-web-app)
  - [KeycloakOptions](#keycloakoptions)
  - [ConfigureKeycloakOptions](#configurekeycloakoptions)
  - [AuthServiceExtensions](#authserviceextensions)
  - [AuthEndpointExtensions](#authendpointextensions)
- [Architectuur — API](#architectuur--api)
  - [KeycloakOptions (API)](#keycloakoptions-api)
  - [ConfigureJwtBearerOptions](#configurejwtbeareroptions)
  - [AuthServiceExtensions (API)](#authserviceextensions-api)
  - [HelloEndpointExtensions](#helloworldendpointextensions)
- [Blazor-integratie](#blazor-integratie)
  - [Routes.razor](#routesrazor)
  - [RedirectToNotLoggedIn.razor](#redirecttonotloggedinrazor)
  - [AccessDenied.razor](#accessdeniedrazor)
  - [NavMenu.razor](#navmenurazor)
  - [Claims.razor](#claimsrazor)
  - [Weather.razor](#weatherrazor)
  - [Counter.razor](#counterrazor)
  - [HelloWorld.razor](#helloworldrazor)
  - [Admin.razor](#adminrazor)
- [Services](#services)
  - [TokenRefreshService](#tokenrefreshservice)
  - [BearerTokenHandler](#bearertokenhandler)
  - [HelloWorldApiClient](#helloworldapiclient)
  - [WeatherForecast](#weatherforecast)
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
│       └── build-images.yml              # Gecombineerde CI/CD pipeline (matrix)
├── BlazorWebAppWithKeycloak/             # Blazor Web App (frontend)
│   ├── Auth/
│   │   ├── AuthEndpointExtensions.cs     # /login en /logout endpoints
│   │   ├── AuthServiceExtensions.cs      # AddKeycloakAuthentication()
│   │   ├── ConfigureKeycloakOptions.cs   # Vult OpenIdConnectOptions
│   │   └── KeycloakOptions.cs            # Sterk-getypeerde configuratie
│   ├── Components/
│   │   ├── Layout/
│   │   │   └── NavMenu.razor             # Login/logout navigatie
│   │   ├── Pages/
│   │   │   ├── AccessDenied.razor        # /niet-aangemeld
│   │   │   ├── Claims.razor              # /claims — token-overzicht
│   │   │   ├── Counter.razor             # /counter — admin-only knop
│   │   │   ├── Admin.razor               # /admin — admin-only API aanroep
│   │   │   ├── HelloWorld.razor          # /hello-world — API aanroep
│   │   │   └── Weather.razor             # /weather — vereist login
│   │   ├── RedirectToNotLoggedIn.razor   # Navigeert naar /niet-aangemeld
│   │   └── Routes.razor                  # AuthorizeRouteView
│   ├── Services/
│   │   ├── TokenRefreshService.cs        # Automatisch access token verversen
│   │   ├── BearerTokenHandler.cs         # Voegt geldig Bearer token toe aan HttpClient
│   │   ├── HelloWorldApiClient.cs        # Typed HttpClient voor de API
│   │   └── WeatherForecast.cs            # Record model voor weersdata
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                        # ForwardedHeaders + omgevingsafhankelijke cookies
├── BlazorWebAppWithKeycloak.API/         # Minimal API (backend)
│   ├── Auth/
│   │   ├── AuthServiceExtensions.cs      # AddKeycloakJwtAuthentication()
│   │   ├── ConfigureJwtBearerOptions.cs  # Vult JwtBearerOptions
│   │   └── KeycloakOptions.cs            # Configuratie + gedeelde RoleClaimType
│   ├── Extensions/
│   │   └── HelloEndpointExtensions.cs    # MapHelloEndpoints()
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
├── docker-compose.yml
├── .env                                  # Secrets en image-namen (in .gitignore)
└── .env.example                          # Voorbeeld zonder gevoelige waarden
```

---

## NuGet-pakketten

**Blazor Web App:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.4" />
```

**API:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
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
| `SaveTokens`                    | `true` — tokens beschikbaar via `GetTokenAsync` voor API-aanroepen en token refresh |
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
- `ExpireTimeSpan = 8 uur` met `SlidingExpiration = true` — sessie verlengt bij activiteit
- Correlation- en nonce-cookies: `SameSite = Unspecified`, `SecurePolicy = None`

> **SameSite = Unspecified:** Keycloak draait op een ander IP dan de Blazor-app. Met `Lax` blokkeert de browser de correlation cookie bij de terugkeer van Keycloak. `Unspecified` stuurt geen `SameSite`-attribuut, waardoor de cookie altijd wordt doorgestuurd.

> **Afstemming met Keycloak:** `ExpireTimeSpan` moet gelijk zijn aan of korter dan de `SSO Session Idle` instelling in Keycloak. Zie [Sessiebeheer](#sessiebeheer) voor de volledige aanbevolen configuratie.

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

### Claims.razor

Route: `/claims`. Gebruikt `<AuthorizeView>` zonder `@attribute [Authorize]` — de dubbele check veroorzaakte problemen bij InteractiveServer. Niet-ingelogde gebruikers worden via `<RedirectToNotLoggedIn />` omgeleid.

### Weather.razor

Route: `/weather`. Gebruikt `<AuthorizeView>` in plaats van `@attribute [Authorize]` om een directe OIDC-server-challenge te vermijden. Het `WeatherForecast` model staat als `record` in een apart bestand in `Services/`.

### Counter.razor

Route: `/counter`. De knop is alleen bedienbaar met de `admin` client-rol via `<AuthorizeView Roles="admin">`. Niet-admins zien de knop uitgeschakeld.

### HelloWorld.razor

Route: `/hello-world`. Roept server-side de API aan via `HelloWorldApiClient`. Toont het antwoord en een knop om opnieuw aan te roepen. Foutafhandeling per HTTP-statuscode (401, 403, overig).

---

### Admin.razor

Route: `/admin`. Roept server-side het admin-endpoint aan via `HelloWorldApiClient.GetAdminAsync()`. Gebruikt geneste `<AuthorizeView>` met expliciete `Context` namen om ambiguïteitsfouten te voorkomen:

- Buitenste `<AuthorizeView>` — controleert of de gebruiker ingelogd is
- Binnenste `<AuthorizeView Roles="admin" Context="adminContext">` — controleert de admin-rol

Niet-admins zien een waarschuwingsmelding. De **Admin API** link in het navigatiemenu is alleen zichtbaar voor gebruikers met de `admin` rol.

---

## Services

### TokenRefreshService

`Services/TokenRefreshService.cs`

Beheert de levenscyclus van access tokens. Wordt aangeroepen door `BearerTokenHandler` vóór elke uitgaande API-request.

**Werking:**

1. Controleert of het opgeslagen `access_token` verlopen is of binnen 30 seconden verloopt (de buffer voorkomt race conditions bij gelijktijdige requests)
2. Als het token nog geldig is, wordt het direct teruggegeven
3. Als het token verlopen is, vraagt de service een nieuw token op bij Keycloak via het `refresh_token`
4. Het nieuwe token wordt teruggeschreven naar de authenticatiecookie zodat alle volgende requests er gebruik van maken

**Methoden:**

| Methode | Retourwaarde | Toelichting |
|---------|-------------|-------------|
| `GetValidAccessTokenAsync()` | `string?` | Geeft een geldig access token terug; vernieuwt automatisch indien nodig |
| `HasValidRefreshTokenAsync()` | `bool` | Controleert of een refresh token beschikbaar is — nuttig voor UI-logica |

> **Geregistreerd als `Scoped`** in `Program.cs`, consistent met `BearerTokenHandler`.

---

### BearerTokenHandler

`Services/BearerTokenHandler.cs`

`DelegatingHandler` die als pipeline-middleware op de `HttpClient` zit. Vraagt via `TokenRefreshService` een geldig access token op en voegt dit toe als `Authorization: Bearer` header. Bij een ontbrekend of niet-vernieuwbaar token (verlopen Keycloak-sessie) geeft de handler `401 Unauthorized` terug zodat de UI de gebruiker naar `/login` kan sturen.

### HelloWorldApiClient

`Services/HelloWorldApiClient.cs`

Typed `HttpClient` voor de API. Endpoint paden staan als `private const`. Biedt twee methoden:

| Methode | Endpoint | Vereiste rol |
|---------|----------|--------------|
| `GetHelloAsync()` | `GET /api/hello` | `user` |
| `GetAdminAsync()` | `GET /api/admin` | `admin` |

Geregistreerd in `Program.cs` met `BearerTokenHandler`:

```csharp
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<BearerTokenHandler>();

builder.Services
    .AddHttpClient<HelloWorldApiClient>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001");
    })
    .AddHttpMessageHandler<BearerTokenHandler>();
```

### WeatherForecast

`Services/WeatherForecast.cs`

Immutable `record` model. Staat in een apart bestand in plaats van als private nested class in de Razor component.

---

## Sessiebeheer

De sessieduur wordt bepaald door drie lagen die op elkaar afgestemd moeten zijn.

### Aanbevolen configuratie (zakelijke app / werkdag)

**Keycloak — Realm Settings → Sessions en Tokens:**

| Instelling | Aanbevolen waarde | Toelichting |
|---|---|---|
| SSO Session Idle | `8u` | Keycloak-sessie verloopt bij inactiviteit |
| SSO Session Max | `10u` | Harde grens, ongeacht activiteit |
| Access Token Lifespan | `5m` | Kort voor veiligheid — `TokenRefreshService` vernieuwt transparant |
| Refresh Token Lifespan | *(erft van SSO Session Idle)* | Automatisch gelijk aan idle timeout |

**ASP.NET Core — `AuthServiceExtensions.cs`:**

```csharp
options.ExpireTimeSpan    = TimeSpan.FromHours(8);  // Gelijk aan SSO Session Idle
options.SlidingExpiration = true;                    // Verlengt bij activiteit
```

### Hoe de drie lagen samenwerken

```
Gebruiker doet een API-request
         │
         ▼
BearerTokenHandler → TokenRefreshService.GetValidAccessTokenAsync()
         │
         ├─ Token nog geldig (expires_at > nu + 30s) → direct doorzetten
         │
         └─ Token verlopen of bijna verlopen
                  │
                  ▼
         Refresh token aanwezig?
                  │
                  ├─ Ja → POST /token met grant_type=refresh_token
                  │         │
                  │         ├─ Keycloak SSO Session nog actief → nieuw access token
                  │         │   Cookie bijgewerkt → request doorgezet
                  │         │
                  │         └─ Keycloak SSO Session verlopen → 401 terug
                  │             UI stuurt gebruiker naar /login
                  │
                  └─ Nee → 401 terug → UI stuurt gebruiker naar /login
```

### Sessieverloop afhandelen in de UI

Als de Keycloak-sessie verlopen is, geeft `BearerTokenHandler` een `401` terug. Vang dit op in Razor components:

```csharp
var result = await ApiClient.GetWeatherAsync();
if (result.StatusCode == HttpStatusCode.Unauthorized)
{
    Navigation.NavigateTo("/login?returnUrl=/weather", forceLoad: true);
}
```

### Kortere sessies (gevoelige toepassingen)

Voor financiële, medische of anderszins gevoelige applicaties gelden strengere richtlijnen (o.a. NEN 7510, ISO 27001):

| Instelling | Waarde |
|---|---|
| SSO Session Idle | `15–30m` |
| SSO Session Max | `4–8u` |
| Access Token Lifespan | `5m` |
| `ExpireTimeSpan` (cookie) | Gelijk aan SSO Session Idle |
| `SlidingExpiration` | `false` — sessie verlengt niet bij activiteit |

---

## Docker

### Services

| Service    | Image                                         | Poort          |
|------------|-----------------------------------------------|----------------|
| `blazor` | `${BLAZOR_IMAGE}` (via `.env`)  | `5000 → 8080`  |
| `api`    | `${API_IMAGE}` (via `.env`)     | `5001 → 8080`  |

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
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}
```

Zonder dit gebruikt de OIDC-handler `http://localhost:5000/signin-oidc` als `redirect_uri` — wat niet overeenkomt met de ingestelde redirect URI in Keycloak.

Lokaal (Development) worden forwarded headers niet verwerkt zodat `http://localhost:5000` correct blijft werken.

---

## CI/CD

Één workflow met matrix strategy bouwt beide projecten:

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
/weather, /claims of /hello-world
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

### API aanroepen vanuit Blazor (met token refresh)

```
HelloWorld.razor → HelloWorldApiClient.GetHelloAsync()
        │
        ▼
BearerTokenHandler → TokenRefreshService.GetValidAccessTokenAsync()
        │
        ├─ Token geldig → direct Bearer header toevoegen
        │
        └─ Token verlopen → refresh via Keycloak → nieuw token in cookie
                │
                ▼
GET /api/hello met Authorization: Bearer <token>
                │
                ▼
API valideert token (issuer, audience, handtekening, rol "user")
                │
                ▼
{ "message": "Hallo, lvdberg!", "timestamp": "..." }
```

### Uitloggen

```
/logout → SignOutAsync(Cookie) → lokale sessie beëindigd
Keycloak SSO-sessie blijft actief
```
