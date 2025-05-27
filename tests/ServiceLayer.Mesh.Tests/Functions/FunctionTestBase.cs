using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Tests.Functions;

public abstract class FunctionTestBase<TFunction>
{
    protected readonly ServiceLayerDbContext DbContext;
    protected readonly Mock<ILogger<TFunction>> LoggerMock = new();

    protected FunctionTestBase()
    {
        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        DbContext = new ServiceLayerDbContext(options);
    }

    protected MeshFile SaveMeshFile(MeshFileStatus status = MeshFileStatus.Extracted, int hoursOld = 1)
    {
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = Guid.NewGuid().ToString(),
            FileId = Guid.NewGuid().ToString(),
            Status = status,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-hoursOld),
        };
        DbContext.MeshFiles.Add(file);
        DbContext.SaveChanges();
        return file;
    }

    protected MeshFile AssertFileStatusUpdated(string fileId, MeshFileStatus expectedStatus)
    {
        var updated = DbContext.MeshFiles.Single(x => x.FileId == fileId);
        Assert.Equal(expectedStatus, updated.Status);
        Assert.True(updated.LastUpdatedUtc > DateTime.UtcNow.AddSeconds(-10));
        return updated;
    }
}
