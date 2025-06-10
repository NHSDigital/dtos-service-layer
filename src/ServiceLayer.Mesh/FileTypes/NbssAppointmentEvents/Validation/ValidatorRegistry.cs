using System.Text.RegularExpressions;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public static partial class ValidatorRegistry
{
    public static IEnumerable<IRecordValidator> GetAllRecordValidators()
    {
        return
        [
            new RegexValidator("Sequence", SequenceRegex(), ErrorCodes.MissingSequence,
                ErrorCodes.InvalidSequence),
            new MaxLengthValidator("BSO", 3, ErrorCodes.MissingBso,
                ErrorCodes.InvalidBso),
            new RegexValidator("Action", ActionRegex(), ErrorCodes.MissingAction,
                ErrorCodes.InvalidAction),
            new MaxLengthValidator("Clinic Code", 5, ErrorCodes.MissingClinicCode,
                ErrorCodes.InvalidClinicCode),
            new RegexValidator("Holding Clinic", YesNoBlankRegex(), ErrorCodes.MissingHoldingClinic,
                ErrorCodes.InvalidHoldingClinic),
            new RegexValidator("Status", StatusRegex(), ErrorCodes.MissingStatus,
                ErrorCodes.InvalidStatus),
            new RegexValidator("Attended Not Scr", YesNoBlankRegex(), ErrorCodes.MissingAttendedNotScr,
                ErrorCodes.InvalidAttendedNotScr),
            new MaxLengthValidator("Appointment ID", 27, ErrorCodes.MissingAppointmentId,
                ErrorCodes.InvalidAppointmentId),
            new NhsNumValidator(),
            new RegexValidator("Episode Type", EpisodeTypeRegex(), ErrorCodes.MissingEpisodeType,
                ErrorCodes.InvalidEpisodeType),
            new DateFormatValidator("Episode Start", "yyyyMMdd", ErrorCodes.MissingEpisodeStart,
                ErrorCodes.InvalidEpisodeStart),
            new MaxLengthValidator("Batch ID", 9, ErrorCodes.MissingBatchId,
                ErrorCodes.InvalidBatchId),
            new RegexValidator("Screen or Asses", ScreenOrAssesRegex(), ErrorCodes.MissingScreenOrAsses,
                ErrorCodes.InvalidScreenOrAsses),
            new RegexValidator("Screen Appt num", ScreenApptNumRegex(), ErrorCodes.MissingScreenApptNum,
                ErrorCodes.InvalidScreenApptNum),
            new RegexValidator("Booked By", BookedByRegex(), ErrorCodes.MissingBookedBy,
                ErrorCodes.InvalidBookedBy),
            new RegexValidator("Cancelled By", CancelledByRegex(), ErrorCodes.MissingCancelledBy,
                ErrorCodes.InvalidCancelledBy),
            new DateFormatValidator("Appt Date", "yyyyMMdd", ErrorCodes.MissingApptDate,
                ErrorCodes.InvalidApptDate),
            new DateFormatValidator("Appt Time", "HHmm", ErrorCodes.MissingApptTime,
                ErrorCodes.InvalidApptTime),
            new MaxLengthValidator("Location", 5, ErrorCodes.MissingLocation,
                ErrorCodes.InvalidLocation),
            new MaxLengthValidator("Clinic Name", 40, ErrorCodes.MissingClinicName,
                ErrorCodes.InvalidClinicName, true),
            new MaxLengthValidator("Clinic Name (Let)", 50, ErrorCodes.MissingClinicNameLet,
                ErrorCodes.InvalidClinicNameLet, true),
            new MaxLengthValidator("Clinic Address 1", 30, ErrorCodes.MissingClinicAddress1,
                ErrorCodes.InvalidClinicAddress1, true),
            new MaxLengthValidator("Clinic Address 2", 30, ErrorCodes.MissingClinicAddress2,
                ErrorCodes.InvalidClinicAddress2, true),
            new MaxLengthValidator("Clinic Address 3", 30, ErrorCodes.MissingClinicAddress3,
                ErrorCodes.InvalidClinicAddress3, true),
            new MaxLengthValidator("Clinic Address 4", 30, ErrorCodes.MissingClinicAddress4,
                ErrorCodes.InvalidClinicAddress4, true),
            new MaxLengthValidator("Clinic Address 5", 30, ErrorCodes.MissingClinicAddress5,
                ErrorCodes.InvalidClinicAddress5, true),
            new MaxLengthValidator("Postcode", 8, ErrorCodes.MissingPostcode,
                ErrorCodes.InvalidPostcode, true),
            new DateFormatValidator("Action Timestamp", "yyyyMMdd-HHmmss", ErrorCodes.MissingActionTimestamp,
                ErrorCodes.InvalidActionTimestamp)
        ];
    }

    public static IEnumerable<IFileValidator> GetAllFileValidators()
    {
        return [
            new HeaderPresenceValidator(),
            new TrailerPresenceValidator(),
            new ExtractIdValidator(),
            new RecordCountValidator()
        ];
    }

    [GeneratedRegex(@"^[BCU]$", RegexOptions.Compiled)]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"^[CH]$", RegexOptions.Compiled)]
    private static partial Regex BookedByRegex();

    [GeneratedRegex(@"^$|^[ CH]$", RegexOptions.Compiled)]
    private static partial Regex CancelledByRegex();

    [GeneratedRegex(@"^[FGHNRST]$", RegexOptions.Compiled)]
    private static partial Regex EpisodeTypeRegex();

    [GeneratedRegex(@"^[AS]$", RegexOptions.Compiled)]
    private static partial Regex ScreenOrAssesRegex();

    [GeneratedRegex(@"^$|^[1-9]$", RegexOptions.Compiled)]
    private static partial Regex ScreenApptNumRegex();

    [GeneratedRegex(@"^(?!000000)\d{6}$", RegexOptions.Compiled)]
    private static partial Regex SequenceRegex();

    [GeneratedRegex(@"^[ABCD]$", RegexOptions.Compiled)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"^$|^[ YN]$", RegexOptions.Compiled)]
    private static partial Regex YesNoBlankRegex();
}
