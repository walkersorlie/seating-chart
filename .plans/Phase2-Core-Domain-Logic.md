# Blazor WASM Seating Chart: Phase 2 - Core Domain Logic

## Week 2 Deliverable: In-Memory Model & CSV Import Working

### Task 2.1: Implement Domain Models in SeatingChart.Core/Models/

Create the following record classes with public properties for JSON serialization:

- **Student.cs**
```csharp
public record Student(Guid Id, string ExternalId, string FirstName, string LastName,
                      string? Grade = null, string? Notes = null)
{ 
    public string DisplayName => $"{FirstName} {LastName}".Trim(); 
}
```

- **Conflict.cs**
```csharp
public record Conflict(Guid StudentAId, Guid StudentBId, string? Reason = null)
{   
    public bool Involves(Guid a, Guid b) =>
        (StudentAId == a && StudentBId == b) || 
        (StudentAId == b && StudentBId == a);
}
```

- **Seat.cs**
```csharp
public record Seat(string Id, string? TableId, int Index, double X, double Y);
```

- **TableDef.cs**
```csharp
public class TableDef
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Table";
    public TableShape Shape { get; set; }  // Rectangle, Round, Square, Custom
    public int SeatCount { get; set; } = 4;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 160;
    public double Height { get; set; } = 80;
    public double Rotation { get; set; }
}

public enum TableShape { Rectangle, Round, Square, Custom }
```

- **LayoutDefinition.cs**
```csharp
public class LayoutDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public LayoutKind Kind { get; set; }  // Grid or Tables
    public int Rows { get; set; } = 5;
    public int Cols { get; set; } = 6;
    public AdjacencyRule Rule { get; set; } = AdjacencyRule.IncludeDiagonals;
    public List<TableDef> Tables { get; set; } = new();
}

public enum LayoutKind { Grid, Tables }
public enum AdjacencyRule { SideBySide, IncludeDiagonals, SameTable, RingNeighbors }
```

- **SeatingChart.cs**
```csharp
public class SeatingChart
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Period 1";
    public string LayoutId { get; set; } = "";
    public Dictionary<string, Guid> Assignments { get; set; } = new(); // seatId -> studentId
}
```

- **AppData.cs**
```csharp
public class AppData
{
    public int SchemaVersion { get; set; } = 1;
    public List<Student> Students { get; set; } = new();
    public List<Conflict> Conflicts { get; set; } = new();
    public List<LayoutDefinition> Layouts { get; set; } = new();
    public List<SeatingChart> Charts { get; set; } = new();
    public string? ActiveChartId { get; set; }
}
```

### Task 2.2: Build CsvImporter with ClassMaps

Create folder `src/SeatingChart.Core/Csv/`:

- **CsvRows.cs** - Define row DTOs for CsvHelper mapping
- **StudentRowMap.cs** - Class map for student rows (all Optional fields)  
- **ConflictRowMap.cs** - Class map for conflict rows
- **ImportResult.cs** - Container with Students, Conflicts, Warnings, Errors lists
- **CsvImporter.cs** - Main CSV parsing logic using CsvHelper

### Task 2.3: Implement Logic for Adjacency Building

Create folder `src/SeatingChart.Core/Layouts/`:

- **AdjacencyBuilder.cs**:
```csharp
public static class AdjacencyBuilder
{
    public static (List<Seat> Seats, Dictionary<string, HashSet<string>> Adj) Build(LayoutDefinition l)
    {
        if (l.Kind == LayoutKind.Grid) return BuildGrid(l);
        else return BuildTables(l);
    }

    // Grid layout: build seats for each row x col
    // Table layout: build perimeter seats around table shapes
    
    static (List<Seat>, Dictionary<string, HashSet<string>>) BuildGrid(LayoutDefinition l) { /* ... */ }
    static (List<Seat>, Dictionary<string, HashSet<string>>) BuildTables(LayoutDefinition l) { /* ... */ }
}
```

### Task 2.4: Implement Conflict Detection Logic

- **ConflictDetector.cs** in `src/SeatingChart.Core/Conflicts/`:
```csharp
public record Violation(string SeatIdA, string SeatIdB, Guid StudentA, Guid StudentB, string? Reason);

public static class ConflictDetector
{
    public static List<Violation> Detect(SeatingChart chart,
        Dictionary<string, HashSet<string>> adj, IEnumerable<Conflict> conflicts) { /* ... */ }
}
```

### Task 2.5: Create ImportMerger for Merge Mode

- **ImportMerger.cs** in `src/SeatingChart.Core/Csv/`:
```csharp
public static class ImportMerger
{
    public static AppData Merge(AppData existing, ImportResult students, ImportResult conflicts)
    {
        // Existing students keep their GUIDs
        // Match incoming by StudentId (case-insensitive)  
        // Preserve existing assignments and manual conflicts
        // Remap new ones to existing record GUIDs when possible
    }
}
```

### Task 2.6: Write Unit Tests for All Core Services

Create folder `tests/SeatingChart.Core.Tests/` with test classes:
- **Csv/ImportMergerTests.cs** - Test student import, conflict import, merge scenarios
- **Csv/CsvImporterTests.cs** - Test quoted fields, BOM handling, case-insensitive headers, missing columns
- **Layouts/AdjacencyBuilderTests.cs** - Test corner seats (3 neighbors with diag, 2 without), edge cases
- **Conflicts/ConflictDetectorTests.cs** - Test violation detection for known conflicts in adj map
- **Serialization/AppDataSerializerTests.cs** - Test round-trip JSON serialize/deserialize

---

## Phase 2 Completion Criteria

All Core classes must:
1. Build from scratch with `dotnet build`
2. Pass all unit tests via `dotnet test`
3. Have no UI dependencies or JS interop

No Blazor, no Razor pages allowed in SeatingChart.Core. Pure domain logic with CSVHelper dependency only.
