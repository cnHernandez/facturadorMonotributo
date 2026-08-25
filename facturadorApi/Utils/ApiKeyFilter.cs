using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ApiKeyFilter : IAuthorizationFilter
{
    private readonly string _apiKey;
    private const string HeaderName = "X-API-KEY";

    public ApiKeyFilter(IConfiguration config)
    {
        _apiKey = config["ApiSettings:ApiKey"] ?? throw new InvalidOperationException("ApiKey no está configurada.");
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var endpoint = context.ActionDescriptor.EndpointMetadata;

        if (endpoint.Any(e => e is AllowAnonymousAttribute) ||
            endpoint.Any(e => e is AuthorizeAttribute))
            return;

        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(HeaderName, out var key) || key != _apiKey)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
