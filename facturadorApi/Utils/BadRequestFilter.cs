using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace Back.Utils
{
    public class BadRequestResultFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is BadRequestObjectResult badRequest)
            {
                string requestPath = context.HttpContext.Request.Path;
                string details;

                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(badRequest.Value);
                    var jsonNode = JsonNode.Parse(json);
                    details = jsonNode?["errors"]?.ToJsonString()
                        ?? jsonNode?["Errors"]?.ToJsonString()
                        ?? badRequest.Value?.ToString()
                        ?? "No determinado";
                }
                catch
                {
                    details = badRequest.Value?.ToString() ?? "No determinado";
                }

                Log.Error("[HTTP-400] [path: {requestPath}] Solicitud incorrecta: {details}", requestPath, details);

                context.Result = new ObjectResult(new
                {
                    message = "Solicitud incorrecta",
                    details = details
                })
                {
                    StatusCode = 400
                };
            }

            await next();
        }
    }
}
