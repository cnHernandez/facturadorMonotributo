using System.Net;
using Serilog;

namespace Back.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException unauthorizedEx)
            {
                Log.Error(unauthorizedEx, "[HTTP-401] Acceso no autorizado");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Acceso no autorizado.",
                    error = unauthorizedEx.Message
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HTTP-500] [path: {requestPath}] Ocurrió un error no manejado", context.Request.Path);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = ex.Message,
                    error = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
