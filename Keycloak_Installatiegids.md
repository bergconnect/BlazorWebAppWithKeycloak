# Keycloak Installatie & Configuratiegids

> **Omgeving:** Docker · Keycloak 26+ (dev mode) · Realm: `homelab` · Client: `blazor-web-app`

---

## Inhoudsopgave

1. [Keycloak opstarten via Docker Compose](#1-keycloak-opstarten-via-docker-compose)
2. [Realm aanmaken](#2-realm-aanmaken)
3. [Client toevoegen](#3-client-toevoegen)
4. [Client rollen aanmaken](#4-client-rollen-aanmaken)
5. [Gebruiker aanmaken en rol toewijzen](#5-gebruiker-aanmaken-en-rol-toewijzen)
6. [Bijlagen](#bijlagen)
   - [Bijlage A — Handige URLs](#bijlage-a--handige-urls)
   - [Bijlage B — Container beheren](#bijlage-b--container-beheren)
   - [Bijlage C — Probleemoplossing](#bijlage-c--probleemoplossing)
   - [Bijlage D — Voorbeeld docker-compose.yml](#bijlage-d--voorbeeld-docker-composeyml)

---

## 1. Keycloak opstarten via Docker Compose

De aanbevolen manier om Keycloak, de Blazor Web App en de API samen te draaien is via de meegeleverde `docker-compose.yml`.

### 1.1 Vereisten

- Docker met Docker Compose
- Een `.env` bestand met het client secret

### 1.2 `.env` aanmaken

Maak een `.env` bestand aan naast `docker-compose.yml`:

```
KEYCLOAK_CLIENT_SECRET=jouw-client-secret-hier
```

> Voeg `.env` toe aan `.gitignore` zodat het secret niet in versiebeheer terechtkomt.

### 1.3 Opstarten

```bash
docker compose up -d
```

| Service    | URL                            |
|------------|--------------------------------|
| Keycloak (gehost) | `https://idp.berg-connect.nl`  |
| Blazor app        | `https://demo.berg-connect.nl` |
| API (intern)      | `http://<host-ip>:5001`        |

### 1.4 Beheerconsole

```
http://<host-ip>:8082/admin
```

| Veld       | Waarde  |
|------------|---------|
| Gebruiker  | `admin` |
| Wachtwoord | `admin` |

> **Let op:** wijzig het beheerderswachtwoord na eerste gebruik.

---

## 2. Handmatig: nieuwe realm aanmaken

### Stap 1 — Inloggen op de beheerconsole

Ga naar `http://<host-ip>:8082/admin` en log in als `admin`.

### Stap 2 — Realm aanmaken

Klik linksboven op **Manage realms** → **Create realm**.

| Veld       | Waarde    |
|------------|-----------|
| Realm name | `homelab` |
| Enabled    | `ON`      |

Klik op **Create**. De console schakelt automatisch over naar de nieuwe realm.

---

## 3. Client toevoegen

### Stap 1 — Navigeren

Klik in het linkermenu op **Clients** → **Create client**.

### Stap 2 — General Settings

| Veld        | Waarde                                     |
|-------------|--------------------------------------------|
| Client type | `OpenID Connect`                           |
| Client ID   | `blazor-web-app`                           |
| Name        | `Blazor Web App`                           |
| Description | `Webapplicatie met Keycloak authenticatie` |

### Stap 3 — Capability Config

| Instelling            | Waarde | Toelichting                                 |
|-----------------------|--------|---------------------------------------------|
| Client authentication | `ON`   | Confidential client met client secret       |
| Authorization         | `OFF`  |                                             |
| Standard flow         | `ON`   | Authorization Code Flow via browser         |
| Direct access grants  | `OFF`  |                                             |

### Stap 4 — Login Settings

| Veld                            | Waarde                                        |
|---------------------------------|-----------------------------------------------|
| Root URL                        | `http://localhost:5000`                       |
| Home URL                        | `http://localhost:5000`                       |
| Valid redirect URIs             | `http://localhost:5000/signin-oidc`           |
| Valid post logout redirect URIs | `http://localhost:5000/signout-callback-oidc` |
| Web origins                     | `http://localhost:5000`                       |

Klik op **Save**.

### Stap 5 — Client secret ophalen

Klik op het tabblad **Credentials** en kopieer de waarde bij **Client secret**.

```bash
# Development
dotnet user-secrets set "Keycloak:ClientSecret" "jouw-secret"

# Productie / Docker
export Keycloak__ClientSecret="jouw-secret"
```

### Stap 6 — Rollen in token opnemen (client-roles mapper)

Standaard worden client-rollen niet als platte claim meegestuurd. Voeg een mapper toe:

1. Ga naar **Clients** → `blazor-web-app` → **Client scopes**
2. Klik op **`blazor-web-app-dedicated`**
3. Klik op **Configure a new mapper** → kies **User Client Role**

| Veld             | Waarde           |
|------------------|------------------|
| Name             | `client-roles`   |
| Client ID        | `blazor-web-app` |
| Multivalued      | `ON`             |
| Token Claim Name | `roles`          |
| Claim JSON Type  | `String`         |
| Add to ID token  | `ON`             |
| Add to access token | `ON`          |
| Add to userinfo  | `ON`             |

Klik op **Save**.

### Stap 7 — Audience mapper toevoegen

De API valideert het `aud`-veld in het access token. Zonder deze mapper bevat het token alleen `"aud": "account"` en weigert de API het token.

Klik op **Configure a new mapper** → kies **Audience**:

| Veld                       | Waarde           |
|----------------------------|------------------|
| Name                       | `audience`       |
| Included Client Audience   | `blazor-web-app` |
| Add to ID token            | `OFF`            |
| Add to access token        | `ON`             |
| Add to token introspection | `ON`             |

Klik op **Save**.

> Na het toevoegen van deze mapper moet de gebruiker opnieuw inloggen zodat een nieuw token met de juiste audience wordt uitgegeven. Verifieer via jwt.io dat `"aud"` nu `["blazor-web-app", "account"]` bevat.

---

## 4. Client rollen aanmaken

Ga naar **Clients** → `blazor-web-app` → **Roles** → **Create role**.

### Rol `admin`

| Veld        | Waarde                                                                         |
|-------------|--------------------------------------------------------------------------------|
| Role name   | `admin`                                                                        |
| Description | `Volledige beheertoegang. Toegang tot alle pagina's inclusief beheer en instellingen.` |

### Rol `user`

| Veld        | Waarde                                                                         |
|-------------|--------------------------------------------------------------------------------|
| Role name   | `user`                                                                         |
| Description | `Standaard gebruikersrol. Geeft toegang tot het dashboard en gedeeld portaal.` |

---

## 5. Gebruiker aanmaken en rol toewijzen

### Stap 1 — Gebruiker aanmaken

Ga naar **Users** → **Create new user**.

| Veld           | Waarde           |
|----------------|------------------|
| Email verified | `ON`             |
| Username       | `jan.de.vries`   |
| Email          | `jan@homelab.nl` |
| First name     | `Jan`            |
| Last name      | `de Vries`       |

Klik op **Create**.

### Stap 2 — Wachtwoord instellen

Ga naar tabblad **Credentials** → **Set password**.

Zet **Temporary** op `OFF` en klik op **Save** → **Save password**.

### Stap 3 — Rol toewijzen

Ga naar tabblad **Role mapping** → **Assign role** → **Client roles**.

Selecteer `admin` of `user` onder **blazor-web-app** en klik op **Assign**.

### Stap 4 — Verificatie via Evaluate

1. Ga naar **Clients** → `blazor-web-app` → **Client scopes** → **Evaluate**
2. Selecteer de gebruiker
3. Klik op **Generated access token**

Het token moet bevatten:

```json
{
  "aud": ["blazor-web-app", "account"],
  "preferred_username": "jan.de.vries",
  "roles": ["admin", "user"]
}
```

> Als `roles` ontbreekt: controleer of **Client ID** in de mapper exact `blazor-web-app` is (hoofdlettergevoelig) en **Multivalued** op `ON` staat.
>
> Als `aud` alleen `account` bevat: controleer of de audience mapper is aangemaakt (stap 7) en log opnieuw in.

---

## Bijlagen

### Bijlage A — Handige URLs

| Doel                      | URL                                                                      |
|---------------------------|--------------------------------------------------------------------------|
| Keycloak beheerconsole    | `https://idp.berg-connect.nl/admin`                                                    |
| Gebruikersportaal         | `https://idp.berg-connect.nl/realms/homelab/account`                                   |
| OIDC discovery endpoint   | `https://idp.berg-connect.nl/realms/homelab/.well-known/openid-configuration`          |
| Blazor Web App            | `https://demo.berg-connect.nl`                                                         |
| API (intern)              | `http://<host>:5001`                                                                   |
| API Hello endpoint        | `http://<host>:5001/api/hello`                                                         |
| API OpenAPI (development) | `http://localhost:5114/openapi/v1.json`                                                |

### Bijlage B — Container beheren

```bash
# Starten
docker compose up -d

# Stoppen (data blijft behouden)
docker compose stop

# Stoppen én verwijderen (data blijft in volume)
docker compose down

# Volledig resetten inclusief alle data
docker compose down -v

# Logs bekijken
docker compose logs -f keycloak
docker compose logs -f blazor
docker compose logs -f api

# Nieuwste images ophalen en herstarten
docker compose pull
docker compose up -d
```

### Bijlage C — Probleemoplossing

| Probleem | Oorzaak | Oplossing |
|----------|---------|-----------|
| `Correlation failed` | Correlation cookie wordt niet teruggestuurd door browser | Controleer `SameSite=Unspecified` en `SecurePolicy=None` op correlation-cookies in `AuthServiceExtensions.cs` |
| `Key not found in key ring` | Data Protection keys niet persistent | Controleer of volume `/app/keys` gemount is en `PersistKeysToFileSystem` in `Program.cs` staat |
| `Unable to obtain configuration` | Blazor/API kan Keycloak niet bereiken | Controleer `MetadataAddress` — moet de interne Docker hostnaam gebruiken (`http://keycloak:8082/...`) |
| `invalid_request: Authentication failed` | PAR niet geconfigureerd | `PushedAuthorizationBehavior.Disable` in `ConfigureKeycloakOptions.cs`; controleer of nieuwe image is gedeployd |
| `Audience validation failed` | Audience mapper ontbreekt of token niet vernieuwd | Voeg audience mapper toe (stap 7), log uit en opnieuw in |
| Rollen werken niet in `AuthorizeView` | `RoleClaimType` klopt niet | Controleer `RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"` in beide projecten |
| API geeft 401 terug | Token niet meegestuurd of `SaveTokens = false` | Controleer `SaveTokens = true` in `ConfigureKeycloakOptions.cs` en `BearerTokenHandler` in `Services/` |

## Bijlage D — Voorbeeld docker-compose.yml

Sla dit bestand op als `docker-compose.yml` naast een `.env` bestand.

**.env** (kopieer van `.env.example` en vul in):
```
KEYCLOAK_CLIENT_SECRET=jouw-client-secret-hier
BLAZOR_IMAGE=<jouw-registry>/demo:latest
API_IMAGE=<jouw-registry>/demo-api:latest
```

> Voeg `.env` toe aan `.gitignore`. Commit alleen `.env.example` met lege of placeholder-waarden.

**docker-compose.yml:**
```yaml
services:

  blazor:
    image: ${BLAZOR_IMAGE}
    pull_policy: always
    container_name: blazor
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # Laat ASP.NET Core de X-Forwarded-Proto header van de reverse proxy verwerken
      # zodat https://demo.berg-connect.nl als basis-URL wordt gebruikt.
      - ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
      - Keycloak__Authority=https://idp.berg-connect.nl/realms/homelab
      - Keycloak__ClientId=blazor-web-app
      - Keycloak__ClientSecret=${KEYCLOAK_CLIENT_SECRET}
      - Keycloak__RequireHttpsMetadata=true
      - Logging__LogLevel__Microsoft.AspNetCore.DataProtection=Error
      - ApiSettings__BaseUrl=http://api:8080
    volumes:
      - dataprotection-keys:/app/keys
    depends_on:
      - api

  api:
    image: ${API_IMAGE}
    pull_policy: always
    container_name: api
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Logging__LogLevel__Microsoft.AspNetCore.DataProtection=Error
      - Keycloak__Authority=https://idp.berg-connect.nl/realms/homelab
      - Keycloak__ClientId=blazor-web-app
      - Keycloak__RequireHttpsMetadata=true

volumes:
  dataprotection-keys:
```

### Omgevingen

| Omgeving | Blazor | API | Keycloak |
|----------|--------|-----|---------|
| Productie | `https://demo.berg-connect.nl` | `http://<host>:5001` (intern) | `https://idp.berg-connect.nl` |
| Development | `http://localhost:5000` | `http://localhost:5114` | `https://idp.berg-connect.nl` |

> Image-namen worden ingelezen uit het `.env` bestand — pas `.env` aan voor jouw registry zonder de `docker-compose.yml` te wijzigen.
