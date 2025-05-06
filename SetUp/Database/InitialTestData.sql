-- Create Participants table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Participants')
BEGIN
    CREATE TABLE Participants (
        ParticipantId UNIQUEIDENTIFIER PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        DOB DATE NOT NULL,
        NHSNumber NVARCHAR(20) NOT NULL
    );
    PRINT 'Participants table created';
END
ELSE
BEGIN
    PRINT 'Participants table already exists';
END

-- Create PathwayTypeEnrolments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PathwayTypeEnrolments')
BEGIN
    CREATE TABLE PathwayTypeEnrolments (
        EnrolmentId UNIQUEIDENTIFIER PRIMARY KEY,
        ParticipantId UNIQUEIDENTIFIER NOT NULL,
        PathwayTypeId UNIQUEIDENTIFIER NOT NULL,
        EnrolmentDate NVARCHAR(100),
        LapsedDate NVARCHAR(100),
        Status NVARCHAR(50) NOT NULL,
        NextActionDate DATE,
        ScreeningName NVARCHAR(100) NOT NULL,
        PathwayTypeName NVARCHAR(100) NOT NULL,
        FOREIGN KEY (ParticipantId) REFERENCES Participants(ParticipantId)
    );
    PRINT 'PathwayTypeEnrolments table created';
END
ELSE
BEGIN
    PRINT 'PathwayTypeEnrolments table already exists';
END

-- Insert test data
-- Clear existing data to prevent duplicates
DELETE FROM PathwayTypeEnrolments;
DELETE FROM Participants;

-- Insert Participants
INSERT INTO Participants (ParticipantId, Name, DOB, NHSNumber)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Mona MILLAR', '1968-02-12', '9686368973'),
    ('22222222-2222-2222-2222-222222222222', 'Iain HUGHES', '1942-02-01', '9686368906'),
    ('33333333-3333-3333-3333-333333333333', 'Mike MEAKIN', '1927-06-19', '9658218873'),
    ('44444444-4444-4444-4444-444444444444', 'Kevin LEACH', '1921-08-08', '9658218881'),
    ('55555555-5555-5555-5555-555555555555', 'Arnold OLLEY', '2016-07-21', '9658218903'),
    ('65555555-5555-5555-5555-555555555555', 'Mina LEECH', '1918-09-19', '9658218989');

-- Insert PathwayTypeEnrolments
INSERT INTO PathwayTypeEnrolments (EnrolmentId, ParticipantId, PathwayTypeId, EnrolmentDate, LapsedDate, Status, NextActionDate, ScreeningName, PathwayTypeName)
VALUES
    ('11111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111113', '', '', 'Active', '2025-05-17', 'Breast Screening', 'Breast Screening Routine'),
    ('11111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111114', '', '', 'Active', '2026-03-22', 'Cervical Screening', 'Cervical Screening Routine'),
    ('11111111-1111-1111-1111-111111111114', '22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111115', '', '', 'Active', '2025-09-21', 'Bowel Screening', 'Bowel Screening Routine');

PRINT 'Initial test data inserted successfully';
