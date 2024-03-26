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

// ���U DateService
builder.Services.AddScoped<IDateService, DateService>();

// ���U BodyService
builder.Services.AddScoped<IBodyService, BodyService>();

// ���U BodyRepository
builder.Services.AddScoped<IBodyRepository, BodyRepository>();

// ���U BiopsyService
builder.Services.AddScoped<IBiopsyService, BiopsyService>();

// ���U BiopsyRepository
builder.Services.AddScoped<IBiopsyRepository, BiopsyRepository>();

// ���U DiagnosisService
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();

// ���U DiagnosisRepository
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();

// ���U DoctorService
builder.Services.AddScoped<IDoctorService, DoctorService>();

// ���U DoctorRepository
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// ���U��L�A��
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