using NHS.MESH.Client.Models;

namespace ServiceLayer.Mesh.Extensions;

public static class ApiErrorResponseExtensions
{
    public static string ToFormattedString(this APIErrorResponse? error)
    {
        return error == null ? "Unknown error" : $"ErrorEvent: {error.ErrorEvent ?? "N/A"}, ErrorCode: {error.ErrorCode ?? "N/A"}, ErrorDescription: {error.ErrorDescription}";
    }
}
