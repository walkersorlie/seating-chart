namespace SeatingChartApp.Core.Models;

public class LayoutDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public LayoutKind Kind { get; set; }  // Grid or Tables
    public int Rows { get; set; } = 5;
    public int Cols { get; set; } = 6;
    public AdjacencyRule Rule { get; set; } = AdjacencyRule.IncludeDiagonals;
    public IList<TableDef> Tables { get; set; } = [];
}

public enum LayoutKind { Grid, Tables }
public enum AdjacencyRule { SideBySide, IncludeDiagonals, SameTable, RingNeighbors }
