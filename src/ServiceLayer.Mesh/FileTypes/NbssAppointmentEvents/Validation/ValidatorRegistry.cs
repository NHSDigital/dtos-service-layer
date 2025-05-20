using System.Text.RegularExpressions;

namespace ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

public static partial class ValidatorRegistry
{
    public static IEnumerable<IRecordValidator> GetAllRecordValidators()
    {
        return
        [
            new InlineRegexValidator("Sequence", SequenceRegex(), "NBSSAPPT012", "NBSSAPPT013"),
            new InlineMaxLengthValidator("Appointment ID", 27, "NBSSAPPT026", "NBSSAPPT027"),
            new InlineMaxLengthValidator("Clinic Address 3", 30, "NBSSAPPT059", "NBSSAPPT060", true)


        ];
    }

    public static IEnumerable<IFileValidator> GetAllFileValidators()
    {
        return [];
    }

    [GeneratedRegex(@"^(?!000000)\d{6}$", RegexOptions.Compiled)]
    private static partial Regex SequenceRegex();
}
