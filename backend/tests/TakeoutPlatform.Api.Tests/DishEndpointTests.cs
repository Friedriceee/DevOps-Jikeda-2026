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
    public async Task Update_dish_returns_updated_dish()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Original Dish",
            price = 10.00m,
            category = "Main",
            inventory = 8,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        var response = await _client.PutAsJsonAsync(
            $"/api/merchant/dishes/{created!.Data!.Id}?merchantId=1",
            new
            {
                name = "Updated Dish",
                price = 12.50m,
                category = "Special",
                imageUrl = "https://example.com/dish.png",
                inventory = 20,
            });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Name, Is.EqualTo("Updated Dish"));
        Assert.That(body?.Data?.Price, Is.EqualTo(12.50m));
        Assert.That(body?.Data?.Inventory, Is.EqualTo(20));
    }

    [Test]
    public async Task Update_dish_for_another_merchant_returns_not_found()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Merchant Dish",
            price = 10.00m,
            inventory = 8,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        var response = await _client.PutAsJsonAsync(
            $"/api/merchant/dishes/{created!.Data!.Id}?merchantId=999",
            new
            {
                name = "Should Not Update",
                price = 12.50m,
                inventory = 20,
            });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
    }

    [Test]
    public async Task Delete_dish_removes_it_from_the_list()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Dish To Delist",
            price = 10.00m,
            inventory = 8,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/merchant/dishes/{created!.Data!.Id}?merchantId=1");
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        var listResponse = await _client.GetAsync("/api/merchant/1/dishes");
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<DishDto>>>();

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(deleteBody?.Success, Is.True);
        Assert.That(listBody?.Data, Is.Empty);
    }

    [Test]
    public async Task Delete_dish_for_another_merchant_returns_not_found()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Protected Dish",
            price = 10.00m,
            inventory = 8,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        var response = await _client.DeleteAsync(
            $"/api/merchant/dishes/{created!.Data!.Id}?merchantId=999");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
    }

    [Test]
    public async Task Update_dish_with_missing_name_returns_bad_request()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = 1,
            name = "Existing Dish",
            price = 10.00m,
            inventory = 8,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();

        var response = await _client.PutAsJsonAsync(
            $"/api/merchant/dishes/{created!.Data!.Id}?merchantId=1",
            new
            {
                name = "",
                price = 10.00m,
                inventory = 8,
            });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
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
