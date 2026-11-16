# Ripptopia demo backend (không dùng database)

Service này dành riêng cho việc demo luồng login hiện tại của Unity. Nó **không kết nối
PostgreSQL/Prisma** và không đọc hoặc ghi dữ liệu trong `BackendDev`.

## Contract được hỗ trợ

| Method | Route | Mục đích |
| --- | --- | --- |
| `GET` | `/health/ready` | Health check cho host |
| `POST` | `/auth/login` | Đăng nhập email/password |
| `POST` | `/auth/refresh` | Đổi refresh token lấy cặp token mới |
| `GET` | `/profile` | Kiểm tra access token và trả profile |

Hai tài khoản demo mặc định:

| Email | Password | User |
| --- | --- | --- |
| `player1@ripptopia.demo` | `Demo123!` | `PlayerOne` |
| `player2@ripptopia.demo` | `Demo123!` | `PlayerTwo` |

Đây là thông tin công khai chỉ dùng để demo. Không tái sử dụng password thật.

## Chạy local

Yêu cầu Node.js 22 trở lên.

```powershell
cd DemoBackend
Copy-Item .env.example .env
npm ci
npm test
npm start
```

Server chạy tại `http://localhost:3334`. Kiểm tra bằng PowerShell:

```powershell
Invoke-RestMethod http://localhost:3334/health/ready

$body = @{
  email = 'player1@ripptopia.demo'
  password = 'Demo123!'
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:3334/auth/login `
  -ContentType 'application/json' `
  -Body $body
```

## Đưa source lên GitHub

GitHub lưu source nhưng không chạy Node backend. Nếu thư mục dự án chưa là Git repository
(trạng thái hiện tại của workspace này), chạy tại thư mục gốc `PhotonQuantumTestEvn`:

```powershell
git init -b main
git add .
git commit -m "Add database-free login demo backend"
```

Sau đó tạo một repository trống trên GitHub. Không chọn tạo sẵn README, `.gitignore` hoặc
license. Sao chép URL repository rồi chạy:

```powershell
git remote add origin https://github.com/USERNAME/REPOSITORY.git
git push -u origin main
```

Trước khi push, luôn kiểm tra `git status` và tuyệt đối không commit `.env`, token, private
key hoặc password thật. File `.env` đã được ignore; `.env.example` chỉ chứa mẫu.

Nếu đã cài và đăng nhập GitHub CLI, có thể thay các bước tạo remote bằng:

```powershell
gh repo create REPOSITORY --private --source=. --remote=origin --push
```

Đổi `--private` thành `--public` nếu thực sự muốn công khai toàn bộ Unity project.

## Tạo URL HTTPS bằng Render

Repository đã có `render.yaml` tại thư mục gốc, cấu hình một Web Service miễn phí ở
Singapore và tự sinh `JWT_SECRET`.

1. Đăng nhập [Render Dashboard](https://dashboard.render.com/) bằng GitHub.
2. Chọn **New > Blueprint**.
3. Kết nối repository vừa push; Render tự đọc `render.yaml`.
4. Chọn **Apply/Deploy Blueprint** và đợi health check thành công.
5. Mở service `ripptopia-login-demo`, sao chép URL dạng
   `https://ripptopia-login-demo-xxxx.onrender.com`.
6. Mở `https://...onrender.com/health/ready`; kết quả cần có
   `"status":"ok"` và `"database":"not-used"`.

Render terminate TLS ở load balancer, nên URL `onrender.com` nhận được là HTTPS dù Node
service bên trong lắng nghe HTTP. Mỗi lần push vào branch đã kết nối, Render tự deploy lại.

Gói Free phù hợp cho demo, nhưng service có thể sleep sau 15 phút không có traffic. Request
đầu tiên sau đó có thể mất khoảng một phút để khởi động. Vì refresh token chỉ nằm trong RAM,
mọi refresh token cũ sẽ mất khi service sleep, restart hoặc redeploy; chỉ cần login lại.

## Nối URL HTTPS vào Unity

Unity client hiện gọi `POST /auth/login` và đã khớp với response của service này.

1. Trong Unity, chọn **Create > Ripp > API Client Settings** nếu chưa có settings asset.
2. Trong asset, thêm/cập nhật một entry:
   - `Environment`: `Staging` (khuyến nghị cho demo) hoặc `Production`.
   - `Base Url`: URL Render, không thêm `/auth/login`, ví dụ
     `https://ripptopia-login-demo-xxxx.onrender.com`.
3. Chọn đúng `Active Environment`.
4. Gán settings asset này vào field `Settings` của component `ApiBootstrap` trong scene
   khởi động/login.
5. Login bằng một trong hai tài khoản demo ở trên.

Không thêm dấu `/` cuối URL cũng được; client tự chuẩn hóa URL. Với `Production`, Unity bắt
buộc URL phải dùng `https://`.

## Tùy biến tài khoản mà vẫn không dùng database

Trên Render, vào **Service > Environment** và thêm biến secret `DEMO_ACCOUNTS_JSON`, ví dụ:

```json
[
  {
    "id": "33333333-3333-4333-8333-333333333333",
    "email": "demo@example.com",
    "password": "change-this-demo-password",
    "displayName": "DemoPlayer",
    "role": "PLAYER"
  }
]
```

Sau khi lưu, Render redeploy service. Danh sách này vẫn chỉ được nạp vào RAM khi process
khởi động; không có database hay file persistence.

## Giới hạn có chủ đích

- Chỉ dùng cho demo/test, không dùng cho production.
- Không có đăng ký, quên mật khẩu, social login hoặc lưu tiến trình.
- Password demo trong RAM không được hash vì không có kho dữ liệu; đặt biến tài khoản ở
  Render thay vì commit nếu không muốn công khai.
- Refresh sessions mất khi process restart; access token đã ký vẫn hợp lệ đến khi hết hạn.
