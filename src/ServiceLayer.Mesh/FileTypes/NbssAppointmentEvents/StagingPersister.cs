using System.Globalization;
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
        return [.. parsedFile.DataRecords.Select(record => new NbssAppointmentEvent
        {
            MeshFileId = fileId,
            BSO = record.Fields["BSO"],
            ExtractId = parsedFile.FileHeader!.ExtractId!,
            Sequence = record.Fields["Sequence"],
            Action = record.Fields["Action"],
            ClinicCode = record.Fields["Clinic Code"],
            HoldingClinic = NullIfWhiteSpace(record.Fields["Holding Clinic"]),
            Status = record.Fields["Status"],
            AttendedNotScreened = NullIfWhiteSpace(record.Fields["Attended Not Scr"]),
            AppointmenId = record.Fields["Appointment ID"],
            NhsNumber = record.Fields["NHS Num"],
            EpisodeType = record.Fields["Episode Type"],
            EpisodeStart = DateOnly.ParseExact(record.Fields["Episode Start"], "yyyyMMdd", CultureInfo.InvariantCulture),
            BatchId = record.Fields["Batch ID"],
            AppointmentType = record.Fields["Screen or Asses"],
            ScreeningAppointmentNumber = NullByteIfWhiteSpace(record.Fields["Screen Appt num"]),
            BookedBy = record.Fields["Booked By"],
            CancelledBy = NullIfWhiteSpace(record.Fields["Cancelled By"]),
            AppointmentDateTime = DateTime.ParseExact(record.Fields["Appt Date"] + record.Fields["Appt Time"], "yyyyMMddHHmm", CultureInfo.InvariantCulture),
            Location = record.Fields["Location"],
            ClinicName = record.Fields["Clinic Name"],
            ClinicNameOnLetters = record.Fields["Clinic Name (Let)"],
            ClinicAddressLine1 = record.Fields["Clinic Address 1"],
            ClinicAddressLine2 = record.Fields["Clinic Address 2"],
            ClinicAddressLine3 = record.Fields["Clinic Address 3"],
            ClinicAddressLine4 = record.Fields["Clinic Address 4"],
            ClinicAddressLine5 = record.Fields["Clinic Address 5"],
            ClinicPostcode = record.Fields["Postcode"],
            ActionTimestamp = DateTime.ParseExact(record.Fields["Action Timestamp"], "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
        })];
    }

    private static string? NullIfWhiteSpace(string input) => string.IsNullOrWhiteSpace(input) ? null : input;

    private static byte? NullByteIfWhiteSpace(string input) => string.IsNullOrWhiteSpace(input) ? null : byte.Parse(input);
}
