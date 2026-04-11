using Application.Common.Interfaces;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PropertyPortal.API.Converters;
using PropertyPortal.API.Filters;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Properties;
using PropertyPortal.Application.DTOs.Residents;
using PropertyPortal.Application.DTOs.Units;
using PropertyPortal.Application.Validators.Properties;
using PropertyPortal.Domain.Entities;
using PropertyPortal.Infrastructure.Data;
using PropertyPortal.Infrastructure.Filters;
using PropertyPortal.Infrastructure.Interceptors;
using PropertyPortal.Infrastructure.Repositories;
using PropertyPortal.Infrastructure.Tenancy;
using PropertyPortal.Infrastructure.UnitOfWork;
using PropertyPortal.Infrastructure.Web.Filters;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("LocalPropertyPortalConnection"), sqlOptions => sqlOptions.CommandTimeout(60))
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

});

/* **********************************************************************************************/
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(AuditInterceptor));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PropertyPortal.Application.Properties.GetPropertiesQuery).Assembly));

////Mapster: this config is only necessary if DTO / Entity field names are different(i.e.dto.Num -> unit.UnitNumber), otherwise it works out of the box
//TypeAdapterConfig<UnitUpdateDto, Unit>.NewConfig().Map(dest => dest.UnitNumber, src => src.Num);  // add using Mapster;

// Calculated fields in PropertyResponseDto 
// Mapster will turn these into SQL: COUNT(Units.Id) and SUM(Units.Rent)
//TypeAdapterConfig<Property, PropertyResponseDto>.NewConfig()
//    .Map(dest => dest.UnitCount, src => src.Units.Count)
//    .Map(dest => dest.TotalMonthlyRent, src => src.Units.Sum(u => u.Rent));
// same thing with null checking
TypeAdapterConfig<Property, PropertyResponseDto>
    .NewConfig()
    .Map(dest => dest.Address, src => src.Address) // force explicit mapping of the Address record
    .Map(dest => dest.UnitCount, src => src.Units != null ? src.Units.Count : 0)
    .Map(dest => dest.TotalMonthlyRent, src => src.Units != null ? src.Units.Sum(u => u.Rent) : 0);

TypeAdapterConfig<Resident, ResidentResponseDto>
    .NewConfig()
    .Map(dest => dest.UnitNumber, src => src.Unit.UnitNumber)
    // Explicitly map the Address complex type
    //.Map(dest => dest.Address, src => src.Address)
    // Flatten the Property Name from the navigation property
    .Map(dest => dest.PropertyName, src => src.Property != null ? src.Property.Name : "Unassigned")
// Pull from the active lease contract, not the unit's market price.
    .Map(dest => dest.RentAmount, src => src.Leases
        .Where(l => l.Status == "Active" &&
                    l.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
                    l.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) &&
                    !l.IsDeleted)
        .Select(l => l.MonthlyRent)
        .FirstOrDefault())
    .Map(dest => dest.LeaseStartDate, src => src.Leases
            .Where(l => l.Status == "Active" &&
                        l.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
                        l.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) &&
                        !l.IsDeleted)
            .Select(l => l.StartDate)
            .FirstOrDefault())
    .Map(dest => dest.LeaseEndDate, src => src.Leases
            .Where(l => l.Status == "Active" &&
                        l.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
                        l.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) &&
                        !l.IsDeleted)
            .Select(l => l.EndDate)
            .FirstOrDefault());

TypeAdapterConfig<ResidentRequestDto, Resident>
    .NewConfig()
    .Map(dest => dest.Address, src => src.Address);

TypeAdapterConfig<Unit, UnitResponseDto>.NewConfig()
    .Map(dest => dest.PropertyName, src => src.Property.Name);

/*************************************************************************************************/

builder.Services.AddValidatorsFromAssembly(typeof(PropertyCreateDtoValidator).Assembly);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ConcurrencyExceptionFilter>(); // Existing
    options.Filters.Add<ApiExceptionFilter>();    // New
    options.Filters.Add<ValidationFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ByteArrayToNullableBase64Converter());
        //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer(); // Required for Swagger to discover endpoints

// Generates the Swagger specification
builder.Services.AddSwaggerGen();

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

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(builder => builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin());
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Serves the generated JSON document
    app.UseSwaggerUI(); // Serves the interactive UI (at /swagger)

    // Map the OpenAPI endpoint
    app.MapOpenApi();
    // Map the Scalar API reference UI
    //app.MapScalarApiReference();
    //// Optional: Redirect the root URL to the Scalar UI
    //app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

/*************************************************************************************************/
app.UseMiddleware<PropertyPortal.API.Middleware.TenantMiddleware>();
/*************************************************************************************************/

app.MapControllers();

app.Run();
