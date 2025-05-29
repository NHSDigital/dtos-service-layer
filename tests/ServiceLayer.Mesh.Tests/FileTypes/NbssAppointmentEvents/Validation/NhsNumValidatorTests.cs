using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class NhsNumValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_NhsNumMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("NHS Num"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "NHS Num",
            "NHS Num is missing",
            ErrorCodes.MissingNhsNum
        );
    }

    [Theory]
    [InlineData("308 407 5425")]       // we don't anticipate spaces
    [InlineData("857320211")]          // too few character
    [InlineData("90238807571")]        // Too many characters
    [InlineData("159278895S")]         // invalid characters
    public void Validate_NhsNumInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["NHS Num"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "NHS Num",
            "NHS Num is in an invalid format",
            ErrorCodes.InvalidNhsNum
        );
    }

    [Theory]
    [InlineData("3244700471")]
    [InlineData("7326012282")]
    [InlineData("6245827145")]
    [InlineData("4745895257")]
    public void Validate_NhsNumInvalidCheckDigit_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["NHS Num"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "NHS Num",
            "NHS Num has invalid check digit",
            ErrorCodes.InvalidNhsNumCheckDigit
        );
    }

    [Theory]
    [InlineData("4941273230")]
    [InlineData("6451357219")]
    [InlineData("3365582983")]
    [InlineData("8799244780")]
    public void Validate_NHSNumValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["NHS Num"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
