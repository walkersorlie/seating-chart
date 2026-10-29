# Blazor WASM Seating Chart: Phase 4 - Students Management

## Weeks 3-4 Deliverable: Full Students Page Working

### Task 4.1: Create CsvUploader Component

Create `src/SeatingChart.Client/Components/CsvUploader.razor`:
```razor
<input type="file" Accept=".csv" OnChange="@OnFileSelected" />

@code {
    private ImportResult? ParseResult;
    private async Task OnFileSelected(InputFileEventArgs e) => 
        ParseResult = await _importService.ReadStudentsAsync(e.File);

    // Display preview table with:
    // - Row count
    // - Warnings list (unknown IDs, missing names)  
    // - Errors list (duplicates, malformed rows)
}
```

### Task 4.2: Create StudentList Component

Create `src/SeatingChart.Client/Components/StudentList.razor`:
```razor
<Table IsCompact="true">
  <THEAD><TR>@("Name|Grade");</TR></THEAD>
  <TBODY>
    @foreach (var s in State.Data.Students.OrderBy(x => x.LastName)) {
      <TR>
        <TD><Button OnClick="@() => ClickStudent(s)">@(s.DisplayName)</Button></TD>
      </TR>
    }
  </TBODY>
</Table>

@code {
    [Parameter] Guid? SelectedId { get; set; }
    private void ClickStudent(Student s) => 
        State.HasFocus = (State.SelectedId ?? Guid.Empty) == s.Id;
}
```

### Task 4.3: Wire CSV Import Service in Client

Create `src/SeatingChart.Client/Services/CsvImportService.cs`:
```csharp
public class CsvImportService(AppStateService state)
{
    private const long MaxBytes = 2_000_000;  // 2 MB limit for input

    public async Task<ImportResult> ReadStudentsAsync(IBrowserFile file) => 
        await using var stream = file.OpenReadStream(MaxBytes);
    using var reader = new StreamReader(stream); // detects UTF-8 BOM
    return CsvImporter.ImportStudents(reader);

    // Same pattern for conflicts CSV
}
```

### Task 4.4: Display Warnings During Import

The `CsvUploader` component should show:
1. **Warnings section** - Unknown IDs, empty name rows, optional field missing  
2. **Errors section** - Duplicate StudentIds, malformed header rows  
3. **Counts summary** - Students imported, conflicts parsed, warnings count, errors count

### Task 4.5: Add Sample Data Button to Home Page

Modify `src/SeatingChart.Client/Pages/Home.razor`:
- Add a button that triggers _importService.ReadStudentsAsync(sampleCsvContent)  
- Use embedded base64-encoded students-sample.csv or load from wwwroot/sample/students-sample.csv

### Task 4.6: Implement Download Template CSV Feature

Reuse the existing downloadText JS function from phase 3 and create a service method that returns CSV template content in pre-formatted string, then use it to generate a downloadable file with .csv extension and BOM for Excel compatibility.

### Task 4.7: Wire Replacement/Merge UI

The `CsvUploader` component should present two actions after parsing succeeds:
1. **Replace all** - Discards existing state.AppData.Students/Conflicts, uses only the import results  
2. **Merge by StudentId (case-insensitive)** - Matches existing records by ExternalId, keeps existing GUIDs, remaps assignments and manual conflicts

"Apply" button calls `AppStateService.ApplyImport(studentsInput, conflictsInput, mergeMode)` which invokes `ImportMerger.Merge()`.

---

## Phase 4 Completion Criteria

Students page must support:
1. Upload students CSV -> shows preview table with warnings/errors  
2. "Download template" button available  
3. Sample data loads from file or embedded base64  
4. Import results displayed before confirmation (row count, name counts)  
5. Warning messages shown for unknown external IDs and empty names  
6. Error messages shown for duplicates and malformed header rows
