# Keycloak Lokale Installatie & Configuratiegids

> **Omgeving:** Docker Desktop · Keycloak 26.5.5 (dev mode) · Realm: `homelab` · Client: `blazor-web-app`

---

## Inhoudsopgave

1. [Keycloak opstarten via Docker Compose](#1-keycloak-opstarten-via-docker-compose)
2. [Nieuwe realm aanmaken: homelab](#2-nieuwe-realm-aanmaken-homelab)
3. [Client toevoegen: blazor-web-app](#3-client-toevoegen-blazor-web-app)
4. [Client rollen aanmaken: admin & user](#4-client-rollen-aanmaken-admin--user)
5. [Gebruiker aanmaken en rol toewijzen](#5-gebruiker-aanmaken-en-rol-toewijzen)

---

## 1. Keycloak opstarten via Docker Compose

### 1.1 Projectstructuur aanmaken

Maak een nieuwe map aan voor de Keycloak-installatie:

```bash
mkdir keycloak-local
cd keycloak-local
```

### 1.2 `docker-compose.yml` aanmaken

Maak het bestand `docker-compose.yml` aan met de volgende inhoud:

```yaml
services:
  keycloak:
    image: quay.io/keycloak/keycloak:26.5.5
    container_name: keycloak
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_LOG_LEVEL: INFO
    command: start-dev
    # start-dev schakelt automatisch HTTP in en zet hostname-strict uit.
    # KC_HTTP_ENABLED, KC_HOSTNAME_STRICT en KC_HOSTNAME_STRICT_HTTPS zijn
    # overbodig in dev-mode vanaf Keycloak 26 en hoeven niet meer opgegeven te worden.
    ports:
      - "8080:8080"
    volumes:
      - keycloak_data:/opt/keycloak/data
    restart: unless-stopped

volumes:
  keycloak_data:
```

> **Let op:** `start-dev` is uitsluitend bedoeld voor lokale ontwikkeling — het schakelt HTTP in, zet caches uit en vereenvoudigt de hostname-configuratie. Gebruik `start` met expliciete TLS-configuratie voor productie.

### 1.3 Container starten

```bash
docker compose up -d
```

Controleer of de container actief is:

```bash
docker compose ps
```

Verwachte output:

```
NAME        IMAGE                                   STATUS          PORTS
keycloak    quay.io/keycloak/keycloak:26.5.5         Up              0.0.0.0:8080->8080/tcp
```

Bekijk de opstartlogs om te bevestigen dat Keycloak gereed is:

```bash
docker compose logs -f keycloak
```

Wacht op de melding:

```
Keycloak 26.5.5 on JVM (powered by Quarkus ...) started in ...
```

### 1.4 Inloggen op de beheerconsole

Open een browser en ga naar:

```
http://localhost:8080/admin
```

Log in met:

| Veld       | Waarde  |
|------------|---------|
| Gebruiker  | `admin` |
| Wachtwoord | `admin` |

---

## 2. Nieuwe realm aanmaken: homelab

Een realm is een afgeschermde omgeving met eigen gebruikers, rollen en clients. De standaard `master`-realm is uitsluitend voor Keycloak-beheer; maak altijd een aparte realm voor je applicatie.

### Stap 1 — Naar Manage realms navigeren

Na het inloggen op de beheerconsole zie je linksboven in de navigatie **Keycloak** met het label **Current realm**. Daaronder staat het menu-item **Manage realms**.

Klik op **Manage realms** in het linkermenu.

```
┌─────────────────────────────────┐
│  homelab   [Current realm]      │
├─────────────────────────────────┤
│  Manage realms                  │
├─────────────────────────────────┤
│  Manage                         │
│    Clients                      │
│    Client scopes                │
│    Realm roles                  │
│    Users                        │
│    Groups                       │
│    Sessions                     │
└─────────────────────────────────┘
```

> **Let op:** linksboven toont Keycloak de naam van de actieve realm met het label **Current realm** ernaast. Zorg dat hier `homelab` staat voordat je verder gaat — zo weet je zeker dat je in de juiste realm werkt.

### Stap 2 — Nieuwe realm aanmaken

Je ziet het **Manage realms**-overzicht met de bestaande `master`-realm. Klik rechtsboven op de blauwe knop **Create realm**.

| Veld         | Waarde    |
|--------------|-----------|
| Realm name   | `homelab` |
| Enabled      | `ON`      |

Klik op **Create**.

### Stap 3 — Controleren

Na het aanmaken schakel je automatisch over naar de `homelab`-realm. Dit is zichtbaar doordat het label linksboven verandert van **Current realm** naar **homelab**.

Je kunt dit ook verifiëren via **Manage realms** — de lijst toont nu zowel `master` als `homelab`:

```
Name       Display name
──────────────────────────
master     Keycloak
homelab
```

De realm-overzichtspagina toont statistieken (0 gebruikers, 0 clients, etc.).

---

## 3. Client toevoegen: blazor-web-app

Een client vertegenwoordigt de applicatie die gebruik maakt van Keycloak voor authenticatie.

### Stap 1 — Naar Clients navigeren

Klik in het linkermenu onder **Manage** op **Clients**.

De **Clients**-pagina opent met drie tabbladen bovenaan:

```
[ Clients list ]  [ Initial access token ]  [ Client registration ]
```

Je ziet de standaard meegeleverde clients zoals `account`, `account-console` en `admin-cli`.

### Stap 2 — Nieuwe client aanmaken

Klik op de blauwe knop **Create client** (naast de zoekbalk).

Er opent een wizard met drie stappen.

#### Stap 2a — General Settings

| Veld            | Waarde                                    |
|-----------------|-------------------------------------------|
| Client type     | `OpenID Connect`                          |
| Client ID       | `blazor-web-app`                                |
| Name            | `Blazor Web App`                                |
| Description     | `Webapplicatie met Keycloak authenticatie`|

Klik onderaan op **Next**.

#### Stap 2b — Capability Config

| Instelling               | Waarde | Toelichting                                                    |
|--------------------------|--------|----------------------------------------------------------------|
| Client authentication    | `ON`   | Maakt dit een *confidential client* met client secret          |
| Authorization            | `OFF`  | Niet nodig voor standaard rolgebaseerde toegang                |
| Standard flow            | `ON`   | Inlogstroom via browser (Authorization Code Flow)              |
| Direct access grants     | `OFF`  | Uitsluitend inschakelen voor testdoeleinden via curl/Postman   |

> **Client authentication ON** zorgt dat de applicatie zichzelf authenticeert met een client secret bij het ophalen van tokens. Kies dit voor server-side webapplicaties.

Klik onderaan op **Next**.

#### Stap 2c — Login Settings

De URLs in dit scherm verwijzen naar de draaiende webapplicatie — in dit geval de Blazor Web App. Het poortnummer is het poortnummer waarop jouw applicatie lokaal bereikbaar is. Controleer dit in de launchsettings van je project (standaard `5001` voor HTTPS of `5000` voor HTTP in een .NET-project).

| Veld                            | Waarde                         | Toelichting                                      |
|---------------------------------|--------------------------------|--------------------------------------------------|
| Root URL                        | `https://localhost:5001`       | Basis-URL van de Blazor Web App                  |
| Home URL                        | `https://localhost:5001`       | Startpagina na inloggen                          |
| Valid redirect URIs             | `https://localhost:5001/*`     | Toegestane callbacks na succesvolle login        |
| Valid post logout redirect URIs | `https://localhost:5001/`      | Terugkeer-URL na uitloggen                       |
| Web origins                     | `https://localhost:5001`       | Toegestane oorsprong voor CORS-verzoeken         |

> **Poortnummer:** vervang `5001` door het poortnummer van jouw applicatie. Je vindt dit in `Properties/launchSettings.json` onder `applicationUrl`, of in de adresbalk van de browser wanneer je de applicatie lokaal opstart.

> **Valid redirect URIs** bepaalt waarheen Keycloak na een succesvolle login mag doorsturen. De `*` staat alle paden onder je domein toe — beperk dit in productie tot specifieke paden.

Klik onderaan op **Save**.

### Stap 3 — Client secret ophalen

Na het opslaan:

1. Klik op het tabblad **Credentials**
2. Kopieer de waarde bij **Client secret**

```
Client secret:  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

Sla deze op in de configuratie van je applicatie. Afhankelijk van het gebruikte framework:

| Instelling    | Waarde                                          |
|---------------|-------------------------------------------------|
| Authority     | `http://localhost:8080/realms/homelab`          |
| Client ID     | `blazor-web-app`                                      |
| Client Secret | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`          |

### Stap 4 — Client rollen opnemen in het token

Standaard worden client-rollen **niet** meegestuurd als platte claim in het ID-token of access token. Keycloak plaatst ze intern onder `resource_access.<client-id>.roles`. Onderstaande mapper vertaalt deze naar een eenvoudige `roles`-claim die door de meeste frameworks direct te gebruiken is.

#### Navigeren naar de dedicated client scope

1. Ga naar **Clients** in het linkermenu
2. Klik op `blazor-web-app` in de lijst
3. Klik op het tabblad **Client scopes**
4. Klik op de blauwe link **`blazor-web-app-dedicated`** in de tabel

Je komt nu op de pagina **Dedicated scopes** van de client. De breadcrumb bovenaan toont:

```
Clients  >  Client details  >  Dedicated scopes
```

De pagina toont twee tabbladen — **Mappers** en **Scope** — en meldt **No mappers** omdat er nog geen mappers zijn aangemaakt.

> Dit is de *dedicated scope* die exclusief voor deze client geldt. Mappers die je hier toevoegt zijn alleen actief voor `blazor-web-app`.

#### Mapper aanmaken

5. Klik op de knop **Configure a new mapper** (rechtsonder op de lege Mappers-pagina)
6. Er verschijnt een lijst met mapper-types — kies **User Client Role**

Vul het formulier in:

| Veld                           | Waarde           | Toelichting                                                         |
|--------------------------------|------------------|---------------------------------------------------------------------|
| Mapper type                    | `User Client Role` | Wordt automatisch ingevuld na de vorige stap                      |
| Name                           | `client-roles`   | Naam ter herkenning in de Keycloak-console                          |
| Client ID                      | `blazor-web-app` | Alleen rollen van déze client worden meegestuurd                    |
| Client Role prefix             | *(leeg laten)*   | Optioneel voorvoegsel voor rolnamen — niet nodig voor standaard gebruik |
| Multivalued                    | `ON`             | Meerdere rollen per gebruiker mogelijk                              |
| Token Claim Name               | `roles`          | Naam van de claim in het token — gebruik deze naam in je applicatie |
| Claim JSON Type                | `String`         | Elke rol wordt als losse string-waarde meegestuurd                  |
| Add to ID token                | `ON`             | Applicatie leest rollen uit het ID token                            |
| Add to access token            | `ON`             | Vereist voor API-aanroepen met Bearer token                         |
| Add to lightweight access token | `OFF`           | Niet nodig voor standaard gebruik                                   |
| Add to userinfo                | `ON`             | Rollen beschikbaar via het UserInfo-endpoint                        |
| Add to token introspection     | `ON`             | Rollen zichtbaar bij token-validatie via het introspection-endpoint |

Klik op **Save**.

Na het opslaan verschijnt de mapper `client-roles` in de lijst onder het tabblad **Mappers**.

#### Aansluiting op je applicatie

Gebruik in je applicatie de claim-naam `roles` voor rolcontrole. De exacte configuratie hangt af van het framework, maar de claim-naam in het token is altijd `roles`.

---

## 4. Client rollen aanmaken: admin & user

Client rollen zijn rollen die specifiek gelden voor één client (`blazor-web-app`). Ze zijn gescheiden van realm-rollen en beter geschikt voor applicatiespecifieke autorisatie.

### Stap 1 — Naar client rollen navigeren

1. Klik in het linkermenu op **Clients**
2. Klik op `blazor-web-app`
3. Klik op het tabblad **Roles**

### Stap 2 — Rol `admin` aanmaken

Klik op **Create role**.

| Veld        | Waarde                                                              |
|-------------|---------------------------------------------------------------------|
| Role name   | `admin`                                                             |
| Description | `Volledige beheertoegang tot de applicatie. Toegang tot alle pagina's inclusief gebruikersbeheer, systeeminstellingen en audit-logs. Uitsluitend voor IT-beheerders en applicatiebeheerders.` |

Klik op **Save**.

### Stap 3 — Rol `user` aanmaken

Klik op **Create role** (of klik op de `←`-knop om terug te gaan naar het rollenlijstje).

| Veld        | Waarde                                                              |
|-------------|---------------------------------------------------------------------|
| Role name   | `user`                                                              |
| Description | `Standaard gebruikersrol voor alle geregistreerde medewerkers en eindgebruikers. Geeft toegang tot het dashboard en het gedeeld portaal. Dit is de basisrol die automatisch wordt toegewezen bij registratie.` |

Klik op **Save**.

### Overzicht rollen

Na aanmaken zie je het volgende overzicht onder **Clients → blazor-web-app → Roles**:

| Rolnaam | Beschrijving                          |
|---------|---------------------------------------|
| `admin` | Volledige beheertoegang               |
| `user`  | Standaard gebruikersrol               |

---

## 5. Gebruiker aanmaken en rol toewijzen

### Stap 1 — Naar Gebruikers navigeren

Klik in het linkermenu op **Users**.

### Stap 2 — Nieuwe gebruiker aanmaken

Klik op **Create new user**.

#### Tabblad: Required user actions

Laat leeg voor een direct actieve account.

#### Formulier invullen

| Veld           | Waarde           |
|----------------|------------------|
| Email verified | `ON`             |
| Username       | `jan.de.vries`   |
| Email          | `jan@homelab.nl` |
| First name     | `Jan`            |
| Last name      | `de Vries`       |

Klik op **Create**.

### Stap 3 — Wachtwoord instellen

1. Klik op het tabblad **Credentials**
2. Klik op **Set password**
3. Vul een wachtwoord in
4. Zet **Temporary** op `OFF` (zodat de gebruiker niet hoeft te wijzigen bij eerste login)
5. Klik op **Save** → bevestig met **Save password**

### Stap 4 — Client rol toewijzen

1. Klik op het tabblad **Role mapping**

   Je ziet de huidige rollen van de gebruiker. De knop **Assign role** heeft een pijltje waarmee je kunt kiezen tussen **Client roles** en **Realm roles**:

   ```
   [ Assign role ▼ ]
        ├── Client roles
        └── Realm roles
   ```

2. Klik op de pijl naast **Assign role** en kies **Client roles**

   Er opent een venster **Assign Client roles to [gebruikersnaam]** met een lijst van alle beschikbare client rollen. De lijst toont rollen van alle clients, gegroepeerd op **Client ID**:

   ```
   Name                  Client ID          Description
   ─────────────────────────────────────────────────────────
   delete-account        account            role_delete-account
   manage-account        account            role_manage-account
   ...
   admin                 blazor-web-app     Volledige beheertoegang...
   user                  blazor-web-app     Standaard gebruikersrol...
   ```

3. Scroll naar beneden tot de rollen met **Client ID** `blazor-web-app` en vink de gewenste rol aan:
   - `user` voor een standaard gebruiker
   - `admin` voor een beheerder

4. Klik op **Assign**

**Verificatie:** na het toewijzen zie je de rol in het overzicht onder Role mapping:

```
Name                        Inherited    Description
admin (blazor-web-app)      False
```

### Stap 5 — Werking van de rol mapper controleren

Nu de gebruiker een rol heeft, kun je via de Evaluate-tool controleren of de rol correct in het token terechtkomt.

1. Ga naar **Clients** → `blazor-web-app`
2. Klik op het tabblad **Client scopes**
3. Klik op het subtabblad **Evaluate** (naast **Setup**)
4. Typ bij **Users** de gebruikersnaam (bijv. `jan.de.vries`) of selecteer deze uit de dropdown
5. Laat **Target audience** leeg

Aan de rechterkant verschijnen vier links:

```
Effective protocol mappers
Effective role scope mappings
Generated access token
Generated ID token          ← klik hier
Generated user info
```

6. Klik op **Generated ID token**

Het ID token wordt getoond als gedecodeerde JSON. Controleer of de `roles`-claim aanwezig is en de juiste waarden bevat:

```json
{
  "iss": "http://192.168.x.x:8080/realms/homelab",
  "aud": "blazor-web-app",
  "typ": "ID",
  "email_verified": true,
  "roles": [
    "user",
    "manage-account",
    "manage-account-links",
    "view-profile"
  ],
  "name": "Jan de Vries",
  "preferred_username": "jan.de.vries",
  "given_name": "Jan",
  "family_name": "de Vries"
}
```

> De claim bevat naast de eigen rol (`user`) ook standaard account-rollen zoals `manage-account` en `view-profile`. Dit is normaal gedrag van Keycloak.

> Als de `roles`-claim ontbreekt, controleer dan of **Client ID** in de mapper exact `blazor-web-app` is (hoofdlettergevoelig) en of **Multivalued** op `ON` staat. Zie [sectie 3 stap 4](#3-client-toevoegen-blazor-web-app).

---


## Bijlage A — Samenvatting configuratie

| Onderdeel              | Waarde                                |
|------------------------|---------------------------------------|
| Keycloak URL           | `http://localhost:8080`               |
| Realm                  | `homelab`                             |
| Client ID              | `blazor-web-app`                            |
| Client type            | Confidential (OpenID Connect)         |
| Rollen                 | `admin`, `user`                       |
| Token claim voor rollen| `roles`                               |
| Registratie            | Ingeschakeld                          |
| Standaard rol          | `user` (blazor-web-app)                     |

### Handige URLs

| Doel                        | URL                                                                 |
|-----------------------------|---------------------------------------------------------------------|
| Beheerconsole               | `http://localhost:8080/admin`                                       |
| Gebruikersaccount portaal   | `http://localhost:8080/realms/homelab/account`                      |
| OIDC discovery endpoint     | `http://localhost:8080/realms/homelab/.well-known/openid-configuration` |
| Loginpagina                 | `http://localhost:8080/realms/homelab/protocol/openid-connect/auth` |

---

## Bijlage B — Container beheren

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

# Keycloak updaten naar nieuwe versie
docker compose pull
docker compose up -d
```

---