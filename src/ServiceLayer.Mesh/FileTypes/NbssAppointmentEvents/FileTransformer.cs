using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

// TODO - NBSS appointment file specific implementation of IFileTransformer. To orchestrate parsing, validation and staging of data (delegated to separate classes)
public class FileTransformer : IFileTransformer
{
    private readonly IFileParser _fileParser;
    private readonly IValidationRunner _validationRunner;
    private readonly IStagingPersister _stagingPersister;

    public FileTransformer(IFileParser fileParser, IValidationRunner validationRunner, IStagingPersister stagingPersister)
    {
        _fileParser = fileParser;
        _validationRunner = validationRunner;
        _stagingPersister = stagingPersister;
    }

    public MeshFileType HandlesFileType => MeshFileType.NbssAppointmentEvents;

    public async Task<IList<ValidationError>> TransformFileAsync(Stream stream, MeshFile metaData)
    {
        // TODO - consider whether we should wrap this parsing in a try-catch and return a List<ValidationError> in case of any unforeseen parsing issues (file is totally unlike anything we expect)
        var parsed = _fileParser.Parse(stream);

        var validationErrors = _validationRunner.Validate(parsed);
        if (!validationErrors.Any())
        {
            await _stagingPersister.WriteStagedData(parsed);
        }

        return validationErrors;
    }
}
