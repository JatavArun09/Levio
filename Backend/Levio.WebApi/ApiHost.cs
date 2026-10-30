using Levio.Infrastructure;
using Levio.Modules.Auditing;
using Levio.Modules.Identity;
using Levio.Modules.LeaveManagement;
using Levio.WebApi.Middleware;
using Microsoft.OpenApi;

namespace Levio.WebApi;

public static class ApiHost
{
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ApiHost).Assembly.GetName().Name
        });
        builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            var legacy = options.InvalidModelStateResponseFactory;
            options.InvalidModelStateResponseFactory = context =>
                context.HttpContext.Request.Path.StartsWithSegments("/api/v1")
                    ? new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
                    {
                        success = false, message = "Request validation failed.", data = (object?)null,
                        errors = context.ModelState.Values.SelectMany(v => v.Errors)
                            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid request value." : e.ErrorMessage).ToArray()
                    }) : legacy(context);
        });
        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddLeaveManagementModule(builder.Configuration);
        builder.Services.AddAuditingModule();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Levio Leave Management API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
                BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Enter JWT Token"
            });
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (context.HttpContext.Request.Path.StartsWithSegments("/api/v1"))
                await response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(response.StatusCode),
                    data = (object?)null, errors = Array.Empty<string>()
                });
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        }
        if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
            app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
