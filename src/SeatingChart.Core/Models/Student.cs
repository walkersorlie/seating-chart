namespace SeatingChartApp.Core.Models;

public record Student(Guid Id, string ExternalId, string FirstName, string LastName,
                      string? Grade = null, string? Notes = null)
{ 
    public string DisplayName => $"{FirstName} {LastName}".Trim(); 
}
