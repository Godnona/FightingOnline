# Integration guide

How the game client talks to the API. Interactive OpenAPI documentation is available at `/docs`
on environments where it is enabled; the machine-readable document is `/docs/json`.

## Basics

- JSON over HTTPS. Send `Content-Type: application/json`.
- Protected routes need `Authorization: Bearer <accessToken>`.
- Every response has an `x-request-id` header. Include it when reporting a bug, it lets us find the request in the logs.

## Errors

All errors look like this:

```json
{ "code": "VALIDATION_ERROR", "message": "Invalid request", "details": {} }
```

Use `code` in the client logic. `message` is for debugging and can change.

| Status | Code                           | When                                                      |
| ------ | ------------------------------ | --------------------------------------------------------- |
| 400    | `VALIDATION_ERROR`             | Invalid body, `details.issues` lists the fields           |
| 400    | `INVALID_PASSWORD_RESET_CODE`  | Reset code wrong, expired, used or out of attempts        |
| 400    | `INVALID_AVATAR`               | Avatar id not in the list                                 |
| 401    | `INVALID_CREDENTIALS`          | Wrong email or password                                   |
| 401    | `AUTHENTICATION_REQUIRED`      | No Bearer token on a protected route                      |
| 401    | `INVALID_ACCESS_TOKEN`         | Bad access token                                          |
| 401    | `ACCESS_TOKEN_EXPIRED`         | Call `/auth/refresh` and retry                            |
| 401    | `INVALID_REFRESH_TOKEN`        | Refresh token unknown, expired, revoked or already used   |
| 401    | `INVALID_PROVIDER_TOKEN`       | Google or Facebook token rejected                         |
| 403    | `ACCOUNT_BANNED`               | Account is banned                                         |
| 403    | `FORBIDDEN`                    | The player's role can't do this (admin-only routes)       |
| 404    | `IDENTITY_NOT_FOUND`           | The account has no sign-in method from that provider      |
| 404    | `PLAYER_NOT_FOUND`             | No player with that id                                    |
| 409    | `EMAIL_ALREADY_IN_USE`         | Email already linked to an account                        |
| 409    | `DISPLAY_NAME_TAKEN`           | Display name already taken                                |
| 409    | `DISPLAY_NAME_CHANGE_TOO_SOON` | Renamed less than 30 days ago, see `details.nextChangeAt` |
| 409    | `IDENTITY_ALREADY_LINKED`      | That provider account belongs to another player           |
| 409    | `PROVIDER_ALREADY_LINKED`      | The account already has one from that provider            |
| 409    | `LAST_IDENTITY`                | The last sign-in method can't be removed                  |
| 429    | `RATE_LIMITED`                 | Too many requests, wait for `Retry-After` seconds         |
| 503    | `PROVIDER_NOT_CONFIGURED`      | That provider is not switched on yet on this server       |
| 503    | `PROVIDER_UNAVAILABLE`         | Could not reach the provider, retry in a moment           |

## Authentication

| Route                        | Auth   | Success | What it does                                             |
| ---------------------------- | ------ | ------- | -------------------------------------------------------- |
| `POST /auth/register`        | -      | 201     | Create an account                                        |
| `POST /auth/login`           | -      | 200     | Sign in with email and password                          |
| `POST /auth/refresh`         | -      | 200     | Get a new token pair                                     |
| `POST /auth/logout`          | -      | 204     | End the current session                                  |
| `POST /auth/logout-all`      | Bearer | 204     | End all sessions of the player                           |
| `POST /auth/password/forgot` | -      | 202     | Email a reset code (same answer if the email is unknown) |
| `POST /auth/password/reset`  | -      | 204     | Set a new password with the code from the email          |

Register body: `email`, `password` and optionally `displayName` and `deviceInfo`. Login body: `email`, `password`, optional `deviceInfo`.

`deviceInfo` is a free text like `"Android 14 / Pixel 8"` saved with the session. If you don't send it, the `User-Agent` is used.

Register and login return:

```json
{
  "user": { "id": "uuid", "displayName": "Player482913", "avatar": null, "role": "PLAYER" },
  "session": {
    "tokenType": "Bearer",
    "accessToken": "jwt",
    "accessTokenExpiresAt": "2026-01-01T00:15:00.000Z",
    "refreshToken": "opaque",
    "refreshTokenExpiresAt": "2026-01-31T00:00:00.000Z"
  }
}
```

`POST /auth/refresh` body is `{ "refreshToken": "...", "deviceInfo": "optional" }` and returns
only `session`. `POST /auth/logout` body is `{ "refreshToken": "..." }`; it intentionally returns
204 even when the token is already invalid. `user.id` is the player id used everywhere else (game
server included).

### Tokens

- The access token lasts 15 minutes. When a request fails with `ACCESS_TOKEN_EXPIRED`, call `/auth/refresh` once and repeat the request.
- **A refresh token can be used only once.** Always save the new pair returned by `/auth/refresh`. If an old refresh token is sent again, the server assumes it was stolen and ends the whole session.
- Because of that, never send two refresh requests at the same time. Queue them.
- The refresh token lasts 30 days and every refresh starts a new 30 days, so active players stay signed in.
- On `INVALID_REFRESH_TOKEN`, delete the saved tokens and show the login screen.
- Keep the tokens in secure storage (Keychain on iOS, Keystore on Android).

### Field rules

- **Email**: not case sensitive. An email already used by any account can't be registered again.
- **Password**: 8 to 128 characters. Very common passwords are rejected.
- **Display name**: 3 to 16 characters, only letters, numbers and `_`, unique (not case sensitive). If omitted, a name like `Player482913` is generated.

### Password reset

Everything happens inside the game, there is no web page.

1. `POST /auth/password/forgot` with `email`. The answer is always 202, so the game can't tell if the account exists. Go to the reset screen.
2. The player gets an email with a **6 digit code**. It expires in 30 minutes and works once.
3. `POST /auth/password/reset` with `email` (the same as step 1), `code` and `newPassword`. Spaces or a dash in the code are fine (`123 456`). All sessions of the player end, so go back to the login screen.

- A code allows 5 attempts. After that, or on `INVALID_PASSWORD_RESET_CODE` in general, offer "Resend code" (step 1 again).
- Asking for a new code kills the previous one.
- At most 5 codes per hour are sent to the same account. Extra requests still answer 202 but send nothing.

## Google and Facebook

The game gets a token from the provider (SDK or the provider's own sign-in screen) and sends it to the API. The API checks it with the provider and answers with its own `user` and `session`, so after that everything works like the email login.

| Route                 | Send in `token`                                              |
| --------------------- | ------------------------------------------------------------ |
| `POST /auth/google`   | The Google **ID token** (not the access token)               |
| `POST /auth/facebook` | The Facebook access token, or the Limited Login token on iOS |

Body: `{ "token": "...", "deviceInfo": "optional" }`. Both answer 200 with the same body as the email login plus `isNewUser`:

```json
{ "user": { ... }, "session": { ... }, "isNewUser": true }
```

- The first sign-in creates the account with a name like `Player482913`. Use `isNewUser` to offer a name change.
- The next sign-ins with the same provider account return the same player.

### Same email, different account

Accounts are never merged by email. If the email of the Google or Facebook account already belongs to another player, the answer is `409 EMAIL_ALREADY_IN_USE`. Show something like "This email already has an account. Sign in with it and link Google in the settings", then use the linking routes below.

### Linking sign-in methods

All need `Authorization: Bearer <accessToken>`. `{provider}` is `google` or `facebook` (and `email` for removing).

| Route                                | Success | What it does                                 |
| ------------------------------------ | ------- | -------------------------------------------- |
| `GET /auth/identities`               | 200     | List the sign-in methods of the player       |
| `POST /auth/identities/{provider}`   | 200     | Link a provider, body `{ "token": "..." }`   |
| `DELETE /auth/identities/{provider}` | 204     | Remove a sign-in method (never the last one) |

Each identity looks like this:

```json
{
  "provider": "GOOGLE",
  "email": "player@gmail.com",
  "linkedAt": "2026-01-01T00:00:00.000Z",
  "lastUsedAt": null
}
```

Linking the same provider account twice is fine (same answer). One account can have only one identity per provider.

### Testing before the real keys exist

Until the Google client ids and Facebook app are set on the server, staging accepts fake tokens for those providers:

- `mock:<id>`, for example `mock:tester1`
- `mock:<id>:<email>`, for example `mock:tester1:tester1@example.com`

The same `<id>` always returns the same player, so this is enough to test new accounts, returning players, the email conflict and linking. Fake tokens stop working for a provider as soon as its real settings are in place, and the production server won't accept them.

## Profile

All need `Authorization: Bearer <accessToken>`.

| Route               | Success | What it does                              |
| ------------------- | ------- | ----------------------------------------- |
| `GET /profile`      | 200     | Profile of the signed-in player           |
| `PATCH /profile`    | 200     | Change the display name and/or the avatar |
| `GET /players/{id}` | 200     | Public profile of another player          |

`GET /profile` and `PATCH /profile` return:

```json
{
  "id": "uuid",
  "displayName": "Player482913",
  "avatar": "avatar_03",
  "role": "PLAYER",
  "createdAt": "2026-01-01T00:00:00.000Z",
  "nextDisplayNameChangeAt": null
}
```

`PATCH /profile` body: `displayName`, `avatar` or both. Only what you send changes.

- **Display name**: same rules as at sign-up. The first change is always allowed, then once every 30 days. `nextDisplayNameChangeAt` is `null` when the player can rename now, otherwise the date when it becomes possible (show it in the settings screen). Sending the current name again is not a change.
- **Avatar**: one id from the avatar list, or `null` to remove it. The list is still a placeholder (`avatar_01` to `avatar_08`) until the game sends the real one.

`GET /players/{id}` returns only `id`, `displayName` and `avatar`.
