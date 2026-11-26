# Publish NuGet packages

The [publish workflow](https://github.com/MiracleDevs/Paradigm.Enterprise/blob/main/.github/workflows/publish.yml) replaces the local API-key-based publisher. It builds, tests, and packs the solution when a `v<version>` tag is pushed, requiring the tagged commit to be on `main`. It excludes `Paradigm.Enterprise.Cli` from publishing. A separate job publishes the remaining packages with [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), using GitHub OIDC to obtain a temporary API key immediately before the push.

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

1. Add a changelog entry and release notes for the intended version, using the existing heading format in `CHANGELOG.md`, then run `pwsh build/set-version.ps1 -Version <new-version>`. This updates `build/Paradigm.Version.props`, plugin metadata, and the template's central package versions. Run `pwsh build/verify-version.ps1` and the repository quality checks. The existing `bash build/increment.version.sh <old-version> <new-version>` command delegates to the same release-preparation script.
2. Merge the reviewed release changes into `main` and tag that commit. For example, when the shared version is `1.1.2`:

   ```bash
   git switch main
   git pull --ff-only
   git tag v1.1.2
   git push origin v1.1.2
   ```

3. Follow **Actions > Publish NuGet packages** and approve the `release` deployment if reviewers are configured.

The tag must exactly match `v` followed by the shared version, and its commit must be reachable from `origin/main`; either a mismatch or a tag on an unmerged branch fails before packaging or authentication. Tags on earlier commits in `main` remain eligible. The workflow fetches the full history to validate this ancestry because GitHub tag triggers do not identify a source branch.

The build job runs the non-integration source tests and packs all packable solution projects. It also builds and validates `Paradigm.Enterprise.Templates.WebApi`: the source template and a solution created by the actual `dotnet new` engine must build against framework packages from the same checkout. The generated solution must discover CPM, resolve the release version, match local package hashes, and pass Development and Production host checks. Version drift or a failed template check stops the release before artifact upload and publishing.

The artifact upload includes the validated template package and excludes `Paradigm.Enterprise.Cli.*.nupkg` and `Paradigm.Enterprise.Cli.*.snupkg`. The remaining `.nupkg` and `.snupkg` files are retained in the run's `nuget-packages` artifact. The publish job downloads this artifact without rebuilding, then `dotnet nuget push` uploads packages and their accompanying symbol packages to nuget.org. The CLI is still built and tested locally and in CI; this workflow does not publish it.

### Tags created on feature branches

Creating a tag locally does not start the workflow. Pushing the tag starts it immediately. Merging a branch into `main` does not start or automatically rerun this tag-triggered workflow.

| Sequence | Result |
| --- | --- |
| Create a tag locally on a feature branch, merge normally into `main`, then push the tag | Publishing can proceed because `main` contains the tagged commit. |
| Push the tag before merging the feature branch | The workflow fails the main-branch check before building or publishing. |
| After that failure, merge normally into `main` and select **Re-run failed jobs** on the original Actions run | The workflow fetches `main` again, and the tagged commit passes the ancestry check. The version check and remaining validation must also pass. |
| Squash or rebase merge the feature branch | These merge methods can replace commit IDs. The original tagged commit may remain outside `main`, so rerunning the original tag can still fail. Use the merge-first release sequence above for these releases. |

A normal merge preserves the original feature commits in `main`'s history. Merging does not move the tag: publishing still uses the original tagged commit, rather than the merge commit or newer changes on `main`.

The recommended sequence is **merge into `main`, tag the resulting commit, then push the tag**. This works with normal, squash, and rebase merges.

### Retry a publishing failure

For a transient publishing failure, rerun the failed job from the same workflow run. It obtains a fresh temporary key and uses `--skip-duplicate` to tolerate package versions already uploaded during a partial publish. Verify packages and symbols on nuget.org after a retry. Published package versions are immutable; use a new version for changed contents.

Local build, pack, and test scripts remain available for development. Publishing takes place through GitHub Actions.
