# Repository Guidelines
- 모든 codex cli 출력값은 한글로
- 빌드하지 말것
- Doc : Opinet_API_Free_24.pdf
- 제품코드(기름구분)는 고급휘발유, 보통휘발유, 자동차경유 만 정리.

## Project Structure & Module Organization
- Root: .NET 8 console app (`OpinetScheduler.csproj`).
- Entry point: `Program.cs` (uses `PeriodicTimer`).
- Models: `Models/AvgAllPriceModel.cs` (API DTOs: `OIL`, `RESULT`, `Root`).
- Docs: `api.md`, `Opinet_API_Free_*.pdf`.
- Build artifacts: `bin/`, `obj/` (generated).

## Build, Test, and Development Commands
- Build: `dotnet build` — compiles for the current framework (`net8.0`).
- Run: `dotnet run` — runs the console app from the project root.
- Watch: `dotnet watch run` — hot-reloads during development.
- Restore: `dotnet restore` — restores NuGet packages (e.g., `Dapper`).
- Format: `dotnet format` — applies code style fixes (if installed).

## Coding Style & Naming Conventions
- Indentation: 4 spaces; braces on new lines (C# conventional).
- Naming: `PascalCase` for classes/properties (`AvgAllPriceModel`, `TRADE_DT` mirrors API); `camelCase` for locals/parameters; constants `PascalCase` or `SCREAMING_SNAKE_CASE` if interop.
- Nullability: `Nullable` enabled; prefer `string?`/`T?` when appropriate.
- Async: prefer `async/await`; avoid blocking calls in async flows.
- Imports/usings: keep minimal; file-scoped namespaces if added later.

## Testing Guidelines
- Framework: recommend `xUnit` with a sibling project `OpinetScheduler.Tests`.
- Naming: `{ClassName}.{Method}_Should{Behavior}`; place test data under `tests/fixtures/` if needed.
- Run tests: `dotnet test` (from the solution root once tests exist).
- Coverage: target ≥80% for core logic (API adapters, schedulers).

## Commit & Pull Request Guidelines
- Commits: imperative present (“Add scheduler loop”, “Refactor models”). Optional scope: `feat:`, `fix:`, `docs:`.
- PRs: include purpose, linked issue, before/after notes, and console output/screenshot when behavior changes.
- Size: keep PRs focused; include migration or config notes if applicable.

## Security & Configuration Tips
- Secrets: never commit keys; use environment variables (e.g., `OPINET_API_KEY`).
- Config: prefer `appsettings.json` + `appsettings.Development.json` if configuration is introduced.
- Dependencies: pin versions via `PackageReference`; run `dotnet list package --outdated` periodically.

