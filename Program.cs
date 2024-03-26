using dotnet_user.Services;
using dotnet_user.Repositories;
using dotnet_user.Repositories.Interface;
using dotnet_user.Services.Interface;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 딩쩤 DateService
builder.Services.AddScoped<IDateService, DateService>();

// 딩쩤 BodyService
builder.Services.AddScoped<IBodyService, BodyService>();

// 딩쩤 BodyRepository
builder.Services.AddScoped<IBodyRepository, BodyRepository>();

// 딩쩤 BiopsyService
builder.Services.AddScoped<IBiopsyService, BiopsyService>();

// 딩쩤 BiopsyRepository
builder.Services.AddScoped<IBiopsyRepository, BiopsyRepository>();

// 딩쩤 DiagnosisService
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();

// 딩쩤 DiagnosisRepository
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();

// 딩쩤 DoctorService
builder.Services.AddScoped<IDoctorService, DoctorService>();

// 딩쩤 DoctorRepository
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// 딩쩤ⓧ쩖찥걷
builder.Services.AddScoped<OutpatientVisitsService>();
builder.Services.AddScoped<EmployeeVisitsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();