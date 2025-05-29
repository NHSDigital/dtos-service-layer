namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public static class ValidationErrorAssertions
{
    public static void ShouldContainValidationError(
        this IEnumerable<ValidationError> errors,
        string expectedField,
        string expectedError,
        string expectedCode,
        ValidationErrorScope expectedScope = ValidationErrorScope.Record,
        int? expectedRowNumber = null)
    {
        var error = errors.FirstOrDefault(e =>
            e.Field == expectedField &&
            e.Error == expectedError &&
            e.Code == expectedCode &&
            (expectedRowNumber == null || e.RowNumber == expectedRowNumber)
        );

        Assert.True(error != null, $"Expected validation error with Scope: '{expectedScope}', Field: '{expectedField}', Error: '{expectedError}', Code: '{expectedCode}'{(expectedRowNumber != null ? $", RowNumber: {expectedRowNumber}" : "")}, but none was found.");

    }
}
