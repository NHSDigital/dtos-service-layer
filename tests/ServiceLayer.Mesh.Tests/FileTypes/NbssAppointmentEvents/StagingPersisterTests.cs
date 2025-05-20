using Microsoft.EntityFrameworkCore;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents;

public class NbssAppointmentEventsTests
{
    private readonly ServiceLayerDbContext _dbContext;
    private readonly StagingPersister _stagingPersister;

    public NbssAppointmentEventsTests()
    {
        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ServiceLayerDbContext(options);

        _stagingPersister = new StagingPersister(_dbContext);
    }

    [Fact]
    public async Task WriteStagedData_WhenMappingSuceeds_SavesToDb()
    {
        // Arrange
        var parsedFile = TestDataBuilder.BuildValidParsedFile();
        var meshFile = new MeshFile()
        {
            FileId = "1",
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "ABC",
            Status = MeshFileStatus.Transforming
        };

        // Act
        await _stagingPersister.WriteStagedData(parsedFile, meshFile);

        // Assert
        Assert.Equal(3, await _dbContext.NbssAppointmentEvents.CountAsync());

        var nbssAppointmentEvent = await _dbContext.NbssAppointmentEvents.FirstAsync();
        var dataRecord = parsedFile.DataRecords.First();
        Assert.Equal(meshFile.FileId, nbssAppointmentEvent.MeshFileId);
        Assert.Equal(dataRecord["BSO"], nbssAppointmentEvent.BSO);
        Assert.Equal(parsedFile.FileTrailer!.ExtractId, nbssAppointmentEvent.ExtractId);
        Assert.Equal(dataRecord["Sequence"], nbssAppointmentEvent.Sequence);
        Assert.Equal(dataRecord["Action"], nbssAppointmentEvent.Action);
        Assert.Equal(dataRecord["Clinic Code"], nbssAppointmentEvent.ClinicCode);
        Assert.Equal(dataRecord["Holding Clinic"], nbssAppointmentEvent.HoldingClinic);
        Assert.Equal(dataRecord["Status"], nbssAppointmentEvent.Status);
        Assert.Equal(dataRecord["Attended Not Scr"], nbssAppointmentEvent.AttendedNotScreened);
        Assert.Equal(dataRecord["Appointment ID"], nbssAppointmentEvent.AppointmenId);
        Assert.Equal(dataRecord["NHS Num"], nbssAppointmentEvent.NhsNumber);
        Assert.Equal(dataRecord["Episode Type"], nbssAppointmentEvent.EpisodeType);
        Assert.Equal(DateOnly.ParseExact(dataRecord.Fields["Episode Start"], "yyyyMMdd"), nbssAppointmentEvent.EpisodeStart);
        Assert.Equal(dataRecord["Batch ID"], nbssAppointmentEvent.BatchId);
        Assert.Equal(dataRecord["Screen or Asses"], nbssAppointmentEvent.AppointmentType);
        Assert.Equal(byte.Parse(dataRecord.Fields["Screen Appt num"]), nbssAppointmentEvent.ScreeningAppointmentNumber);
        Assert.Equal(dataRecord["Booked By"], nbssAppointmentEvent.BookedBy);
        Assert.Equal(dataRecord["Cancelled By"], nbssAppointmentEvent.CancelledBy);
        Assert.Equal(DateTime.ParseExact(dataRecord.Fields["Appt Date"] + dataRecord.Fields["Appt Time"], "yyyyMMddHHmm", null), nbssAppointmentEvent.AppointmentDateTime);
        Assert.Equal(dataRecord["Location"], nbssAppointmentEvent.Location);
        Assert.Equal(dataRecord["Clinic Name"], nbssAppointmentEvent.ClinicName);
        Assert.Equal(dataRecord["Clinic Name (Let)"], nbssAppointmentEvent.ClinicNameOnLetters);
        Assert.Equal(dataRecord["Clinic Address 1"], nbssAppointmentEvent.ClinicAddressLine1);
        Assert.Equal(dataRecord["Clinic Address 2"], nbssAppointmentEvent.ClinicAddressLine2);
        Assert.Equal(dataRecord["Clinic Address 3"], nbssAppointmentEvent.ClinicAddressLine3);
        Assert.Equal(dataRecord["Clinic Address 4"], nbssAppointmentEvent.ClinicAddressLine4);
        Assert.Equal(dataRecord["Clinic Address 5"], nbssAppointmentEvent.ClinicAddressLine5);
        Assert.Equal(dataRecord["Postcode"], nbssAppointmentEvent.ClinicPostcode);
        Assert.Equal(DateTime.ParseExact(dataRecord.Fields["Action Timestamp"], "yyyyMMdd-HHmmss", null), nbssAppointmentEvent.ActionTimestamp);
    }
}
