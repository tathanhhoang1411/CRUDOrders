using CRUDOrders.Application.Behaviors;
using CRUDOrders.Infrastructure.Cache;
using CRUDOrders.Infrastructure.Data;
using CRUDOrders.Infrastructure.Identity;
using CRUDOrders.Infrastructure.Services;
using CRUDOrders.Application.Mapping;
using CRUDOrders.Presentation.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using MediatR;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────────
// Configure Serilog
// ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ──────────────────────────────────────────────────────────────────
// Core services
// ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // De validation duoc xu ly thong nhat boi MediatR ValidationBehavior,
        // dam bao format response luon la Result<T> chuan hoa (rule #6)
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    // 1. Tat ca phan he (Full API)
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "🌟 CRUD Orders API — Tất cả phân hệ",
        Version = "v1",
        Description = "Tổng hợp toàn bộ API quản lý xác thực, sản phẩm và đơn hàng"
    });

    // 2. Phan he 1: Xac thuc & Phan quyen
    c.SwaggerDoc("auth", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "🔐 Phân hệ 1: Xác thực & Phân quyền (Auth)",
        Version = "v1",
        Description = "API đăng nhập, cấp phát JWT token, quản lý tài khoản người dùng"
    });

    // 3. Phan he 2: Quan ly San pham & Ton kho
    c.SwaggerDoc("products", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "📦 Phân hệ 2: Quản lý Sản phẩm & Tồn kho (Products)",
        Version = "v1",
        Description = "API CRUD sản phẩm, kiểm soát tồn kho, chống bán vượt tồn kho (Optimistic Concurrency)"
    });

    // 4. Phan he 3: Quan ly Don hang
    c.SwaggerDoc("orders", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "🛒 Phân hệ 3: Quản lý Đơn hàng (Orders)",
        Version = "v1",
        Description = "API tạo đơn hàng, trừ kho trong giao dịch ACID, cập nhật trạng thái đơn, tra cứu lịch sử"
    });

    // Loc endpoint theo phan he: v1 hien thi tat ca, cac phan he khac loc theo GroupName
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "v1") return true;
        return string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase);
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhap token JWT trong format: Bearer {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ──────────────────────────────────────────────────────────────────
// Database
// ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ──────────────────────────────────────────────────────────────────
// AutoMapper
// ──────────────────────────────────────────────────────────────────
var mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
{
    cfg.AddProfile(new MappingProfile());
});
var mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

// ──────────────────────────────────────────────────────────────────
// MediatR + Pipeline Behaviors
// ──────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Dang ky Pipeline Behaviors theo thu tu xu ly:
// 1. ValidationBehavior chay truoc — kiem tra input
// 2. CachingBehavior chay sau — tra ve cache neu co
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

// ──────────────────────────────────────────────────────────────────
// FluentValidation + Pagination Options
// ──────────────────────────────────────────────────────────────────
builder.Services.Configure<CRUDOrders.Application.Common.PaginationSettings>(
    builder.Configuration.GetSection(CRUDOrders.Application.Common.PaginationSettings.SectionName));

builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

// ──────────────────────────────────────────────────────────────────
// Redis Cache (bat/tat qua Features:EnableRedisCache)
// ──────────────────────────────────────────────────────────────────
var enableRedis = builder.Configuration.GetValue<bool>("Features:EnableRedisCache");
if (enableRedis)
{
    var redisConn = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Redis:ConnectionString is not configured");
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    Log.Information("Redis Cache: ENABLED ({ConnectionString})", redisConn);
}
else
{
    builder.Services.AddSingleton<ICacheService, NullCacheService>();
    Log.Information("Redis Cache: DISABLED (using NullCacheService)");
}

// ──────────────────────────────────────────────────────────────────
// Infrastructure Services
// ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

// Repositories
builder.Services.AddScoped(typeof(CRUDOrders.Infrastructure.Data.Repositories.IRepository<>),
    typeof(CRUDOrders.Infrastructure.Data.Repositories.Repository<>));
builder.Services.AddScoped<CRUDOrders.Infrastructure.Data.Repositories.IProductRepository,
    CRUDOrders.Infrastructure.Data.Repositories.ProductRepository>();
builder.Services.AddScoped<CRUDOrders.Infrastructure.Data.Repositories.IUserRepository,
    CRUDOrders.Infrastructure.Data.Repositories.UserRepository>();
builder.Services.AddScoped<CRUDOrders.Infrastructure.Data.Repositories.IOrderRepository,
    CRUDOrders.Infrastructure.Data.Repositories.OrderRepository>();

// ──────────────────────────────────────────────────────────────────
// JWT Authentication
// ──────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "CRUDOrders",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "CRUDOrders.Users",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────────────────────────
// Build
// ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.InitializeAsync();
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "🌟 Tất cả phân hệ (Full API)");
        c.SwaggerEndpoint("/swagger/auth/swagger.json", "🔐 Phân hệ 1: Xác thực & Tài khoản");
        c.SwaggerEndpoint("/swagger/products/swagger.json", "📦 Phân hệ 2: Quản lý Sản phẩm & Tồn kho");
        c.SwaggerEndpoint("/swagger/orders/swagger.json", "🛒 Phân hệ 3: Quản lý Đơn hàng");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseExceptionMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Tu dong chuyen huong tu root / ve /swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }
