using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents;

public static class TestDataBuilder
{
    public static ParsedFile BuildValidParsedFile(int numberOfRecords = 3)
    {
        var header = new FileHeaderRecord
        {
            RecordTypeIdentifier = "NBSSAPPT_HDR",
            ExtractId = "00000107",
            TransferStartDate = "20250519",
            TransferStartTime = "153806",
            RecordCount = numberOfRecords.ToString()
        };

        var trailer = new FileTrailerRecord
        {
            RecordTypeIdentifier = "NBSSAPPT_END",
            ExtractId = "00000107",
            TransferEndDate = "20250519",
            TransferEndTime = "153957",
            RecordCount = numberOfRecords.ToString()
        };

        var dataRecords = Enumerable.Range(1, numberOfRecords)
            .Select(BuildValidFileDataRecord)
            .ToList();

        return new ParsedFile
        {
            FileHeader = header,
            FileTrailer = trailer,
            DataRecords = dataRecords
        };
    }

    public static FileDataRecord BuildValidFileDataRecord(int rowNumber = 1)
    {
        return new FileDataRecord
        {
            RowNumber = rowNumber,
            Fields =
            {
                ["NBSSAPPT_FLDS"] = "NBSSAPPT_DATA",
                ["Sequence"] = rowNumber.ToString("D6"),
                ["BSO"] = "KMK",
                ["Action"] = "B",
                ["Clinic Code"] = "BU003",
                ["Holding Clinic"] = "N",
                ["Status"] = "B",
                ["Attended Not Scr"] = "",
                ["Appointment ID"] = "BU003-67291-RA1-DN-T1130-1",
                ["NHS Num"] = "9111104716",
                ["Episode Type"] = "F",
                ["Episode Start"] = "20250317",
                ["Batch ID"] = "KMK001334",
                ["Screen or Asses"] = "S",
                ["Screen Appt num"] = "1",
                ["Booked By"] = "H",
                ["Cancelled By"] = "",
                ["Appt Date"] = "20250327",
                ["Appt Time"] = "1130",
                ["Location"] = "BU",
                ["Clinic Name"] = "BREAST CARE UNIT",
                ["Clinic Name (Let)"] = "BREAST CARE UNIT",
                ["Clinic Address 1"] = "BREAST CARE UNIT",
                ["Clinic Address 2"] = "MILTON KEYNES HOSPITAL",
                ["Clinic Address 3"] = "STANDING WAY",
                ["Clinic Address 4"] = "MILTON KEYNES",
                ["Clinic Address 5"] = "MK6 5LD",
                ["Postcode"] = "MK6 5LD",
                ["Action Timestamp"] = "20250317-132635",
            }
        };
    }

    public static FileDataRecord BuildFileDataRecordWithField(string fieldName, string? fieldValue, int rowNumber = 1)
    {
        var fileDataRecord = BuildValidFileDataRecord(rowNumber);

        if (fieldValue == null)
        {
            fileDataRecord.Fields.Remove(fieldName);
        }
        else
        {
            fileDataRecord.Fields[fieldName] = fieldValue;
        }

        return fileDataRecord;
    }
}
