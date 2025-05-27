using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileTransformer(
    IFileParser fileParser,
    IValidationRunner validationRunner,
    IStagingPersister stagingPersister)
    : FileTransformerBase
{
    protected override MeshFileType HandlesFileType => MeshFileType.NbssAppointmentEvents;

    public override async Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData)
    {
        // TODO - wrap this parsing in a try-catch and return a List<ValidationError> in case of any unforeseen parsing issues (file is totally unlike anything we expect)
        var parsed = fileParser.Parse(stream);

        var validationErrors = validationRunner.Validate(parsed);
        if (!validationErrors.Any())
        {
            await stagingPersister.WriteStagedData(parsed, metaData);
        }

        return validationErrors;
    }
}
