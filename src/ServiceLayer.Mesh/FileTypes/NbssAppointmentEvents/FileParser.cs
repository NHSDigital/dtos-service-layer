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
        csv.Context.RegisterClassMap<FileHeaderRecordMap>();
        csv.Context.RegisterClassMap<FileTrailerRecordMap>();
        var rowNumber = 0;
        var columnHeadings = new List<string>();

        while (csv.Read())
        {
            var recordIdentifier = GetFieldValue(csv, RecordTypeIdentifier);

            switch (recordIdentifier)
            {
                case HeaderIdentifier:
                    result.FileHeader = csv.GetRecord<FileHeaderRecord>();
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
                    result.FileTrailer = csv.GetRecord<FileTrailerRecord>();
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

    private static string? GetFieldValue(CsvReader csv, int index)
    {
        return index < csv.Parser.Count ? csv.GetField(index)?.Trim('"') : null;
    }

    private static FileDataRecord ParseDataRecord(CsvReader csv, List<string> columnHeadings, int rowNumber)
    {
        const int dataFieldStartIndex = 1;

        var record = new FileDataRecord { RowNumber = rowNumber };

        foreach (var (heading, index) in columnHeadings.Select((header, index) => (header, index + dataFieldStartIndex)))
        {
            if (index < csv.Parser.Count)
            {
                record.Fields[heading] = GetFieldValue(csv, index) ?? string.Empty;
            }
        }

        return record;
    }

    public sealed class FileTrailerRecordMap : ClassMap<FileTrailerRecord>
    {
        public FileTrailerRecordMap()
        {
            Map(m => m.RecordTypeIdentifier).Index(0);
            Map(m => m.ExtractId).Index(1);
            Map(m => m.TransferEndDate).Index(2);
            Map(m => m.TransferEndTime).Index(3);
            Map(m => m.RecordCount).Index(4);
        }
    }

    public sealed class FileHeaderRecordMap : ClassMap<FileHeaderRecord>
    {
        public FileHeaderRecordMap()
        {
            Map(m => m.RecordTypeIdentifier).Index(0);
            Map(m => m.ExtractId).Index(1);
            Map(m => m.TransferStartDate).Index(2);
            Map(m => m.TransferStartTime).Index(3);
            Map(m => m.RecordCount).Index(4);
        }
    }
}

