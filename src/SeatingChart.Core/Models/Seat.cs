namespace SeatingChartApp.Core.Models;

public record Seat(Guid Id, Guid TableId, int Index, double X, double Y);
