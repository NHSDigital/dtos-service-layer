using System.Text.RegularExpressions;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public static partial class ValidatorRegistry
{
    public static IEnumerable<IRecordValidator> GetAllRecordValidators()
    {
        return
        [
            new InlineRegexValidator("Sequence", SequenceRegex(), ErrorCodes.MissingSequence,
                ErrorCodes.InvalidSequence),
            new InlineMaxLengthValidator("BSO", 3, ErrorCodes.MissingBso,
                ErrorCodes.InvalidBso),
            new InlineRegexValidator("Action", ActionRegex(), ErrorCodes.MissingAction,
                ErrorCodes.InvalidAction),
            new InlineMaxLengthValidator("Clinic Code", 5, ErrorCodes.MissingClinicCode,
                ErrorCodes.InvalidClinicCode),
            new InlineRegexValidator("Holding Clinic", YesNoBlankRegex(), ErrorCodes.MissingHoldingClinic,
                ErrorCodes.InvalidHoldingClinic),
            new InlineRegexValidator("Status", StatusRegex(), ErrorCodes.MissingStatus,
                ErrorCodes.InvalidStatus),
            new InlineRegexValidator("Attended Not Scr", YesNoBlankRegex(), ErrorCodes.MissingAttendedNotScr,
                ErrorCodes.InvalidAttendedNotScr),
            new InlineMaxLengthValidator("Appointment ID", 27, ErrorCodes.MissingAppointmentId,
                ErrorCodes.InvalidAppointmentId),
            new InlineMaxLengthValidator("Clinic Name", 40, ErrorCodes.MissingClinicName,
                ErrorCodes.InvalidClinicName, true),
            new InlineMaxLengthValidator("Clinic Name (Let)", 50, ErrorCodes.MissingClinicNameLet,
                ErrorCodes.InvalidClinicNameLet, true),
            new InlineMaxLengthValidator("Clinic Address 1", 30, ErrorCodes.MissingClinicAddress1,
                ErrorCodes.InvalidClinicAddress1, true),
            new InlineMaxLengthValidator("Clinic Address 2", 30, ErrorCodes.MissingClinicAddress2,
                ErrorCodes.InvalidClinicAddress2, true),
            new InlineMaxLengthValidator("Clinic Address 3", 30, ErrorCodes.MissingClinicAddress3,
                ErrorCodes.InvalidClinicAddress3, true),
            new InlineMaxLengthValidator("Clinic Address 4", 30, ErrorCodes.MissingClinicAddress4,
                ErrorCodes.InvalidClinicAddress4, true),
            new InlineMaxLengthValidator("Clinic Address 5", 30, ErrorCodes.MissingClinicAddress5,
                ErrorCodes.InvalidClinicAddress5, true),
            new InlineMaxLengthValidator("Postcode", 8, ErrorCodes.MissingPostcode,
                ErrorCodes.InvalidPostcode, true),
        ];
    }

    public static IEnumerable<IFileValidator> GetAllFileValidators()
    {
        return [];
    }

    [GeneratedRegex(@"^[BCU]$", RegexOptions.Compiled)]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"^[ABCD]$", RegexOptions.Compiled)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"^(?!000000)\d{6}$", RegexOptions.Compiled)]
    private static partial Regex SequenceRegex();

    [GeneratedRegex(@"^$|^[ YN]$", RegexOptions.Compiled)]
    private static partial Regex YesNoBlankRegex();
}
