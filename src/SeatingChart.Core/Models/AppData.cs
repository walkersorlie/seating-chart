namespace SeatingChartApp.Core.Models;

public class AppData
{
    public int SchemaVersion { get; set; } = 1;
    public IList<Student> Students { get; set; } = [];
    public IList<Conflict> Conflicts { get; set; } = [];
    public IList<LayoutDefinition> Layouts { get; set; } = [];
    public IList<SeatingChart> Charts { get; set; } = [];
    public string? ActiveChartId { get; set; }
}
