using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public partial class ExtractIdValidator() :
    HeaderFieldRegexValidator(h => h.ExtractId, "Extract ID", ExtractIdRegex(), ErrorCodes.MissingExtractId, ErrorCodes.InvalidExtractId)
{
    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex ExtractIdRegex();

    protected override IEnumerable<ValidationError> RunAdditionalChecks(ParsedFile file, string value, bool hasErrored)
    {
        if (file.FileTrailer != null && file.FileHeader!.ExtractId != file.FileTrailer.ExtractId)
        {
            yield return new ValidationError
            {
                Field = "Extract ID",
                Code = ErrorCodes.InconsistentExtractId,
                Error = "Extract ID does not match value in header",
                Scope = ValidationErrorScope.Trailer
            };
        }
    }
}
