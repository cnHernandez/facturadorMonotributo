using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace Back.Utils
{
    public class NotFoundResultFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var request = context.HttpContext.Request;
            var requestInfo = new StringBuilder();
            requestInfo.AppendLine($"Método: {request.Method}");
            requestInfo.AppendLine($"Ruta: {request.Path}");

            if (request.QueryString.HasValue)
                requestInfo.AppendLine($"QueryString: {request.QueryString}");

            if (context.Result is NotFoundResult || context.Result is NotFoundObjectResult)
            {
                Log.Error("Recurso no encontrado (NotFound) - {RequestInfo}", requestInfo.ToString());
                context.Result = new ObjectResult(new
                {
                    message = "Recurso no encontrado",
                    details = request.Path.ToString()
                })
                {
                    StatusCode = 404
                };
            }
        }

        public void OnActionExecuting(ActionExecutingContext context) { }
    }
}
