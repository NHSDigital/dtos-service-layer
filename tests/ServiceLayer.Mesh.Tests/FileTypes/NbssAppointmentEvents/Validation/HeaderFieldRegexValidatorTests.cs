using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public partial class HeaderFieldRegexValidatorTests
{
    private const string FieldName = "TestField";
    private const string MissingCode = "ERR001";
    private const string InvalidFormatCode = "ERR002";
    private readonly Regex _pattern = TestRegex();

    [Fact]
    public void Validate_NullValue_ShouldReturnMissingError()
    {
        // Arrange
        var file = TestDataBuilder.BuildValidParsedFile();
        file.FileHeader!.ExtractId = null;

        var validator = new HeaderFieldRegexValidator(x => x.ExtractId, FieldName, _pattern, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(file).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is missing", MissingCode, ValidationErrorScope.Header);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Validate_ValueNotMatchingPattern_ShouldReturnInvalidFormatError(string invalidValue)
    {
        // Arrange
        var file = TestDataBuilder.BuildValidParsedFile();
        file.FileHeader!.ExtractId = invalidValue;

        var validator = new HeaderFieldRegexValidator(x => x.ExtractId, FieldName, _pattern, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(file).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is in an invalid format", InvalidFormatCode,ValidationErrorScope.Header);
    }

    [Theory]
    [InlineData("AB12")]
    [InlineData("CD34")]
    public void Validate_ValueMatchingPattern_ShouldReturnNoErrors(string validValue)
    {
        // Arrange
        var file = TestDataBuilder.BuildValidParsedFile();
        file.FileHeader!.ExtractId = validValue;

        var validator = new HeaderFieldRegexValidator(x => x.ExtractId, FieldName, _pattern, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(file).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [GeneratedRegex(@"^[A-Z]{2}\d{2}$", RegexOptions.Compiled)]
    private static partial Regex TestRegex();
}
