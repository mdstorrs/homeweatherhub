# Moving Home Weather Hub from Conetix to SmartASP

Goal: move the API (and website) to SmartASP **without losing any readings**. Conetix is never changed by
this process; it stays a complete fallback until you cancel it.

What moves: stations, station settings, users, **all hourly rollups (history back to 2021)**, the last
**7 days** of per-entry readings, and the last 30 days of the log. About 25 MB instead of 1.37 GB.
Afterwards the API keeps 7 days of per-entry readings and 30 days of log (clean-up runs every 6 hours).

Files in this folder:

| File | What it does |
|---|---|
| `01_schema.sql` | Creates the tables in the new, empty SmartASP database (all in `dbo`) |
| `../migrations/001_…`, `002_…`, `003_…` | Procedures: save reading (Brisbane time), hourly rollups, 7-day clean-up |
| `migrate-data.ps1` | Copies the data Conetix → SmartASP and checks it (`-Mode Full`, `Delta`, `Verify`) |
| `move-config.example.json` | Template for `move-config.json` (SmartASP connection string; git-ignored) |
| `run-sql.ps1` | Runs a `.sql` file against SmartASP (`-Database Target`) or Conetix (`-Database Source`) |
| `compare-apis.ps1` | Compares old and new API answers field by field |

---

## A. Before the move (any time, no downtime)

- [ ] **SmartASP database**: create it, note the server name, database name, user and password.
      Check the SQL Server version is 2019 or newer, and that remote connections are allowed (needed for the copy).
- [ ] **SmartASP site for the API**: create it. Note its temporary address (e.g. `xxxx.smartasp.net`)
      and how to publish to it (FTP or Web Deploy).
- [ ] **.NET**: check which ASP.NET Core versions SmartASP supports. .NET 8 support ends 10 Nov 2026,
      so publish .NET 10 if offered (or publish self-contained).
- [ ] **DNS** (checked 2026-10-06): the domain uses `ns1/ns2.homeweatherhub.com`, i.e. **Conetix's DNS**.
      `homeweatherhub.com`, `www` (CNAME) and `api` → `202.74.70.117` (Conetix), TTL **21600 s (6 h)**.
      SmarterASP: main site `mdstorrs-001-site7` (temp URL https://mdstorrs-001-site7.ftempurl.com/),
      API subsite `mdstorrs-001-subsite8`, both on **`208.98.35.67`**.
      **At least 24 h before the switch, lower the TTL** of those records to 300 s in Conetix's DNS (Plesk).
- [ ] **HTTPS on SmarterASP** (the app and website use `https://`; stations post over plain HTTP).
      SmarterASP's free certificate **can't be issued before the switch**: Let's Encrypt checks
      `http://<host>/.well-known/acme-challenge/…`, which DNS still sends to Conetix.
      Instead, reuse Conetix's certificate: a Let's Encrypt **wildcard** for `homeweatherhub.com` + `*.homeweatherhub.com`,
      valid until **8 Dec 2026**.
      1. Plesk → Websites & Domains → homeweatherhub.com → SSL/TLS Certificates → download the certificate (.pem with key).
      2. `openssl pkcs12 -export -in homeweatherhub.pem -inkey homeweatherhub.pem -out homeweatherhub.pfx` (choose a password).
      3. SmarterASP → install your own certificate (PFX) → bind to site7 (`homeweatherhub.com`, `www`) and subsite8 (`api`).
      4. Never commit the .pem/.pfx (they contain the private key); delete local copies after upload.
- [ ] **Website (Website2)**: decide whether it moves to SmartASP too. It only calls the API, so it can move
      before, with, or after the API.
- [ ] **Old logger on Conetix**: something on the Conetix account is still running an old API build that logs
      every request to `WSData` with IP "TEST" (bot traffic). Find it in Plesk and stop it; it isn't moving.

## B. Rehearsal (no downtime; can be repeated)

1. On the **SmartASP** database, run in order:
   `01_schema.sql` → `../migrations/001_sp_WSReportData_DateAdded.sql` → `../migrations/002_hourly_rollup.sql`
   → `../migrations/003_purge_old_data.sql`.
   **Never run 003 on Conetix.**
2. Copy `move-config.example.json` to `move-config.json` and put the SmartASP password in `TargetConnectionString`.
   The Conetix (source) connection string is read from the API's Visual Studio user secrets, so it isn't stored again.
   Run SQL scripts against SmartASP with `.\run-sql.ps1 -Database Target -SqlFile <file>`.
3. Copy the data:
   ```
   .\migrate-data.ps1 -Mode Full
   ```
   It ends with a check table; every row must say PASS. (Re-run with `-Force` to start again.)
4. Publish the API to SmartASP from branch `feature/move-to-smartasp` (**.NET 10**; don't publish this branch to
   Conetix, which only has .NET 8) using the Visual Studio profile **"SmartASP - Web Deploy"**
   (site `mdstorrs-001-subsite8`, folder `HomeWeatherHub/api`; the FTP profile works too).
   - That profile sets `EnvironmentName=SmartASP`, so it publishes only `api/appsettings.Secrets.SmartASP.json`
     (the SmartASP connection string, git-ignored), never the Conetix `appsettings.Secrets.json`.
   - The API refuses to start on SmartASP if that file is missing, so it can't fall back to the Conetix database.
   - Don't put a dot in publish profile names (e.g. "SmarterASP.Net"): the build treats it as a file extension
     and silently ignores the profile's settings.
5. Compare the two APIs. Before DNS changes, ask SmarterASP's server directly by IP with the real host name:
   ```
   .\compare-apis.ps1 -NewUrl "http://208.98.35.67" -NewHostHeader "api.homeweatherhub.com"
   ```
   It must report 0 differences. (SmartASP won't receive station readings yet, so `Current` may be a few
   minutes behind; that's expected until the switch.)

## C. Switch day

1. `.\migrate-data.ps1 -Mode Delta` brings SmartASP up to date with readings since the rehearsal.
2. Run `compare-apis.ps1` again (0 differences).
3. **Switch DNS** (in Conetix's DNS): change the A records for `api.homeweatherhub.com` and `homeweatherhub.com`
   to `208.98.35.67` (`www` follows as a CNAME). Make sure HTTPS is ready (see section A). Stations start posting there within minutes
   (the TTL); some posts may still reach Conetix while DNS spreads. They're saved there and copied in step 5.
4. Check SmartASP is receiving readings: `Current` in the app shows a reading from the last minute,
   and `WSReport` on SmartASP has new rows.
5. **After 24 hours**: `.\migrate-data.ps1 -Mode Delta` copies anything that still reached Conetix and rebuilds
   those hours. The checks must all PASS.
6. Point the website at SmartASP too if it's moving (it already uses `api.homeweatherhub.com`).

No downtime is needed: stations keep posting the whole time; at most, a few readings land on Conetix for a while
and are copied across in step 5.

## D. After the move

- [ ] Watch for a week: readings arriving, History and charts correct, `WSData` free of new errors.
- [ ] After about 7 days, confirm the clean-up ran: the oldest per-entry reading on SmartASP is about 7 days old,
      and `WSData` has no "maintenance" errors.
- [ ] **Before 8 Dec 2026: switch both SmarterASP sites to SmarterASP's free certificate** (it validates once DNS
      points at SmarterASP). The imported Conetix certificate expires then and can't be renewed from the new host.
- [ ] **Move DNS hosting off Conetix before cancelling it.** The domain's DNS servers (`ns1/ns2.homeweatherhub.com`)
      are Conetix's; if Conetix is cancelled first, the domain stops resolving entirely. Move the DNS records to the
      domain registrar or to SmarterASP (change the nameservers at the registrar), and wait 48 h.
- [ ] Keep Conetix running (untouched) for at least 30 days as the fallback, then cancel it when you're happy.
- [ ] Change the old `storrs` password (shared with your other projects) when convenient. Home Weather Hub
      no longer uses it.

## Rollback (if something goes wrong after the switch)

Point `api.homeweatherhub.com` back at Conetix. Nothing there was changed, so it carries on as before.
Readings that SmartASP received in the meantime are still in the SmartASP database; copying them back to
Conetix (and rebuilding those hours with `sp_WSRollupBackfill`) is a one-off job (ask Claude). Nothing is lost either way.
