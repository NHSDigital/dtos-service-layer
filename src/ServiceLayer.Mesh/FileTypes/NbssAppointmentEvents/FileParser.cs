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
                ColumnHeadings = new List<string>(),
                DataRecords = new List<FileDataRecord>()
            };

            // Check for null stream
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null");
            }

            // Use StreamReader with explicit encoding (adjust as needed)
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                // Configure CsvHelper for pipe-separated values
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
                    int rowNumber = 0;

                    // Read through the CSV file
                    while (csv.Read())
                    {
                        rowNumber++;

                        // Get the record identifier from the first field
                        string? recordIdentifier = GetFieldValue(csv, (int)FileRecordTypeEnum.RecordTypeIdentifier);

                        if (string.IsNullOrWhiteSpace(recordIdentifier))
                        {
                            Console.WriteLine($"Empty record identifier found at row {rowNumber}, skipping this record");
                            continue;
                        }

                        switch (recordIdentifier)
                        {
                            case HEADER_IDENTIFIER:
                                // Process file header (first line)
                                result.FileHeader = ParseRecordAsType<FileHeaderRecord>(csv,
                                    (rec, values) =>
                                    {
                                        rec.RecordTypeIdentifier = values[(int)FileRecordTypeEnum.RecordTypeIdentifier];
                                        rec.ExtractId = values[(int)FileRecordTypeEnum.ExtractId];
                                        rec.TransferStartDate = values[(int)FileRecordTypeEnum.Date];
                                        rec.TransferStartTime = values[(int)FileRecordTypeEnum.Time];
                                        rec.RecordCount = values[(int)FileRecordTypeEnum.RecordCount];
                                    });
                                break;

                            case FIELDS_IDENTIFIER:
                                // Process field headers (second line)
                                result.ColumnHeadings = ParseColumnHeadings(csv);
                                break;

                            case DATA_IDENTIFIER:
                                // Process data records
                                if (result.ColumnHeadings.Count == 0)
                                {
                                    throw new InvalidOperationException("Field headers (NBSSAPPT_FLDS) must appear before data records.");
                                }
                                result.DataRecords.Add(ParseDataRecord(csv, result.ColumnHeadings, rowNumber));
                                break;

                            case FOOTER_IDENTIFIER:
                                // Process file trailer (last line)
                                result.FileTrailer = ParseRecordAsType<FileTrailerRecord>(csv,
                                    (rec, values) =>
                                    {
                                        rec.RecordTypeIdentifier = values[(int)FileRecordTypeEnum.RecordTypeIdentifier];
                                        rec.ExtractId = values[(int)FileRecordTypeEnum.ExtractId];
                                        rec.TransferEndDate = values[(int)FileRecordTypeEnum.Date];
                                        rec.TransferEndTime = values[(int)FileRecordTypeEnum.Time];
                                        rec.RecordCount = values[(int)FileRecordTypeEnum.RecordCount];
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
            int dataFieldStartIndex = (int)FileRecordTypeEnum.RecordTypeIdentifier + 1;

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

            int dataFieldStartIndex = (int)FileRecordTypeEnum.RecordTypeIdentifier + 1;

            // Start at index 1 to skip the record type identifier
            for (int i = dataFieldStartIndex; i < csv.Parser.Count && (i - dataFieldStartIndex) < columnHeadings.Count; i++)
            {
                // Get the column name from the headings
                string columnName = columnHeadings[i - dataFieldStartIndex];

                // Get the field value and remove quotes if present
                string? value = GetFieldValue(csv, i);

                // Add to the fields dictionary
                record.Fields[columnName] = value;
            }

            return record;
        }
    }
}
