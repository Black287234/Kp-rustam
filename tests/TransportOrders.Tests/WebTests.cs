using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TransportOrders.Tests;

/// <summary>Приложение целиком: страницы и API отвечают с правильными кодами.</summary>
public class WebTests
{
    private sealed class App : WebApplicationFactory<Program>
    {
        public readonly string File = Path.Combine(Path.GetTempPath(), $"transport-web-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSetting("ConnectionStrings:Transport", $"Data Source={File};Pooling=False");
    }

    [Test, DisplayName("Основные страницы открываются")]
    [Arguments("/")]
    [Arguments("/Trips")]
    [Arguments("/Trips/Card/6")]
    [Arguments("/Cars")]
    [Arguments("/Employees")]
    [Arguments("/Board")]
    public async Task Pages_open(string url)
    {
        await using var app = new App();
        var response = await app.CreateClient().GetAsync(url);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test, DisplayName("Несуществующая запись — страница «Запись не найдена» с кодом 404")]
    public async Task Missing_record_gives_404_page()
    {
        await using var app = new App();
        var response = await app.CreateClient().GetAsync("/Trips/Card/9999");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await response.Content.ReadAsStringAsync()).Contains("Запись не найдена");
    }

    [Test, DisplayName("API: занятость автомобиля и ответ 404 в JSON")]
    public async Task Api_answers()
    {
        await using var app = new App();
        var client = app.CreateClient();
        var day = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        var busy = await client.GetStringAsync($"/api/cars/1/busy?from={day}T10:30&to={day}T11:30");
        await Assert.That(busy).Contains("\"busy\":true");
        var missing = await client.GetAsync("/api/cars/999");
        await Assert.That(missing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await missing.Content.ReadAsStringAsync()).Contains("не найден");
    }

    [Test, DisplayName("Форма изменения назначенной заявки не открывается — переход в карточку")]
    public async Task Edit_of_assigned_request_redirects_to_card()
    {
        await using var app = new App();
        var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Trips/Edit/4");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location!.ToString()).Contains("/Trips/Card/4");
    }

    [Test, DisplayName("Сообщения о нечисловом вводе выводятся по-русски")]
    public async Task Binding_messages_are_russian()
    {
        await using var app = new App();
        var client = app.CreateClient();
        var form = await client.GetStringAsync("/Cars/Create");
        var token = System.Text.RegularExpressions.Regex.Match(form, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var response = await client.PostAsync("/Cars/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["Plate"] = "М555ММ 86", ["Make"] = "УАЗ", ["Model"] = "Патриот",
            ["Kind"] = "Sedan", ["PassengerSeats"] = "abc", ["ReleaseYear"] = "", ["State"] = "Operational"
        }));
        var html = await response.Content.ReadAsStringAsync();
        await Assert.That(html).Contains("неверное значение для поля");
        await Assert.That(html).DoesNotContain("is not valid");
        await Assert.That(html).DoesNotContain("is invalid");
    }
}
