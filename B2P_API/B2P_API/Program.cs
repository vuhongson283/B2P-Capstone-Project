using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
