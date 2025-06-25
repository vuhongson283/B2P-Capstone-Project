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
