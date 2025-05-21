using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class SequenceValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_SequenceMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Sequence"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Sequence",
            "Sequence is missing",
            ErrorCodes.MissingSequence
        );
    }

    [Theory]
    [InlineData("1")]       // Missing leading zeroes
    [InlineData("000000")]  // Zero is invalid
    [InlineData("1000000")] // Too large
    [InlineData("")]        // Blank
    [InlineData("asdf")]    // NaN
    public void Validate_SequenceInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Sequence"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Sequence",
            "Sequence is in an invalid format",
            ErrorCodes.InvalidSequence
            );
    }

    [Theory]
    [InlineData("000001")]
    [InlineData("999999")]
    public void Validate_SequenceValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Sequence"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
