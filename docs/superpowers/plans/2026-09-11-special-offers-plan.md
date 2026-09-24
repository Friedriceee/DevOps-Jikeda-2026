# US-05 US-06 Special Offers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the merchant special-offer create, edit, delete, and supporting list flow on the existing Sprint 1 branch.

**Architecture:** Extend the existing `Merchant` aggregate with an EF Core `SpecialOffer` entity. Keep request validation in DTOs, persistence and ownership checks in `SpecialOfferService`, and HTTP translation in `SpecialOfferController`. Add a Vue page that uses the current Pinia store and Axios wrapper, with pure helpers for validation and request shaping.

**Tech Stack:** .NET 8, ASP.NET Core Web API, EF Core 8, PostgreSQL, SQLite in-memory integration tests, Vue 3, Vite, Element Plus, Pinia, Vitest.

## Global Constraints

- The report class diagram remains the naming baseline; the smallest required extension is `Merchant 1 —— 0..* SpecialOffer`.
- `SpecialOffer` uses an integer database-generated primary key; no manual ID allocation or composite key is permitted.
- All API responses use the existing `ApiResult` envelope: `{ success, data, message }`.
- Create uses `POST /api/merchant/special-offers` with `merchantId` in the body.
- Update uses `PUT /api/merchant/special-offers/{offerId}?merchantId={merchantId}` with `merchantId` only in the query string.
- Delete uses `DELETE /api/merchant/special-offers/{offerId}?merchantId={merchantId}`.
- List uses `GET /api/merchant/{merchantId}/special-offers` to support the management page.
- `minPrice` is positive, has at most two decimal places, and `amountRemission` is positive, has at most two decimal places, and is less than `minPrice`.
- Missing merchants return 404 on create; missing or cross-merchant offers return 404 on update and delete; invalid input returns unified 400.
- The backend maps monetary values to `numeric(18,2)` and indexes `MerchantId`.
- Do not modify unrelated dish behavior or introduce authentication in this Sprint 1 slice.

---

### Task 1: Backend special-offer vertical slice

**Files:**
- Create: `backend/tests/TakeoutPlatform.Api.Tests/SpecialOfferEndpointTests.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/SpecialOffer.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/CreateSpecialOfferRequest.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/UpdateSpecialOfferRequest.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/SpecialOfferResponse.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/SpecialOfferService.cs`
- Create: `backend/src/TakeoutPlatform.Api/Features/Merchant/SpecialOfferController.cs`
- Create: `backend/src/TakeoutPlatform.Api/Migrations/202609110002_CreateSpecialOffers.cs`
- Modify: `backend/src/TakeoutPlatform.Api/Features/Merchant/Merchant.cs`
- Modify: `backend/src/TakeoutPlatform.Api/Data/AppDbContext.cs`
- Modify: `backend/src/TakeoutPlatform.Api/Program.cs`
- Modify: `backend/src/TakeoutPlatform.Api/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**
- `CreateSpecialOfferRequest` exposes `int MerchantId`, `decimal MinPrice`, `decimal AmountRemission` and implements `IValidatableObject`.
- `UpdateSpecialOfferRequest` exposes `decimal MinPrice`, `decimal AmountRemission` and implements `IValidatableObject`.
- `SpecialOfferResponse` is `record SpecialOfferResponse(int Id, int MerchantId, decimal MinPrice, decimal AmountRemission)`.
- `SpecialOfferService.CreateAsync(CreateSpecialOfferRequest, CancellationToken)` returns `(bool MerchantFound, SpecialOfferResponse? Offer)`.
- `SpecialOfferService.UpdateAsync(int offerId, int merchantId, UpdateSpecialOfferRequest, CancellationToken)` returns `SpecialOfferResponse?`.
- `SpecialOfferService.DeleteAsync(int offerId, int merchantId, CancellationToken)` returns `bool`.
- `SpecialOfferService.ListAsync(int merchantId, CancellationToken)` returns `IReadOnlyList<SpecialOfferResponse>?`.

- [ ] **Step 1: Write the failing backend integration tests.**

Add tests using the existing `ApiTestFactory`, SQLite in-memory database, and `ApiResponse<T>` record. The tests must exercise real HTTP endpoints:

```csharp
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
```

Add these additional concrete test methods using the same `_client`, response
types, and request values:

- `Create_offer_for_missing_merchant_returns_not_found` posts `merchantId = 999` and asserts 404, `Success = false`, and `Message = "商家不存在"`.
- `Invalid_offer_amount_returns_bad_request` uses `[TestCase(10, 10)]`, `[TestCase(10, 11)]`, and `[TestCase(0, 1)]`, posts each pair, and asserts 400.
- `Update_offer_returns_updated_offer` creates `(50, 5)`, puts `(80, 8)` at `/api/merchant/special-offers/{id}?merchantId=1`, and asserts 200 plus the updated fields.
- `Update_offer_for_another_merchant_returns_not_found` creates as merchant 1, puts the offer with `merchantId=999`, and asserts 404 with `Success = false`.
- `Delete_offer_removes_it_from_the_list` creates an offer, deletes it with `merchantId=1`, then gets merchant 1's list and asserts the delete response is 200 with `Success = true` and the list is empty.
- `Delete_offer_for_another_merchant_returns_not_found` creates as merchant 1, deletes with `merchantId=999`, and asserts 404 with `Success = false`.
- `List_offers_returns_only_the_requested_merchants_offers` creates two offers for merchant 1, gets `/api/merchant/1/special-offers`, and asserts 200, `Success = true`, and two response records whose `MerchantId` is 1.

Use body values such as `merchantId = 1`, `minPrice = 50.00m`, and `amountRemission = 5.00m`. Assert `success`, `data`, status code, ownership isolation, and the exact response fields rather than only checking that a request did not throw.

- [ ] **Step 2: Run the backend tests and confirm the expected red state.**

Run:

```text
dotnet test backend/TakeoutPlatform.sln --filter SpecialOfferEndpointTests
```

Expected on a fully provisioned machine: the new requests fail with 404 or the test project fails to compile because the special-offer endpoints and response types do not exist yet. On this host, record the already observed environment error if no .NET SDK is installed instead of claiming a red test run.

- [ ] **Step 3: Add the entity, DTO validation, service, controller, and DI.**

Implement the following behavior:

```csharp
public class SpecialOffer
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public decimal MinPrice { get; set; }
    public decimal AmountRemission { get; set; }
    public Merchant Merchant { get; set; } = null!;
}
```

Both request DTOs reject non-positive monetary values, more than two decimal places, and a remission that is greater than or equal to the threshold. The service checks merchant existence before create, queries by both `offerId` and `merchantId` for update and delete, orders list results by `Id`, and maps entities to `SpecialOfferResponse`.

`SpecialOfferController` must expose the four routes in the global constraints, return `CreatedAtAction` for create, `Ok` for update/list/delete, return 404 for the service's null/false ownership results, and guard a non-positive query `merchantId` with unified 400. Register the service in `Program.cs`.

Extend `Merchant` with `ICollection<SpecialOffer> SpecialOffers`, add the `DbSet`, configure `numeric(18,2)`, the `MerchantId` index, the required cascade foreign key, and add the migration plus model snapshot entries.

- [ ] **Step 4: Run the focused backend tests and verify green.**

Run:

```text
dotnet test backend/TakeoutPlatform.sln --filter SpecialOfferEndpointTests
```

Expected: all focused tests pass with zero failures. Then run the full backend suite:

```text
dotnet test backend/TakeoutPlatform.sln
```

Expected: the existing dish tests and all special-offer tests pass.

- [ ] **Step 5: Commit the backend slice.**

```text
git add backend/src backend/tests
git commit -m "feat(backend): add merchant special offer management"
```

---

### Task 2: Frontend special-offer management page

**Files:**
- Create: `frontend/src/utils/specialOffer.js`
- Create: `frontend/src/utils/__tests__/specialOffer.spec.js`
- Create: `frontend/src/views/merchant/MerchantSpecialOfferView.vue`
- Modify: `frontend/src/api/merchant.js`
- Modify: `frontend/src/router/index.js`
- Modify: `frontend/src/views/HomeView.vue`

**Interfaces:**
- `isValidSpecialOffer(minPrice, amountRemission)` returns `boolean`.
- `buildSpecialOfferPayload(form, merchantId)` returns `{ merchantId, minPrice, amountRemission }` with numeric values.
- `buildSpecialOfferUpdatePayload(form)` returns `{ minPrice, amountRemission }` without `merchantId`.
- `createSpecialOffer(payload)`, `listSpecialOffers(merchantId)`, `updateSpecialOffer(offerId, merchantId, payload)`, and `deleteSpecialOffer(offerId, merchantId)` use the existing `http` client and route contract.

- [ ] **Step 1: Write the failing frontend helper tests.**

Create Vitest tests with these behaviors:

```js
it('accepts a positive threshold and a smaller positive remission', () => {
  expect(isValidSpecialOffer(50, 5)).toBe(true)
  expect(isValidSpecialOffer('50.00', '5.00')).toBe(true)
})

it('rejects zero, negative, equal, greater, non-numeric, and over-precision values', () => {
  expect(isValidSpecialOffer(0, 1)).toBe(false)
  expect(isValidSpecialOffer(50, 0)).toBe(false)
  expect(isValidSpecialOffer(50, 50)).toBe(false)
  expect(isValidSpecialOffer(50, 60)).toBe(false)
  expect(isValidSpecialOffer('x', 1)).toBe(false)
  expect(isValidSpecialOffer(50.001, 5)).toBe(false)
})

it('builds a numeric create payload', () => {
  expect(buildSpecialOfferPayload({ minPrice: ' 50.00 ', amountRemission: '5.00' }, 1))
    .toEqual({ merchantId: 1, minPrice: 50, amountRemission: 5 })
})

it('builds an update payload without merchantId', () => {
  expect(buildSpecialOfferUpdatePayload({ minPrice: '80', amountRemission: '8' }))
    .toEqual({ minPrice: 80, amountRemission: 8 })
})
```

- [ ] **Step 2: Run the focused frontend tests and confirm red.**

Run:

```text
npm run test -- src/utils/__tests__/specialOffer.spec.js
```

Expected on a provisioned frontend: the import fails because `specialOffer.js` does not exist yet. If dependencies are unavailable, record the environment failure and continue with the same test-first order.

- [ ] **Step 3: Implement helpers, API functions, route, page, and entry point.**

Use finite numeric conversion and a two-decimal check in `isValidSpecialOffer`. `MerchantSpecialOfferView.vue` should use the current merchant ID from `useMerchantStore`, load the list on mount, validate before submitting, refresh after each mutation, use `ElMessageBox.confirm` before delete, and leave update/delete ownership to the API query parameter. Keep the page's form model limited to `minPrice` and `amountRemission`; do not send `merchantId` in the update body.

Add the route:

```js
{
  path: '/merchant/special-offers',
  name: 'merchant-special-offers',
  component: () => import('@/views/merchant/MerchantSpecialOfferView.vue'),
}
```

Add a second navigation button to `HomeView.vue` so the feature is reachable from the existing skeleton page.

- [ ] **Step 4: Run focused and full frontend tests, then build.**

Run:

```text
npm run test -- src/utils/__tests__/specialOffer.spec.js
npm run test
npm run build
```

Expected: focused helper tests pass, the existing suite remains green, and Vite exits successfully.

- [ ] **Step 5: Commit the frontend slice.**

```text
git add frontend/src
git commit -m "feat(frontend): add merchant special offer management"
```

---

### Task 3: Contract synchronization and final verification

**Files:**
- Modify: `docs/api-contract.md`

- [ ] **Step 1: Update the contract to match the ownership-safe PUT route.**

Change the US-06 PUT entry to include `?merchantId={merchantId}` and document that the update body contains only `minPrice` and `amountRemission`, while the query parameter identifies the merchant for the Sprint 1 ownership check. Keep the existing US-05 and US-07 field names and response envelope.

- [ ] **Step 2: Run static repository checks.**

Run:

```text
git diff --check
git status --short
```

Expected: no whitespace errors and only the planned special-offer files are changed.

- [ ] **Step 3: Run the complete verification set.**

Run the full backend and frontend commands from Tasks 1 and 2. On the current host, the known blockers are that `dotnet test` cannot start without a .NET SDK and the first npm install hit an `EPERM` npm-cache/file-lock error. Retry frontend installation with a repository-local cache if needed; report any remaining backend limitation explicitly and rely on GitHub Actions for the remote backend build/test gate.

- [ ] **Step 4: Commit the contract update.**

```text
git add docs/api-contract.md
git commit -m "docs: align special offer ownership contract"
```

- [ ] **Step 5: Push the requested branch and create the PR.**

Verify the branch name before pushing:

```text
git branch --show-current
git push -u origin US-05+06-ZHAO-HAOTIAN
```

Create a PR from `US-05+06-ZHAO-HAOTIAN` to `main` with a Conventional Commit title such as:

```text
feat: implement merchant special offers for US-05 and US-06
```

The PR body must summarize the backend entity/API/migration, frontend page and validation, tests run, and the local .NET SDK limitation if it remains. Do not claim CI is green until the GitHub Actions checks actually finish.
