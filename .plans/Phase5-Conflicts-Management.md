# Blazor WASM Seating Chart: Phase 5 - Conflicts Management

## Week 4 Deliverable: Independent Conflicts Page Working

### Task 5.1: Create ConflictPanel Component with Two Dropdowns

Create `src/SeatingChart.Client/Components/ConflictPanel.razor`:
```razor
<Select @bind-Value="@selectedStudentA" OnChange="@() => SelectStudent('A')">
  <option>Search...</option>
  @foreach (var s in State.Data.Students) {
    <option value="@s.Id">@s.DisplayName</option>
  }
</Select>

<Select @bind-Value="@selectedStudentB" OnChange="@() => SelectStudent('B')">
  <option>Search...</option>
  @foreach (var s in State.Data.Students) {
    <option value="@s.Id">@s.DisplayName</option>
  }
</Select>

<Label>@textReasonText</Label>
<input type="text" bind-Value="@reasonText" placeholder="Optional reason" />

<Button OnClick="@() => AddConflict()">Add Conflict</Button>

<span @bind="conflictList">
  @foreach (var c in State.Data.ConFLICTS) {
    @(c.studentA.DisplayName) |@(c.StudyB.DisplayName) -@c.Reason
    <Button OnClick="@() => RemoveConflict(c)">X</Button>
  }
</span>

@code {
    private Guid? selectedStudentA, selectedStudentB;
    private string textReasonText = @ "";
    
    private async Task SelectStudent(string which) => 
        // Build the key for whichever selector is active
    
    private void AddConflict() => 
        if (selectedStudentA != null && selectedStudentB != null && 
            (!State.Data.ConFlicts.Involves(studentA, studentB))) {
          State.AddConflict(selectedStudentA.Value, selectedStudentB.Value, @textReason);
        } else if (studentA == studentB) {
          // Self-pair warning
    
    private void RemoveConflict(Conflict c) => 
      State.RemoveConflict(c.StudentAId, c.StudyBId);
    
    private conflict? GetSelected() => 
      State.Data.CONFLICTS.Find(c => 
        ((c.StudentAId == StudentA && c.StudentBId == studentB) ||
         (c.StudentAId == SelectB && c.StudentId == selectB)));
}
```

### Task 5.2: Build CSV Importer for Conflicts File

Reuse the Core logic from Phase 2 and create a similar component `CsvUploaderConflicts.razor`:
- Same file input with type "file" and ".csv" filter
- Calls `await _importService.ReadConflictsAsync(file)` (same pattern as students import)  
- Shows same three-section preview: counts, warnings, errors (`ImportResult`)

### Task 5.3: Implement Merge for Conflicts CSV File

The conflicts file merge flow:
1. Read conflicts.csv with `CsvImporter.ImportConflicts(reader, existingStudents)`  
2. Unknown StudentIds become warnings (skipped but logged)  
3. Self-conflicts become warnings and are not added to the list  
4. Duplicates (A-B and B-A) collapse into one conflict  
5. Use `ImportMerger.Merge()` logic from Phase 2 but tailored only for conflicts

### Task 5.4: Prevent Duplicate Self-Conflict Submissions

The `AddConflict()` method should validate:
1. If either student is null/unknown, show warning + skip  
2. If both students have the same GUID (self-conflict), warn and skip  
3. Duplicates are collapsed via a `(guid, Guid)` ordered pairs dedup set

### Task 5.5: Display List of All Configured Conflicts

The conflict panel can show two views:
1. **Inline list** - Each row shows A | B - reason × remove-button  
2. **Collapsible section** - Toggle arrow expands to full table with columns (Student A, Student B, Severity/Reason, When Added)

### Task 5.6: Allow Both Manual and CSV Conflicts Input

The user workflow should be:
1. Upload students file → Apply import  
2. (Optional) Upload conflicts file → Apply merge  
3. Return to home screen  
4. From there, click "Edit Conflicts" link to open conflict panel  
5. Add conflicts manually via dropdowns OR upload another CSV later

---

## Phase 5 Completion Criteria

Conflicts page must support:
1. Two-dropdown picker with searchable student lists  
2. Text field for optional reason note  
3. Warning when students have the same GUID or either is unknown  
4. Duplicate pairs deduplicated via ordered pair set  
5. Import conflicts CSV and merge against existing state.AppData.Conflicts  
6. Warnings shown for skipped rows (unknown IDs, self-pairs)  
7. List of all configured conflicts visible on page
