using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class RegexValidatorTests
{
    private const string FieldName = "TestField";
    private const string MissingCode = "ERR001";
    private const string InvalidFormatCode = "ERR002";
    private readonly Regex _pattern = new(@"^[A-Z]{2}\d{2}$", RegexOptions.Compiled);

    [Fact]
    public void Validate_NullValue_ShouldReturnMissingError()
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 1
        };
        record.Fields.Clear();

        var validator = new RegexValidator(FieldName, _pattern, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is missing", MissingCode, 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Validate_ValueNotMatchingPattern_ShouldReturnInvalidFormatError(string invalidValue)
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 2
        };
        record.Fields.Add(FieldName, invalidValue);

        var validator = new RegexValidator(FieldName, _pattern, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is in an invalid format", InvalidFormatCode, 2);
    }

    [Theory]
    [InlineData("AB12")]
    [InlineData("CD34")]
    public void Validate_ValueMatchingPattern_ShouldReturnNoErrors(string validValue)
    {
        var record = new FileDataRecord
        {
            RowNumber = 3
        };
        record.Fields.Add(FieldName, validValue);

        var validator = new RegexValidator(FieldName, _pattern, MissingCode, InvalidFormatCode);

        var errors = validator.Validate(record).ToList();

        Assert.Empty(errors);
    }
}
