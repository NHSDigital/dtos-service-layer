namespace ServiceLayer.API.Shared;

public class CreatePathwayParticipantDto
{
    public required Guid PathwayTypeId { get; set; }
    public required string PathwayTypeName { get; set; }
    public required string ScreeningName { get; set; }
    public required string NhsNumber { get; set; }
    public required DateOnly DOB { get; set; }
    public required string Name { get; set; }
}
