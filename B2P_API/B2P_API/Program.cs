<<<<<<< HEAD
﻿using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Services;
using Microsoft.EntityFrameworkCore;
=======
﻿using B2P_API.Repositories;
using B2P_API.Services;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API;
using Microsoft.EntityFrameworkCore;
using B2P_API.Repository;
>>>>>>> SangNN2

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
<<<<<<< HEAD
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
=======

// ✅ Đăng ký DbContext 
builder.Services.AddDbContext<SportBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Đăng ký Repository và Service
builder.Services.AddScoped<BlogRepository>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<CommentService>();

>>>>>>> SangNN2
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
