using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using TransportOrders.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Кириллица выводится в разметку как есть, без замены на числовые ссылки
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic, UnicodeRanges.GeneralPunctuation));
builder.Services.AddSingleton<ICarSource, MemoryCarSource>();

var app = builder.Build();

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
