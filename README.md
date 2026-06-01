# ResetFlow

ResetFlow is a .NET 10 / WPF MVP for rebuilding a personal Windows setup after a reset or clean install.

## Structure

- `src/ResetFlow.App`: WPF GUI
- `src/ResetFlow.Core`: feature catalog, execution engine, backup/rollback, logging, configuration
- `tests/ResetFlow.Core.Tests`: focused unit tests for the core flow

## Build And Test

```powershell
dotnet build ResetFlow.sln
dotnet test ResetFlow.sln
```

## Publish

```powershell
dotnet publish src/ResetFlow.App/ResetFlow.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
