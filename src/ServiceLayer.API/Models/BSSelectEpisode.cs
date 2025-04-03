using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.API.Models;

public class BSSelectEpisode
{
    [JsonPropertyName("episode_id")]
    [Required(ErrorMessage = "The episode_id is required")]
    public string? EpisodeId { get; set; }

    [JsonPropertyName("nhs_number")]
    [Required(ErrorMessage = "The nhs_number is required")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "The nhs_number must be exactly 10 digits")]
    public string? NhsNumber { get; set; }

    [JsonPropertyName("date_of_birth")]
    [Required(ErrorMessage = "The date_of_birth is required")]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("first_given_name")]
    [Required(ErrorMessage = "The first_given_name is required")]
    public string? FirstGivenName { get; set; }

    [JsonPropertyName("family_name")]
    [Required(ErrorMessage = "The family_name is required")]
    public string? FamilyName { get; set; }
}
