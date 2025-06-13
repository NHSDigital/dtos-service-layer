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
        var lastUpdated = DateTime.UtcNow.AddHours(-hoursOld);
        var fileId = Guid.NewGuid().ToString();

        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = Guid.NewGuid().ToString(),
            FileId = fileId,
            Status = status,
            LastUpdatedUtc = lastUpdated
        };

        var fileEvent = new MeshFileEvent
        {
            FileId = fileId,
            Status = status,
            TimestampUtc = lastUpdated,
            Source = FileEventSource.DiscoveryFunction
        };

        DbContext.MeshFiles.Add(file);
        DbContext.MeshFileEvents.Add(fileEvent);
        DbContext.SaveChanges();
        return file;
    }

    protected MeshFile AssertFileUnchanged(string fileId, MeshFileStatus expectedStatus,
        DateTime expectedLastUpdatedUtc)
    {
        var unchanged = DbContext.MeshFiles.Single(x => x.FileId == fileId);
        Assert.Equal(expectedStatus, unchanged.Status);
        Assert.Equal(expectedLastUpdatedUtc, unchanged.LastUpdatedUtc);
        return unchanged;
    }

    protected MeshFile AssertFileUpdated(string fileId, MeshFileStatus expectedStatus, FileEventSource expectedSource)
    {
        var updated = DbContext.MeshFiles
            .Single(x => x.FileId == fileId);
        Assert.Equal(expectedStatus, updated.Status);
        Assert.True(updated.LastUpdatedUtc > DateTime.UtcNow.AddSeconds(-10));

        var lastEvent = DbContext.MeshFileEvents
            .Where(x => x.FileId == fileId)
            .OrderByDescending(x => x.TimestampUtc)
            .First();

        Assert.Equal(expectedStatus, lastEvent.Status);
        Assert.True(lastEvent.TimestampUtc > DateTime.UtcNow.AddSeconds(-10));
        Assert.Equal(expectedSource, lastEvent.Source);

        return updated;
    }
}
