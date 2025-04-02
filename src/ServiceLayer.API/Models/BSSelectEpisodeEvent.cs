using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.API.Models;

public class BSSelectEpisodeEvent
{
    [JsonPropertyName("nhs_number")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "nhs_number must be exactly 10 digits.")]
    public required string NhsNumber { get; set; }

    [JsonPropertyName("date_of_birth")]
    public required DateOnly DateOfBirth { get; set; }

    [JsonPropertyName("first_given_name")]
    public required string FirstGivenName { get; set; }

    [JsonPropertyName("family_name")]
    public required string FamilyName { get; set; }
}
