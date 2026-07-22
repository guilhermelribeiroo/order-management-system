using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebAPI.Filters
{
    public class JsonExceptionFilter : IExceptionFilter
    {
        private readonly IWebHostEnvironment _env;
        public JsonExceptionFilter(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is ValidationException validationException)
            {
                var result = new ObjectResult(new
                {
                    message = validationException.Errors.Select(x => x.ErrorMessage)
                })
                {
                    StatusCode = 400
                };

                context.Result = result;
            }
            else
            {
                var result = new ObjectResult(new
                {
                    message = "A server error occurred.",
                    detailedMessage = _env.IsDevelopment() ? context.Exception.Message : null
                })
                {
                    StatusCode = 500
                };
                context.Result = result;
            }
        }
    }
}
