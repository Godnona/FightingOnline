# Ripp API client

This folder contains the Unity-side API foundation described by the current integration guide.

## Implemented

- Environment-specific base URL and production HTTPS validation.
- JSON requests through `UnityWebRequest`.
- Automatic `Content-Type`, `Accept`, and Bearer headers.
- Typed success/error results, `Retry-After`, and `x-request-id` capture.
- Email registration/login and password recovery.
- Google, Facebook, and Apple provider-token endpoints (provider SDK integration is outside this layer).
- Automatic single-flight `/auth/refresh` with rotated-token protection.
- Current-session logout and logout-all.
- Identity list/link/removal for email, Google, Facebook, and Apple.
- Apple web-login URL generation with `returnUrl`, PKCE `codeChallenge`, and staging `mockUser` support.
- Current/public profile and profile updates.
- In-memory token storage for development.
- Structured validation and display-name cooldown error details.

## Not yet implemented in this Unity client

- Native Apple SDK/deep-link UI. `LoginWithAppleAsync` accepts an identity token or staging `mock:*` token;
  `CreateAppleWebLoginRequest(returnUrl, mockUser)` generates the browser URL and PKCE verifier once the
  app scheme and callback handling are configured.
- Inventory, game-social, matchmaking, and matches APIs.

Refresh is configured automatically and is single-flight: a refresh token is single-use and a duplicate
request can invalidate the session.

## Setup

1. Create an `ApiClientSettings` asset using **Create > Ripp > API Client Settings**.
2. Add the Local, Staging, and Production URLs supplied by backend.
3. Add `ApiBootstrap` to one GameObject in the startup scene and assign the settings asset.
4. Subscribe to `ApiBootstrap.SessionInvalidated` and return the player to the login screen.
5. Replace `InMemoryTokenStorage` with Keychain/Keystore-backed storage before production.

Example:

```csharp
ApiResult<AuthResponse> result =
    await ApiBootstrap.Api.Auth.LoginAsync(email, password, deviceInfo);

if (!result.IsSuccess)
{
    Debug.LogError($"API error {result.Error?.code}; request id: {result.RequestId}");
}
```

The default `UnityJsonSerializer` intentionally supports only ordinary object DTOs. If future contracts require top-level arrays or dynamic `details`, replace `IJsonSerializer` with a Newtonsoft-backed implementation.
