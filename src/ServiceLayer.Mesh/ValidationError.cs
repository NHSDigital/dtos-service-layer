namespace ServiceLayer.Mesh;

public class ValidationError
{
    public int? RowNumber { get; set; }

    public required string Field { get; set; }

    public required string Code { get; set; }

    public required string Error { get; set; }
}
