using CsvHelper;
using CsvHelper.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using System.Globalization;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents
{
    public class FileParser : IFileParser
    {
        // Define constants for record types
        private const string HEADER_RECORD_TYPE = "NBSSAPPT_HDR";
        private const string FIELDS_RECORD_TYPE = "NBSSAPPT_FLDS";
        private const string DATA_RECORD_TYPE = "NBSSAPPT_DATA";
        private const string FOOTER_RECORD_TYPE = "NBSSAPPT_END";

        public ParsedFile Parse(Stream stream)
        {
            var result = new ParsedFile
            {
                ColumnHeadings = new List<string>(),
                DataRecords = new List<FileDataRecord>()
            };

            // Don't dispose the stream as it's passed in
            using (var reader = new StreamReader(stream, leaveOpen: true))
            {
                // Configure CsvHelper for pipe-separated values
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = "|",
                    Quote = '"',
                    Escape = '\\',
                    HasHeaderRecord = false,
                    Mode = CsvMode.RFC4180
                };

                using (var csv = new CsvReader(reader, config))
                {
                    int rowNumber = 0;

                    while (csv.Read())
                    {
                        rowNumber++;

                        // Get the record type from the first field
                        string recordType = csv.GetField(0);

                        if (string.IsNullOrEmpty(recordType))
                        {
                            continue; // Skip empty lines
                        }

                        switch (recordType)
                        {
                            case FIELDS_RECORD_TYPE:
                                // Extract column headings from the fields record
                                result.ColumnHeadings = ExtractFieldsFromRecord(csv);
                                break;

                            case HEADER_RECORD_TYPE:
                                result.FileHeader = ExtractFileControlRecord(csv);
                                break;

                            case DATA_RECORD_TYPE:
                                if (result.ColumnHeadings.Count == 0)
                                {
                                    throw new InvalidOperationException("Field definitions must appear before data records.");
                                }
                                result.DataRecords.Add(ExtractDataRecord(csv, result.ColumnHeadings, rowNumber));
                                break;

                            case FOOTER_RECORD_TYPE:
                                result.FileTrailer = ExtractFileControlRecord(csv);
                                break;

                            default:
                                throw new InvalidOperationException($"Unknown record type: {recordType}");
                        }
                    }
                }
            }

            return result;
        }

        private List<string> ExtractFieldsFromRecord(CsvReader csv)
        {
            var fieldCount = csv.Parser.Count;
            var fields = new List<string>(fieldCount - 1);

            // Skip the first field (record type) and extract all other fields
            for (int i = 1; i < fieldCount; i++)
            {
                fields.Add(csv.GetField(i));
            }

            return fields;
        }

        private FileControlRecord ExtractFileControlRecord(CsvReader csv)
        {
            var record = new FileControlRecord();

            // Get field count to ensure we don't go out of bounds
            int fieldCount = csv.Parser.Count;

            // Map fields to properties with correct property names
            record.RecordTypeIdentifier = csv.GetField(0);

            if (fieldCount > 1) record.ExtractId = csv.GetField(1);
            if (fieldCount > 2) record.TransferStartDate = csv.GetField(2);
            if (fieldCount > 3) record.TransferStartTime = csv.GetField(3);
            if (fieldCount > 4) record.RecordCount = csv.GetField(4);

            return record;
        }

        private FileDataRecord ExtractDataRecord(CsvReader csv, List<string> columnHeadings, int rowNumber)
        {
            var record = new FileDataRecord
            {
                RowNumber = rowNumber
            };

            // Map each field to its corresponding column heading
            for (int i = 1; i < csv.Parser.Count && i - 1 < columnHeadings.Count; i++)
            {
                var columnName = columnHeadings[i - 1];
                record.Fields[columnName] = csv.GetField(i);
            }

            return record;
        }
    }
}
