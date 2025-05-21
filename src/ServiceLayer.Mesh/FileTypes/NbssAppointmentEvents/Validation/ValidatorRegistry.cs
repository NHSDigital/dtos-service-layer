using System.Text.RegularExpressions;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public static partial class ValidatorRegistry
{
    public static IEnumerable<IRecordValidator> GetAllRecordValidators()
    {
        return
        [
            new InlineRegexValidator("Sequence", SequenceRegex(), ErrorCodes.MissingSequence, ErrorCodes.InvalidSequence),
            new InlineMaxLengthValidator("Appointment ID", 27, "NBSSAPPT026", "NBSSAPPT027"),
            new InlineMaxLengthValidator("Clinic Name", 40, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Name (Let)", 50, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Address 1", 30, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Address 2", 30, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Address 3", 30, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Address 4", 30, "NBSSAPPT059", "NBSSAPPT060", true),
            new InlineMaxLengthValidator("Clinic Address 5", 30, "NBSSAPPT059", "NBSSAPPT060", true),

        ];
    }

    public static IEnumerable<IFileValidator> GetAllFileValidators()
    {
        return [];
    }

    [GeneratedRegex(@"^(?!000000)\d{6}$", RegexOptions.Compiled)]
    private static partial Regex SequenceRegex();
}
