namespace SeatingChartApp.Core.Models;

public class TableDef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public TableShape Shape { get; set; }  // Rectangle, Round, Square, Custom
    public int SeatCount { get; set; } = 4;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 160;
    public double Height { get; set; } = 80;
    public double Rotation { get; set; }
}

public enum TableShape { Rectangle, Round, Square, Custom }
