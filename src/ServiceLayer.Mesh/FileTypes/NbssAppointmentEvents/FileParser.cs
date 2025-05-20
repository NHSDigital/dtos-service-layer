using CsvHelper;
using CsvHelper.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using System.Globalization;
using System.Text;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents
{
    public class FileParser : IFileParser
    {
        // Define constants for record type identifiers
        private const string HEADER_IDENTIFIER = "NBSSAPPT_HDR";
        private const string FIELDS_IDENTIFIER = "NBSSAPPT_FLDS";
        private const string DATA_IDENTIFIER = "NBSSAPPT_DATA";
        private const string FOOTER_IDENTIFIER = "NBSSAPPT_END";

        /// <summary>
        /// Parse a stream of appointment data
        /// </summary>
        public ParsedFile Parse(Stream stream)
        {
            var result = new ParsedFile
            {
                DataRecords = new List<FileDataRecord>()
            };

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null");
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = "|",
                    Quote = '"',
                    Escape = '\\',
                    HasHeaderRecord = false,
                    Mode = CsvMode.RFC4180,
                    BadDataFound = null
                };

                using (var csv = new CsvReader(reader, config))
                {
                    int rowNumber = result.DataRecords.Count - 2;
                    var columnHeadings = new List<string>();

                    // Read through the CSV file
                    while (csv.Read())
                    {
                        rowNumber++;
                        string? recordIdentifier = GetFieldValue(csv, (int)FileRecordType.RecordTypeIdentifier);

                        switch (recordIdentifier)
                        {
                            case HEADER_IDENTIFIER:
                                result.FileHeader = ParseRecordAsType<FileHeaderRecord>(csv,
                                    (rec, values) =>
                                    {
                                        rec.RecordTypeIdentifier = values[(int)FileRecordType.RecordTypeIdentifier];
                                        rec.ExtractId = values[(int)FileRecordType.ExtractId];
                                        rec.TransferStartDate = values[(int)FileRecordType.Date];
                                        rec.TransferStartTime = values[(int)FileRecordType.Time];
                                        rec.RecordCount = values[(int)FileRecordType.RecordCount];
                                    });
                                break;

                            case FIELDS_IDENTIFIER:
                                columnHeadings = ParseColumnHeadings(csv);
                                break;

                            case DATA_IDENTIFIER:
                                // Process data records
                                if (columnHeadings.Count == 0)
                                {
                                    throw new InvalidOperationException("Field headers (NBSSAPPT_FLDS) must appear before data records.");
                                }

                                result.DataRecords.Add(ParseDataRecord(csv, columnHeadings, rowNumber));
                                break;

                            case FOOTER_IDENTIFIER:
                                result.FileTrailer = ParseRecordAsType<FileTrailerRecord>(csv,
                                    (rec, values) =>
                                    {
                                        rec.RecordTypeIdentifier = values[(int)FileRecordType.RecordTypeIdentifier];
                                        rec.ExtractId = values[(int)FileRecordType.ExtractId];
                                        rec.TransferEndDate = values[(int)FileRecordType.Date];
                                        rec.TransferEndTime = values[(int)FileRecordType.Time];
                                        rec.RecordCount = values[(int)FileRecordType.RecordCount];
                                    });
                                break;

                            default:
                                throw new InvalidOperationException($"Unknown record identifier: {recordIdentifier}");
                        }
                    }
                }
            }

            return result;
        }

        private static List<string> ParseColumnHeadings(CsvReader csv)
        {
            var headings = new List<string>();
            int dataFieldStartIndex = (int)FileRecordType.RecordTypeIdentifier + 1;

            // Start at index 1 to skip the record type identifier
            for (int i = dataFieldStartIndex; i < csv.Parser.Count; i++)
            {
                // Remove quotes if present
                string heading = GetFieldValue(csv, i) ?? string.Empty;
                if (!string.IsNullOrEmpty(heading))
                {
                    headings.Add(heading);
                }
            }

            return headings;
        }

        private static T ParseRecordAsType<T>(CsvReader csv, Action<T, Dictionary<int, string?>> populateAction) where T : new()
        {
            var record = new T();
            var values = ExtractFieldValues(csv);
            populateAction(record, values);
            return record;
        }

        private static Dictionary<int, string?> ExtractFieldValues(CsvReader csv)
        {
            var values = new Dictionary<int, string?>();
            for (int i = 0; i < csv.Parser.Count; i++)
            {
                values[i] = GetFieldValue(csv, i);
            }
            return values;
        }

        private static string? GetFieldValue(CsvReader csv, int index)
        {
            return index < csv.Parser.Count ? csv.GetField(index)?.Trim('"') : null;
        }

        private static FileDataRecord ParseDataRecord(CsvReader csv, List<string> columnHeadings, int rowNumber)
        {
            var record = new FileDataRecord
            {
                RowNumber = rowNumber
            };

            int dataFieldStartIndex = (int)FileRecordType.RecordTypeIdentifier + 1;

            for (int i = dataFieldStartIndex; i < csv.Parser.Count && (i - dataFieldStartIndex) < columnHeadings.Count; i++)
            {
                string columnName = columnHeadings[i - dataFieldStartIndex];
                string? value = GetFieldValue(csv, i);
                record.Fields[columnName] = value ?? string.Empty;
            }

            return record;
        }
    }
}
