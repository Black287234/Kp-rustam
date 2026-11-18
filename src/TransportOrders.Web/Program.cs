using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using TransportOrders.Web.Controllers;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(o => BindingTexts.Apply(o.ModelBindingMessageProvider));
// Кириллица выводится в разметку как есть, без замены на числовые ссылки
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement, UnicodeRanges.LatinExtendedA, UnicodeRanges.Cyrillic, UnicodeRanges.GeneralPunctuation, UnicodeRanges.Arrows));

// База SQLite: файл App_Data/transport.db рядом с приложением
var dataFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataFolder);
var connectionString = builder.Configuration.GetConnectionString("Transport")
    ?? $"Data Source={Path.Combine(dataFolder, "transport.db")}";
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new SqliteDatabase(connectionString));
builder.Services.AddSingleton<SchemaMigrator>();
builder.Services.AddScoped<CarStore>();
builder.Services.AddScoped<EmployeeStore>();
builder.Services.AddScoped<TripStore>();
builder.Services.AddScoped<RequestDesk>();
builder.Services.AddScoped<CarAvailability>();
builder.Services.AddScoped<DispatchDesk>();
builder.Services.AddScoped<FleetGuard>();

var app = builder.Build();

// Создание и обновление структуры базы, загрузка учебных данных при первом запуске
app.Services.GetRequiredService<SchemaMigrator>().Run();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Failure");
}

// Для несуществующих адресов показывается собственная страница с кодом ответа
app.UseStatusCodePagesWithReExecute("/Home/Code/{0}");
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Нужен тестам для запуска приложения целиком.</summary>
public partial class Program;
