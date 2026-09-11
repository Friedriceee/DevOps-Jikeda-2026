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

    [Test]
    public async Task Update_dish_returns_updated_fields()
    {
        var created = await CreateDishAsync(name: "Noodles", price: 12.50m, inventory: 8);

        var response = await _client.PutAsJsonAsync($"/api/merchant/dishes/{created.Id}", new
        {
            merchantId = 1,
            name = "Noodles (Large)",
            price = 15.00m,
            category = "Main",
            inventory = 20,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Name, Is.EqualTo("Noodles (Large)"));
        Assert.That(body?.Data?.Price, Is.EqualTo(15.00m));
        Assert.That(body?.Data?.Inventory, Is.EqualTo(20));
    }

    [Test]
    public async Task Update_dish_with_invalid_price_returns_bad_request()
    {
        var created = await CreateDishAsync();

        var response = await _client.PutAsJsonAsync($"/api/merchant/dishes/{created.Id}", new
        {
            merchantId = 1,
            name = "Noodles",
            price = 0m,
            inventory = 8,
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_nonexistent_dish_returns_not_found()
    {
        var response = await _client.PutAsJsonAsync("/api/merchant/dishes/999", new
        {
            merchantId = 1,
            name = "Ghost Dish",
            price = 10.00m,
            inventory = 1,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Message, Is.EqualTo("菜品不存在"));
    }

    [Test]
    public async Task Update_dish_owned_by_another_merchant_returns_not_found()
    {
        var created = await CreateDishAsync();

        var response = await _client.PutAsJsonAsync($"/api/merchant/dishes/{created.Id}", new
        {
            merchantId = 999,
            name = "Hijacked",
            price = 10.00m,
            inventory = 1,
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Delete_dish_removes_it_from_the_list()
    {
        var created = await CreateDishAsync();

        var response = await _client.DeleteAsync($"/api/merchant/dishes/{created.Id}?merchantId=1");
        var listResponse = await _client.GetFromJsonAsync<ApiResponse<List<DishDto>>>("/api/merchant/1/dishes");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(listResponse?.Data, Is.Empty);
    }

    [Test]
    public async Task Delete_nonexistent_dish_returns_not_found()
    {
        var response = await _client.DeleteAsync("/api/merchant/dishes/999?merchantId=1");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Message, Is.EqualTo("菜品不存在"));
    }

    [Test]
    public async Task Delete_without_merchant_id_returns_bad_request()
    {
        var created = await CreateDishAsync();

        var response = await _client.DeleteAsync($"/api/merchant/dishes/{created.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<DishDto> CreateDishAsync(
        string name = "Kung Pao Chicken", decimal price = 28.00m, int inventory = 100)
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name,
            price,
            inventory,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();
        return body!.Data!;
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record DishDto(int Id, int MerchantId, string Name, decimal Price, string? Category, string? ImageUrl, int Inventory);
}
