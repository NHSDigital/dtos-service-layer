namespace ServiceLayer.Mesh.Tests;

public class ValidationErrorComparer : IEqualityComparer<ValidationError>
{
    public bool Equals(ValidationError? x, ValidationError? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Field == y.Field &&
            x.Error == y.Error &&
            x.Code == y.Code &&
            x.RowNumber == y.RowNumber;
    }

    public int GetHashCode(ValidationError obj)
    {
        return HashCode.Combine(obj.Field, obj.Error, obj.Code, obj.RowNumber);
    }
}
