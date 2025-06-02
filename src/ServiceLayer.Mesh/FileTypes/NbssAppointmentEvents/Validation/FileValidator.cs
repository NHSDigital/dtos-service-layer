using System.Text.RegularExpressions;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public partial class FileValidator : IFileValidator
{
    private readonly HeaderFieldRegexValidator _headerExtractIdValidator = new(
        x => x.ExtractId, "Extract ID", ExtractIdRegex(),
        ErrorCodes.MissingExtractId, ErrorCodes.InvalidExtractId);

    private readonly HeaderFieldRegexValidator _headerIdRecordCountValidator = new(
        x => x.RecordCount, "Record count", RecordCountRegex(),
        ErrorCodes.MissingRecordCount, ErrorCodes.InvalidRecordCount);

    public IEnumerable<ValidationError> Validate(ParsedFile file)
    {
        foreach (var error in ValidateHeaderPresence(file))
        {
            yield return error;
        }

        foreach (var error in ValidateTrailerPresence(file))
        {
            yield return error;
        }

        foreach (var error in ValidateExtractId(file))
        {
            yield return error;
        }

        foreach (var error in ValidateRecordCount(file))
        {
            yield return error;
        }
    }

    private static IEnumerable<ValidationError> ValidateHeaderPresence(ParsedFile file)
    {
        if (file.FileHeader == null)
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.MissingHeader,
                Error = "Header is missing",
                Scope = ValidationErrorScope.File
            };
        }
    }

    private static IEnumerable<ValidationError> ValidateTrailerPresence(ParsedFile file)
    {
        if (file.FileTrailer == null)
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.MissingTrailer,
                Error = "Trailer is missing",
                Scope = ValidationErrorScope.File
            };
        }
    }

    private IEnumerable<ValidationError> ValidateExtractId(ParsedFile file)
    {
        if (file.FileHeader == null) yield break;

        foreach (var error in _headerExtractIdValidator.Validate(file))
        {
            yield return error;
        }

        if (file.FileTrailer != null && file.FileHeader.ExtractId != file.FileTrailer.ExtractId)
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

    private IEnumerable<ValidationError> ValidateRecordCount(ParsedFile file)
    {
        if (file.FileHeader == null) yield break;

        var headerRecordCountErrors = _headerIdRecordCountValidator.Validate(file).ToList();

        foreach (var error in headerRecordCountErrors)
        {
            yield return error;
        }

        if (file.FileTrailer != null && file.FileHeader.RecordCount != file.FileTrailer.RecordCount)
        {
            yield return new ValidationError
            {
                Field = "Record count",
                Code = ErrorCodes.InconsistentRecordCount,
                Error = "Record count does not match value in header",
                Scope = ValidationErrorScope.Trailer
            };
        } else if (headerRecordCountErrors.Count == 0 && file.DataRecords.Count != int.Parse(file.FileHeader.RecordCount!))
        {
            yield return new ValidationError
            {
                Code = ErrorCodes.UnexpectedRecordCount,
                Error = "Record count does not match value in header and trailer",
                Scope = ValidationErrorScope.File
            };
        }
    }

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex ExtractIdRegex();

    [GeneratedRegex(@"^(?!000000)\d{6}$")]
    private static partial Regex RecordCountRegex();
}
