# Blazor WASM Seating Chart: Architecture Plan

*.NET 10 (LTS) standalone Blazor WebAssembly, zero-cost static hosting, LocalStorage persistence, no backend.*

## 1. Key Decisions

| Concern | Decision | Why |
| --- | --- | --- |
| Framework | `net10.0` standalone Blazor WebAssembly (`dotnet new blazorwasm`) | .NET 10 is an LTS release; standalone WASM publishes to plain static files |
| Architecture | Clean Architecture and Vertical Slice Architecture | Cleanest solution to separate concerns and make adding new features easy |
| Hosting | GitHub Pages (primary), Cloudflare Pages or Netlify (alternatives) | All free for this use case; no server runtime needed |
| Persistence | Browser `localStorage` via a thin JS-interop wrapper | Simple, no dependencies; \~5 MB limit is plenty for a few hundred students |
| CSV parsing | CsvHelper (NuGet), wrapped in a small CsvImporter class | Handles quoting, BOM, header matching and bad rows for us; use explicit ClassMaps and test a Release publish to stay trim-safe |
| State | Singleton `AppStateService` with change events | No Redux or Fluxor needed at this size |
| UI | Plain Razor components, CSS Grid and SVG, scoped CSS | No paid component library; SVG makes round tables easy |
| Privacy | All student data stays in the browser | Important for school data: nothing is uploaded anywhere |
| Tests | xUnit project using NSubstitute that references only the Core library | Conflict detection and CSV parsing are where the bugs hide |
| Solution layout | SeatingChart.Core class library + SeatingChart.Client Blazor app + SeatingChart.Core.Tests | Domain logic has no UI or browser dependencies, so it is unit-tested with plain dotnet test |

**Important privacy note for the README:** LocalStorage is per browser and per device, and clearing browser data erases it. Plan for **Export/Import JSON backup** from day one.

## 2. Architecture Diagrams

### 2.1 Layered architecture

```
+-----------------------------------------------------------------+
|                       BROWSER (Static Files)                    |
|                                                                 |
|  +-------------------- UI LAYER (Razor) ---------------------+  |
|  |  Pages: Students | Conflicts | Layouts | SeatingChart      |  |
|  |  Components: SeatView, TableView, GridCanvas, TableCanvas, |  |
|  |              ConflictPanel, CsvUploader, StudentList       |  |
|  +----------------------------+-------------------------------+  |
|                               | inject / events                  |
|  +-------------------- APPLICATION LAYER -------------------+    |
|  |  AppStateService  (single source of truth, OnChange)     |    |
|  |  CsvImportService     SeatingService (assign/swap/auto)  |    |
|  |  ConflictService      LayoutService  (presets, custom)   |    |
|  +----------------------------+-----------------------------+    |
|                               |                                  |
|  +-------------------- CORE LIB (SeatingChart.Core) --------+    |
|  |  Models: Student, Conflict, LayoutDefinition, TableDef,  |    |
|  |          Seat, SeatingChart, AppData                     |    |
|  |  Engines: AdjacencyBuilder, ConflictDetector,            |    |
|  |           CsvImporter, AutoSeater                        |    |
|  +----------------------------+-----------------------------+    |
|                               |                                  |
|  +-------------------- INFRASTRUCTURE ----------------------+    |
|  |  IStorageService -> LocalStorageService -> storage.js    |    |
|  |  JSON (System.Text.Json source-generated context)        |    |
|  +----------------------------------------------------------+    |
+-----------------------------------------------------------------+
        ^ served as static files by GitHub Pages / Cloudflare / Netlify
```

**Project boundary:** the DOMAIN box is the `SeatingChart.Core` class library (no Blazor or JS dependencies). UI, browser-facing services and infrastructure live in `SeatingChart.Client`. Dependencies point one way: Client → Core and Tests → Core.

### 2.2 Data flow: CSV import to conflict highlighting

```
 students.csv --> CsvUploader (InputFile) --> CsvImporter --> ImportResult
                                                             |  (students, conflicts, warnings)
                                                             v
                                                    AppStateService.Merge()
                                                             |
                          +----------------------------------+--------------+
                          v                                                 v
                 LocalStorageService.Save()                       OnChange event
                                                                            |
   Layout selected --> AdjacencyBuilder --> AdjacencyMap                    v
                                   |                              SeatingChart page re-renders
                                   v                                        |
   Assignments + Conflicts + AdjacencyMap --> ConflictDetector --> Violations
                                                                            |
                                                  SeatView gets CSS class 'conflict' (red + icon)
```

### 2.3 Component tree

```
App.razor
└─ MainLayout.razor (nav, Export/Import backup, storage-status badge)
   ├─ Pages/Home.razor              (getting-started, sample data button)
   ├─ Pages/Students.razor          (CsvUploader + StudentList)
   ├─ Pages/Conflicts.razor         (ConflictPanel: add/remove pairs)
   ├─ Pages/Layouts.razor           (preset picker + LayoutEditor)
   └─ Pages/Chart.razor             (main workspace)
      ├─ UnassignedStudentList      (drag source / click-to-select)
      ├─ GridCanvas  ──► SeatView × N          (grid layouts)
      ├─ TableCanvas ──► TableView ─► SeatView × N  (table layouts, SVG)
      └─ ViolationSummary           (list of conflicting pairs, click to focus)
```

### 2.4 Layout kinds and adjacency rules

```
Grid (rows x cols)              Rectangular table             Round table
[A][B][C][D]                    [S1][S2][S3]                    S1  S2
[E][F][G][H]                    [S6][S5][S4]                  S6      S3
                                                               S5  S4
Adjacent = left/right/front/    Adjacent = neighbors along    Adjacent = ring neighbors (±1)
back (+ optional diagonals)     the perimeter, or all seats   or whole table
                                at same table
```

## 3. Domain Model

```csharp
public record Student(Guid Id, string ExternalId, string FirstName, string LastName,
                      string? Grade = null, string? Notes = null)
{ public string DisplayName => $"{FirstName} {LastName}".Trim(); }

public record Conflict(Guid StudentAId, Guid StudentBId, string? Reason = null)
{   // Order-independent identity
    public bool Involves(Guid a, Guid b) =>
        (StudentAId == a && StudentBId == b) || (StudentAId == b && StudentBId == a);
}

public enum TableShape { Rectangle, Round, Square, Custom }
public enum LayoutKind { Grid, Tables }
public enum AdjacencyRule { SideBySide, IncludeDiagonals, SameTable, RingNeighbors }

public class TableDef            // used when LayoutKind.Tables
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Table";
    public TableShape Shape { get; set; }
    public int SeatCount { get; set; } = 4;
    public double X { get; set; }  public double Y { get; set; }
    public double Width { get; set; } = 160; public double Height { get; set; } = 80;
    public double Rotation { get; set; }
}

public class LayoutDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public LayoutKind Kind { get; set; }
    public int Rows { get; set; } = 5;     // Grid
    public int Cols { get; set; } = 6;     // Grid
    public AdjacencyRule Rule { get; set; } = AdjacencyRule.IncludeDiagonals;
    public List<TableDef> Tables { get; set; } = new();
}

public record Seat(string Id, string? TableId, int Index, double X, double Y);
// Seat IDs: grid -> "r{row}c{col}", tables -> "{tableId}:{index}"

public class SeatingChart
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Period 1";
    public string LayoutId { get; set; } = "";
    public Dictionary<string, Guid> Assignments { get; set; } = new(); // seatId -> studentId
}

public class AppData            // the single JSON blob persisted
{
    public int SchemaVersion { get; set; } = 1;
    public List<Student> Students { get; set; } = new();
    public List<Conflict> Conflicts { get; set; } = new();
    public List<LayoutDefinition> Layouts { get; set; } = new();
    public List<SeatingChart> Charts { get; set; } = new();
    public string? ActiveChartId { get; set; }
}
```

## 4. Recommended Project Structure

```
SeatingChart/
├─ .github/workflows/deploy.yml
├─ global.json                      # pins .NET 10 SDK
├─ SeatingChart.slnx                # .NET 10 default solution format
├─ README.md
├─ src/
│  ├─ SeatingChart.Core/            # class library: no Blazor, no JS interop
│  │  ├─ SeatingChart.Core.csproj   # references CsvHelper
│  │  ├─ Models/        Student.cs, Conflict.cs, Seat.cs, TableDef.cs,
│  │  │                 LayoutDefinition.cs, SeatingChart.cs, AppData.cs
│  │  ├─ Layouts/       AdjacencyBuilder.cs, LayoutPresets.cs
│  │  ├─ Conflicts/     ConflictDetector.cs
│  │  ├─ Seating/       SeatingService.cs, AutoSeater.cs
│  │  ├─ Csv/           CsvImporter.cs, CsvRows.cs (rows + ClassMaps),
│  │  │                 ImportResult.cs, ImportMerger.cs
│  │  └─ Serialization/ AppJsonContext.cs, AppDataSerializer.cs (Migrate)
│  └─ SeatingChart.Client/          # Blazor WASM app: UI + browser APIs
│     ├─ SeatingChart.Client.csproj # references Core
│     ├─ Program.cs, App.razor, _Imports.razor
│     ├─ Services/      IStorageService.cs, LocalStorageService.cs,
│     │                 AppStateService.cs, CsvImportService.cs, BackupService.cs
│     ├─ Layout/        MainLayout.razor, NavMenu.razor
│     ├─ Pages/         Home, Students, Conflicts, Layouts, Chart (.razor)
│     ├─ Components/
│     │  ├─ CsvUploader.razor, StudentList.razor, ConflictPanel.razor
│     │  ├─ GridCanvas.razor, TableCanvas.razor, TableView.razor
│     │  ├─ SeatView.razor, LayoutEditor.razor, ViolationSummary.razor
│     │  └─ UnassignedStudentList.razor
│     └─ wwwroot/
│        ├─ index.html, 404.html (copy of index.html on deploy)
│        ├─ .nojekyll, _redirects (Netlify)
│        ├─ css/app.css
│        ├─ js/storage.js, js/download.js
│        └─ sample/ students-sample.csv, conflicts-sample.csv
└─ tests/
   └─ SeatingChart.Core.Tests/
      ├─ Csv/            CsvImporterTests.cs, ImportMergerTests.cs
      ├─ Layouts/        AdjacencyBuilderTests.cs
      ├─ Conflicts/      ConflictDetectorTests.cs
      ├─ Seating/        SeatingServiceTests.cs, AutoSeaterTests.cs
      └─ Serialization/  AppDataSerializerTests.cs   # round trip + migration
```

The split rule: anything that needs `IJSRuntime`, `IBrowserFile` or Razor goes in Client; everything else goes in Core. Keeping the JSON serializer in Core lets tests check exactly what gets written to LocalStorage.

## 5. File-by-File Implementation Guide

### 5.0 Solution setup: Core library, client and tests

The domain logic lives in a UI-free class library, so xUnit tests run with plain `dotnet test` and need no browser.

```bash
dotnet new sln -n SeatingChart            # .NET 10 creates SeatingChart.slnx
dotnet new classlib -n SeatingChart.Core -o src/SeatingChart.Core -f net10.0
dotnet new blazorwasm -n SeatingChart.Client -o src/SeatingChart.Client -f net10.0
dotnet new xunit -n SeatingChart.Core.Tests -o tests/SeatingChart.Core.Tests -f net10.0
dotnet sln add src/SeatingChart.Core src/SeatingChart.Client tests/SeatingChart.Core.Tests
dotnet add src/SeatingChart.Client reference src/SeatingChart.Core
dotnet add tests/SeatingChart.Core.Tests reference src/SeatingChart.Core
dotnet add src/SeatingChart.Core package CsvHelper
```

`SeatingChart.Core.csproj` (replace the template's contents; the package reference is added by the last command, shown here for clarity):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>SeatingChartApp.Core</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CsvHelper" Version="33.*" />
  </ItemGroup>
</Project>
```

**Namespace gotcha:** the model class `SeatingChart` clashes with a root namespace also called `SeatingChart` (the compiler resolves the name to the namespace, not the type). Keep the project and folder names as shown, but set `RootNamespace` to `SeatingChartApp.Core` and `SeatingChartApp.Client`, and use those in `using` lines and `_Imports.razor`. Give the test project the same treatment if the template's default namespace causes the same clash.

Pin the CsvHelper version to the latest stable release when you create the project; `33.*` is only a floating placeholder.

### 5.1 `SeatingChart.Client.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>SeatingChartApp.Client</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>  <!-- smaller download -->
    <BlazorEnableCompression>true</BlazorEnableCompression>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.*" />
    <ProjectReference Include="..\SeatingChart.Core\SeatingChart.Core.csproj" />
    <!-- keep Core's row classes and ClassMaps intact under IL trimming -->
    <TrimmerRootAssembly Include="SeatingChart.Core" />
  </ItemGroup>
</Project>
```

`global.json`: `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }`

### 5.2 `wwwroot/js/storage.js` and `download.js`

```javascript
// storage.js (ES module)
export function get(key)        { return localStorage.getItem(key); }
export function set(key, value) { localStorage.setItem(key, value); }
export function remove(key)     { localStorage.removeItem(key); }

// download.js (global) used for JSON backup and CSV export
window.downloadText = (filename, text, mime) => {
  const blob = new Blob([text], { type: mime });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob); a.download = filename; a.click();
  URL.revokeObjectURL(a.href);
};
```

Add `<script src="js/download.js"></script>` to `index.html` before the Blazor script.

### 5.3 `Services/LocalStorageService.cs`

```csharp
public interface IStorageService
{
    Task<T?> LoadAsync<T>(string key);
    Task SaveAsync<T>(string key, T value);
}

public class LocalStorageService(IJSRuntime js) : IStorageService
{
    private IJSObjectReference? _module;
    private async ValueTask<IJSObjectReference> Mod() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/storage.js");

    public async Task<T?> LoadAsync<T>(string key)
    {
        var json = await (await Mod()).InvokeAsync<string?>("get", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, AppJsonContext.Default.Options);
    }

    public async Task SaveAsync<T>(string key, T value) =>
        await (await Mod()).InvokeVoidAsync("set", key,
            JsonSerializer.Serialize(value, AppJsonContext.Default.Options));
}
```

Catch `JSException` on save (quota exceeded) and surface a toast. Use one key (`seatingchart.v1`) holding the whole `AppData`.

### 5.4 `Services/AppStateService.cs`

Responsibilities: load `AppData` on startup, expose it, mutate through methods, and persist **debounced (about 300 ms)** after each change.

```csharp
public class AppStateService(IStorageService storage)
{
    private const string Key = "seatingchart.v1";
    public AppData Data { get; private set; } = new();
    public bool Loaded { get; private set; }
    public event Action? OnChange;

    public async Task InitAsync()
    {
        Data = await storage.LoadAsync<AppData>(Key) ?? Seed();
        Data.Layouts = Data.Layouts.Count > 0 ? Data.Layouts : LayoutPresets.All();
        Loaded = true; Notify(false);
    }
    public void AddConflict(Guid a, Guid b, string? reason) { /* dedupe via Involves */ Notify(); }
    public void RemoveConflict(Guid a, Guid b)               { /* ... */ Notify(); }
    public void Assign(string chartId, string seatId, Guid studentId) { /* remove prior seat */ Notify(); }
    public void Unassign(string chartId, string seatId)      { /* ... */ Notify(); }
    private void Notify(bool save = true) { OnChange?.Invoke(); if (save) _ = SaveDebouncedAsync(); }
}
```

Pages call `await State.InitAsync()` in `OnInitializedAsync` (or `Program.cs` before `RunAsync`). Subscribe in `OnInitialized` and unsubscribe in `Dispose`.

### 5.5 `SeatingChart.Core/Csv/CsvImporter.cs` (CsvHelper)

CsvHelper handles quoting, escaped quotes, embedded commas and newlines, and header matching. We add explicit ClassMaps (no auto-mapping, which keeps it trim-friendly), case-insensitive headers, tolerance for short rows, and per-row warnings. The importer takes a `TextReader`, so tests can pass a `StringReader`.

```csharp
// Csv/CsvRows.cs
public class StudentRow
{
    public string? StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Grade { get; set; }
    public string? Notes { get; set; }
    public string? ConflictsWith { get; set; }   // "S004;S009"
}

public class ConflictRow
{
    public string? StudentIdA { get; set; }
    public string? StudentIdB { get; set; }
    public string? Reason { get; set; }
}

// Optional() = the column may be missing from the file
public sealed class StudentRowMap : ClassMap<StudentRow>
{
    public StudentRowMap()
    {
        Map(m => m.StudentId).Optional();
        Map(m => m.FirstName).Optional();
        Map(m => m.LastName).Optional();
        Map(m => m.Grade).Optional();
        Map(m => m.Notes).Optional();
        Map(m => m.ConflictsWith).Optional();
    }
}
// ConflictRowMap: the same pattern for ConflictRow

// Csv/ImportResult.cs
public class ImportResult
{
    public List<Student> Students { get; } = new();
    public List<Conflict> Conflicts { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
}

// Csv/CsvImporter.cs
public static class CsvImporter
{
    private readonly record struct Link(string From, string To, int Row, string? Reason);

    private static CsvConfiguration Config() => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        TrimOptions = TrimOptions.Trim,
        PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(), // case-insensitive headers
        MissingFieldFound = null,                                              // short rows are OK
    };

    public static ImportResult ImportStudents(TextReader reader)
    {
        var result = new ImportResult();
        var byExternalId = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);
        var links = new List<Link>();

        using var csv = new CsvReader(reader, Config());
        csv.Context.RegisterClassMap<StudentRowMap>();
        try
        {
            csv.Read(); csv.ReadHeader();
            while (csv.Read())
            {
                int row = csv.Parser.Row;                      // header is row 1
                var r = csv.GetRecord<StudentRow>()!;
                var first = r.FirstName ?? "";
                var last = r.LastName ?? "";
                if (first.Length == 0 && last.Length == 0)
                { result.Warnings.Add($"Row {row}: no name, skipped."); continue; }

                var ext = string.IsNullOrEmpty(r.StudentId) ? $"ROW{row}" : r.StudentId;
                if (byExternalId.ContainsKey(ext))
                { result.Errors.Add($"Row {row}: duplicate StudentId '{ext}', skipped."); continue; }

                var student = new Student(Guid.NewGuid(), ext, first, last, r.Grade, r.Notes);
                byExternalId[ext] = student;
                result.Students.Add(student);

                foreach (var other in (r.ConflictsWith ?? "").Split(';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    links.Add(new Link(ext, other, row, null));
            }
        }
        catch (CsvHelperException ex)
        {
            result.Errors.Add($"Row {csv.Parser.Row}: {ex.Message}");
        }

        ResolveLinks(result, byExternalId, links);   // second pass: IDs -> GUIDs
        return result;
    }

    public static ImportResult ImportConflicts(TextReader reader, IEnumerable<Student> students)
    {
        var result = new ImportResult();
        var lookup = students.ToDictionary(s => s.ExternalId, StringComparer.OrdinalIgnoreCase);
        var links = new List<Link>();

        using var csv = new CsvReader(reader, Config());
        csv.Context.RegisterClassMap<ConflictRowMap>();
        try
        {
            csv.Read(); csv.ReadHeader();
            while (csv.Read())
            {
                var r = csv.GetRecord<ConflictRow>()!;
                links.Add(new Link(r.StudentIdA ?? "", r.StudentIdB ?? "", csv.Parser.Row, r.Reason));
            }
        }
        catch (CsvHelperException ex)
        {
            result.Errors.Add($"Row {csv.Parser.Row}: {ex.Message}");
        }

        ResolveLinks(result, lookup, links);
        return result;
    }

    // Unknown IDs and self-conflicts become warnings; A-B and B-A collapse into one conflict.
    private static void ResolveLinks(ImportResult result,
        IReadOnlyDictionary<string, Student> lookup, List<Link> links)
    {
        var seen = new HashSet<(Guid, Guid)>();
        foreach (var l in links)
        {
            if (!lookup.TryGetValue(l.From, out var a) || !lookup.TryGetValue(l.To, out var b))
            { result.Warnings.Add($"Row {l.Row}: unknown StudentId in '{l.From}' / '{l.To}', conflict skipped."); continue; }
            if (a.Id == b.Id)
            { result.Warnings.Add($"Row {l.Row}: {l.From} cannot conflict with itself."); continue; }

            var key = a.Id.CompareTo(b.Id) < 0 ? (a.Id, b.Id) : (b.Id, a.Id);
            if (seen.Add(key)) result.Conflicts.Add(new Conflict(a.Id, b.Id, l.Reason));
        }
    }
}
```

Notes: the delimiter is a fixed comma. Do not enable CsvHelper's delimiter auto-detection, because `ConflictsWith` uses `;` inside a field. CsvHelper builds its readers with expression trees, which run interpreted in WebAssembly; that is fine for a few hundred rows. Before relying on it, publish a Release build once and import the sample CSV in the browser; if the trimmer removes something CsvHelper needs, the `TrimmerRootAssembly` entry in 5.1 keeps Core's row classes.

### 5.6 `Services/CsvImportService.cs` (client)

A thin wrapper that turns an `IBrowserFile` into a `TextReader` and hands it to Core.

```csharp
public class CsvImportService(AppStateService state)
{
    private const long MaxBytes = 2_000_000;   // InputFile's default limit is only 500 KB

    public async Task<ImportResult> ReadStudentsAsync(IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(MaxBytes);
        using var reader = new StreamReader(stream);   // detects a UTF-8 BOM
        return CsvImporter.ImportStudents(reader);
    }

    public async Task<ImportResult> ReadConflictsAsync(IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(MaxBytes);
        using var reader = new StreamReader(stream);
        return CsvImporter.ImportConflicts(reader, state.Data.Students);
    }
}
```

The import flow:

1. `CsvUploader` previews the `ImportResult`: counts, warnings (unknown IDs, skipped rows) and errors (duplicate IDs, malformed rows).
2. The user picks *Replace all* or *Merge by StudentId* and clicks **Apply**.
3. `AppStateService.ApplyImport` calls Core's `ImportMerger`. In merge mode, existing students keep their GUIDs (matched by `StudentId`, case-insensitive) so seat assignments and manual conflicts survive, and imported conflicts are remapped onto those GUIDs.
4. The optional `conflicts.csv` is read against the students already in state, so import the students file first.

Because the parsing lives in Core, this class stays tiny and the tests feed `StringReader` samples straight into `CsvImporter`.

### 5.7 `SeatingChart.Core/Layouts/AdjacencyBuilder.cs`

Builds `(IReadOnlyList<Seat> seats, Dictionary<string, HashSet<string>> adjacency)` from a `LayoutDefinition`.

```csharp
public static class AdjacencyBuilder
{
    public static (List<Seat> Seats, Dictionary<string, HashSet<string>> Adj) Build(LayoutDefinition l)
        => l.Kind == LayoutKind.Grid ? BuildGrid(l) : BuildTables(l);

    static (List<Seat>, Dictionary<string, HashSet<string>>) BuildGrid(LayoutDefinition l)
    {
        var seats = new List<Seat>(); var adj = new Dictionary<string, HashSet<string>>();
        for (int r = 0; r < l.Rows; r++) for (int c = 0; c < l.Cols; c++)
            seats.Add(new Seat($"r{r}c{c}", null, r * l.Cols + c, c, r));
        foreach (var s in seats) adj[s.Id] = new();
        bool diag = l.Rule == AdjacencyRule.IncludeDiagonals;
        for (int r = 0; r < l.Rows; r++) for (int c = 0; c < l.Cols; c++)
            for (int dr = -1; dr <= 1; dr++) for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                if (!diag && dr != 0 && dc != 0) continue;   // side-by-side + front/back only
                int nr = r + dr, nc = c + dc;
                if (nr < 0 || nc < 0 || nr >= l.Rows || nc >= l.Cols) continue;
                adj[$"r{r}c{c}"].Add($"r{nr}c{nc}");
            }
        return (seats, adj);
    }
    // BuildTables: for each TableDef create seats around the perimeter (round: angle = 2πi/n;
    // rectangle: distribute along the edges). Rule SameTable -> all seats mutually adjacent;
    // otherwise perimeter neighbors i±1 (mod n) are adjacent.
}
```

Seat positions are computed once per layout so the same data drives both rendering and adjacency.

### 5.8 `SeatingChart.Core/Conflicts/ConflictDetector.cs`

```csharp
public record Violation(string SeatIdA, string SeatIdB, Guid StudentA, Guid StudentB, string? Reason);

public static class ConflictDetector
{
    public static List<Violation> Detect(SeatingChart chart,
        Dictionary<string, HashSet<string>> adj, IEnumerable<Conflict> conflicts)
    {
        var seatOf = chart.Assignments.ToDictionary(kv => kv.Value, kv => kv.Key); // student -> seat
        var result = new List<Violation>();
        foreach (var c in conflicts)
        {
            if (seatOf.TryGetValue(c.StudentAId, out var sa) &&
                seatOf.TryGetValue(c.StudentBId, out var sb) &&
                adj.TryGetValue(sa, out var n) && n.Contains(sb))
                result.Add(new(sa, sb, c.StudentAId, c.StudentBId, c.Reason));
        }
        return result;
    }
}
```

Complexity is O(conflicts) per evaluation, which is trivial. In `Chart.razor`, recompute in `OnParametersSet` or after any state change, then build a `HashSet<string> conflictSeatIds` that `SeatView` reads to apply `.conflict` styling.

### 5.9 `SeatingChart.Core/Layouts/LayoutPresets.cs`

Provide factory methods that return ready-made layouts:

| Preset | Definition |
| --- | --- |
| Classroom 5×6 | Grid, 5 rows × 6 cols, diagonals included |
| Classroom 4×8 | Grid, rows × cols adjustable |
| 6 Rectangular Tables (4 seats) | 2×3 arrangement, `Rectangle`, `SeatCount = 4`, `SameTable` |
| 6 Rectangular Tables (6 seats) | Rectangle, `SeatCount = 6` |
| 5 Round Tables (6 seats) | `Round`, `RingNeighbors` |
| Horseshoe / U-shape | Grid with disabled cells (`DisabledSeats` set) |
| Custom | Starts empty and is edited in `LayoutEditor` |

**Add `HashSet<string> DisabledSeats` to `LayoutDefinition`** so grids can have gaps (aisles, teacher desk).

### 5.10 Components

| Component | Responsibility | Notes |
| --- | --- | --- |
| `SeatView` | One seat: student name, empty state, conflict style | Parameters: `Seat`, `Student?`, `IsConflict`, `OnDrop`, `OnClick` |
| `GridCanvas` | CSS Grid: `grid-template-columns: repeat(@Cols, 1fr)` | Renders `SeatView` per cell; skips `DisabledSeats` |
| `TableCanvas` | One SVG container (`viewBox`), loops over `TableView` | Round: `<circle>`; rect: `<rect>`; seats are `<g>` with `<foreignObject>` or `<text>` |
| `TableView` | Draws one table and its seats | Optional drag handle to reposition tables in edit mode |
| `UnassignedStudentList` | Students not yet seated | Drag source; click-to-select then click-seat for touch devices |
| `ConflictPanel` | Two dropdowns (searchable) + reason + Add; list with Remove | Prevents duplicates and self-pairs |
| `LayoutEditor` | Grid rows/cols, table add/remove, shape, seat count, rule | Live preview; "Save as new layout" |
| `ViolationSummary` | Lists current violations; click to scroll to or flash the seats | Also shows count badge in the nav |
| `CsvUploader` | `InputFile`, preview table, warnings, apply | Reused for the conflicts file |

**Drag-and-drop:** use native HTML5 events (`@ondragstart`, `@ondragover:preventDefault`, `@ondrop`) and keep the dragged student ID in a field of `Chart.razor`. Dropping on an occupied seat **swaps** the two students. Provide the click-to-select fallback because HTML5 drag does not work reliably on touch screens.

**Conflict styling:**

```css
.seat.conflict { border: 2px solid #c62828; background: #ffebee; animation: pulse 1.2s infinite; }
.seat.conflict::after { content: "⚠"; position: absolute; top: 2px; right: 4px; }
```

Also use an icon (not only red/green color) so color-blind teachers can see violations.

### 5.11 `SeatingChart.Core/Seating/AutoSeater.cs` (phase 2 nice-to-have)

Randomly place unseated students, then run hill-climbing: repeatedly swap two seats and keep the swap if the violation count drops. Restart a few times, stop at zero violations or after about 2000 iterations. It runs fast enough on the UI thread for classes under 40.

### 5.12 `Services/BackupService.cs`

`ExportJson()` calls `downloadText("seating-backup-2026-10-24.json", ...)`; `ImportJson(string)` validates `SchemaVersion` before replacing state. Add `Migrate(AppData)` hooks now so future schema changes do not strand saved data. Also provide **Export chart as CSV** (seat label, student) and **Print** via `window.print()` with a `@media print` stylesheet.

Put the serializer and `Migrate` in Core (`AppDataSerializer`) so round trips and migrations are unit-tested; the client `BackupService` only handles the file download and upload. The CSV export can use CsvHelper's `CsvWriter`, also in Core.

### 5.13 `Program.cs`

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<SeatingService>();
builder.Services.AddScoped<BackupService>();
await builder.Build().RunAsync();
```

### 5.14 `SeatingChart.Core/Serialization/AppJsonContext.cs`

```csharp
[JsonSerializable(typeof(AppData))]
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppJsonContext : JsonSerializerContext { }
```

Source generation keeps serialization trim-safe and fast. Note: records with constructor parameters need the matching properties to be public.

## 6. Sample CSV Data

### 6.1 `students.csv` (conflicts embedded in the `ConflictsWith` column)

```csv
StudentId,FirstName,LastName,Grade,Notes,ConflictsWith
S001,Ava,Johnson,7,,S004;S009
S002,Liam,Martinez,7,Needs front row,
S003,Noah,Kim,7,,S007
S004,Emma,Williams,7,,S001
S005,Olivia,Brown,7,"Hearing aid, left ear",
S006,Elijah,Davis,7,,S010
S007,Sophia,Garcia,7,,S003
S008,Mason,Patel,7,,
S009,Isabella,Nguyen,7,,S001
S010,Lucas,Thompson,7,,S006;S012
S011,Mia,Anderson,7,,
S012,Ethan,O'Brien,7,"Prefers quiet area",S010
```

### 6.2 `conflicts.csv` (optional second file)

```csv
StudentIdA,StudentIdB,Reason
S002,S008,Distracts each other
S005,S011,Prior incident
S001,S004,Ongoing dispute
```

**Rules to document in the UI:**

- Header names are case-insensitive. Only `FirstName` or `LastName` is required; other columns are optional.
- `StudentId` must be unique. If it is missing, generate one from the row number.
- Conflicts are **symmetric**, so declaring A→B is enough. Duplicates (such as S001-S004 appearing in both files) are merged automatically.
- Wrap values containing commas in double quotes.
- Save the file as UTF-8 (a Excel "CSV UTF-8" export is fine).
- Offer **Download template CSV** and **Load sample data** buttons.

## 7. Persistence Strategy

- One key (`seatingchart.v1`) with the whole `AppData` JSON. Estimated size: 200 students ≈ 40 KB, far below the quota.
- Debounced save after each mutation; also save on `beforeunload` if the debounce is pending.
- On load failure (corrupt JSON), keep the raw string under `seatingchart.v1.corrupt`, start fresh, and warn the user.
- Provide a visible **Backup** button and a banner at first launch reading: *"Data is stored only in this browser. Export a backup regularly."*
- Future (optional, still free): File System Access API for saving to disk, or an optional Supabase or Firebase free tier. Keep `IStorageService` abstract so this swap is cheap.

## 8. Static Hosting and Deployment

### 8.1 GitHub Pages (recommended)

Repo settings: **Settings → Pages → Source: GitHub Actions**.

`.github/workflows/deploy.yml`:

```yaml
name: Deploy Blazor WASM to GitHub Pages
on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      - name: Test
        run: dotnet test tests/SeatingChart.Core.Tests --configuration Release
      - name: Publish
        run: dotnet publish src/SeatingChart.Client -c Release -o release
      - name: Fix base href for project site
        run: sed -i 's|<base href="/" />|<base href="/${{ github.event.repository.name }}/" />|' release/wwwroot/index.html
      - name: SPA fallback and disable Jekyll
        run: |
          cp release/wwwroot/index.html release/wwwroot/404.html
          touch release/wwwroot/.nojekyll
      - uses: actions/upload-pages-artifact@v3
        with:
          path: release/wwwroot

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

**Gotchas:**

- A *project* site lives at `https://<user>.github.io/<repo>/`, so the base href must be `/<repo>/`. For a user site (`<user>.github.io` repo) or a custom domain, leave it as `/` and delete the `sed` step.
- `404.html` as a copy of `index.html` makes deep links (`/chart`) work on refresh.
- Keep `.nojekyll` or files starting with `_` (such as `_framework`) will be dropped.
- Pin Action versions to the current major release you have verified, and check the Actions marketplace if a major has moved on.

### 8.2 Cloudflare Pages (alternative)

Unlimited bandwidth on the free tier and no base-href hacks, since it serves from the domain root. Easiest path: keep the GitHub Actions build and add a deploy step with `cloudflare/wrangler-action` using `pages deploy release/wwwroot --project-name=seating-chart`, or upload the `release/wwwroot` folder manually. SPA routing works automatically when there is no top-level `404.html`.

### 8.3 Netlify (alternative)

Drag-and-drop deployment of the published `wwwroot` folder works well. Add `wwwroot/_redirects` containing `/* /index.html 200`. For CI builds, Netlify's build image does not include .NET 10 by default, so build in GitHub Actions and deploy the output with the Netlify CLI.

### 8.4 Cost summary

| Item | Cost |
| --- | --- |
| GitHub Pages / Cloudflare Pages / Netlify free tier | $0 |
| GitHub Actions minutes (public repo) | $0 |
| Domain (optional) | $0 using `*.github.io` / `*.pages.dev` |
| Backend, database, auth | None needed |

A public repo is free on all three. Private-repo Pages support depends on your GitHub plan, so confirm before relying on it. Since student data never enters the repo, a public repo is fine as long as you **never commit real student CSVs**; add `*.csv` to `.gitignore` except `wwwroot/sample/`.

## 9. Suggested Build Order

1. **Scaffold:** create the solution from section 5.0 (`SeatingChart.Core`, `SeatingChart.Client`, `SeatingChart.Core.Tests`), `global.json`, and a CI pipeline that deploys the empty template. This proves hosting early.
2. **Core and tests first:** models, `CsvImporter` (CsvHelper), `AdjacencyBuilder`, `ConflictDetector`, `ImportMerger`, each with xUnit tests before any UI exists.
3. **Persistence:** `storage.js`, `LocalStorageService`, `AppStateService`, backup export/import.
4. **Students page:** CSV upload, preview, warnings, sample data.
5. **Conflicts page:** manual add/remove plus CSV merge.
6. **Grid chart:** `GridCanvas`, assignment by click and drag, conflict highlighting, `ViolationSummary`.
7. **Table layouts:** `TableCanvas` with rectangle and round tables, seat counts per table, adjacency rules.
8. **Layout editor:** custom tables, disabled seats, save custom presets.
9. **Polish:** auto-seater, print stylesheet, undo/redo (a simple snapshot stack of `Assignments`), accessibility (keyboard seat selection, ARIA labels), mobile layout.

## 10. Testing, Performance and Risks

- **Unit tests** (xUnit): CSV import through CsvHelper (quoted fields, BOM, header case, missing columns), symmetric dedupe, unknown IDs, corner/edge grid adjacency (a corner seat has 3 neighbors with diagonals, 2 without), ring wraparound for round tables, and the swap-on-drop logic.
- **Download size:** the first load is a few MB (the .NET runtime). Brotli precompression is on by default at publish, and every later load is cached. Keep `InvariantGlobalization` on and avoid heavy packages (CsvHelper is the one deliberate exception; compare the published size before and after adding it).
- **Loading UX:** customize the `#app` loading placeholder in `index.html` so users see a spinner instead of a blank page.
- **Risks:** (a) data loss from clearing browser storage → backup UX; (b) the base-href mismatch on GitHub Pages → the `sed` step; (c) drag-and-drop on touch devices → click-to-select fallback; (d) CsvHelper under IL trimming → explicit ClassMaps, TrimmerRootAssembly for Core and one Release-publish import test in the browser; (e) student privacy → local-only storage, no analytics, no committed data.
- **Future (still free):** multiple saved classes with a class switcher, shareable chart via URL-compressed JSON, PWA offline support (add the `service-worker.published.js` from the PWA template), and PDF export via the browser's print dialog.
