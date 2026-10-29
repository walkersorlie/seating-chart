# Blazor WASM Seating Chart: Phase 8 - Layout Editor & Presets

## Weeks 6-7 Deliverable: Custom Layouts with Save Functionality

### Task 8.1: Create LayoutEditor Component

Create `src/SeatingChart.Client/Components/LayoutEditor.razor`:
```razor
<Div Title="Layout Editor">
  <h3>Create or Edit Layout</h3>
  
  <h5>Grid Mode</h5>
  <Row>
    @("Rows"): <input type="number" bind-value="@gridRows" min="1" max="20" />
    @("Cols"): <input type="number" bind-value="@gridCols" min="1" max="20" />
    <Select bind-value="@gridAdjacencyRule">
      <option value="SideBySide">@AdjacentRules.SideBySide</option>
      <option value="IncludeDiagonals">@AdjacentRules.IncludeDiags</option>
    </Select>
  </Row>

  <h5>Table Mode</h5>
  <Row>
    @("Count"): <input type="number" bind-value="@seatCount" min="2" max="16" />
    <Select bind-value="@tableShape">
      <option value="Rectangle">@Shapes.Rectangle</option>
      <option value="Round">@Shapes.Round</option>
    </Select>
  </Row>

  <h5>Disabled Seats (Grid)</h5>
  <Table>
    @for row from 0 to @gridRows {
      <TR>
        @for col from 0 to @gridCols {
          <TD bind-bool="@disabledSeats.Contains(@($"r{row}c{col}\")">
            <input type="checkbox" onunchecked="() => DisableSeat($"r{row}c{col}")@unchecked />
            @(seat ? "X" : "@")
          </TD>
        }
      </TR>
    }
  </Table>

  <Button Title="Save as new layout">Save</Button>
  <Button @onclick="@LoadPreset">Load Preset...</Button>
  
  <h5>Saved Layouts</h5>
  <Select Bind-value="@ActiveLayoutId" Onchange ="@ApplySelected">
    @foreach (var layout in State.Data.Layouts.OrderBy(l => l.Name)) {
      <option bind:value="@layout.Id">@layout.Name ?: $"{Layout.Id} (@* no name *)"</option>
    }
  </Select>

  @code {
    private GridDefinition gridDef = new(1, 6);
    private TableShape tableShape;
    private Set<string> disabledSeats = new(); // For gap/aisle support in grids
    
    private async Task DisableSeat(string seatId) => 
      enabledSeats.Add(seatId);

    private void ApplySelected() => 
      (layout, gridDefinition: gridDef, adjacentRuleEnabled: adjacencyRule) = 
        LayoutPresets.All().FirstOrDefault(l => l.Id == @selectedLayoutId);
  }
```
