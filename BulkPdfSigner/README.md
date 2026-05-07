# BulkPDFSigner

Use InnoSetup software to create the installer using the `create_installer.iss` build file.

## API key

The licensing API key is **not** stored in source. It's injected at build time:

- **CI:** set the `LICENSE_API_KEY` GitHub Actions secret. The release workflow passes it through as the `BULK_PDF_SIGNER_API_KEY` env var to `dotnet publish`.
- **Local dev:** set the env var before building, e.g. `export BULK_PDF_SIGNER_API_KEY=...` (PowerShell: `$env:BULK_PDF_SIGNER_API_KEY="..."`).
- **Override per-build:** pass `-p:ApiKey=...` directly to `dotnet build` / `dotnet publish`.

If the key is missing at build time the binary still compiles, but calls to the licensing server return 401 at runtime.
