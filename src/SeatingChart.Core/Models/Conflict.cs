namespace SeatingChartApp.Core.Models;

public record Conflict(Guid StudentAId, Guid StudentBId, string? Reason = null)
{   
    public bool Involves(Guid a, Guid b) =>
        (StudentAId == a && StudentBId == b) || 
        (StudentAId == b && StudentBId == a);
}
