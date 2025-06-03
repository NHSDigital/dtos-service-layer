using Moq;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public abstract class ValidationTestBase
{
    private readonly Mock<IValidationRunnerConfiguration> _configurationMock = new();

    protected readonly ValidationRunner SystemUnderTest;

    protected ValidationTestBase()
    {
        var recordValidators = ValidatorRegistry.GetAllRecordValidators();
        var fileValidators = ValidatorRegistry.GetAllFileValidators();

        _configurationMock.Setup(c => c.MaximumValidationErrors).Returns(100);

        SystemUnderTest = new ValidationRunner(_configurationMock.Object, fileValidators, recordValidators);
    }

    protected static ParsedFile ValidParsedFile =>
        TestDataBuilder.BuildValidParsedFile();

    protected static ParsedFile ParsedFileWithModifiedRecord(Action<FileDataRecord> mutate)
    {
        var file = TestDataBuilder.BuildValidParsedFile();
        mutate(file.DataRecords[0]);    // Modify the first record
        return file;
    }

    protected List<ValidationError> Validate(ParsedFile file){
        return SystemUnderTest.Validate(file).ToList();
    }
}
