namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public static class ValidationErrorAssertions
{
    public static ValidationError ShouldBeSingleValidationError(
        this IEnumerable<ValidationError> errors,
        string expectedField,
        string expectedError,
        string expectedCode,
        int? expectedRowNumber = null)
    {
        var error = Assert.Single(errors);
        Assert.Equal(expectedField, error.Field);
        Assert.Equal(expectedError, error.Error);

        Assert.Equal(expectedCode, error.Code);

        if (expectedRowNumber != null)
        {
            Assert.Equal(expectedRowNumber, error.RowNumber);
        }

        return error;
    }
}
