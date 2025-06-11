using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class HeaderPresenceValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_HeaderMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            null,
            "Header is missing",
            ErrorCodes.MissingHeader,
            ValidationErrorScope.File
        );
    }
}
