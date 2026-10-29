# Blazor WASM Seating Chart: Phase 1 - Foundation & Infrastructure

## Week 1 Deliverable: Working Solution Skeleton

### Task 1.1: Initialize Solution Structure
- Create `SeatingChart.slnx` solution file (.NET 10 default format)
- Pin .NET 10 SDK in `global.json`: `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }`

### Task 1.2: Create Core Library Project
```bash
dotnet new classlib -n SeatingChart.Core -o src/SeatingChart.Core -f net10.0
dotnet add src/SeatingChart.Core package CsvHelper
```
- Update `RootNamespace` to `SeatingChartApp.Core` in `SeatingChart.Core.csproj`
- Add `CsvHelper` NuGet package reference

### Task 1.3: Create Client Blazor Project
```bash
dotnet new blazorwasm -n SeatingChart.Client -o src/SeatingChart.Client -f net10.0
```
- Set root namespace to `SeatingChartApp.Client` in the resulting csproj
- Configure properties from section 5.1 (`SeatingChart.Client.csproj`)

### Task 1.4: Create Unit Test Project
```bash
dotnet new xunit -n SeatingChart.Core.Tests -o tests/SeatingChart.Core.Tests -f net10.0
```
- Configure root namespace as `SeatingChartApp.Core.Tests`

### Task 1.5: Wire Up Dependencies
```bash
dotnet sln add src/SeatingChart.Core src/SeatingChart.Client tests/SeatingChart.Core.Tests
dotnet add src/SeatingChart.Client reference src/SeatingChart.Core
dotnet add tests/SeatingChart.Core.Tests reference src/SeatingChart.Core
```

### Task 1.6: Create Sample CSV Data
Create directories and files under `src/SeatingChart.Client/wwwroot/sample/`:
- `students-sample.csv` with sample student data + embedded conflicts column
- `conflicts-sample.csv` with optional conflict definitions

### Task 1.7: Create CI Pipeline
Create `.github/workflows/deploy.yml` with full build → test → publish → deploy pipeline per section 8.1

---

## File Checklist for Phase 1 Completion

Core library:
- [ ] `src/SeatingChart.Core/SeatingChart.Core.csproj`
- [ ] Unit tests project reference working, builds clean

Client app:
- [ ] `src/SeatingChart.Client/SeatingChart.Client.csproj`
- [ ] Standard Blazor template files in place
- [ ] Sample CSVs exist under `wwwroot/sample/`

Repo structure:
- [ ] `.github/workflows/deploy.yml` CI pipeline
- [ ] `global.json` pinned to .NET 10.0

Build verification:
- [ ] `dotnet build` succeeds on solution
- [ ] Unit test project builds (tests don't need to pass yet)

Deploy verification:
- [ ] CI runs successfully against empty solution
