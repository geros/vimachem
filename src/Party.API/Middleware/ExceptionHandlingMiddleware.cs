using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Party.API.Domain.Exceptions;

namespace Party.API.Middleware;

public class ExceptionHandlingMiddleware {
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionHandlingMiddleware> _logger;

	public ExceptionHandlingMiddleware(RequestDelegate next,
		ILogger<ExceptionHandlingMiddleware> logger) {
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context) {
		try {
			await _next(context);
		} catch (Exception ex) {
			_logger.LogError(ex, "Unhandled exception");
			await HandleExceptionAsync(context, ex);
		}
	}

	private static async Task HandleExceptionAsync(HttpContext context, Exception ex) {
		var (statusCode, message) = ex switch {
			NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
			DomainException => (StatusCodes.Status400BadRequest, ex.Message),
			_ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
		};

		context.Response.StatusCode = statusCode;
		context.Response.ContentType = "application/json";

		await context.Response.WriteAsJsonAsync(new ProblemDetails {
			Status = statusCode,
			Title = ex.GetType().Name,
			Detail = message,
			Instance = Activity.Current?.Id ?? context.TraceIdentifier
		});
	}
}
