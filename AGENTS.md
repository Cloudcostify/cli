# AGENTS.md

Canonical agent rules: `saas-factory-labs/SaaS-Factory@main/AGENTS.md`
(https://github.com/saas-factory-labs/SaaS-Factory/blob/main/AGENTS.md) — a
**private** repository. Requires authenticated GitHub access to
`saas-factory-labs/SaaS-Factory`.

## Required startup sequence

Before planning or implementing any change in this repository, an agent must:

1. Obtain authenticated GitHub access to `saas-factory-labs/SaaS-Factory`. If
   access is unavailable, fail closed and report the blocker before
   proceeding — do not plan or implement.
2. From `saas-factory-labs/SaaS-Factory@main`, read completely, in order:
   1. `AGENTS.md`
   2. `docs/agent-contract/shared-agent-rules.md`
   3. `docs/content/ai-rules/README.md`
   4. Only the baseline, backend, frontend, infrastructure, test,
      development-workflow, and documentation rule sections under
      `docs/content/ai-rules/` relevant to the task.
3. Classify the anticipated change against the mandatory review-gate routing
   table in `docs/content/ai-rules/development-workflow/review-gates.md`,
   then load and apply every triggered prompt under `docs/content/ai-prompts/`
   before writing production code. Record every canonical gate's status per
   the status contract defined in `review-gates.md`, with evidence, in the
   plan and pull request. Reclassify and apply any newly triggered prompts
   whenever the scope of the change changes.
4. Read this file completely. It narrows the canonical rules for this
   repository; it does not replace, duplicate, or weaken them. In particular,
   the canonical `AGENTS.md`'s build commands, source layout, and
   repository-ownership sections describe the SaaS-Factory repository itself
   and do not apply here — use the commands and layout below instead.
5. Read a nearer `AGENTS.md` below the target directory if one exists; it may
   add scoped constraints.
6. Inspect the current source and tests in this repository before proposing
   or making changes.

If the private repository, any rule listed above, or a triggered review
prompt cannot be loaded completely, stop and report the blocker. Do not
substitute assumptions for canonical content, and do not copy canonical rule
text into this repository — always fetch and follow it from
`saas-factory-labs/SaaS-Factory@main`.

## Repository purpose and ownership boundary

Public .NET global tool (`Cloudcostify.Cli`, command `cloudcostify`) that
estimates cloud infrastructure cost from Pulumi (and other IaC) preview
output, for local use and in CI/CD pipelines ahead of `Cloudcostify/github-action`.
This repository owns CLI UX, IaC-provider parsing, local credential/config
handling, and the API client. It does not own the pricing/estimation API
itself — that lives in `Cloudcostify/platform`.

## Components

- `src/CostEstimationCli/` — main CLI application (`Configuration/`,
  `Models/`, `Repositories/`, `Services/`, `UI/`, `Program.cs`).
- `src/CostEstimationCli.Tests/` — unit tests.
- `docs/CostEstimationCli.slnx` — the solution file. **Not at repository
  root** — build/test commands must reference this path explicitly.
- `docs/` — user-facing docs (installation, configuration, CI/CD integration,
  supported providers/resources).
- `samples/` — sample IaC projects for manual testing.
- `artifacts/` — build/pack output (generated; do not hand-edit).

## Build, test, package, release

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack src/CostEstimationCli/CostEstimationCli.csproj --configuration Release --output ./artifacts/nupkg
```

`global.json` pins the .NET 10 SDK and the `Microsoft.Testing.Platform` test
runner.

- CI is `.github/workflows/build-and-test.yml`: restore, build, test with
  coverage, pack, then (on push to `main`/`develop`) an integration job that
  runs the built CLI against the live API.
- `src/CostEstimationCli/CostEstimationCli.csproj` conditionally references
  SaaS-Factory's `AppBlueprint.CliKit` as a local `ProjectReference` when
  `AppBlueprintCliKitProjectPath` resolves (a developer machine with a
  sibling SaaS-Factory checkout), and otherwise falls back to the
  `SaaS-Factory.AppBlueprint.CliKit` NuGet package. Local project-reference
  builds and the packaged NuGet-based build can diverge. Per the canonical
  package-consumption verification rule, a change touching this dependency
  must also be verified against the packaged (NuGet) path, not only the
  local project reference.
- Releases are published as GitHub release tags
  (`releases/download/<tag>/cloudcostify-<rid>`), consumed by
  `Cloudcostify/github-action`. Publishing a release is a human-authorized
  action, not something an agent does unprompted.
- Contribution conventions (branch/commit style, code style, test
  frameworks) are documented in `CONTRIBUTING.md` and `.editorconfig`; follow
  those rather than duplicating them here.

## Security-sensitive areas

- `CLOUDCOSTIFY_API_KEY`, `PULUMI_ACCESS_TOKEN`, and the other
  `CLOUDCOSTIFY_PULUMI_PROJECT_*` values, read from environment variables or
  from `~/.cloudcostify/config.json` (written by `cloudcostify --setup`).
- The `--setup` wizard's browser redirect and local credential storage.
- Code that shells out to or parses third-party IaC tool output (Pulumi
  Automation API, Bicep) — treat that output as untrusted input.

## Required verification

- `dotnet build` and `dotnet test` (as above) for any change.
- `dotnet pack` when packaging or dependency metadata changes.
- When a change touches the `AppBlueprint.CliKit` reference, also verify the
  packed/NuGet-consumption path, not just the local project reference.
- Apply the canonical review-gate routing before any change to credential
  handling, the setup wizard, or outbound HTTP/API-client behavior.
