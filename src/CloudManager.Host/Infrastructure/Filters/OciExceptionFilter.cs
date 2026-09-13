namespace CloudManager.Host.Infrastructure.Filters;

using CloudManager.Infrastructure.OracleCloud;

using Oci.Common.Model;

// Converts OCI call failures to ProblemDetails
public sealed class OciExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (OciException ex)
        {
            var statusCode = (int)ex.StatusCode;
            return TypedResults.Problem(
                statusCode: (statusCode >= 400) && (statusCode <= 599) ? statusCode : StatusCodes.Status502BadGateway,
                title: ex.FormatError());
        }
        catch (InvalidOperationException ex)
        {
            // Unresolved profile or region
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
