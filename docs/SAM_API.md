# SAM.gov Get Opportunities Public API v2 — client design notes

This is the reference the `SamOpportunitiesClient` is built against. Sourced from
the official GSA Open Technology docs (`open.gsa.gov/api/get-opportunities-public-api`)
and corroborating guides, read at build time. Confirm against the live docs when
wiring the real API key (see HUMAN_TODO).

## Endpoint

```
GET https://api.sam.gov/opportunities/v2/search
```

* Base URL is configurable via `Sam__BaseUrl` (prod default above; the alpha/test
  host is `https://api-alpha.sam.gov/prodlike`).
* Auth: **`api_key` query-string parameter** (SAM.gov account → *Account Details*
  → request a public API key). Passed as `?api_key=...`. The client also supports
  sending it as the `X-Api-Key` header via config, but query param is the
  documented method for this API.

## Rate limits

| Account type                    | Requests / day |
| ------------------------------- | -------------- |
| Non-federal, **no role**        | 10             |
| Non-federal, **with a role**    | 1,000          |
| Federal system account          | 1,000          |

The owner's keyed account is "non-federal with a role" → **1,000/day**. Our
scheduler pulls a small window a few times a day at `limit=1000`, so a day is a
handful of requests. A `RateLimitGuard` caps requests per run defensively.

## Request parameters

| Param         | Req? | Notes                                                                 |
| ------------- | ---- | --------------------------------------------------------------------- |
| `api_key`     | yes  | account API key                                                       |
| `postedFrom`  | yes  | `MM/dd/yyyy` — start of posted-date window                            |
| `postedTo`    | yes  | `MM/dd/yyyy` — end of posted-date window (**max 1-year span**)        |
| `limit`       | no   | page size, **max 1000** (default 1)                                   |
| `offset`      | no   | zero-based paging offset (default 0)                                  |
| `ptype`       | no   | notice type code(s) — see table                                      |
| `ncode`       | no   | NAICS code filter                                                     |
| `ccode`       | no   | classification (PSC) code filter                                      |
| `typeOfSetAside` | no | set-aside code filter                                                |
| `state`       | no   | place-of-performance state                                            |
| `deptname` / `organizationName` | no | agency/department name filter                             |
| `title`       | no   | keyword in title                                                      |

We deliberately pull a **broad window with no server-side filters** (only date +
paging) and do all NAICS/PSC/keyword/agency/set-aside/state/type matching
*locally* in the deterministic engine, because a single user base spans many
filter combinations and local matching keeps request count minimal.

### `ptype` notice-type codes

| Code | Meaning                              | Our `NoticeType`          |
| ---- | ------------------------------------ | ------------------------- |
| `p`  | Presolicitation                      | `Presolicitation`         |
| `o`  | Solicitation                         | `Solicitation`            |
| `k`  | Combined Synopsis/Solicitation       | `CombinedSynopsis`        |
| `r`  | Sources Sought                       | `SourcesSought`           |
| `s`  | Special Notice                       | `SpecialNotice`           |
| `a`  | Award Notice                         | `AwardNotice`             |
| `u`  | Justification (J&A)                  | `Justification`           |
| `g`  | Sale of Surplus Property             | `SaleOfSurplus`           |
| `i`  | Intent to Bundle Requirements (DoD)  | `IntentToBundle`          |

The four notice types the product's profile UI exposes (per spec) are
**Solicitation, Presolicitation, Sources Sought, Combined Synopsis** — the rest
are still ingested and stored.

## Response envelope

```jsonc
{
  "totalRecords": 12345,
  "limit": 1000,
  "offset": 0,
  "opportunitiesData": [
    {
      "noticeId": "a1b2c3...",                       // natural key
      "title": "IT Support Services",
      "solicitationNumber": "W912...-24-R-0001",
      "fullParentPathName": "DEPT OF DEFENSE.DEPT OF THE ARMY.ARMY CONTRACTING...",
      "fullParentPathCode": "057.2100.....",
      "postedDate": "2024-05-01",
      "type": "Combined Synopsis/Solicitation",       // -> NoticeType
      "baseType": "Combined Synopsis/Solicitation",
      "archiveType": "auto15",
      "archiveDate": "2024-06-01",
      "typeOfSetAside": "SBA",                         // -> SetAsideCode
      "typeOfSetAsideDescription": "Total Small Business Set-Aside (FAR 19.5)",
      "responseDeadLine": "2024-05-20T17:00:00-04:00", // -> ResponseDeadline (UTC)
      "naicsCode": "541519",
      "naicsCodes": ["541519"],
      "classificationCode": "D399",                    // PSC -> PscCode
      "active": "Yes",
      "award": null,                                   // present on award notices
      "pointOfContact": [
        { "type": "primary", "fullName": "Jane Doe", "email": "jane@agency.gov",
          "phone": "555-0100", "title": "Contract Specialist" }
      ],
      "placeOfPerformance": {
        "city":    { "code": "...", "name": "Huntsville" },
        "state":   { "code": "AL",  "name": "Alabama" },
        "zip":     "35808",
        "country": { "code": "USA", "name": "UNITED STATES" }
      },
      "officeAddress": { "zipcode": "35808", "city": "Huntsville", "state": "AL", "countryCode": "USA" },
      "description": "https://api.sam.gov/opportunities/v1/noticedesc?noticeid=a1b2c3...",
      "organizationType": "OFFICE",
      "additionalInfoLink": null,
      "uiLink": "https://sam.gov/opp/a1b2c3.../view",  // human link
      "links": [ { "rel": "self", "href": "https://api.sam.gov/..." } ],
      "resourceLinks": [ "https://sam.gov/api/prod/opps/v3/opportunities/resources/files/..." ]
    }
  ],
  "links": [ { "rel": "self", "href": "..." } ]
}
```

Notes the normalizer handles:
* `type`/`baseType` are **display strings** ("Combined Synopsis/Solicitation") not
  the `ptype` codes — mapped to `NoticeType` by a display-string lookup.
* `responseDeadLine` may be null, may carry an offset, or be date-only —
  normalized to UTC (`DateTimeOffset` → `Utc`).
* `description` is a **URL** to the notice-description resource, not the text. We
  store the link; text can be resolved lazily (a `noticedesc` fetch) — left as a
  documented enhancement to conserve the request budget.
* `placeOfPerformance` may be null or partial — flattened defensively.
* The entire raw record is retained in `Notice.RawJson` (jsonb) for
  forward-compatibility.

## Set-aside codes (`typeOfSetAside`)

`SBA` Total Small Business · `SBP` Partial Small Business · `8A` 8(a) ·
`8AN` 8(a) Sole Source · `HZC` HUBZone · `HZS` HUBZone Sole Source ·
`SDVOSBC` SDVOSB · `SDVOSBS` SDVOSB Sole Source · `WOSB` WOSB ·
`WOSBSS` WOSB Sole Source · `EDWOSB` EDWOSB · `EDWOSBSS` EDWOSB Sole Source ·
`LAS` Local Area (disaster) · `IEE` Indian Economic Enterprise ·
`ISBEE` Indian Small Business EE · `BI` Buy Indian · `VSA`/`VSS` Veteran-related.
(Full mapping in `SetAsideCode` enum + `SetAsideRef` seed.)
