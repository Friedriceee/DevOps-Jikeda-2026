using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.User;
using TakeoutPlatform.Api.Features.Pricing;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Checkout;
using TakeoutPlatform.Api.Features.Seed;
using TakeoutPlatform.Api.Features.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "请求参数不合法"
                : error.ErrorMessage)
            .FirstOrDefault() ?? "请求参数不合法";

        return new BadRequestObjectResult(ApiResult<object>.Fail(message));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes and should come from an environment secret.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            RoleClaimType = "role",
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<DishService>();
builder.Services.AddScoped<SpecialOfferService>();
builder.Services.AddScoped<MerchantRegistrationService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Order.OrderService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Order.MerchantFulfillmentService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Rider.RiderDeliveryService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Rider.RiderRegistrationService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<AdminBootstrapOptions>(builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Audit.AuditLogService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Audit.IAuditLogger>(services =>
    services.GetRequiredService<TakeoutPlatform.Api.Features.Audit.AuditLogService>());
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Admin.AdminAccountService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Admin.AdminCouponService>();
builder.Services.AddScoped<TakeoutPlatform.Api.Features.Admin.AdminOrderService>();
builder.Services.AddSingleton<IDiscountStrategy, MerchantDiscountStrategy>();
builder.Services.AddSingleton<IDiscountStrategy, PlatformCouponDiscountStrategy>();

const string DevCors = "dev-cors";
builder.Services.AddCors(options =>
    options.AddPolicy(DevCors, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Local/staging containers can opt into migration and demo accounts through
// configuration. Production keeps both operations under deployment control.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
await StagingSeeder.RunAsync(app.Services);
await AdminBootstrap.RunAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCors);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// 供集成测试（WebApplicationFactory）引用
public partial class Program { }
