namespace ServiceLayer.API.Shared;

public class CreatePathwayParticipantDto
{
    public required string PathwayTypeName { get; set; }
    public required string ScreeningName { get; set; }
    public DateOnly DOB { get; set; }
    public required string NhsNumber { get; set; }
    public required string Name { get; set; }
}
