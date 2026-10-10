using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

public class MenuFactoryEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _merchant = null!;
    [SetUp] public void SetUp()
    {
        _factory = new();
        _factory.EnsureDatabase();
        _merchant = _factory.CreateAuthenticatedClient(AccountRole.Merchant, 1);
    }
    [TearDown] public void TearDown() { _merchant.Dispose(); _factory.Dispose(); }

    [TestCase("Standard", null)]
    [TestCase("Bundle", 3)]
    [TestCase("Promotional", null)]
    public async Task Create_round_trips_type_metadata_and_ignores_spoofed_merchant(string type, int? size)
    {
        var start = DateTimeOffset.UtcNow.AddHours(-1);
        var response = await _merchant.PostAsJsonAsync("/api/merchant/dishes", new
        {
            name = "Menu factory test", price = 10, inventory = 5, merchantId = 2,
            itemType = type, bundleSize = size,
            promotionStartsAtUtc = type == "Promotional" ? (DateTimeOffset?)start : null,
            promotionEndsAtUtc = type == "Promotional" ? (DateTimeOffset?)start.AddHours(2) : null,
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.That(data.GetProperty("itemType").GetString(), Is.EqualTo(type));
        Assert.That(data.GetProperty("merchantId").GetInt32(), Is.EqualTo(1));
        Assert.That(data.GetProperty("isAvailable").GetBoolean(), Is.True);
        if (size.HasValue) Assert.That(data.GetProperty("bundleSize").GetInt32(), Is.EqualTo(size));
    }

    [TestCase("Unknown", null)]
    [TestCase("Bundle", 1)]
    [TestCase("Promotional", null)]
    public async Task Invalid_factory_settings_return_English_bad_request(string type, int? size)
    {
        var response = await _merchant.PostAsJsonAsync("/api/merchant/dishes", new
        { name = "Meal", price = 10, inventory = 3, itemType = type, bundleSize = size });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Match("[\\u4e00-\\u9fff]"));
    }

    [Test]
    public async Task Update_can_change_type_and_list_reports_stock_availability()
    {
        var created = await _merchant.PostAsJsonAsync("/api/merchant/dishes", new { name = "Meal", price = 10, inventory = 3 });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetInt32();
        var updated = await _merchant.PutAsJsonAsync($"/api/merchant/dishes/{id}", new
        { name = "Bundle", price = 20, inventory = 0, itemType = "Bundle", bundleSize = 2 });
        Assert.That(updated.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var response = await _merchant.GetFromJsonAsync<JsonElement>("/api/merchant/1/dishes");
        var dish = response.GetProperty("data")[0];
        Assert.That(dish.GetProperty("itemType").GetString(), Is.EqualTo("Bundle"));
        Assert.That(dish.GetProperty("isAvailable").GetBoolean(), Is.False);
    }

    [Test]
    public async Task Customer_cannot_create_a_menu_item()
    {
        using var customer = _factory.CreateAuthenticatedClient(AccountRole.Customer, 1);
        var result = await customer.PostAsJsonAsync("/api/merchant/dishes", new { name = "Meal", price = 10, inventory = 1 });
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
