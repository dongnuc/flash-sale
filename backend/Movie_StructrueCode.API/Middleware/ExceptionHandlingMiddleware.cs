
using Movie_StructureCode.Domain.Exceptions;
using System.Text.Json;

namespace Movie_StructrueCode.API.Middleware
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) 
            => _logger = logger;
        
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message);
                await HandleExceptionAsync(context, ex);
            }

        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = GetStatusCode(ex);

            var response = new
            {
                title = GetTitle(ex),
                status = statusCode,
                detail = ex.Message,
                errors = GetErrors(ex)
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        //private static object GetErrors(Exception ex)
        //{
        //    throw new NotImplementedException();
        //}

        private static object GetTitle(Exception ex)
        => ex switch
        {
            DomainException applicationException => applicationException.Title,
            _ => "Server Error"
        };

        private static int GetStatusCode(Exception ex)
        => ex switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            FormatException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        // chưa hiểu   
        private static IReadOnlyCollection<Movie_StructureCode.Application.Exceptions.ValidationError> 
            GetErrors(Exception exception)
        {
            IReadOnlyCollection<Movie_StructureCode.Application.Exceptions.ValidationError> errors = null;
            if(exception is Movie_StructureCode.Application.Exceptions.ValidationException validationException)
            {
                errors = validationException.Errors;
            }
            return errors;
        }


    }
}
