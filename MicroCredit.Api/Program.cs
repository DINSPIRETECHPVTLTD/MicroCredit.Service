using System.Text;
using System.Text.Json.Serialization;
using MicroCredit.Api.Abstractions;
using MicroCredit.Api.Middlewares;
using MicroCredit.Application;
using MicroCredit.Application.Common;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Contracts;
using MicroCredit.Infrastructure;
using MicroCredit.Infrastructure.Providers.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

static void WriteStartupError(Exception ex)
{
    try
    {
        var dir = Path.Combine(Path.GetTempPath(), "MicroCredit");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "startup-failure.txt");
        File.WriteAllText(path, $"{DateTime.UtcNow:O}{Environment.NewLine}{ex}{Environment.NewLine}{ex.StackTrace}");
    }
    catch { /* best-effort only */ }
}

try
{
    var builder = WebApplication.CreateBuilder(args);

    SerilogProvider.Configure(builder.Configuration);
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
            if (jwtSettings?.Key is not { Length: > 0 })
                return;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserContext, UserContext>();

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT token in the format: Bearer {your token}"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();


    app.MapControllers();

    try
    {
        Log.Information("Starting MicroCredit API");
        app.Run();
    }
    finally
    {
        Log.CloseAndFlush();
    }
}
catch (Exception ex)
{
    WriteStartupError(ex);
    if (Log.Logger != null)
        Log.Fatal(ex, "Application failed to start");
    throw;
}
