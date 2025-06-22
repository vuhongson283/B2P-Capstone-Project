using B2P_API.Repositories;
using B2P_API.Services;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API;
using Microsoft.EntityFrameworkCore;
using B2P_API.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Đăng ký DbContext 
builder.Services.AddDbContext<SportBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
