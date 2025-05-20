using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class StagingPersister(ServiceLayerDbContext dbContext) : IStagingPersister
{
    public async Task WriteStagedData(ParsedFile parsedFile, MeshFile meshFile)
    {
        var nbssAppointmentEvents = MapFileDataRecordsToNbssAppointmentEvents(parsedFile, meshFile.FileId);

        await dbContext.NbssAppointmentEvents.AddRangeAsync(nbssAppointmentEvents);
        await dbContext.SaveChangesAsync();
    }

    private static List<NbssAppointmentEvent> MapFileDataRecordsToNbssAppointmentEvents(ParsedFile parsedFile, string fileId)
    {
        var events = new List<NbssAppointmentEvent>();

        foreach (var record in parsedFile.DataRecords)
        {
            events.Add(new NbssAppointmentEvent
            {
                MeshFileId = fileId,
                BSO = record.Fields["BSO"],
                ExtractId = parsedFile.FileHeader!.ExtractId!,
                Sequence = record.Fields["Sequence"],
                Action = record.Fields["Action"],
                ClinicCode = record.Fields["Clinic Code"],
                HoldingClinic = record.Fields["Holding Clinic"],
                Status = record.Fields["Status"],
                AttendedNotScreened = record.Fields["Attended Not Scr"],
                AppointmenId = record.Fields["Appointment ID"],
                NhsNumber = record.Fields["NHS Num"],
                EpisodeType = record.Fields["Episode Type"],
                EpisodeStart = DateOnly.ParseExact(record.Fields["Episode Start"], "yyyyMMdd"),
                BatchId = record.Fields["Batch ID"],
                AppointmentType = record.Fields["Screen or Asses"],
                ScreeningAppointmentNumber = byte.Parse(record.Fields["Screen Appt num"]),
                BookedBy = record.Fields["Booked By"],
                CancelledBy = record.Fields["Cancelled By"],
                AppointmentDateTime = DateTime.ParseExact(record.Fields["Appt Date"] + record.Fields["Appt Time"], "yyyyMMddHHmm", null),
                Location = record.Fields["Location"],
                ClinicName = record.Fields["Clinic Name"],
                ClinicNameOnLetters = record.Fields["Clinic Name (Let)"],
                ClinicAddressLine1 = record.Fields["Clinic Address 1"],
                ClinicAddressLine2 = record.Fields["Clinic Address 2"],
                ClinicAddressLine3 = record.Fields["Clinic Address 3"],
                ClinicAddressLine4 = record.Fields["Clinic Address 4"],
                ClinicAddressLine5 = record.Fields["Clinic Address 5"],
                ClinicPostcode = record.Fields["Postcode"],
                ActionTimestamp = DateTime.ParseExact(record.Fields["Action Timestamp"], "yyyyMMdd-HHmmss", null)
            });
        }

        return events;
    }
}
