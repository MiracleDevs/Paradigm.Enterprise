# Aspire deployment publishing

Use the current [Aspire deployment overview](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/overview) to verify target-specific integrations and CLI behavior before changing a publishing topology.

## Choose the target

Require the user to choose Azure Container Apps or Azure App Service and to define the production database topology. Local SQL Server and PostgreSQL containers are development resources; do not silently publish them as production architecture.

After dependency review and approval, add the official target integration and model its environment in the AppHost. Model production secrets as unresolved secret parameters or references to the target secret store. Never publish values from the root `.env` into generated artifacts.

## Generate the handoff

Inspect before executing:

```powershell
aspire publish --list-steps --environment Production
```

Generate disposable artifacts:

```powershell
aspire publish --environment Production --output-path artifacts/aspire
```

Verify that the output contains the expected Bicep and manifest files, has stable resource names, and contains no credentials or developer-only endpoints. Treat generated Bicep as output: configure infrastructure through AppHost provisioning APIs and regenerate instead of hand-editing it.

Do not add a provisioning pipeline unless the user asks for one. When a later pipeline consumes the output, pin the Aspire CLI/toolchain, restore before publish, keep secrets in the pipeline secret store, and validate the generated artifacts before deployment.
