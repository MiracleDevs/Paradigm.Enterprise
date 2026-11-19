# Publish NuGet packages

The [publish workflow](../../.github/workflows/publish.yml) replaces the local API-key-based publisher. It builds, tests, and packs the solution when a `v<version>` tag is pushed. A separate job publishes those packages with [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), using GitHub OIDC to obtain a temporary API key immediately before the push.

## One-time setup

1. In the repository's GitHub **Settings > Environments**, create an environment named `release`. Restrict its deployment tags to `v*` and configure any required reviewers. Protect release tags with a repository ruleset so only release maintainers can create them.
2. Add an environment secret named `NUGET_USER` containing the nuget.org **profile username**, rather than an email address or organization name. Use an account authorized to publish all repository packages.
3. Sign in to nuget.org with that account and open **Trusted Publishing**. Create a GitHub policy with these values:

   | Field | Value |
   | --- | --- |
   | Repository owner | `MiracleDevs` |
   | Repository | `Paradigm.Enterprise` |
   | Workflow file | `publish.yml` |
   | Environment | `release` |
   | Package pattern | `Paradigm.Enterprise.*` |

   Enter only the workflow filename. Choose the individual or organization that owns the packages as the policy owner, and allow publishing new versions. Also allow creating new packages when introducing a package ID. If packages have different owners, create a policy for each owner as needed.
4. Merge the workflow before creating the release tag. After the first successful trusted publish, revoke the old long-lived publishing key and remove any stored copies.

The workflow needs no stored NuGet API key. Only the publish job has `id-token: write`, and its `release` environment must match the NuGet policy. Account and environment configuration must be completed on GitHub and nuget.org before the first release.

## Release

1. Set the intended version in `build/Paradigm.Version.props` and align the changelog and plugin version. Run `pwsh build/verify-version.ps1` and the repository quality checks.
2. Merge the reviewed release changes into `main` and tag that commit. For example, when the shared version is `1.1.2`:

   ```bash
   git switch main
   git pull --ff-only
   git tag v1.1.2
   git push origin v1.1.2
   ```

3. Follow **Actions > Publish NuGet packages** and approve the `release` deployment if reviewers are configured.

The tag must exactly match `v` followed by the shared version; a mismatch fails before packaging or authentication. The build job runs the non-integration source tests and packs all packable solution projects, including the CLI. Both `.nupkg` and `.snupkg` files are retained in the run's `nuget-packages` artifact. The publish job downloads this artifact without rebuilding, then `dotnet nuget push` uploads packages and their accompanying symbol packages to nuget.org.

For a transient publishing failure, rerun the failed job from the same workflow run. It obtains a fresh temporary key and uses `--skip-duplicate` to tolerate package versions already uploaded during a partial publish. Verify packages and symbols on nuget.org after a retry. Published package versions are immutable; use a new version for changed contents.

Local build, pack, and test scripts remain available for development. Publishing takes place through GitHub Actions.
