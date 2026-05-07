# BulkPDFSigner

A Windows desktop app that signs PDFs in bulk using a USB-token X.509 certificate (DSC). Built for Indian telecom-circle workflows — DOT and SACFA approvals — where signed PDFs are submitted to government portals and must be cryptographically signed by an authorised user's hardware-backed cert.

## At a glance

- **Windows app** — .NET 8 WinForms, single self-contained `.exe`, distributed as both **MSI** (per-user install with Start Menu shortcut, no admin required) and **EXE** (used by the in-app auto-updater).
- **Licensing API** — Python Flask service on **Google Cloud Run** (`asia-south1`), backed by a Google Sheet for license records. Native service-account auth via Application Default Credentials — no JSON keys in source, secrets, or runtime.
- **Auto-update** — clients check GitHub Releases on launch and self-replace when a newer version is published. New builds reach all installed clients within a day.

## Architecture

```
┌─────────────────────────────────────────┐
│           Windows Client                │
│  ┌─────────────────────────────────┐    │
│  │  Form1 (UI)                     │    │
│  │   ├── LicenseClient   (HTTP)    │    │     1.  Cert select + license check
│  │   ├── PdfSigningService (iText) │    │     2.  iText signing
│  │   ├── UpdateService   (GitHub)  │    │     3.  Self-update from Releases
│  │   └── AppLogger       (file)    │    │
│  └─────────────────────────────────┘    │
│  Cache: %LOCALAPPDATA%\BulkPdfSigner\   │
│   ├── license.json   (24 h TTL)         │
│   └── log.txt                           │
└─────────────────────────────────────────┘
                  │  HTTPS + X-API-KEY
                  ▼
┌─────────────────────────────────────────┐
│   Cloud Run: bulk-pdf-signer-license-   │
│              provider (asia-south1)     │
│   Flask + gspread + google-auth (ADC)   │
│   Identity: bulk-pdf-signer-api SA      │
└─────────────────────────────────────────┘
                  │  Google Sheets API
                  ▼
┌─────────────────────────────────────────┐
│   Google Sheet: "Licenses" + "Log" tabs │
│   Spreadsheet ID:                       │
│     1FKnY8mhgBd8cbHmAORP0BjeiwxSLnMF1zPEnCW2H_a4 │
└─────────────────────────────────────────┘

  GitHub Actions  ──[OIDC + WIF]──▶  GCP   (no JSON key in repo secrets)
```

## Repository layout

| Path | What it is |
|---|---|
| [BulkPdfSigner/](BulkPdfSigner/) | Windows app source (.NET 8, WinForms) |
| [BulkPdfSigner_Tests/](BulkPdfSigner_Tests/) | xUnit tests for the signing service |
| [installer/](installer/) | WiX 5 SDK-style project that produces the MSI |
| [license_api/](license_api/) | Flask app + Dockerfile for Cloud Run |
| [.github/workflows/release.yml](.github/workflows/release.yml) | Builds + releases the Windows MSI/EXE on `v*.*.*` tags |
| [.github/workflows/deploy-license-api.yml](.github/workflows/deploy-license-api.yml) | Auto-deploys the licensing API to Cloud Run on push to `main` |

## How licensing works

1. On launch the Windows client opens the Windows certificate store and asks the user to pick a cert.
2. It extracts the cert's `SERIALNUMBER=` DN component (USB-token serial); if absent, falls back to the CN.
3. **Cache hit (< 24 h) →** activate silently and start polling.
4. **Cache miss →** show "this can take up to 30 seconds" warning (Render legacy; Cloud Run is now ~130 ms cold start), then `GET /license?serialnum=<serial>`.
5. **404 →** create a 2-day Trial via `POST /license`.
6. Background poll every 1 h. On revocation (404) wipes cache, switches to Trial. On expiry, wipes cache, notifies user.

License types: `ALL` (365 d), `BULKPDF` (180 d), `SACFA` (90 d), `TRIAL` (2 d, ≤ 5 PDFs/batch).

## How auto-update works

1. After license activation the client GETs `https://api.github.com/repos/phanitejak/BulkPDFSigner/releases/latest`.
2. If the tag's version > `Application.ProductVersion`, prompts the user with release notes.
3. On accept: downloads the `.exe` asset, verifies SHA256 against the `.exe.sha256` sidecar.
4. Self-replaces using NTFS rename: current `.exe` → `.exe.old`, new `.exe` into place, spawns it, exits.
5. Next launch sweeps any leftover `.exe.old`.

## Local development

### Windows app

Requires .NET 8 SDK with the Windows Desktop pack. On Linux you can build but not run.

```bash
# build
dotnet build BulkPdfSigner/BulkPdfSigner.csproj -c Release -p:EnableWindowsTargeting=true

# build a self-contained single-file exe (Linux or Windows)
BULK_PDF_SIGNER_API_KEY=<key> dotnet publish BulkPdfSigner/BulkPdfSigner.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableWindowsTargeting=true
```

The API key is injected as an `[AssemblyMetadata]` attribute at build time — set `BULK_PDF_SIGNER_API_KEY` in the env or pass `-p:ApiKey=...`. Without it, the binary still compiles but runtime calls fail with HTTP 401.

### Licensing API

```bash
cd license_api/
pip install -r requirements.txt

# Local: use your own gcloud credentials
gcloud auth application-default login

# Run
python app.py            # Flask dev server
# or
gunicorn --bind 0.0.0.0:8080 app:app
```

`app.py` is dual-mode: prefers `SERVICE_JSON` env var when present (legacy Render path), otherwise uses Application Default Credentials. Cloud Run uses ADC.

## Releasing

### Windows app

```bash
git tag -a vMAJOR.MINOR.PATCH -m "vX.Y.Z - <user-facing summary>"
git push origin vX.Y.Z
```

[release.yml](.github/workflows/release.yml) builds tests, compiles a self-contained single-file exe, builds the MSI via WiX, attaches both with SHA256 sidecars to a GitHub Release. Existing v1.1.0+ clients pick up the update on next launch.

### Licensing API

Push to `main` with changes under `license_api/**`. [deploy-license-api.yml](.github/workflows/deploy-license-api.yml) builds the container and deploys to Cloud Run. CI smoke-tests liveness, auth, and Sheets reach before considering the deploy successful.

## Operations

### Where things live

| Resource | Location |
|---|---|
| Cloud Run service | `mystical-axiom-466007-i4` / `asia-south1` / `bulk-pdf-signer-license-provider` |
| Service URL | `https://bulk-pdf-signer-license-provider-496807224907.asia-south1.run.app` |
| Runtime SA | `bulk-pdf-signer-api@mystical-axiom-466007-i4.iam.gserviceaccount.com` |
| WIF pool | `projects/496807224907/locations/global/workloadIdentityPools/github-pool` |
| Licensing Sheet | [docs.google.com/spreadsheets/d/1FKnY8mhgBd8cbHmAORP0BjeiwxSLnMF1zPEnCW2H_a4](https://docs.google.com/spreadsheets/d/1FKnY8mhgBd8cbHmAORP0BjeiwxSLnMF1zPEnCW2H_a4) — `Licenses` + `Log` tabs |
| GitHub Actions secret | `LICENSE_API_KEY` (shared between client builds and the Cloud Run env var) |
| Client log file | `%LOCALAPPDATA%\BulkPdfSigner\log.txt` (per user) |
| Client license cache | `%LOCALAPPDATA%\BulkPdfSigner\license.json` (per user) |

### Rotating the API key

The API key is a shared secret between the client (embedded at build time) and the Cloud Run env var. To rotate:

1. Generate a new key: `python3 -c "import secrets; print('api_kpteja_' + secrets.token_hex(16))"` (do not paste it into chat).
2. Update GCP: `gcloud run services update bulk-pdf-signer-license-provider --region=asia-south1 --set-env-vars API_KEY=<new>`
3. Update GitHub: repo Settings → Secrets and variables → Actions → `LICENSE_API_KEY`.
4. Tag a new release. Existing clients on the *old* key will get HTTP 401 until they auto-update.

### Rotating the runtime service account

Because there is no SA JSON key anywhere, "rotation" means deleting the SA and creating a new one with the same display name. Cloud Run can be redeployed pointing at the new SA's email; the licensing Sheet needs to be re-shared with the new email.

## Security posture

- **No service-account JSON keys** anywhere — repo, secrets, runtime, or developer machines for production access. Cloud Run uses ADC; GitHub Actions uses Workload Identity Federation.
- **Build-time API key injection** — the licensing API key is never in source, only in the GitHub Actions secret + Cloud Run env var. Compiled binaries embed it via `[AssemblyMetadata]`; local builds without the env var still compile but fail at runtime auth.
- **`.gitignore` blocks** common credential filename patterns (service-account JSONs, p12, pfx, project-prefixed downloads, `secrets/`, `.env`).
- **Recommended GitHub setting:** enable Repo Settings → Code security → **Secret scanning push protection** so the next leaked credential is blocked at push time.

## Contact

Author: Phaniteja K — kondapalliphaniteja@gmail.com
