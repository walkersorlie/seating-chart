# Blazor WASM Seating Chart: Phase 7 - Table Layouts

## Weeks 5-6 Deliverable: Canvas Supports Multiple Table Shapes

### Task 7.1: Create TableView Component

Create `src/SeatingChart.Client/Components/TableView.razor`:
```razor
<svg class="table-svg" viewBox="@viewBox">
  <g Transform="@transform">
    @if (shape == Shape.Rectangle) { <rect x="@x" y="@y" width="@width" height="@height" />; }
    @* Circle shape *@ else if (Shape.Round) { <circle cx="@cx" cy="@cy" r="@radius"/>; }
    @square: <rect> with special corners
  
    @foreach (var seat in seats) {
      <g Transform=@@translate(@seat.x, @seat.y)">
        <SeatView student=@student isConflict=@isConflict onclick=@onclick />
      </g>
    }
  </g>
</svg>

@code {
    [Parameter] Seat[] seats;
    [parameter] TableDef tableDef = new();
    private (double, double) GetCenter() => ((table.width / 2 + table.X), (table.height / 2 + table.y));
}
```

### Task 7.2: Build TableCanvas SVG Renderer

Create `src/SeatingChart.Client/Components/TableCanvas.razor`:
```razor
<svg Class="@((layout == LayoutKind.Table ? "canvas-table" : String.Empty) @* or vice versa *@)">
  <template For=@@layouts.tables">
    <TableView layout=@layout />
  </template>
</svg>

@code {
    private List<LayoutDefinition> layouts;
}
```

### Task 7.3: Implement Round Table Ring Adjacency

Update `AdjacencyBuilder.BuildTables()` in Core to handle ring neighbors:
```csharp
static (List<Seat>, Dictionary<string, HashSet<string>>) BuildTables(LayoutDefinition l)
{
    foreach (var tableDef in l.Tables) {
        if (tableDef.Shape == TableShape.Round) {
            int n = tableDef.SeatCount;
            for (int i = 0; i < n; i++) {
                double angle = 2.0 * Math.PI * i / n;
                seats.Add(new Seat($"t{@tableDef.Id}:{i}", tableDef.Id, i,
                  Radius: 50, radius: 50, Angle: angle));
            }
        } else if (Shape.Rectangle) {
          // perimeter seats along edges
        }
    }
}
```

### Task 7.4: Support Rectangular Tables with Perimeter Seats

For rectangular tables, place seats around the perimeter only:
```csharp
static (List<Seat>, Dictionary<string, HashSet<string>>) BuildTables(LayoutDefinition l)
{
    foreach (var tableDef in l.Tables) {
        if (tableDef.Shape == Shape.Rectangle) {
            int seatCount = tableDef.SeatCount;
            // Distribute seats along perimeter edges
            for each edge position in perimeterEdges(table.width, table.height, seatCount) {
              seats.Add(new Seat($"t{tableDef.Id}:{i}", tableDef.Id, i, ...));
            }
        } else if (Shape.Round) {
          // ring neighbors for round tables
        }
    }
}
```

### Task 7.5: Allow Custom Table Positions and Rotation

Add `X` and `Y` offsets in `TableDef`:
```csharp
public class TableDef {
  public double X { get; set;}
  public double Y { get; set;}
}
```

Tables can be dragged to arbitrary positions when in edit mode.

### Task 7.6: Configure Seat Counts Per Table

Include `SeatCount` property in `TableDef`:
```csharp
public class TableDef {
  public int SeatCount { get; set;} = 4;
}
```

---

## Phase 7 Completion Criteria

Table layouts must support:
1. Canvas switches from grid mode to table layout mode  
2. Round table uses ring adjacency (neighbors i±1)  
3. Rectangular table shows only perimeter seats  
4. Each table can have custom seat count  
5. Tables placed at arbitrary positions via x/y offsets  

