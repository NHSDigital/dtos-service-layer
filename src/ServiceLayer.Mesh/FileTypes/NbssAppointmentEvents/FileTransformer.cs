using Microsoft.Extensions.Logging;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileTransformer(
    IFileParser fileParser,
    IValidationRunner validationRunner,
    IStagingPersister stagingPersister,
    ILogger<FileTransformer> logger)
    : FileTransformerBase
{
    protected override MeshFileType HandlesFileType => MeshFileType.NbssAppointmentEvents;

    public override async Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData)
    {
        try
        {
            var parsed = fileParser.Parse(stream);
            var validationErrors = validationRunner.Validate(parsed);

            if (!validationErrors.Any())
            {
                await stagingPersister.WriteStagedData(parsed, metaData);
            }

            return validationErrors;
        }
        catch (FileParsingException ex)
        {
            return HandleFileParsingException(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedException(ex, metaData);
        }
    }

    private List<ValidationError> HandleFileParsingException(FileParsingException ex)
    {
        logger.LogError("File parsing failed with validation error. Code: {ErrorCode}, Message: {ErrorMessage}",
            ex.Code, ex.ErrorMessage);

        return
        [
            new ValidationError
            {
                Code = ex.Code,
                Error = ex.ErrorMessage,
                Scope = ValidationErrorScope.File
            }
        ];
    }

    private IList<ValidationError> HandleUnexpectedException(Exception ex, MeshFile metaData)
    {
        logger.LogError(ex, "System error occurred while parsing NBSS appointment file. File: {FileName}",
            metaData?.FileId ?? "Unknown");

        return
        [
            new ValidationError
            {
                Code = ErrorCodes.UnableToParseFile,
                Error = "Unable to parse file",
                Scope = ValidationErrorScope.File
            }
        ];
    }
}
