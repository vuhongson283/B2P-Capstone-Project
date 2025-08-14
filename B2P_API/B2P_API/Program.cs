using B2P_API;
using B2P_API.Hubs;
using B2P_API.Interface;
using B2P_API.Map;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Repository;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Đọc connection string
var connectionString = builder.Configuration.GetConnectionString("MyCnn");

// Đăng ký DbContext
builder.Services.AddDbContext<SportBookingDbContext>(options =>
	options.UseSqlServer(connectionString));

// Đăng ký AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// THÊM SIGNALR
builder.Services.AddSignalR();

// FIX CORS cho SignalR - Dùng policy cụ thể
builder.Services.AddCors(options =>
{
	options.AddPolicy("SignalRPolicy", policy =>
	{
		policy.WithOrigins(
				"http://localhost:3000",
				"https://localhost:3000",
				"http://localhost:3001",
				"https://yourdomain.com")
			  .AllowAnyMethod()
			  .AllowAnyHeader()
			  .AllowCredentials()
			  .SetIsOriginAllowed(origin => true); // Cho phép mọi origin (chỉ khi DEV)
	});
});

// ✅ JWT Authentication Configuration
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	var jwtSettings = builder.Configuration.GetSection("JWT");

	options.TokenValidationParameters = new TokenValidationParameters
	{
		RoleClaimType = "roleId", // ✅ THÊM MỚI từ nhánh test
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,

		ValidIssuer = jwtSettings["Issuer"],
		ValidAudience = jwtSettings["Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(jwtSettings["AccessSecret"]!)
		),

		ClockSkew = TimeSpan.Zero
	};

	options.Events = new JwtBearerEvents
	{
		OnAuthenticationFailed = context =>
		{
			Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
			return Task.CompletedTask;
		},
		OnTokenValidated = context =>
		{
			Console.WriteLine($"JWT Token validated for user: {context.Principal?.FindFirst("userId")?.Value}");
			return Task.CompletedTask;
		}
	};
});

// ✅ Add Authorization
builder.Services.AddAuthorization();

// Controllers + JSON config
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

		// ✅ CHỈ DÙNG CÁC OPTIONS CÓ SẴN:
		options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.WriteIndented = true; // For debugging
	});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Caching
builder.Services.AddMemoryCache();

// Suppress automatic 400 responses
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.SuppressModelStateInvalidFilter = true;
});

// Đăng ký các Repository & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<RatingRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<RatingService>();

builder.Services.AddScoped<ISliderManagementRepository, SliderManagementRepository>();
builder.Services.AddScoped<SliderManagementService>();

builder.Services.AddScoped<ICourtCategoryRepository, CourtCategoryRepository>();
builder.Services.AddScoped<CourtCategoryService>();

builder.Services.AddScoped<IFacilityRepositoryForUser, FacilityRepository>();
builder.Services.AddScoped<IFacilityManageRepository, FacilityManageRepository>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<FacilityService>();

builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISMSService, eSmsService>();

builder.Services.AddScoped<AccountManagementRepository>();
builder.Services.AddScoped<IAccountManagementRepository, AccountManagementRepository>();
builder.Services.AddScoped<AccountManagementService>();

builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<AccountService>();

builder.Services.AddScoped<BlogRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<BlogService>();

builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<CommentService>();

builder.Services.AddScoped<CourtRepository>();
builder.Services.AddScoped<ICourtRepository, CourtRepository>();
builder.Services.AddScoped<CourtServices>();

builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<BookingService>();

builder.Services.AddScoped<ITimeSlotManagementRepository, TimeSlotManagementRepository>();
builder.Services.AddScoped<ITimeSlotManagementService, TimeslotManagementService>();

builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();

builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ReportService>();

// ✅ THÊM MỚI từ nhánh test - Auth services
builder.Services.AddScoped<JWTHelper>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<VNPayService>();

builder.Services.Configure<ESMSSettings>(builder.Configuration.GetSection("ESMSSettings"));
builder.Services.Configure<ZaloPayConfig>(builder.Configuration.GetSection("ZaloPay"));
builder.Services.AddHttpClient<ZaloPayService>(client =>
{
	client.Timeout = TimeSpan.FromSeconds(30);
	client.DefaultRequestHeaders.Add("User-Agent", "ZaloPayAPI/1.0");
});
builder.Services.AddScoped<ZaloPayService>();

builder.Services.AddScoped<IBookingNotificationService, BookingNotificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SignalRPolicy"); // Áp dụng policy cụ thể

// Security headers
app.Use(async (context, next) =>
{
	context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
	context.Response.Headers.Add("X-Frame-Options", "DENY");
	context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
	await next();
});

// Request logging middleware
app.Use(async (context, next) =>
{
	var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
	logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
	await next();
	logger.LogInformation($"Response: {context.Response.StatusCode}");
});

// Health check endpoint
app.MapGet("/health", () => new
{
	service = "B2P API with ZaloPay",
	status = "running",
	timestamp = DateTimeOffset.UtcNow
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingHub>("/bookinghub");

app.Run();