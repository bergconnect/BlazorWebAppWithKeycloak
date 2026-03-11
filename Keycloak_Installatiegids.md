# Keycloak Lokale Installatie & Configuratiegids

> **Omgeving:** Docker Desktop · Keycloak 26.5.5 (dev mode) · Realm: `homelab` · Client: `blazor-web-app`

---

## Inhoudsopgave

1. [Keycloak opstarten via Docker Compose](#1-keycloak-opstarten-via-docker-compose)
2. [Nieuwe realm aanmaken: homelab](#2-nieuwe-realm-aanmaken-homelab)
3. [Client toevoegen: blazor-web-app](#3-client-toevoegen-blazor-web-app)
4. [Client rollen aanmaken: admin & user](#4-client-rollen-aanmaken-admin--user)
5. [Gebruiker aanmaken en rol toewijzen](#5-gebruiker-aanmaken-en-rol-toewijzen)
6. [Zelfregistratie inschakelen](#6-zelfregistratie-inschakelen)
7. [Standaard rol toewijzen aan nieuwe gebruikers](#7-standaard-rol-toewijzen-aan-nieuwe-gebruikers)
8. [Rollen testen en verifiëren](#8-rollen-testen-en-verifiëren)

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

### Stap 1 — Realmkeuzemenu openen

Klik linksboven in de navigatiebalk op de naam van de huidige realm (`master`). Er verschijnt een dropdown.

```
┌─────────────────────────┐
│  master              ▼  │
├─────────────────────────┤
│  + Create realm         │
└─────────────────────────┘
```

### Stap 2 — Nieuwe realm aanmaken

Klik op **Create realm**.

Vul het formulier in:

| Veld        | Waarde    |
|-------------|-----------|
| Realm name  | `homelab` |
| Enabled     | `ON`      |

Klik op **Create**.

### Stap 3 — Controleren

Na aanmaken schakel je automatisch over naar de `homelab`-realm. Dit zie je linksboven:

```
┌─────────────────────────┐
│  homelab             ▼  │
└─────────────────────────┘
```

De realm-overzichtspagina toont statistieken (0 gebruikers, 0 clients, etc.).

---

## 3. Client toevoegen: blazor-web-app

Een client vertegenwoordigt de Blazor-applicatie die gebruik maakt van Keycloak voor authenticatie.

### Stap 1 — Naar Clients navigeren

Klik in het linkermenu op **Clients**.

### Stap 2 — Nieuwe client aanmaken

Klik op **Create client** (rechtsboven).

#### Pagina 1 — General Settings

| Veld            | Waarde              |
|-----------------|---------------------|
| Client type     | `OpenID Connect`    |
| Client ID       | `blazor-web-app`    |
| Name            | `Blazor Web App`    |
| Description     | `Blazor Server applicatie met Keycloak authenticatie` |

Klik op **Next**.

#### Pagina 2 — Capability Config

| Instelling               | Waarde |
|--------------------------|--------|
| Client authentication    | `ON`   |
| Authorization            | `OFF`  |
| Standard flow            | `ON`   |
| Direct access grants     | `OFF`  |

> **Client authentication ON** maakt dit een *confidential client* — de applicatie authenticeert zichzelf met een geheim bij het ophalen van tokens. Dit is vereist voor server-side Blazor.

Klik op **Next**.

#### Pagina 3 — Login Settings

| Veld                        | Waarde                        |
|-----------------------------|-------------------------------|
| Root URL                    | `https://localhost:5001`      |
| Home URL                    | `https://localhost:5001`      |
| Valid redirect URIs         | `https://localhost:5001/*`    |
| Valid post logout redirect URIs | `https://localhost:5001/` |
| Web origins                 | `https://localhost:5001`      |

Klik op **Save**.

### Stap 3 — Client secret ophalen

Na het opslaan:

1. Klik op het tabblad **Credentials**
2. Kopieer de waarde bij **Client secret**

```
Client secret:  xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

Sla deze op in `appsettings.json` van de Blazor-applicatie:

```json
"Keycloak": {
  "Authority":     "http://localhost:8080/realms/homelab",
  "ClientId":      "blazor-web-app",
  "ClientSecret":  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

### Stap 4 — Client rollen opnemen in het token

Standaard worden client-rollen **niet** meegestuurd in het ID-token of access token. Keycloak plaatst ze in het token onder `resource_access.<client-id>.roles`, maar Blazor verwacht ze als een platte `roles`-claim. Onderstaande mapper zorgt voor die vertaling.

#### Navigeren naar de dedicated client scope

1. Ga naar **Clients** → `blazor-web-app`
2. Klik op het tabblad **Client scopes**
3. Klik op de blauwe link **`blazor-web-app-dedicated`**

> Dit is de *dedicated scope* die exclusief voor deze client geldt. Mappers die je hier toevoegt zijn alleen actief voor `blazor-web-app`.

#### Mapper aanmaken

4. Klik op **Add mapper** → **By configuration**
5. Kies in de lijst **User Client Role**

Vul de mapper in:

| Veld                     | Waarde            | Toelichting                                              |
|--------------------------|-------------------|----------------------------------------------------------|
| Name                     | `client-roles`    | Naam ter herkenning in de Keycloak-console               |
| Client ID                | `blazor-web-app`  | Alleen rollen van déze client worden meegestuurd         |
| Token Claim Name         | `roles`           | Naam van de claim in het token — moet overeenkomen met `RoleClaimType` in Blazor |
| Claim JSON Type          | `String`          | Elke rol wordt als losse string-waarde meegestuurd       |
| Add to ID token          | `ON`              | Blazor leest rollen uit het ID token                     |
| Add to access token      | `ON`              | Vereist voor API-aanroepen met Bearer token              |
| Add to userinfo          | `ON`              | Rollen beschikbaar via het UserInfo-endpoint             |
| Multivalued              | `ON`              | Meerdere rollen per gebruiker mogelijk                   |

Klik op **Save**.

#### Werking controleren

Na het opslaan kun je direct verifiëren of de mapper correct werkt:

1. Klik op het tabblad **Client scopes** van `blazor-web-app`
2. Klik rechtsboven op **Evaluate**
3. Kies een testgebruiker en klik op **Generated ID token**
4. Zoek in de JSON-output naar de `roles`-claim:

```json
{
  "preferred_username": "jan.de.vries",
  "roles": [
    "user"
  ]
}
```

> Als de `roles`-claim ontbreekt, controleer dan of **Client ID** exact `blazor-web-app` is (hoofdlettergevoelig) en of **Multivalued** op `ON` staat.

#### Aansluiting op Blazor

Zorg dat `Program.cs` de claim-naam en rol-type correct configureert:

```csharp
options.ClaimActions.MapJsonKey("roles", "roles");

options.TokenValidationParameters = new()
{
    NameClaimType = "preferred_username",
    RoleClaimType = "roles"   // moet overeenkomen met Token Claim Name
};
```

Klik op **Save**.

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
2. Klik op **Assign role**
3. Klik linksboven op de filter-dropdown en kies **Filter by clients**
4. Zoek op `blazor-web-app`
5. Vink de gewenste rol aan — bijvoorbeeld `admin` voor een beheerder, of `user` voor een standaardgebruiker
6. Klik op **Assign**

**Verificatie:** onder het tabblad Role mapping zie je nu de toegewezen rol:

```
Role                          Source
admin (blazor-web-app)        blazor-web-app
```

---

## 6. Zelfregistratie inschakelen

Zelfregistratie laat nieuwe gebruikers zichzelf aanmelden via een registratiepagina, zonder tussenkomst van een beheerder.

### Stap 1 — Realm Settings openen

Klik in het linkermenu op **Realm settings**.

### Stap 2 — Login-tab openen

Klik op het tabblad **Login**.

### Stap 3 — Registratie inschakelen

Zet de volgende instellingen:

| Instelling               | Waarde | Toelichting                                         |
|--------------------------|--------|-----------------------------------------------------|
| User registration        | `ON`   | Toont de "Registreren"-link op de loginpagina       |
| Email as username        | `ON`   | Gebruikers loggen in met e-mailadres                |
| Forgot password          | `ON`   | Wachtwoord herstellen via e-mail                    |
| Remember me              | `ON`   | "Onthoud mij"-optie op de loginpagina               |
| Verify email             | `ON`   | Gebruiker moet e-mailadres bevestigen (aanbevolen)  |

Klik op **Save**.

### Stap 4 — Registratiepagina testen

De registratiepagina is nu bereikbaar via:

```
http://localhost:8080/realms/homelab/protocol/openid-connect/registrations
    ?client_id=blazor-web-app
    &response_type=code
    &redirect_uri=https://localhost:5001/
```

Of via de normale loginpagina — onderaan staat nu de link **Register**.

---

## 7. Standaard rol toewijzen aan nieuwe gebruikers

Nieuwe gebruikers die zich registreren krijgen standaard géén rollen. Met de onderstaande configuratie krijgt elke nieuwe gebruiker automatisch de `user`-rol van de `blazor-web-app`-client.

### Stap 1 — Default roles configureren

1. Klik in het linkermenu op **Realm settings**
2. Klik op het tabblad **User registration**

### Stap 2 — Default rol toewijzen

1. Klik op **Assign role** (of **Add roles** — afhankelijk van Keycloak versie)
2. Verander de filter linksboven naar **Filter by clients**
3. Zoek op `blazor-web-app`
4. Vink `user` aan
5. Klik op **Assign**

Het overzicht toont nu:

```
Default roles
─────────────────────────────────────
user (blazor-web-app)
```

### Stap 3 — Werking verifiëren

1. Open een incognitovenster
2. Ga naar `http://localhost:8080/realms/homelab/account`
3. Klik op **Register** en maak een testgebruiker aan
4. Ga terug naar de Keycloak Admin Console → **Users** → selecteer de nieuwe gebruiker
5. Klik op **Role mapping** — de rol `user (blazor-web-app)` staat er automatisch bij

---

## 8. Rollen testen en verifiëren

Er zijn vier manieren om te controleren of een gebruiker de juiste rollen heeft: via de Keycloak-beheerconsole, via de ingebouwde Evaluate-tool, via een raw JWT-token, en via de Blazor-applicatie zelf.

---

### 8.1 Verificatie in de Keycloak Admin Console

Dit is de snelste controle — direct in de beheerconsole.

1. Ga naar `http://localhost:8080/admin` en zorg dat je in de **homelab**-realm zit
2. Klik in het linkermenu op **Users**
3. Klik op de gebruiker die je wilt controleren
4. Klik op het tabblad **Role mapping**

Je ziet twee secties:

```
Assigned roles
──────────────────────────────────────────────
user (blazor-web-app)        blazor-web-app

Effective roles
──────────────────────────────────────────────
user (blazor-web-app)        blazor-web-app
```

> **Assigned roles** = direct toegewezen rollen.
> **Effective roles** = alle rollen inclusief geërfde rollen via Composite Roles.

---

### 8.2 Token inspecteren via de Evaluate-tool

Keycloak heeft een ingebouwde tool waarmee je precies kunt zien welk token een gebruiker ontvangt, inclusief alle claims en rollen — zonder dat je hoeft in te loggen als die gebruiker.

1. Ga naar **Clients** → `blazor-web-app`
2. Klik op het tabblad **Client scopes**
3. Klik op de knop **Evaluate** (rechtsboven)
4. Vul in bij **User**: de gebruikersnaam die je wilt testen (bijv. `jan.de.vries`)
5. Klik op **Generated access token**

Er verschijnt een volledig gedecodeerd access token. Zoek in de JSON-output naar de `roles`-claim:

```json
{
  "sub": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "preferred_username": "jan.de.vries",
  "email": "jan@homelab.nl",
  "roles": [
    "user"
  ]
}
```

Voor een admin-gebruiker zie je:

```json
{
  "preferred_username": "admin.gebruiker",
  "roles": [
    "admin",
    "user"
  ]
}
```

Klik ook op **Generated ID token** om het ID-token apart te inspecteren — dit is het token dat Blazor gebruikt voor de claimsidentiteit.

> Als de `roles`-claim ontbreekt, is de token mapper niet correct ingesteld. Zie [Stap 4 van sectie 3](#stap-4--client-rollen-opnemen-in-het-token).

---

### 8.3 Token decoderen via curl en jwt.io

Je kunt een token ook buiten de browser opvragen en handmatig decoderen.

#### Stap 1 — Direct access grants tijdelijk inschakelen

Om via `curl` een token op te vragen moet **Direct access grants** aan staan op de client. Schakel dit tijdelijk in voor testdoeleinden:

1. Ga naar **Clients** → `blazor-web-app` → tabblad **Settings**
2. Zet **Direct access grants** op `ON`
3. Klik op **Save**

> Vergeet niet dit na het testen weer uit te zetten.

#### Stap 2 — Token ophalen via curl

```bash
curl -s -X POST \
  http://localhost:8080/realms/homelab/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=blazor-web-app" \
  -d "client_secret=JOUW-CLIENT-SECRET" \
  -d "username=jan.de.vries" \
  -d "password=JOUW-WACHTWOORD" \
  -d "grant_type=password" \
  | jq .
```

De respons ziet er zo uit:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "id_token":     "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type":   "Bearer",
  "expires_in":   300
}
```

#### Stap 3 — Token decoderen

**Optie A — Visueel via jwt.io:**

1. Ga naar [https://jwt.io](https://jwt.io)
2. Plak de waarde van `access_token` of `id_token` in het linkerveld
3. Rechts verschijnt de gedecodeerde payload — zoek naar de `roles`-claim:

```json
{
  "iss": "http://localhost:8080/realms/homelab",
  "preferred_username": "jan.de.vries",
  "roles": ["user"]
}
```

**Optie B — Direct in de terminal met curl en jq:**

```bash
# Sla het token op in een variabele
TOKEN=$(curl -s -X POST \
  http://localhost:8080/realms/homelab/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=blazor-web-app" \
  -d "client_secret=JOUW-CLIENT-SECRET" \
  -d "username=jan.de.vries" \
  -d "password=JOUW-WACHTWOORD" \
  -d "grant_type=password" \
  | jq -r .access_token)

# Decodeer de payload (middelste deel van het JWT)
echo $TOKEN | cut -d'.' -f2 | base64 --decode 2>/dev/null | jq .
```

Verwachte output voor een gebruiker met de `user`-rol:

```json
{
  "iss": "http://localhost:8080/realms/homelab",
  "preferred_username": "jan.de.vries",
  "email": "jan@homelab.nl",
  "roles": [
    "user"
  ]
}
```

Alleen de `roles`-claim tonen:

```bash
echo $TOKEN | cut -d'.' -f2 | base64 --decode 2>/dev/null | jq '.roles'
```

Output:

```json
[
  "user"
]
```

> **Tip voor macOS:** gebruik `base64 -D` in plaats van `base64 --decode`.

---

### 8.4 Rollen testen in de Blazor-applicatie

Als de Blazor-applicatie al draait, kun je rollen end-to-end testen. Het `/dashboard` uit het eerder opgeleverde project toont al alle claims inclusief de `roles`-waarden.

Voor een gerichte rolcontrole kun je een eenvoudige testpagina toevoegen:

```razor
@page "/rol-test"
@attribute [Authorize]
@inject BlazorKeycloak.Services.UserInfoService UserInfo

<h3>Rolcontrole</h3>

<p>Ingelogd als: <strong>@UserInfo.UserName</strong></p>

<table class="table table-bordered w-auto">
    <thead class="table-dark">
        <tr><th>Rol</th><th>Toegang</th></tr>
    </thead>
    <tbody>
        <tr>
            <td><code>admin</code></td>
            <td>@(UserInfo.IsInRole("admin") ? "✅ Ja" : "❌ Nee")</td>
        </tr>
        <tr>
            <td><code>user</code></td>
            <td>@(UserInfo.IsInRole("user") ? "✅ Ja" : "❌ Nee")</td>
        </tr>
    </tbody>
</table>

<h4 class="mt-4">Alle rollen uit token:</h4>
<ul>
    @foreach (var rol in UserInfo.Roles)
    {
        <li><code>@rol</code></li>
    }
</ul>
```

Voeg de pagina toe aan de navigatie in `MainLayout.razor`:

```razor
<AuthorizeView>
    <Authorized>
        <li class="nav-item">
            <a class="nav-link" href="/rol-test">🧪 Roltest</a>
        </li>
    </Authorized>
</AuthorizeView>
```

---

### 8.5 Overzicht testmethoden

| Methode | Wat het toont | Geschikt voor |
|---------|---------------|---------------|
| Admin Console → Role mapping | Toegewezen en effectieve rollen | Snelle beheerderscheck |
| Evaluate-tool in Keycloak | Exacte token-inhoud zonder inloggen | Debuggen van token mapper |
| jwt.io | Visueel gedecodeerde token payload | Handmatige token-inspectie |
| `curl` + `jq` | Token en claims via de commandoregel | Scripting en automatisering |
| Blazor `/rol-test` | End-to-end rolcontrole in de app | Verificatie vanuit de applicatie |

---

## Bijlage A — Samenvatting configuratie

| Onderdeel              | Waarde                                |
|------------------------|---------------------------------------|
| Keycloak URL           | `http://localhost:8080`               |
| Realm                  | `homelab`                             |
| Client ID              | `blazor-web-app`                      |
| Client type            | Confidential (OpenID Connect)         |
| Rollen                 | `admin`, `user`                       |
| Token claim voor rollen| `roles`                               |
| Registratie            | Ingeschakeld                          |
| Standaard rol          | `user` (blazor-web-app)               |

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

## Bijlage C — Veelvoorkomende problemen

| Probleem | Oorzaak | Oplossing |
|----------|---------|-----------|
| Loginpagina niet bereikbaar | Container nog niet opgestart | Wacht 30-60 seconden, controleer logs |
| `Invalid redirect_uri` | Redirect URI niet toegevoegd aan client | Voeg `https://localhost:5001/*` toe bij Valid redirect URIs |
| Rollen komen niet aan in Blazor | Token mapper ontbreekt of verkeerd geconfigureerd | Zie [Stap 4 van sectie 3](#stap-4--client-rollen-opnemen-in-het-token) |
| Nieuwe gebruiker heeft geen rol | Default role niet ingesteld | Zie [sectie 7](#7-standaard-rol-toewijzen-aan-nieuwe-gebruikers) |
| Client secret verlopen | Secret geregenereerd in Keycloak | Kopieer nieuw secret via Clients → Credentials |
