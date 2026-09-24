using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// 订单接口的集成测试（走完整 HTTP 管线）。
/// 覆盖：创建待支付订单、按用户查询、取消待支付订单、取消不存在订单、以及请求校验。
/// 下单前先通过 /api/merchant/dishes 建一个带库存的菜品（Merchant Id=1 来自种子）。
/// </summary>
public class OrderEndpointTests
{
    private ApiTestFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _merchantClient = null!;

    private const int MerchantId = 1;
    private const int UserId = 1;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiTestFactory();
        _factory.EnsureDatabase();
        _client = _factory.CreateAuthenticatedClient(AccountRole.Customer, UserId);
        _merchantClient = _factory.CreateAuthenticatedClient(AccountRole.Merchant, MerchantId);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _merchantClient.Dispose();
        _factory.Dispose();
    }

    private async Task<int> CreateDishAsync(int inventory, decimal price = 10.00m)
    {
        var response = await _merchantClient.PostAsJsonAsync("/api/merchant/dishes", new
        {
            merchantId = MerchantId,
            name = "Endpoint Dish",
            price,
            inventory,
        });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<DishDto>>();
        return body!.Data!.Id;
    }

    private object NewOrderPayload(int dishId, int dishNum) => new
    {
        userId = UserId,
        addressId = 1,
        price = 20.00m,
        orderTimestamp = DateTime.UtcNow,
        needUtensils = 1,
        riderPrice = 3.00m,
        shoppingCart = new[]
        {
            new { merchantId = MerchantId, dishId, dishNum },
        },
    };

    [Test]
    public async Task Create_order_returns_created_pending_order()
    {
        var dishId = await CreateDishAsync(inventory: 50);

        var response = await _client.PostAsJsonAsync("/api/orders", NewOrderPayload(dishId, 2));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Status, Is.EqualTo(0)); // Pending
        Assert.That(body?.Data?.UserId, Is.EqualTo(UserId));
    }

    [Test]
    public async Task List_orders_returns_created_orders_for_user()
    {
        var dishId = await CreateDishAsync(inventory: 50);
        await _client.PostAsJsonAsync("/api/orders", NewOrderPayload(dishId, 1));

        var response = await _client.GetAsync($"/api/orders/user/{UserId}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<OrderDto>>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Cancel_pending_order_succeeds_and_removes_it()
    {
        var dishId = await CreateDishAsync(inventory: 50);
        var createResponse = await _client.PostAsJsonAsync("/api/orders", NewOrderPayload(dishId, 2));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();

        var cancelResponse = await _client.DeleteAsync($"/api/orders/{created!.Data!.Id}");
        var cancelBody = await cancelResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(cancelResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(cancelBody?.Success, Is.True);

        // 取消后列表应为空
        var listResponse = await _client.GetAsync($"/api/orders/user/{UserId}");
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<OrderDto>>>();
        Assert.That(listBody?.Data, Is.Empty);
    }

    [Test]
    public async Task Cancel_missing_order_returns_not_found()
    {
        var response = await _client.DeleteAsync("/api/orders/99999");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
    }

    [Test]
    public async Task Create_order_with_empty_cart_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            userId = UserId,
            addressId = 1,
            price = 20.00m,
            orderTimestamp = DateTime.UtcNow,
            needUtensils = 1,
            riderPrice = 3.00m,
            shoppingCart = Array.Empty<object>(),
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_order_with_insufficient_stock_returns_bad_request()
    {
        var dishId = await CreateDishAsync(inventory: 1);

        var response = await _client.PostAsJsonAsync("/api/orders", NewOrderPayload(dishId, 5));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Does.Contain("库存"));
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);
    private sealed record DishDto(int Id, int MerchantId, string Name, decimal Price, string? Category, string? ImageUrl, int Inventory);
    private sealed record OrderDto(int Id, int UserId, int AddressId, decimal Price, DateTime OrderTimestamp, int Status, int NeedUtensils, List<OrderDishDto> Dishes);
    private sealed record OrderDishDto(int MerchantId, int DishId, int DishNum);
}
