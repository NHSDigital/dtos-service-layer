using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class EpisodeTypeValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_EpisodeTypeMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Episode Type"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Episode Type",
            "Episode Type is missing",
            ErrorCodes.MissingEpisodeType
        );
    }

    [Theory]
    [InlineData("E")]       // invalid character
    [InlineData("I")]       // invalid character
    [InlineData("$")]       // invalid character
    [InlineData("f")]       // lowercase
    [InlineData("")]        // Blank
    [InlineData(" ")]       // Whitespace
    [InlineData("FG")]      // Too many characters
    [InlineData("RST")]     // Too many characters
    public void Validate_EpisodeTypeInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Episode Type"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Episode Type",
            "Episode Type is in an invalid format",
            ErrorCodes.InvalidEpisodeType
            );
    }

    [Theory]
    [InlineData("F")]
    [InlineData("G")]
    [InlineData("H")]
    [InlineData("N")]
    [InlineData("R")]
    [InlineData("S")]
    [InlineData("T")]
    public void Validate_EpisodeTypeValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Episode Type"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
