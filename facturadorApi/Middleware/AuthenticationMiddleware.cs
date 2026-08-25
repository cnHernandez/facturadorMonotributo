using System.Text.Json;
using System.Security.Claims;

namespace Back.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Dictionary<string, Dictionary<string, List<string>>>? _permissions;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;

            var rolesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "conf", "AuthRole.json");
            if (File.Exists(rolesFilePath))
            {
                var json = File.ReadAllText(rolesFilePath);
                _permissions = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(json);
            }
            else
            {
                _permissions = new Dictionary<string, Dictionary<string, List<string>>>();
            }
        }

        public async Task Invoke(HttpContext context)
        {
            var controller = context.GetRouteValue("controller")?.ToString() + "Controller";
            var action = context.GetRouteValue("action")?.ToString();

            if (controller != null && action != null &&
                _permissions!.TryGetValue(controller, out var actions) &&
                actions.TryGetValue(action, out var allowedRoles))
            {
                var user = context.User;
                if (user == null || !user.Identity!.IsAuthenticated)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "No autenticado." }));
                    return;
                }

                var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

                if (!allowedRoles.Any(role => userRoles.Contains(role)))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Acceso denegado." }));
                    return;
                }
            }

            await _next(context);
        }
    }
}
