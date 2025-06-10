using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class TrailerPresenceValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_TrailerMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileTrailer = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            null,
            "Trailer is missing",
            ErrorCodes.MissingTrailer,
            ValidationErrorScope.File
        );
    }
}
