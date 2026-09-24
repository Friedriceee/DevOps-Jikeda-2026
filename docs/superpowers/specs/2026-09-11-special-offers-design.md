# US-05 US-06 Special Offer Design

## Scope

This design implements Sprint 1 merchant user stories US-05 and US-06:

- US-05: a merchant creates a threshold discount offer.
- US-06: a merchant edits or deletes one of the merchant's offers.

The offer list read path is included as the supporting dependency needed to
display existing offers and select an offer for edit or delete. It uses the
US-07 contract already recorded in `docs/api-contract.md` without adding any
additional business behavior.

## Design baseline and class relationship

The report class diagram defines `Merchant` as the owner of merchant-side
entities and shows the agreed domain naming style. It does not draw a
`SpecialOffer` class, so this feature extends the baseline with the smallest
relationship required by the user stories:

```text
Merchant 1 ---------------- 0..* SpecialOffer
          owns/offers             merchantId FK
```

`SpecialOffer` is an EF Core entity with an auto-generated integer `Id`, a
required `MerchantId`, and decimal `MinPrice` and `AmountRemission` values.
`Merchant` exposes `ICollection<SpecialOffer> SpecialOffers`. The entity does
not copy the legacy repository's composite key or manual ID allocation.

## API design

The API follows the current repository's `ApiResult` envelope and feature
folder structure:

```text
POST   /api/merchant/special-offers
PUT    /api/merchant/special-offers/{offerId}?merchantId={merchantId}
DELETE /api/merchant/special-offers/{offerId}?merchantId={merchantId}
GET    /api/merchant/{merchantId}/special-offers
```

The first sprint has no authentication, so `merchantId` remains explicit in
the request. Create carries it in the request body. Update and delete carry it
in the query string, matching the existing dish ownership pattern. Update
does not accept a merchant ID in its body and cannot move an offer between
merchants.

Successful create returns HTTP 201 and the created `SpecialOfferResponse`.
Successful update returns HTTP 200 and the updated response. Successful delete
returns the existing empty `ApiResult` payload. A missing merchant returns
404 on create. A missing offer or an offer owned by another merchant returns
404 on update and delete. Invalid values return the unified 400 response.

## Validation and persistence

DTO validation enforces:

- `merchantId` is positive on create.
- `minPrice` is positive and has at most two decimal places.
- `amountRemission` is positive, has at most two decimal places, and is less
  than `minPrice`.

The `minPrice >= 0` contract is therefore respected, while the relational
rule makes `minPrice = 0` invalid because no positive remission can be less
than zero. EF Core maps both decimal fields to `numeric(18,2)`, creates an
index on `MerchantId`, and configures cascade delete from merchant to offers.
The schema change is represented by a migration and an updated model
snapshot.

`SpecialOfferService` owns merchant existence checks, offer ownership checks,
entity construction, normalization, and response mapping. Controllers only
validate route/query guards, call the service, and translate service results
to HTTP responses.

## Frontend design

The Vue page `MerchantSpecialOfferView.vue` uses the existing Pinia merchant
store and `http.js` client. It provides:

- a list of the current merchant's offers;
- a form for creating an offer;
- inline edit mode for an existing offer;
- a confirmation dialog before deletion;
- client-side validation matching the backend rules;
- refresh after create, update, or delete so server state is authoritative.

The page is registered at `/merchant/special-offers`. API functions stay in
`frontend/src/api/merchant.js`. Pure validation and request-shaping helpers
live in `frontend/src/utils/specialOffer.js` so they can be tested without a
browser or network.

## Testing strategy

Backend integration tests use the existing `ApiTestFactory` and SQLite
in-memory database. They cover create success, missing merchant, invalid
threshold/remission values, update success, update ownership isolation, delete
success, delete ownership isolation, and listing.

Frontend Vitest tests cover valid and invalid offer values, create payload
construction, and update payload construction. The existing frontend suite,
frontend production build, and backend solution test command remain the final
verification gates. If the local machine lacks a .NET SDK, the limitation is
reported explicitly and the GitHub Actions CI result is used as the remote
backend verification gate.
