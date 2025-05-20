using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public abstract class ValidationTestBase
{
    protected readonly ValidationRunner SystemUnderTest;

    protected ValidationTestBase()
    {
        var recordValidators = ValidatorRegistry.GetAllRecordValidators();
        var fileValidators = ValidatorRegistry.GetAllFileValidators();

        SystemUnderTest = new ValidationRunner(fileValidators, recordValidators);
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
