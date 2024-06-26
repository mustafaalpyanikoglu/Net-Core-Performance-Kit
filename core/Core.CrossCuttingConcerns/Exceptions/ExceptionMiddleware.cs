﻿using System.Net.Mime;
using Core.CrossCuttingConcerns.Exceptions.Handlers;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Core.CrossCuttingConcerns.Exceptions
{
    /// <summary>
    /// Middleware to handle and log exceptions occurring during HTTP request processing.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly HttpExceptionHandler _httpExceptionHandler;
        private readonly LoggerServiceBase _loggerService;
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the ExceptionMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="contextAccessor">The IHttpContextAccessor to access the HttpContext.</param>
        /// <param name="loggerService">The LoggerServiceBase implementation used for logging exceptions.</param>
        public ExceptionMiddleware(RequestDelegate next, IHttpContextAccessor contextAccessor, LoggerServiceBase loggerService)
        {
            _next = next;
            _contextAccessor = contextAccessor;
            _loggerService = loggerService;
            _httpExceptionHandler = new HttpExceptionHandler();
        }

        /// <summary>
        /// Invokes the middleware to handle exceptions during HTTP request processing.
        /// </summary>
        /// <param name="context">The current HttpContext.</param>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await LogException(context, exception);
                await HandleExceptionAsync(context.Response, exception);
            }
        }

        private Task HandleExceptionAsync(HttpResponse response, Exception exception)
        {
            response.ContentType = MediaTypeNames.Application.Json;
            _httpExceptionHandler.Response = response;
            return _httpExceptionHandler.HandleExceptionAsync(exception);
        }

        private Task LogException(HttpContext context, Exception exception)
        {
            List<LogParameter> logParameters =
                new() {
                    new LogParameter { Type = context.GetType().Name, Value = exception.ToString() }
                };

            LogDetail logDetail =
                new() {
                    MethodName = _next.Method.Name,
                    Parameters = logParameters,
                    User = _contextAccessor.HttpContext?.User.Identity?.Name ?? "?"
                };

            _loggerService.Info(JsonSerializer.Serialize(logDetail));
            return Task.CompletedTask;
        }
    }
}
