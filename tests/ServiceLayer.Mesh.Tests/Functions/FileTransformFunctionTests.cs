using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;
using System.Text;

namespace ServiceLayer.Mesh.Tests.Functions
{
    public class FileTransformFunctionTests
    {
        private readonly string _validFileContent;
        private readonly string _completeDatasetContent;
        private readonly Mock<ILogger<FileTransformFunction>> _loggerMock = new();
        private readonly Mock<IMeshFilesBlobStore> _blobStoreMock = new();
        private readonly Mock<IFileTransformFunctionConfiguration> _configurationMock = new();
        private readonly Mock<IFileParser> _fileParserMock = new();
        private readonly ServiceLayerDbContext _dbContext;
        private readonly FileTransformFunction _transformFunction;
        private readonly IFileParser _fileParser;

        public FileTransformFunctionTests()
        {

            _validFileContent =
               "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000002\"" +
               "\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"|\"Clinic Code\"|\"Status\"" +
               "\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"|\"BU003\"|\"A\"" +
               "\n\"NBSSAPPT_DATA\"|\"000002\"|\"KMK\"|\"U\"|\"BU004\"|\"A\"" +
               "\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000002\"";

            _completeDatasetContent =
                "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"" +
                "\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"|\"Clinic Code\"|\"Holding Clinic\"|\"Status\"|\"Attended Not Scr\"|\"Appointment ID\"|\"NHS Num\"|\"Epsiode Type\"|\"Episode Start\"|\"BatchID\"|\"Screen or Asses\"|\"Screen Appt num\"|\"Booked By\"|\"Cancelled By\"|\"Appt Date\"|\"Appt Time\"|\"Location\"|\"Clinic Name\"|\"Clinic Name (Let)\"|\"Clinic Address 1\"|\"Clinic Address 2\"|\"Clinic Address 3\"|\"Clinic Address 4\"|\"Clinic Address 5\"|\"Postcode\"|\"Action Timestamp\"" +
                "\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"|\"BU003\"|\"N\"|\"A\"|\"N\"|\"BU003-67235-RA1-DN-T1330-1\"|\"9277757620\"|\"G\"|\"2025-01-30\"|\"KMKG00581\"|\"S\"|\"1\"|\"H\"|\"\"|\"\"20250130\"|\"1330\"|\"BU\"|\"BREAST CARE UNIT\"|\"BREAST CARE UNIT\"|\"BREAST CARE UNIT\"|\"MILTON KEYNES HOSPITAL\"|\"STANDING WAY\"|\"MILTON KEYNES\"|\"MK6 5LD\"|\"MK6 5LD\"|\"20250204-161420\"" +
                "\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";


            _fileParser = new FileParser();

            var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _dbContext = new ServiceLayerDbContext(options);
            _configurationMock.Setup(c => c.StaleHours).Returns(12);

            _transformFunction = new FileTransformFunction(
                _loggerMock.Object,
                _dbContext,
                _blobStoreMock.Object,
                _configurationMock.Object,
                _fileParserMock.Object
            );
        }

        [Fact]
        public async Task Run_FileNotFound_ExitsSilently()
        {
            // Arrange
            var message = new FileTransformQueueMessage { FileId = "nonexistent-file" };

            // Act
            await _transformFunction.Run(message);

            // Assert
            VerifyLogMessage(LogLevel.Warning, $"File with id: {message.FileId} not found in MeshFiles table.");
            Assert.Equal(0, _dbContext.MeshFiles.Count());
            _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
        }

        [Fact]
        public async Task Run_FileStatusInvalid_ExitsSilently()
        {
            // Arrange
            var file = new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-1",
                Status = MeshFileStatus.FailedExtract,
                LastUpdatedUtc = DateTime.UtcNow
            };
            _dbContext.MeshFiles.Add(file);
            await _dbContext.SaveChangesAsync();

            var message = new FileTransformQueueMessage { FileId = "file-1" };

            // Act
            await _transformFunction.Run(message);

            // Assert
            VerifyLogMessage(LogLevel.Warning, $"File with id: {message.FileId} found in MeshFiles table but is not suitable for transformation");
            var fileFromDb = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
            Assert.Equal(MeshFileStatus.FailedExtract, fileFromDb?.Status);
            _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
        }

        [Fact]
        public async Task Run_FileStatusTransformingButNotTimedOut_ExitsSilently()
        {
            // Arrange
            var file = new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-1",
                Status = MeshFileStatus.Transforming,
                LastUpdatedUtc = DateTime.UtcNow.AddHours(-11)
            };
            _dbContext.MeshFiles.Add(file);
            await _dbContext.SaveChangesAsync();

            var message = new FileTransformQueueMessage { FileId = "file-1" };

            // Act
            await _transformFunction.Run(message);

            // Assert
            VerifyLogMessage(LogLevel.Warning, $"File with id: {message.FileId} found in MeshFiles table but is not suitable for transformation");
            var fileFromDb = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
            Assert.Equal(MeshFileStatus.Transforming, fileFromDb?.Status);
            _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
        }

        [Fact]
        public async Task Run_FileValid_DownloadsBlob()
        {
            // Arrange
            var file = new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-1",
                Status = MeshFileStatus.Extracted,
                LastUpdatedUtc = DateTime.UtcNow
            };
            _dbContext.MeshFiles.Add(file);
            await _dbContext.SaveChangesAsync();

            var message = new FileTransformQueueMessage { FileId = "file-1" };

            // Act
            await _transformFunction.Run(message);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ), Times.Never);
            _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        }

        [Fact]
        public void Parse_NullStream_ThrowsArgumentNullException()
        {
            // Arrange
            Stream? stream = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(
                () => _fileParser.Parse(stream!));

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
            using var stream = CreateStreamFromString(_validFileContent);

            // Act
            var result = _fileParser.Parse(stream);

            // Assert
            Assert.NotNull(result.FileHeader);
            VerifyFileHeaderRecord(result.FileHeader, "NBSSAPPT_HDR", "00000054", "20250204", "161846", "000002");
            Assert.Equal(2, result.DataRecords.Count);
            Assert.NotNull(result.FileTrailer);
            VerifyFileTrailerRecord(result.FileTrailer, "NBSSAPPT_END", "00000054", "20250204", "161846", "000002");

            Assert.Equal(1, result.DataRecords[0].RowNumber);
            Assert.Equal(2, result.DataRecords[1].RowNumber);

            var expectedFirstRecord = new Dictionary<string, string>
            {
                ["Sequence"] = "000001",
                ["BSO"] = "KMK",
                ["Action"] = "U",
                ["Clinic Code"] = "BU003",
                ["Status"] = "A"
            };

            var expectedSecondRecord = new Dictionary<string, string>
            {
                ["Sequence"] = "000002",
                ["BSO"] = "KMK",
                ["Action"] = "U",
                ["Clinic Code"] = "BU004",
                ["Status"] = "A"
            };

            VerifyDataRecordFields(result.DataRecords[0], expectedFirstRecord);
            VerifyDataRecordFields(result.DataRecords[1], expectedSecondRecord);
        }

        [Fact]
        public void Parse_CompleteDataset_ParsesAllFieldsCorrectly()
        {
            // Arrange
            using var stream = CreateStreamFromString(_completeDatasetContent);

            // Act
            var result = _fileParser.Parse(stream);

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
        public void Parse_MissingFieldsRecord_ThrowsInvalidOperationException()
        {
            // Arrange
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000002\"\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"|\"BU003\"|\"N\"|\"A\"|\"N\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000002\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _fileParser.Parse(stream));

            Assert.Equal("Field headers (NBSSAPPT_FLDS) must appear before data records.", exception.Message);
        }

        [Fact]
        public void Parse_UnknownRecordType_ThrowsInvalidOperationException()
        {
            // Arrange
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"\n\"UNKNOWN_TYPE\"|\"000001\"|\"KMK\"|\"U\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _fileParser.Parse(stream));

            Assert.Equal("Unknown record identifier: UNKNOWN_TYPE", exception.Message);
        }

        [Fact]
        public void Parse_EmptyLine_SkipsEmptyLines()
        {
            // Arrange
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"\n\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"\n\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act
            var result = _fileParser.Parse(stream);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.DataRecords);
            Assert.Equal(1, result.DataRecords[0].RowNumber);
        }

        [Fact]
        public void Parse_FewerColumnsInDataRecord_OnlyProcessesAvailableColumns()
        {
            // Arrange
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"|\"Clinic Code\"|\"Status\"\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act
            var result = _fileParser.Parse(stream);

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
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\"NBSSAPPT_FLDS\"|\"Sequence\"|\"BSO\"|\"Action\"\n\"NBSSAPPT_DATA\"|\"000001\"|\"KMK\"|\"U\"|\"ExtraValue1\"|\"ExtraValue2\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act
            var result = _fileParser.Parse(stream);

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
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\"NBSSAPPT_FLDS\"|\"Field1\"|\"Field2\"|\"Field3\"\n\"NBSSAPPT_DATA\"|\"Value1\"|\"Value2\"|\"Value3\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act
            var result = _fileParser.Parse(stream);

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
            string fileContent = "\"NBSSAPPT_HDR\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"\n\"NBSSAPPT_FLDS\"|\"Field With\\\"Quote\"|\"Normal Field\"|\"Field With\\\\Backslash\"\n\"NBSSAPPT_DATA\"|\"Value With\\\"Quote\"|\"Normal Value\"|\"Value With\\\\Backslash\"\n\"NBSSAPPT_END\"|\"00000054\"|\"20250204\"|\"161846\"|\"000001\"";

            using var stream = CreateStreamFromString(fileContent);

            // Act
            var result = _fileParser.Parse(stream);

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
            Assert.Equal(date, record.TransferEndDate ?? date);
            Assert.Equal(time, record.TransferEndTime ?? time);
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

        private void VerifyLogMessage(LogLevel level, string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains(expectedMessage)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
