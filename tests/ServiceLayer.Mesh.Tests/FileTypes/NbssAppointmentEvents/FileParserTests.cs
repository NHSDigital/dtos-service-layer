using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using static ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.FileParser;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents;

public class FileParserTests
{
    private readonly FileParser _fileParser;
    private readonly string _testDataPath;

    public FileParserTests()
    {
        _fileParser = new FileParser();
        _testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "FileTypes", "NbssAppointmentEvents", "TestData");
    }

    private FileStream GetTestFileStream(string fileName)
    {
        var filePath = Path.Combine(_testDataPath, fileName);
        return File.OpenRead(filePath);
    }

    [Fact]
    public void Parse_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        Stream? stream = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _fileParser.Parse(stream!));

        Assert.Equal("stream", exception.ParamName);
    }

    [Fact]
    public void Parse_EmptyStream_ReturnsEmptyParsedFile()
    {
        // Arrange
        using var stream = CreateStreamFromString("");

        // Act
        var result = _fileParser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.FileHeader);
        Assert.Null(result.FileTrailer);
        Assert.Empty(result.DataRecords);
    }

    [Fact]
    public void Parse_ValidFile_ReturnsParsedFileWithCorrectStructure()
    {
        // Arrange
        using var fileStream = GetTestFileStream("ValidFile.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result.FileHeader);
        VerifyFileHeaderRecord(result.FileHeader, "NBSSAPPT_HDR", "00000107", "20250317", "133128", "000002");
        Assert.Equal(2, result.DataRecords.Count);
        Assert.NotNull(result.FileTrailer);
        VerifyFileTrailerRecord(result.FileTrailer, "NBSSAPPT_END", "00000107", "20250317", "133129", "000002");

        Assert.Equal(1, result.DataRecords[0].RowNumber);
        Assert.Equal(2, result.DataRecords[1].RowNumber);

        var expectedFirstRecord = new Dictionary<string, string>
        {
            ["Sequence"] = "000001",
            ["BSO"] = "KMK",
            ["Action"] = "B",
            ["Clinic Code"] = "BU003",
            ["Status"] = "B"
        };

        var expectedSecondRecord = new Dictionary<string, string>
        {
            ["Sequence"] = "000002",
            ["BSO"] = "KMK",
            ["Action"] = "B",
            ["Clinic Code"] = "BU004",
            ["Status"] = "B"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedFirstRecord);
        VerifyDataRecordFields(result.DataRecords[1], expectedSecondRecord);
    }

    [Fact]
    public void Parse_CompleteDataset_ParsesAllFieldsCorrectly()
    {
        // Arrange
        using var fileStream = GetTestFileStream("CompleteDataset.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);

        var expectedData = new Dictionary<string, string>
        {
            ["Sequence"] = "000001",
            ["BSO"] = "KMK",
            ["Action"] = "U",
            ["Clinic Code"] = "BU003",
            ["Holding Clinic"] = "N",
            ["Status"] = "A",
            ["Attended Not Scr"] = "N",
            ["Appointment ID"] = "BU003-67235-RA1-DN-T1330-1",
            ["NHS Num"] = "9277757620",
            ["Epsiode Type"] = "G",
            ["Episode Start"] = "2025-01-30",
            ["BatchID"] = "KMKG00581",
            ["Screen or Asses"] = "S",
            ["Screen Appt num"] = "1",
            ["Booked By"] = "H",
            ["Cancelled By"] = "",
            ["Appt Date"] = "20250130",
            ["Appt Time"] = "1330",
            ["Location"] = "BU",
            ["Clinic Name"] = "BREAST CARE UNIT",
            ["Clinic Name (Let)"] = "BREAST CARE UNIT",
            ["Clinic Address 1"] = "BREAST CARE UNIT",
            ["Clinic Address 2"] = "MILTON KEYNES HOSPITAL",
            ["Clinic Address 3"] = "STANDING WAY",
            ["Clinic Address 4"] = "MILTON KEYNES",
            ["Clinic Address 5"] = "MK6 5LD",
            ["Postcode"] = "MK6 5LD",
            ["Action Timestamp"] = "20250204-161420"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedData);
    }

    [Fact]
    public void Parse_MissingFieldsRecord_ThrowsFileParsingException()
    {
        // Arrange
        using var fileStream = GetTestFileStream("MissingFields.dat");

        // Act & Assert
        var exception = Assert.Throws<FileParsingException>(() => _fileParser.Parse(fileStream));

        Assert.Equal(ErrorCodes.MissingFieldHeadings, exception.Code);
        Assert.Equal("Field headings are missing", exception.Message);
    }

    [Fact]
    public void Parse_UnknownRecordType_ThrowsFileParsingException()
    {
        // Arrange
        using var fileStream = GetTestFileStream("UnknownRecord.dat");

        // Act & Assert
        var exception = Assert.Throws<FileParsingException>(() => _fileParser.Parse(fileStream));

        Assert.Equal(ErrorCodes.UnknownRecordTypeIdentifier, exception.Code);
        Assert.Equal("Unknown Record Identifier UNKNOWN_TYPE", exception.Message);
    }

    [Fact]
    public void Parse_EmptyLine_SkipsEmptyLines()
    {
        // Arrange
        using var fileStream = GetTestFileStream("EmptyLines.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);
    }

    [Fact]
    public void Parse_FewerColumnsInDataRecord_OnlyProcessesAvailableColumns()
    {
        // Arrange
        using var fileStream = GetTestFileStream("FewerColumns.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);

        var expectedData = new Dictionary<string, string>
        {
            ["Sequence"] = "000001",
            ["BSO"] = "KMK",
            ["Action"] = "U"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedData);
        Assert.False(result.DataRecords[0].Fields.ContainsKey("Clinic Code"));
        Assert.False(result.DataRecords[0].Fields.ContainsKey("Status"));
    }

    [Fact]
    public void Parse_ExtraColumnsInDataRecord_IgnoresExtraColumns()
    {
        // Arrange
        using var fileStream = GetTestFileStream("ExtraColumns.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);

        var expectedData = new Dictionary<string, string>
        {
            ["Sequence"] = "000001",
            ["BSO"] = "KMK",
            ["Action"] = "U"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedData);
        Assert.Equal(3, result.DataRecords[0].Fields.Count);
    }

    [Fact]
    public void Parse_QuotedValues_TrimsQuotes()
    {
        // Arrange
        using var fileStream = GetTestFileStream("QuotedValues.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);

        var expectedData = new Dictionary<string, string>
        {
            ["Field1"] = "Value1",
            ["Field2"] = "Value2",
            ["Field3"] = "Value3"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedData);
    }

    [Fact]
    public void Parse_WithEscapedCharacters_HandlesCorrectly()
    {
        // Arrange
        using var fileStream = GetTestFileStream("EscapedChars.dat");

        // Act
        var result = _fileParser.Parse(fileStream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.DataRecords);
        Assert.Equal(1, result.DataRecords[0].RowNumber);

        var expectedData = new Dictionary<string, string>
        {
            ["Field With\"Quote"] = "Value With\"Quote",
            ["Normal Field"] = "Normal Value",
            ["Field With\\Backslash"] = "Value With\\Backslash"
        };

        VerifyDataRecordFields(result.DataRecords[0], expectedData);
    }

    [Fact]
    public void VerifyFileHeaderRecordMap_MapsCorrectly()
    {
        // Arrange
        using var reader = CreateConfiguredCsvReader("HeaderMapping.dat");
        reader.Context.RegisterClassMap<FileHeaderRecordMap>();

        // Act
        reader.Read();
        var result = reader.GetRecord<FileHeaderRecord>();

        // Assert
        Assert.Equal("NBSSAPPT_HDR", result.RecordTypeIdentifier);
        Assert.Equal("00000054", result.ExtractId);
        Assert.Equal("20250204", result.TransferStartDate);
        Assert.Equal("161846", result.TransferStartTime);
        Assert.Equal("000002", result.RecordCount);
    }

    [Fact]
    public void VerifyFileTrailerRecordMap_MapsCorrectly()
    {
        // Arrange
        using var reader = CreateConfiguredCsvReader("TrailerMapping.dat");
        reader.Context.RegisterClassMap<FileTrailerRecordMap>();

        // Act
        reader.Read();
        var result = reader.GetRecord<FileTrailerRecord>();

        // Assert
        Assert.Equal("NBSSAPPT_END", result.RecordTypeIdentifier);
        Assert.Equal("00000054", result.ExtractId);
        Assert.Equal("20250204", result.TransferEndDate);
        Assert.Equal("161846", result.TransferEndTime);
        Assert.Equal("000002", result.RecordCount);
    }


    [Fact]
    public void Parse_DataRecordBeforeFields_ThrowsFileParsingExceptionWithCorrectCode()
    {
        // Arrange
        using var stream = GetTestFileStream("DataBeforeFields.dat");

        // Act & Assert
        var exception = Assert.Throws<FileParsingException>(() => _fileParser.Parse(stream));

        Assert.Equal(ErrorCodes.MissingFieldHeadings, exception.Code);
        Assert.Equal("Field headings are missing", exception.Message);
    }

    [Fact]
    public void Parse_UnknownRecordTypeWithNullIdentifier_ThrowsFileParsingExceptionWithNull()
    {
        // Arrange
        using var stream = GetTestFileStream("NullRecordType.dat");

        // Act & Assert
        var exception = Assert.Throws<FileParsingException>(() => _fileParser.Parse(stream));

        Assert.Equal(ErrorCodes.UnknownRecordTypeIdentifier, exception.Code);
        Assert.Equal("Unknown Record Identifier ", exception.Message);
    }

    [Fact]
    public void Parse_UnknownRecordTypeWithEmptyString_ThrowsFileParsingExceptionWithNull()
    {
        // Arrange
        using var stream = GetTestFileStream("EmptyRecordType.dat");

        // Act & Assert
        var exception = Assert.Throws<FileParsingException>(() => _fileParser.Parse(stream));

        Assert.Equal(ErrorCodes.UnknownRecordTypeIdentifier, exception.Code);
        Assert.Equal("Unknown Record Identifier ", exception.Message);
    }

    [Fact]
    public void Parse_FileParsingExceptionWithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FileParsingException("TEST001", "Test error", innerException);

        // Assert
        Assert.Equal("TEST001", exception.Code);
        Assert.Equal("Test error", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal("Test error", exception.Message);
    }

    [Fact]
    public void Parse_FileWithOnlyHeader_CompletesSuccessfully()
    {
        // Arrange
        using var stream = GetTestFileStream("HeaderMapping.dat");

        // Act
        var result = _fileParser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FileHeader);
        Assert.Equal("NBSSAPPT_HDR", result.FileHeader.RecordTypeIdentifier);
        Assert.Null(result.FileTrailer);
        Assert.Empty(result.DataRecords);
    }

    [Fact]
    public void Parse_FileWithOnlyTrailer_CompletesSuccessfully()
    {
        // Arrange
        using var stream = GetTestFileStream("TrailerMapping.dat");

        // Act
        var result = _fileParser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.FileHeader);
        Assert.NotNull(result.FileTrailer);
        Assert.Equal("NBSSAPPT_END", result.FileTrailer.RecordTypeIdentifier);
        Assert.Empty(result.DataRecords);
    }

    [Fact]
    public void Parse_FileWithOnlyFields_CompletesSuccessfully()
    {
        // Arrange
        using var stream = GetTestFileStream("FieldsOnly.dat");

        // Act
        var result = _fileParser.Parse(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.FileHeader);
        Assert.Null(result.FileTrailer);
        Assert.Empty(result.DataRecords);
    }

    // Helper methods
    private CsvReader CreateConfiguredCsvReader(string fileName)
    {
        var streamReader = new StreamReader(GetTestFileStream(fileName));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "|",
            Quote = '"',
            Escape = '\\',
            HasHeaderRecord = false,
            Mode = CsvMode.RFC4180
        };

        return new CsvReader(streamReader, config);
    }

    private static MemoryStream CreateStreamFromString(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }

    private static void VerifyFileHeaderRecord(
        FileHeaderRecord record,
        string recordType,
        string extractId,
        string date,
        string time,
        string count)
    {
        Assert.NotNull(record);
        Assert.Equal(recordType, record.RecordTypeIdentifier);
        Assert.Equal(extractId, record.ExtractId);
        Assert.Equal(date, record.TransferStartDate);
        Assert.Equal(time, record.TransferStartTime);
        Assert.Equal(count, record.RecordCount);
    }

    private static void VerifyFileTrailerRecord(
        FileTrailerRecord record,
        string recordType,
        string extractId,
        string date,
        string time,
        string count)
    {
        Assert.NotNull(record);
        Assert.Equal(recordType, record.RecordTypeIdentifier);
        Assert.Equal(extractId, record.ExtractId);
        Assert.Equal(date, record.TransferEndDate);
        Assert.Equal(time, record.TransferEndTime);
        Assert.Equal(count, record.RecordCount);
    }

    private static void VerifyDataRecordFields(
        FileDataRecord record,
        Dictionary<string, string> expectedFields)
    {
        Assert.NotNull(record);

        foreach (var value in expectedFields)
        {
            Assert.True(record.Fields.ContainsKey(value.Key), $"Field '{value.Key}' not found in record");
            Assert.Equal(value.Value, record[value.Key]);
        }
    }
}
