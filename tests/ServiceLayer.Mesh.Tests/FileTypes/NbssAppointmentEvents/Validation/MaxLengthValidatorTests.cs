using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class MaxLengthValidatorTests
{
    private const string FieldName = "TestField";
    private const string MissingCode = "ERR100";
    private const string TooLongCode = "ERR101";

    [Theory]
    [InlineData(false, "TestField is missing or empty")]
    [InlineData(true, "TestField is missing")]
    public void Validate_NullValue_ShouldReturnMissingError(bool allowEmpty, string expectedError)
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 1
        };
        record.Fields.Clear();

        var validator = new MaxLengthValidator(FieldName, 5, MissingCode, TooLongCode, allowEmpty);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, expectedError, MissingCode, 1);
    }

    [Fact]
    public void Validate_EmptyValueDisallowed_ShouldReturnMissingError()
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 2
        };
        record.Fields.Add(FieldName, "");

        var validator = new MaxLengthValidator(FieldName, 5, MissingCode, TooLongCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, "TestField is missing or empty", MissingCode, 2);
    }

    [Fact]
    public void Validate_EmptyValueAllowed_ShouldReturnNoErrors()
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 3
        };
        record.Fields.Add(FieldName, "");

        var validator = new MaxLengthValidator(FieldName, 5, MissingCode, TooLongCode, true);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(5, "123456")]
    [InlineData(7, "12345678")]
    public void Validate_ValueExceedingMaxLength_ShouldReturnTooLongError(int maxLength, string tooLongValue)
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 4
        };
        record.Fields.Add(FieldName, tooLongValue);

        var validator = new MaxLengthValidator(FieldName, maxLength, MissingCode, TooLongCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        errors.ShouldContainValidationError(FieldName, $"TestField exceeds maximum length of {maxLength}", TooLongCode, 4);
    }

    [Theory]
    [InlineData(5, "123")]
    [InlineData(6, "123456")]
    [InlineData(7, "123456")]
    public void Validate_ValueWithinMaxLength_ShouldReturnNoErrors(int maxLength, string validValue)
    {
        // Arrange
        var record = new FileDataRecord
        {
            RowNumber = 5
        };
        record.Fields.Add(FieldName, validValue);

        var validator = new MaxLengthValidator(FieldName, maxLength, MissingCode, TooLongCode);

        // Act
        var errors = validator.Validate(record).ToList();

        // Assert
        Assert.Empty(errors);
    }
}
