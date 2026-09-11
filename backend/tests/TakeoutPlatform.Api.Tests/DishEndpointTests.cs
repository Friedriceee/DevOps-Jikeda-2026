using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;

namespace TakeoutPlatform.Api.Tests;

public class DishEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_dish_returns_created_dish()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Kung Pao Chicken",
            price = 28.00m,
            category = "Sichuan",
            imageUrl = (string?)null,
            inventory = 100,
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Name, Is.EqualTo("Kung Pao Chicken"));
        Assert.That(body?.Data?.MerchantId, Is.EqualTo(1));
    }

    [Test]
    public async Task List_dishes_returns_created_dishes()
    {
        await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Noodles",
            price = 12.50m,
            category = "Main",
            inventory = 8,
        });

        var response = await _client.GetAsync("/api/merchant/1/dishes");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<DishDto>>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Has.Count.EqualTo(1));
        Assert.That(body?.Data?[0].Name, Is.EqualTo("Noodles"));
    }

    [Test]
    public async Task Missing_name_returns_unified_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            price = 10.00m,
            inventory = 1,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Does.Contain("name"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task Non_positive_price_returns_bad_request(decimal price)
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Invalid Dish",
            price,
            inventory = 1,
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Missing_merchant_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 999,
            name = "Dish",
            price = 10.00m,
            inventory = 1,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Is.EqualTo("商家不存在"));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record DishDto(int Id, int MerchantId, string Name, decimal Price, string? Category, string? ImageUrl, int Inventory);
}
