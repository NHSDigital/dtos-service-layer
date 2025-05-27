using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class BatchIdValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_BatchIdMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Batch ID"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Batch ID",
            "Batch ID is missing or empty",
            ErrorCodes.MissingBatchId
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BatchIdBlank_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Batch ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Batch ID",
            "Batch ID is missing or empty",
            ErrorCodes.MissingBatchId
        );
    }

    [Theory]
    [InlineData("1234567890")]  // 10 characters
    [InlineData("12345678901")] // 11 characters
    public void Validate_BatchIdTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Batch ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Batch ID",
            "Batch ID exceeds maximum length of 9",
            ErrorCodes.InvalidBatchId
        );
    }

    [Theory]
    [InlineData("KMKT00001")]
    [InlineData("KMKT001")]
    public void Validate_BatchIdValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Batch ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        Assert.Empty(validationErrors);
    }
}
