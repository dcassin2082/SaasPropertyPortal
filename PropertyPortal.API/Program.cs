using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyPortal.API.Converters;
using PropertyPortal.API.Filters;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Infrastructure.Data;
using PropertyPortal.Infrastructure.Interceptors;
using PropertyPortal.Infrastructure.Repositories;
using PropertyPortal.Infrastructure.Tenancy;
using PropertyPortal.Infrastructure.UnitOfWork;
using PropertyPortal.Infrastructure.Web.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("LocalPropertyPortalConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

/* **********************************************************************************************/
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(AuditInterceptor));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PropertyPortal.Application.Properties.GetPropertiesQuery).Assembly));

/*************************************************************************************************/

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ConcurrencyExceptionFilter>(); // Existing
    options.Filters.Add<ApiExceptionFilter>();    // New
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ByteArrayToNullableBase64Converter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Authentication & JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Prevents mapping 'sub' to long XML namespaces - must be added before any Authentication configuration
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // clear the weird Microsoft claim mapping

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

/*************************************************************************************************/
app.UseMiddleware<PropertyPortal.API.Middleware.TenantMiddleware>();
/*************************************************************************************************/

app.MapControllers();

app.Run();
