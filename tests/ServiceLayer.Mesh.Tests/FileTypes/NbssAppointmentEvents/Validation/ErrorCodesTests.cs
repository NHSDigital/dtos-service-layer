using System.Reflection;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ErrorCodesTests
{
    [Fact]
    public void AllErrorCodes_ShouldBeUnique()
    {
        var duplicates = GetErrorCodes()
            .GroupBy(kvp => kvp.Value)
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate error code values found: {string.Join(", ", duplicates.Select(g => g.Key))}");
    }

    [Fact]
    public void AllErrorCodes_ShouldMatchExpectedFormat()
    {
        foreach (var kvp in GetErrorCodes())
        {
            Assert.Matches(@"^NBSSAPPT\d{3}$", kvp.Value);
        }
    }

    private static Dictionary<string, string> GetErrorCodes()
    {
        return typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(
                f => f.Name,
                f => f.GetValue(null)?.ToString() ?? string.Empty);
    }
}
