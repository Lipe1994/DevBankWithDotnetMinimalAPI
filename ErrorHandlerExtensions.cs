using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace DevBankWithDotnetMinimalAPI;

public static class ErrorHandlerExtensions
{
    public static IApplicationBuilder UseErrorHandler(
                                      this IApplicationBuilder appBuilder)
    {
        return appBuilder.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var exceptionHandlerFeature = context
                                                .Features
                                                .Get<IExceptionHandlerFeature>();

                if (exceptionHandlerFeature != null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

                    await context.Response.WriteAsync("");
                }
            });
        });
    }
}