<<<<<<< HEAD

﻿using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Services;
using Microsoft.EntityFrameworkCore;
﻿using B2P_API.Repositories;
using B2P_API.Response;
using B2P_API;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SportBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
builder.Services.AddMemoryCache();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ICourtCategoryRepository, CourtCategoryRepository>();
builder.Services.AddScoped<CourtCategoryService>();
builder.Services.AddScoped<IFacilityRepositoryForUser, FacilityRepository>();
builder.Services.AddScoped<FacilityService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISMSService, eSMSService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Đọc chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IFacilityRepository, FacilityManageRepository>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
// Thêm DbContext vào DI container
builder.Services.AddDbContext<SportBookingDbContext>(options =>
    options.UseSqlServer(connectionString));



// ✅ Đăng ký Repository và Service
builder.Services.AddScoped<BlogRepository>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<CommentService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.Run();
=======
using B2P_API.Interface;
using B2P_API.Map;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register DbContext
builder.Services.AddDbContext<SportBookingDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAutoMapper(typeof(MappingProfile));

//Add Scoped Service
builder.Services.AddScoped<AccountManagementService>();
builder.Services.AddScoped<AccountService>();

//Add Scoped Interface 
builder.Services.AddScoped<IAccountManagementRepository, AccountManagementRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
//Add Scoped Repo
builder.Services.AddScoped<AccountManagementRepository>(); 
builder.Services.AddScoped<AccountRepository>(); 




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
>>>>>>> TrongVV
