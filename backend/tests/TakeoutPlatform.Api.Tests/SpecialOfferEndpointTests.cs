using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;

namespace TakeoutPlatform.Api.Tests;

public class SpecialOfferEndpointTests
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
    public async Task Create_offer_returns_created_offer()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/special-offers", new
        {
            merchantId = 1,
            minPrice = 50.00m,
            amountRemission = 5.00m,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SpecialOfferDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.MerchantId, Is.EqualTo(1));
        Assert.That(body?.Data?.MinPrice, Is.EqualTo(50.00m));
        Assert.That(body?.Data?.AmountRemission, Is.EqualTo(5.00m));
    }

    [Test]
    public async Task Create_offer_for_missing_merchant_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/special-offers", new
        {
            merchantId = 999,
            minPrice = 50.00m,
            amountRemission = 5.00m,
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
        Assert.That(body?.Message, Is.EqualTo("商家不存在"));
    }

    [TestCase(10, 10)]
    [TestCase(10, 11)]
    [TestCase(0, 1)]
    public async Task Invalid_offer_amount_returns_bad_request(decimal minPrice, decimal amountRemission)
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/special-offers", new
        {
            merchantId = 1,
            minPrice,
            amountRemission,
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Update_offer_returns_updated_offer()
    {
        var offerId = await CreateOfferAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/merchant/special-offers/{offerId}?merchantId=1",
            new
            {
                minPrice = 80.00m,
                amountRemission = 8.00m,
            });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SpecialOfferDto>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data?.Id, Is.EqualTo(offerId));
        Assert.That(body?.Data?.MinPrice, Is.EqualTo(80.00m));
        Assert.That(body?.Data?.AmountRemission, Is.EqualTo(8.00m));
        Assert.That(body?.Data?.MerchantId, Is.EqualTo(1));
    }

    [Test]
    public async Task Update_offer_for_another_merchant_returns_not_found()
    {
        var offerId = await CreateOfferAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/merchant/special-offers/{offerId}?merchantId=999",
            new
            {
                minPrice = 80.00m,
                amountRemission = 8.00m,
            });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
    }

    [Test]
    public async Task Delete_offer_removes_it_from_the_list()
    {
        var offerId = await CreateOfferAsync();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/merchant/special-offers/{offerId}?merchantId=1");
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        var listResponse = await _client.GetAsync("/api/merchant/1/special-offers");
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<SpecialOfferDto>>>();

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(deleteBody?.Success, Is.True);
        Assert.That(listBody?.Data, Is.Empty);
    }

    [Test]
    public async Task Delete_offer_for_another_merchant_returns_not_found()
    {
        var offerId = await CreateOfferAsync();

        var response = await _client.DeleteAsync(
            $"/api/merchant/special-offers/{offerId}?merchantId=999");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(body?.Success, Is.False);
    }

    [Test]
    public async Task List_offers_returns_only_the_requested_merchants_offers()
    {
        await CreateOfferAsync(50.00m, 5.00m);
        await CreateOfferAsync(80.00m, 8.00m);

        var response = await _client.GetAsync("/api/merchant/1/special-offers");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SpecialOfferDto>>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body?.Success, Is.True);
        Assert.That(body?.Data, Has.Count.EqualTo(2));
        Assert.That(body?.Data, Has.All.Property(nameof(SpecialOfferDto.MerchantId)).EqualTo(1));
    }

    private async Task<int> CreateOfferAsync(
        decimal minPrice = 50.00m,
        decimal amountRemission = 5.00m)
    {
        var response = await _client.PostAsJsonAsync("/api/merchant/special-offers", new
        {
            merchantId = 1,
            minPrice,
            amountRemission,
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SpecialOfferDto>>();

        Assert.That(body?.Data, Is.Not.Null);
        return body!.Data!.Id;
    }

    private sealed record ApiResponse<T>(bool Success, T? Data, string? Message);

    private sealed record SpecialOfferDto(
        int Id,
        int MerchantId,
        decimal MinPrice,
        decimal AmountRemission);
}
