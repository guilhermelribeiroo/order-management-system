using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using WebAPI.Filters;

namespace UnitTests.WebAPI
{
    public class JsonExceptionFilterTests
    {
        private readonly JsonExceptionFilter _filter;
        public JsonExceptionFilterTests()
        {
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns("Development");
            _filter = new JsonExceptionFilter(envMock.Object);
        }

        [Fact]
        public void OnException_ValidationException_ShouldReturn400()
        {
            var context = BuildExceptionContext(new ValidationException("invalid"));

            _filter.OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void OnException_ValidationException_ShouldReturnErrorMessages()
        {
            var failures = new List<ValidationFailure>
            {
                new("CustomerId", "CustomerId is required"),
                new("Items", "Order must contain at least one item")
            };
            var context = BuildExceptionContext(new ValidationException(failures));

            _filter.OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            var body = result.Value!;
            var messages = body.GetType().GetProperty("message")!.GetValue(body) as IEnumerable<string>;
            Assert.Contains("CustomerId is required", messages!);
            Assert.Contains("Order must contain at least one item", messages!);
        }

        [Fact]
        public void OnException_GenericException_ShouldReturn500()
        {
            var context = BuildExceptionContext(new Exception("Something broke"));

            _filter.OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public void OnException_GenericException_ShouldReturnDetailedMessage()
        {
            var context = BuildExceptionContext(new Exception("Something broke"));

            _filter.OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            var body = result.Value!;
            var detail = body.GetType().GetProperty("detailedMessage")!.GetValue(body) as string;
            Assert.Equal("Something broke", detail);
        }

        // --- Helpers ---
        private static ExceptionContext BuildExceptionContext(Exception exception)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }
    }
}