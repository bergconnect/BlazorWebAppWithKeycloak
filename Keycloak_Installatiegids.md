# Keycloak Installatie & Configuratiegids

> **Omgeving:** Docker · Keycloak 26+ (dev mode) · Realm: `homelab` · Client: `blazor-web-app`

---

## Inhoudsopgave

1. [Keycloak opstarten via Docker Compose](#1-keycloak-opstarten-via-docker-compose)
2. [Realm importeren via realm-export.json](#2-realm-importeren-via-realm-exportjson)
3. [Handmatig: nieuwe realm aanmaken](#3-handmatig-nieuwe-realm-aanmaken)
4. [Handmatig: client toevoegen](#4-handmatig-client-toevoegen)
5. [Handmatig: client rollen aanmaken](#5-handmatig-client-rollen-aanmaken)
6. [Gebruiker aanmaken en rol toewijzen](#6-gebruiker-aanmaken-en-rol-toewijzen)
7. [Bijlagen](#bijlagen)
   - [Bijlage A — Handige URLs](#bijlage-a--handige-urls)
   - [Bijlage B — Container beheren](#bijlage-b--container-beheren)
   - [Bijlage C — Probleemoplossing](#bijlage-c--probleemoplossing)
   - [Bijlage D — Voorbeeld docker-compose.yml](#bijlage-d--voorbeeld-docker-composeyml)

---

## 1. Keycloak opstarten via Docker Compose

De aanbevolen manier om Keycloak, de Blazor Web App en de API samen te draaien is via de meegeleverde `docker-compose.yml`.

### 1.1 Vereisten

- Docker met Docker Compose
- Het bestand `realm-export.json` in dezelfde map als `docker-compose.yml`
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
| Keycloak   | `http://<host-ip>:8082`        |
| Blazor app | `http://<host-ip>:5000`        |
| API        | `http://<host-ip>:5001`        |

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

## 2. Realm importeren via realm-export.json

De meegeleverde `realm-export.json` bevat de volledige configuratie van de `homelab` realm inclusief client, rollen en mappers. Bij het opstarten van de container importeert Keycloak deze automatisch.

### Werking

De `docker-compose.yml` bevat twee sleutelinstellingen:

```yaml
command: start-dev --import-realm
volumes:
  - ./realm-export.json:/opt/keycloak/data/import/realm-export.json:ro
```

Bij elke **eerste** start (leeg volume) importeert Keycloak de realm automatisch. Een bestaande realm wordt niet overschreven.

### Opnieuw importeren

Wil je de realm opnieuw importeren na wijzigingen in `realm-export.json`:

```bash
docker compose down -v
docker compose up -d
```

> `-v` verwijdert het Keycloak data-volume zodat de volgende start als een schone installatie wordt behandeld.

### Realm exporteren

Na handmatige wijzigingen in de beheerconsole kun je de realm exporteren om `realm-export.json` bij te werken:

```bash
docker exec -it keycloak /opt/keycloak/bin/kc.sh export \
  --dir /opt/keycloak/data/export \
  --realm homelab \
  --users realm_file

docker cp keycloak:/opt/keycloak/data/export/homelab-realm.json ./realm-export.json
```

---

## 3. Handmatig: nieuwe realm aanmaken

Volg deze stappen als je de realm handmatig wilt aanmaken zonder importbestand.

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

## 4. Handmatig: client toevoegen

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

## 5. Handmatig: client rollen aanmaken

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

## 6. Gebruiker aanmaken en rol toewijzen

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
| Keycloak beheerconsole    | `http://<host>:8082/admin`                                               |
| Gebruikersportaal         | `http://<host>:8082/realms/homelab/account`                              |
| OIDC discovery endpoint   | `http://<host>:8082/realms/homelab/.well-known/openid-configuration`     |
| Blazor Web App            | `http://<host>:5000`                                                     |
| API                       | `http://<host>:5001`                                                     |
| API Hello endpoint        | `http://<host>:5001/api/hello`                                           |
| API OpenAPI (development) | `http://<host>:5001/openapi/v1.json`                                     |

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

Sla dit bestand op als `docker-compose.yml` naast `realm-export.json` en een `.env` bestand.

**.env:**
```
KEYCLOAK_CLIENT_SECRET=jouw-client-secret-hier
```

**docker-compose.yml:**
```yaml
services:

  keycloak:
    image: quay.io/keycloak/keycloak:latest
    pull_policy: always
    container_name: keycloak
    command: start-dev --import-realm
    environment:
      KC_BOOTSTRAP_ADMIN_USERNAME: admin
      KC_BOOTSTRAP_ADMIN_PASSWORD: admin
      KC_HTTP_PORT: 8082
      # Publieke hostname — Keycloak gebruikt dit als issuer in tokens
      # en voor het genereren van redirect-URLs.
      KC_HOSTNAME: 192.168.2.43
      KC_HOSTNAME_PORT: 8082
      KC_HOSTNAME_STRICT: false
    ports:
      - "8082:8082"
    volumes:
      - keycloak_data:/opt/keycloak/data
      - ./realm-export.json:/opt/keycloak/data/import/realm-export.json:ro

  blazor:
    image: git.berg-connect.nl/lvdberg/demo:latest
    pull_policy: always
    container_name: blazor
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      # Authority = publieke URL: browser redirects + issuer-validatie van tokens.
      # Moet overeenkomen met KC_HOSTNAME zodat de issuer in tokens klopt.
      - Keycloak__Authority=http://192.168.2.43:8082/realms/homelab
      - Keycloak__ClientId=blazor-web-app
      - Keycloak__ClientSecret=${KEYCLOAK_CLIENT_SECRET}
      - Keycloak__RequireHttpsMetadata=false
      - Logging__LogLevel__Microsoft.AspNetCore.DataProtection=Error
      # Interne Docker URL naar de API-container
      - ApiSettings__BaseUrl=http://api:8080
    volumes:
      # Data Protection keys persistent opslaan zodat cookies
      # container-herstarts overleven
      - dataprotection-keys:/app/keys
    depends_on:
      - keycloak
      - api

  api:
    image: git.berg-connect.nl/lvdberg/demo-api:latest
    pull_policy: always
    container_name: api
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Logging__LogLevel__Microsoft.AspNetCore.DataProtection=Error
      # Authority = publieke URL voor issuer-validatie van inkomende JWT-tokens.
      - Keycloak__Authority=http://192.168.2.43:8082/realms/homelab
      # MetadataAddress = interne Docker URL voor het ophalen van JWKS-sleutels.
      # De server communiceert intern; de browser heeft deze URL niet nodig.
      - Keycloak__MetadataAddress=http://keycloak:8082/realms/homelab/.well-known/openid-configuration
      - Keycloak__ClientId=blazor-web-app
      - Keycloak__RequireHttpsMetadata=false
    depends_on:
      - keycloak

volumes:
  keycloak_data:
  dataprotection-keys:
```

### URL-splitsing uitgelegd

| Service | Instelling | Waarde | Reden |
|---------|------------|--------|-------|
| Keycloak | `KC_HOSTNAME` | `192.168.2.43` | Genereert publieke URLs in tokens en redirects |
| Blazor | `Authority` | `http://192.168.2.43:8082/...` | Issuer in tokens matcht de publieke hostname |
| API | `Authority` | `http://192.168.2.43:8082/...` | Valideert issuer van inkomende JWT-tokens |
| API | `MetadataAddress` | `http://keycloak:8082/...` | Haalt JWKS intern op via Docker-netwerk |

> **Pas `192.168.2.43` aan** naar het IP-adres of de hostname van jouw server. Vervang ook de image-namen naar de juiste registry-paden.
