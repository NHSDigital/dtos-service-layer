using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class EpisodeStartValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_EpisodeStartMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Episode Start"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Episode Start",
            "Episode Start is missing",
            ErrorCodes.MissingEpisodeStart
        );
    }

    [Theory]
    [InlineData("20250631")]              // too many days in June
    [InlineData("202S0630")]              // invalid character
    [InlineData("202506")]                // too short
    [InlineData("30062025")]              // ddMMyyyy and not valid as yyyyMMdd
    [InlineData("250630")]                // too short, ddMMyy
    [InlineData("20250630-145621")]       // Includes time
    [InlineData("20250229")]              // Not a leap year
    public void Validate_EpisodeStartInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Episode Start"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Episode Start",
            "Episode Start is in an invalid format",
            ErrorCodes.InvalidEpisodeStart
            );
    }

    [Theory]
    [InlineData("20250101")]
    [InlineData("20250228")]
    [InlineData("20250331")]
    [InlineData("20251231")]
    [InlineData("20240229")]
    [InlineData("19990331")]
    public void Validate_EpisodeStartValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Episode Start"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
