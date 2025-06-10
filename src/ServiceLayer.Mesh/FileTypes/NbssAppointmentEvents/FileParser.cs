using CsvHelper;
using CsvHelper.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using System.Globalization;
using System.Text;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;

public class FileParser : IFileParser
{
    private const string HeaderIdentifier = "NBSSAPPT_HDR";
    private const string FieldsIdentifier = "NBSSAPPT_FLDS";
    private const string DataIdentifier = "NBSSAPPT_DATA";
    private const string TrailerIdentifier = "NBSSAPPT_END";
    private const int RecordTypeIdentifier = 0;

    /// <summary>
    /// Parse a stream of appointment data
    /// </summary>
    public ParsedFile Parse(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "Stream cannot be null");
        }

        var result = new ParsedFile();

        using var reader = CreateStreamReader(stream);
        using var csv = CreateCsvReader(reader);

        var rowNumber = 0;
        var fields = new List<string>();

        while (csv.Read())
        {
            var recordIdentifier = GetFieldValue(csv, RecordTypeIdentifier);

            switch (recordIdentifier)
            {
                case HeaderIdentifier:
                    result.FileHeader = ParseHeader(csv);
                    break;

                case FieldsIdentifier:
                    fields = ParseFields(csv);
                    break;

                case DataIdentifier:
                    rowNumber++;
                    result.DataRecords.Add(ParseDataRecord(csv, fields, rowNumber));
                    break;

                case TrailerIdentifier:
                    result.FileTrailer = ParseTrailer(csv);
                    break;

                default:
                    recordIdentifier ??= "No Record Identifier found";
                    throw new FileParsingException(
                        ErrorCodes.UnknownRecordTypeIdentifier,
                        $"Unknown Record Identifier {recordIdentifier}");
            }
        }

        return result;
    }

    private static List<string> ParseFields(CsvReader csv)
    {
        return Enumerable.Range(1, csv.Parser.Count - 1)
        .Select(i => GetFieldValue(csv, i))
        .Where(x => !string.IsNullOrEmpty(x))
        .ToList()!;
    }

    private static string? GetFieldValue(CsvReader csv, int index) => index < csv.Parser.Count ? csv.GetField(index) : null;
    private static StreamReader CreateStreamReader(Stream stream) => new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
    private static FileHeaderRecord ParseHeader(CsvReader csv) => csv.GetRecord<FileHeaderRecord>();
    private static FileTrailerRecord ParseTrailer(CsvReader csv) => csv.GetRecord<FileTrailerRecord>();
    private static CsvReader CreateCsvReader(StreamReader reader)
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

        var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<FileHeaderRecordMap>();
        csv.Context.RegisterClassMap<FileTrailerRecordMap>();

        return csv;
    }

    private static FileDataRecord ParseDataRecord(CsvReader csv, List<string> columnHeadings, int rowNumber)
    {
        if (columnHeadings.Count == 0)
        {
            throw new FileParsingException(ErrorCodes.MissingFieldHeadings, "Field headings are missing");
        }

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
