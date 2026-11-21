# Setup đăng nhập Google, Facebook và Apple

## Test ngay trong Unity Editor

Backend local/staging phải có `SOCIAL_AUTH_MOCK=true` và chưa cấu hình provider thật.

1. Thêm `FacebookLoginController` vào một Empty GameObject.
2. Gán `LoginBtnFacebook` vào **Login Button**, gán text `ErrorAlertText` vào **Alert Text**.
3. Trong On Click của `LoginBtnFacebook`, chọn `FacebookLoginController.LoginWithFacebook`.
4. Thêm `GoogleLoginController` vào một Empty GameObject khác.
5. Gán `LoginBtnGG` và `ErrorAlertText` tương tự.
6. Trong On Click của `LoginBtnGG`, chọn `GoogleLoginController.LoginWithGoogle`.
7. Giữ **Use Mock Token In Editor** được bật. Mỗi provider dùng email mock khác nhau nên sẽ
   tạo hai tài khoản test riêng.

Khi backend trả về thành công, cả hai script đều lưu `PlayerSession`, lưu token API và chuyển tới
`MainMenuScene`. Tên do backend tạo sẽ xuất hiện trên Main Menu.

## Facebook thật

Project đã có Facebook SDK. Cấu hình app trong `Facebook > Edit Settings`, bao gồm Facebook App ID,
Client Token, Android package/signature hoặc iOS bundle ID.

Trên backend, đặt:

```env
FACEBOOK_APP_ID=<facebook-app-id>
FACEBOOK_APP_SECRET=<facebook-app-secret>
```

Trong bản Android/iOS/Windows, script tự khởi tạo SDK, xin quyền `public_profile` và `email`, rồi gửi
Facebook access token tới `POST /auth/facebook`. Không đưa App Secret vào Unity project.

## Google thật

Project hiện chưa có Google Sign-In SDK. Import Google Sign-In for Unity, sau đó:

1. Vào `Project Settings > Player > Scripting Define Symbols`.
2. Thêm define `GOOGLE_SIGN_IN` cho Android/iOS.
3. Trên component `GoogleLoginController`, nhập **Web Client ID** — OAuth client có type
   `Web application`, không phải Android client ID.
4. Cấu hình đúng package name, SHA-1/SHA-256 signing certificate và iOS URL scheme theo Google SDK.
5. Trên backend, đặt `GOOGLE_CLIENT_IDS` chứa cùng Web Client ID rồi deploy lại.

Script yêu cầu `RequestIdToken = true` và gửi `GoogleSignInUser.IdToken` tới `POST /auth/google`.
Không gửi access token hoặc server auth code vào endpoint này.

Nếu dùng SDK Google khác, không cần define `GOOGLE_SIGN_IN`: từ callback của SDK, lấy ID token và gọi
`GoogleLoginController.SubmitGoogleIdToken(idToken)`.

## Apple

- Test JSON API trên staging: gọi `ApiBootstrap.Api.Auth.LoginWithAppleAsync("mock:<user>")`.
- Khi có identity token thật từ Apple SDK, truyền token đó vào cùng method.
- Test luồng web: gọi
  `ApiBootstrap.Api.Auth.CreateAppleWebLoginRequest(returnUrl, "<mock-user>")`, giữ lại
  `codeVerifier`, rồi mở `url` bằng `Application.OpenURL`.
- Luồng web chỉ quay lại game sau khi backend và Android/iOS đã thống nhất app scheme, ví dụ
  `ripptopia://`, đồng thời client phải giữ PKCE verifier để hoàn tất callback. Không tự đưa Apple
  private key hoặc client secret vào Unity project.
