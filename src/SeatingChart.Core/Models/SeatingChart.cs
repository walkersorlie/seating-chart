namespace SeatingChartApp.Core.Models;

public class SeatingChart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public Guid LayoutId { get; set; }
    public IDictionary<string, Guid>? Assignments { get; set; } // seatId -> studentId
}
