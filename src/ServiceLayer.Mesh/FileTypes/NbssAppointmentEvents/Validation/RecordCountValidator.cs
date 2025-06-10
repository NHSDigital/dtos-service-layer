using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public partial class RecordCountValidator() :
    HeaderFieldRegexValidator(h => h.RecordCount, "Record count", RecordCountRegex(), ErrorCodes.MissingRecordCount, ErrorCodes.InvalidRecordCount)
{
    [GeneratedRegex(@"^(?!000000)\d{6}$")]
    private static partial Regex RecordCountRegex();

    protected override IEnumerable<ValidationError> RunAdditionalChecks(ParsedFile file, string value, bool hasErrored)
    {
        if (file.FileTrailer != null && file.FileHeader!.RecordCount != file.FileTrailer.RecordCount)
        {
            yield return new ValidationError
            {
                Field = "Record count",
                Code = ErrorCodes.InconsistentRecordCount,
                Error = "Record count does not match value in header",
                Scope = ValidationErrorScope.Trailer
            };
        }
        else if (!hasErrored && file.DataRecords.Count != int.Parse(file.FileHeader!.RecordCount!))
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.UnexpectedRecordCount,
                Error = "Record count does not match value in header and trailer",
                Scope = ValidationErrorScope.File
            };
        }
    }
}
