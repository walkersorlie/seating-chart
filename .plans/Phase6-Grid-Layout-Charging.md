# Blazor WASM Seating Chart: Phase 6 - Grid Layout Charting

## Week 5 Deliverable: Main Grid Canvas with Assignment & Detection Working

### Task 6.1: Create SeatView Razor Component

Create `src/SeatingChart.Client/Components/SeatView.razor`:
```razor
@* Renders a single seat cell *@
<Div Class="@getClass()" ondrop=@@(Drop) ondragenter=@(DropEnter) ondragleave=@(DropLeave)>
  @if (student != null) {
    <H5 class="m-0">@student.DisplayName</H5>
    @if (isConflict && reason != null) {
      <small>@reason</small>
    }
    @if (@onClick) { <OnInput OnClick="@onclick" /> ; }
  } else if (@seatId == null) { <span class="text-muted">Empty</span >}
</Div>

@code {
    [Parameter] Seat Seat { get; set;}
    [Parameter] Student? Student {get;set;}
    [Parameter] bool IsConflict {get;set;}
    [Parameter] string? reason {get;set;}
    [Parameter] Func<string, Task<bool>> onDragEnter {get;set;}
    // And so on...

    private string getclass() => 
      "seat d-flex align-items-center justify-content-center align-middle border rounded @((isConflict ? conflict : String.Empty)"");
}
```

### Task 6.2: Build GridCanvas with CSS Grid Layout

Create `src/SeatingChart.Client/Components/GridCanvas.razor`:
```razor
<Div Class="grid-container">
  <template For="@seats" @rendermode=RenderMode.Unspecified>
    <SeatView seat=@seat Item="@((adjacency, seat) => seat)" OnDrop=@(drop) />
  </template>
</Div>

@code {
    private List<seat> seats;
    [Parameter] LayoutDefinition Layout { get;set; }
    [Parameter] SeatingChart? Chart { get; set; }
    
    protected override void OnInitialized() => 
      (seats, adjacency) = AdjacencyBuilder.Build(Layout);

    private async Task<bool> drop(string seatId) => 
        await onChart.Assign(seatId); // Called from chart.razor
}
```

### Task 6.3: Wire Drop-to-Seat Assignment Logic

In `src/SeatingChart.Client/Pages/Chart.razor`:
```razor
<Div Class="@((chart.Layout == LayoutKind.Grid ? "canvas-grid" : String.Empty)") @* or vice versa *@">
  <h2>@(Chart.Name)</h2>
  <GridCanvas seats=@seats adjacency=@adjacency layout=@State.Data.GetLayout(ActiveChartId) />
  <Div @ref="@unassignedList">
    <UnassignedStudentList onDrop=@(drop) onSelect=@(select) />
  </Div>
  <ViolationSummary Violations=@violations />

  @code {
      private SeatingChart chart = new();
      private Set<violation> violations; // Recompute in OnParametersSet or after state changes
      private async Task<bool> drop(string seatId) => 
        await Seating.AssignOrSwap(seatId, draggedStudentId);
      
      private void Select(Student student, Guid id) => 
        Chart.void(SetSelection(id));
  }
```

### Task 6.4: Add Click-to-Select Fallback for Touch Devices

The `SeatView` component should handle both drag-and-drop and click events:
1. If dropped by user → swap or assign as appropriate  
2. If clicked (especially on touch devices) → open selection modal or directly assign  
3. Use native HTML5 drag-and-drop events where available, with click fallback  

### Task 6.5: Implement Conflict Highlighting in SeatView

Modify `SeatView` to accept `isConflict` and `reason` parameters:
```razor
@code {
    private bool getIsConflict() => 
        violations.Any(v => v.SeatIdA == this.seat.Id || v.SeatIdB == this.seat.Id);
}
```

Apply CSS rule when a seat is involved in a conflict (red border with pulse animation):
```css
.seat.conflict {
  border: 2px solid #c62828; background: #ffebee; animation: pulse 1.2s infinite;
}
.seat::after { content: "⚠"; position: absolute; top: 2px; right: 4px; }
```

### Task 6.6: Create ViolationSummary Component

Create `src/SeatingChart.Client/Components/ViolationSummary.razor`:
```razor
<Div @ref="@violationContainer" Class="violation-summary">
  <h3>Violations (@violations.Count)</h3>
  <Ul>
    @foreach (var v in violations) {
      <Li @onclick ="() => ScrollToSeats(v)">
        @v.studentA.DisplayName | @(v.StudentB.DisplayName) - @v.Reason
      </LI>
    }
  </Ul>
</Div>

@code {
  [Parameter] Set<violation> Violations { get; set;}
}
```

### Task 6.7: Compute Violations Before Rendering

In `Chart.razor` or parent component, compute violations before rendering the grid:
```csharp
var violations = ConflictDetector.Detect(chart, adjacency, State.Data.Conflicts);
```

---

## Phase 6 Completion Criteria

Main grid chart must support:
1. Render CSS Grid layout with SeatView per cell  
2. Skip disabled seats if configuration includes them  
3. Drag-and-drop student → seat assignment (swap on occupied target)  
4. Click fallback for touch devices when drag doesn't work  
5. Conflict-highlighted seats displayed with red border + icon  
6. Violation list shows count + allows scrolling to affected seats  
7. Unassigned students list ready for assignment
