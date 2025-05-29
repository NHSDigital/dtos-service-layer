using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class DateFormatValidatorTests
{
    private const string FieldName = "TestField";
    private const string MissingCode = "ERR001";
    private const string InvalidFormatCode = "ERR002";
    private const string Format = "yyyyMMdd";

    [Fact]
    public void Validate_NullValue_ShouldReturnMissingError()
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 1
        };
        record.Fields.Clear();

        var validator = new DateFormatValidator(FieldName, Format, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is missing", MissingCode, 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("20250631")]
    public void Validate_ValueNotMatchingPattern_ShouldReturnInvalidFormatError(string invalidValue)
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 2
        };
        record.Fields.Add(FieldName, invalidValue);

        var validator = new DateFormatValidator(FieldName, Format, MissingCode, InvalidFormatCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"{FieldName} is in an invalid format", InvalidFormatCode, 2);
    }

    [Theory]
    [InlineData("20250630")]
    [InlineData("19990807")]
    public void Validate_ValueMatchingPattern_ShouldReturnNoErrors(string validValue)
    {
        var record = new FileDataRecord
        {
            RowNumber = 3
        };
        record.Fields.Add(FieldName, validValue);

        var validator = new DateFormatValidator(FieldName, Format, MissingCode, InvalidFormatCode);

        var errors = validator.Validate(record).ToList();

        Assert.Empty(errors);
    }
}
