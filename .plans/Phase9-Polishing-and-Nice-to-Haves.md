# Blazor WASM Seating Chart: Phase 9 - Polishing & Nice-to-Haves

## Week 7-8 Deliverable: Auto-Seater, Print Export, Accessibility

### Task 9.1: Implement AutoSeater Hill-Climbing Logic

Create `AutoSeater.cs` in `src/SeatingChart.Core/Seating/`:
```csharp
public static class AutoSeater
{
    public static SeatingChart Optimize(SeatingChart chart, List<Student> unassigned) =>
        HillClimb(chart, unassigned);

    private static int EvaluateViolationCount(SeatingChart chart, IEnumerable<Conflict> conflicts, Dictionary<string, HashSet<string>> adjacency) =>
        ConflictDetector.Detect(chart, adjacency).Count + /* other metrics */;

    // Randomly place students, then iteratively swap seats that reduce violation count.
    // Restart a few times with different random seeds. Stop at zero violations or ~2000 iterations.
}
```

### Task 9.2: Add Undo/Redo via Snapshot Stack

Create simple undo stack of `Assignments` dictionaries in `AppStateService` (phase 3). Each mutation pushes before the change, pop on undo button click. For a lightweight undo, just store last N states instead of all mutations.

### Task 9.3: Create Print Stylesheet

Add to `wwwroot/css/print.css`:
```css
@media print {
  @media print {
    body { background: white; }
    .seat.conflict { border-color: black; }
    @hide: none;
    /* Hide interactive elements like nav menus, button icons */
    .no-print { display: none }
  }
}
```

### Task 9.4: Export Seating Chart as CSV

Use CsvHelper's `CsvWriter` from phase 2 logic to write seat assignments to a CSV file with columns (Seat Label - e.g., "r4c1", Student Name). This helps generate records for attendance or reports.

### Task 9.5: Accessibility Improvements

Add ARIA attributes to seats (`role="button" aria-label="@student.DisplayName ? student.DisplayName : @seat.Id)"`). Support keyboard navigation (tab order through unassigned students, enter to select, escape to close modal). Ensure contrast ratios meet WCAG standards.

### Task 9.6: Mobile-Responsive Layout Adjustments

Use a viewport meta tag: `meta name="viewport" content="width=device-width, initial-scale=1.0">`. Make SeatView and TableView responsive with CSS `@media (@max-width: 768px)` rules that switch from grid to single-column layout or allow horizontal scrolling.
