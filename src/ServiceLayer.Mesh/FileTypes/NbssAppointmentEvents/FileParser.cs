using CsvHelper;
using CsvHelper.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using System.Globalization;
using System.Text;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileParser : IFileParser
{
    private const string HeaderIdentifier = "NBSSAPPT_HDR";
    private const string FieldsIdentifier = "NBSSAPPT_FLDS";
    private const string DataIdentifier = "NBSSAPPT_DATA";
    private const string FooterIdentifier = "NBSSAPPT_END";

    private const int RecordTypeIdentifier = 0;
    private const int ExtractId = 1;
    private const int Date = 2;
    private const int Time = 3;
    private const int RecordCount = 4;

    /// <summary>
    /// Parse a stream of appointment data
    /// </summary>
    public ParsedFile Parse(Stream stream)
    {
        var result = new ParsedFile();

        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "Stream cannot be null");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "|",
            Quote = '"',
            Escape = '\\',
            HasHeaderRecord = false,
            Mode = CsvMode.RFC4180,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);
        var rowNumber = 0;
        var columnHeadings = new List<string>();

        while (csv.Read())
        {
            var recordIdentifier = GetFieldValue(csv, RecordTypeIdentifier);

            switch (recordIdentifier)
            {
                case HeaderIdentifier:
                    result.FileHeader = ParseRecordAsType<FileHeaderRecord>(csv,
                        (rec, values) =>
                        {
                            rec.RecordTypeIdentifier = values[RecordTypeIdentifier];
                            rec.ExtractId = values[ExtractId];
                            rec.TransferStartDate = values[Date];
                            rec.TransferStartTime = values[Time];
                            rec.RecordCount = values[RecordCount];
                        });
                    break;

                case FieldsIdentifier:
                    columnHeadings = ParseColumnHeadings(csv);
                    break;

                case DataIdentifier:
                    rowNumber++;
                    if (columnHeadings.Count == 0)
                    {
                        throw new InvalidOperationException("Field headers (NBSSAPPT_FLDS) must appear before data records.");
                    }

                    result.DataRecords.Add(ParseDataRecord(csv, columnHeadings, rowNumber));
                    break;

                case FooterIdentifier:
                    result.FileTrailer = ParseRecordAsType<FileTrailerRecord>(csv,
                        (rec, values) =>
                        {
                            rec.RecordTypeIdentifier = values[RecordTypeIdentifier];
                            rec.ExtractId = values[ExtractId];
                            rec.TransferEndDate = values[Date];
                            rec.TransferEndTime = values[Time];
                            rec.RecordCount = values[RecordCount];
                        });
                    break;

                default:
                    throw new InvalidOperationException($"Unknown record identifier: {recordIdentifier}");
            }
        }

        return result;
    }

    private static List<string> ParseColumnHeadings(CsvReader csv)
    {
        return Enumerable.Range(1, csv.Parser.Count - 1)
        .Select(i => GetFieldValue(csv, i))
        .Where(x => !string.IsNullOrEmpty(x))
        .ToList()!;
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

        int dataFieldStartIndex = RecordTypeIdentifier + 1;

        for (int i = dataFieldStartIndex; i < csv.Parser.Count && (i - dataFieldStartIndex) < columnHeadings.Count; i++)
        {
            string columnName = columnHeadings[i - dataFieldStartIndex];
            string? value = GetFieldValue(csv, i);
            record.Fields[columnName] = value ?? string.Empty;
        }

        return record;
    }
}
