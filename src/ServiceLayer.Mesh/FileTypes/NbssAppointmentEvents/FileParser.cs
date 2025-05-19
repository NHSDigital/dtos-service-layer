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
        /// Internal method to parse a stream of appointment data
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
                    Mode = CsvMode.RFC4180
                };

                using (var csv = new CsvReader(reader, config))
                {
                    int rowNumber = 0;

                    // You need to call Read() before accessing data
                    while (csv.Read())
                    {
                        rowNumber++;

                        // Get the record identifier from the first field
                        string recordIdentifier = csv.GetField(0)?.Trim('"');

                        switch (recordIdentifier)
                        {
                            case HEADER_IDENTIFIER:
                                // Process file header (first line)
                                result.FileHeader = ParseFileControlRecord(csv);
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
                                result.FileTrailer = ParseFileControlRecord(csv);
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

            // Start at index 1 to skip the record type identifier (NBSSAPPT_FLDS)
            for (int i = 1; i < csv.Parser.Count; i++)
            {
                // Remove quotes if present
                string heading = csv.GetField(i)?.Trim('"');
                if (!string.IsNullOrEmpty(heading))
                {
                    headings.Add(heading);
                }
            }

            return headings;
        }

        private static FileControlRecord ParseFileControlRecord(CsvReader csv)
        {
            var record = new FileControlRecord
            {
                // Remove quotes if present
                RecordTypeIdentifier = csv.GetField(0)?.Trim('"'),
                ExtractId = csv.Parser.Count > 1 ? csv.GetField(1)?.Trim('"') : null,
                TransferStartDate = csv.Parser.Count > 2 ? csv.GetField(2)?.Trim('"') : null,
                TransferStartTime = csv.Parser.Count > 3 ? csv.GetField(3)?.Trim('"') : null,
                RecordCount = csv.Parser.Count > 4 ? csv.GetField(4)?.Trim('"') : null
            };

            return record;
        }

        private static FileDataRecord ParseDataRecord(CsvReader csv, List<string> columnHeadings, int rowNumber)
        {
            var record = new FileDataRecord
            {
                RowNumber = rowNumber
            };

            // Start at index 1 to skip the record type identifier (NBSSAPPT_DATA)
            for (int i = 1; i < csv.Parser.Count && (i - 1) < columnHeadings.Count; i++)
            {
                // Get the column name from the headings
                string columnName = columnHeadings[i - 1];

                // Get the field value and remove quotes if present
                string value = csv.GetField(i)?.Trim('"');

                // Add to the fields dictionary
                record.Fields[columnName] = value;
            }

            return record;
        }
    }
}
