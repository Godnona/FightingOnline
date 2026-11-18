# Setup UI đăng ký, đăng nhập và tên người chơi

Các script trong thư mục này dùng API client và backend đã có sẵn trong project.

## 1. LoginPage

1. Mở scene `Assets/Scenes/LoginPage.unity`.
2. Giữ object `ApiBootstrap` và bảo đảm field **Settings** đang trỏ tới
   `Assets/APIClient/Configuration/ApiClientSettings.asset`.
3. Tạo Empty GameObject tên `LoginUiController`, thêm component `LoginUiController`.
4. Gán bốn panel cấp cao vào nhóm **Panels**:
   - Login Panel: `Login`
   - Register Panel: `Register`
   - Forgot Password Panel: `ForgotPassword`
   - Reset Password Panel: `ResetPassword`
5. Gán các `TMP_InputField` và `TMP_Text` đúng nhóm:
   - Login: `EmailInput`, `PasswordInput`, `ErrorAlertText`
   - Register: `EmailInput`, `NameInput`, `PasswordInput`, `ConfirmPassword`, `ErrorAlert`
   - Forgot Password: `EmailInput`, `ErrorAlert`
   - Reset Password: `EmailInput`, `CodeInput`, `NewPasswordInput`, `ConfirmPassword`, `ErrorAlert`
6. Trong **Submit Buttons**, đặt Size = 4 rồi gán các button submit:
   `LoginBtn`, button `Register` trong panel Register, `Send Code`, `SavePassword`.
7. Để **Main Menu Scene Name** là `MainMenuScene` và bật **Login After Register** nếu muốn
   đăng ký xong vào thẳng Main Menu.

### Button On Click

Kéo object `LoginUiController` vào từng sự kiện **On Click** và chọn:

| Button | Hàm |
|---|---|
| `LoginBtn` | `LoginUiController.Login` |
| `RegisterBtn` ở panel Login | `LoginUiController.ShowRegister` |
| `FogotPasswordBtn` | `LoginUiController.ShowForgotPassword` |
| button `Register` ở panel Register | `LoginUiController.Register` |
| `BackToLogin` ở Register | `LoginUiController.ShowLogin` |
| `Send Code` | `LoginUiController.SendResetCode` |
| `BackToLogin` ở Forgot Password | `LoginUiController.ShowLogin` |
| `SavePassword` | `LoginUiController.SaveNewPassword` |
| `BackToLogin` ở Reset Password | `LoginUiController.ShowLogin` |

Hai button Google/Facebook dùng component riêng. Xem `SETUP_SOCIAL_LOGIN_VI.md` trong cùng thư mục.

## 2. MainMenuScene

1. Mở scene `Assets/Scenes/MainMenuScene.unity`.
2. Chọn object TMP tên `Name` đang hiển thị `Name:`.
3. Thêm component `MainMenuPlayerName` trực tiếp lên object đó.
4. Field **Player Name Text** có thể để trống (script tự lấy TMP trên cùng object), hoặc kéo chính
   component `TextMeshPro - Text (UI)` vào field này.
5. Giữ **Prefix** là `Name: ` và bật **Refresh From Server**.

Sau khi login/register thành công, `PlayerSession` giữ tên khi chuyển scene. Main Menu hiển thị tên
ngay, sau đó gọi `GET /profile` để đồng bộ lại với server.

## 3. Chạy backend/API

Asset `ApiClientSettings.asset` hiện quyết định API được gọi:

- Dùng staging: chọn `Staging` và nhập URL HTTPS của server staging.
- Dùng local: thêm entry `Local` với `http://localhost:3333`, chọn `Local`, rồi chạy trong
  thư mục `Backend`:

```powershell
npm install
npm run db:up
npm run db:migrate
npm run dev
```

Local cần Node.js 22+, npm 10+ và Docker Desktop. Kiểm tra server bằng
`http://localhost:3333/health/ready`; Swagger ở `http://localhost:3333/docs`.

## 4. Test nhanh

1. Mở `LoginPage` và bấm Play.
2. Register với email mới, tên dài 3-16 ký tự (chữ/số/`_`), mật khẩu 8-128 ký tự.
3. Khi thành công game chuyển sang `MainMenuScene` và dòng đầu hiển thị `Name: <tên>`.
4. Nếu tắt **Login After Register**, quay lại Login và đăng nhập bằng tài khoản vừa tạo.

Lưu ý: token hiện được giữ trong RAM bởi `InMemoryTokenStorage`; thoát game sẽ cần đăng nhập lại.
Không lưu access/refresh token vào `PlayerPrefs` cho bản production.
