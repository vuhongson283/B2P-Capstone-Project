using B2P_API;
using B2P_API.Interface;
using B2P_API.Map;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Repository;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using static B2P_API.Services.TwilioSMSService;
using B2P_API.Utils;

var builder = WebApplication.CreateBuilder(args);

// Đọc connection string
var connectionString = builder.Configuration.GetConnectionString("MyCnn");

// Đăng ký DbContext
builder.Services.AddDbContext<SportBookingDbContext>(options =>
    options.UseSqlServer(connectionString));

// Đăng ký AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// **THÊM CORS - Cho phép tất cả (Development)**
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Cấu hình JSON để tránh vòng lặp
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

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

// Đăng ký các Repository & Service
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<ISliderManagementRepository, SliderManagementRepository>();
builder.Services.AddScoped<SliderManagementService>();

builder.Services.AddScoped<ICourtCategoryRepository, CourtCategoryRepository>();
builder.Services.AddScoped<CourtCategoryService>();

builder.Services.AddScoped<IFacilityRepositoryForUser, FacilityRepository>(); // Nếu dùng
builder.Services.AddScoped<IFacilityRepository, FacilityManageRepository>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
// Đăng ký trực tiếp class
builder.Services.AddScoped<FacilityService>();

builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISMSService, TwilioSMSService>();

builder.Services.AddScoped<AccountManagementRepository>();
builder.Services.AddScoped<IAccountManagementRepository, AccountManagementRepository>();
builder.Services.AddScoped<AccountManagementService>();

builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<AccountService>();

builder.Services.AddScoped<BlogRepository>();
builder.Services.AddScoped<BlogService>();

builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<CommentService>();

builder.Services.AddScoped<CourtRepository>();
builder.Services.AddScoped<CourtServices>();

// Booking services
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<BookingService>();

// TimeSlot services
builder.Services.AddScoped<ITimeSlotManagementRepository, TimeSlotManagementRepository>();
builder.Services.AddScoped<ITimeSlotManagementService, TimeslotManagementService>();

// Bank Account services
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();

// Excel Export services
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
ExcelPackage.License.SetNonCommercialPersonal("B2P");

// Report services
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<ReportService>();
// Đăng ký TwilioSettings
builder.Services.Configure<TwilioSettings>(
    builder.Configuration.GetSection("Twilio"));
var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
