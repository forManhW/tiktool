# Nghiệp vụ tài khoản, VIP và mua bán game (triển khai ở V2)

> Tài liệu đặc tả để làm V2. Chưa code ở V1. Các mục **[Cần quyết định]** là lựa chọn kinh doanh của chủ sản phẩm. Tài liệu ghi sẵn đề xuất cho từng mục.

## 1. Vai trò

| Vai trò | Ai | Quyền |
| --- | --- | --- |
| **Khách** | Chưa đăng nhập | Xem cửa hàng, tải và chơi **bản dùng thử** mọi game, dùng đầy đủ hub sự kiện LIVE với bản dùng thử |
| **Thành viên** | Đã đăng ký, chưa VIP | Như khách, cộng thêm: lưu cấu hình mapping lên cloud, lịch sử đơn hàng |
| **VIP** | Có gói VIP còn hạn | Tải và chơi **bản full** mọi game đang phát hành, nhận cập nhật |
| **Admin** | Chỉ chủ TikTool | Quản lý game, giá, đơn hàng, người dùng, hoàn tiền, khóa tài khoản |

Chỉ có **một người bán là chủ TikTool**. Không có tài khoản nhà phát triển và không có upload game từ bên ngoài. Game chỉ được đưa lên qua công cụ nội bộ (`tools/pack-game.mjs`) bằng private key của chủ.

## 2. Bản dùng thử và bản full

Mỗi game có hai gói build riêng (xem mục 4 của `KE_HOACH_NEN_TANG.md`):

| | Bản dùng thử (trial) | Bản full |
| --- | --- | --- |
| Ai tải được | Mọi người | VIP còn hạn |
| Giới hạn đề xuất **[Cần quyết định cho từng game]** | Ví dụ: tối đa 15 phút mỗi phiên, tối đa 3 mapping, có watermark "TikTool Trial" trên overlay, khóa một số effect | Không giới hạn |
| Tải từ | Link công khai | Link ký có hạn 5 phút, cấp theo license |

Giới hạn trial phải nằm **trong code của bản trial**. Đừng dựa vào hub để chặn, vì hub chạy trên máy người dùng.

## 3. Mô hình bán

**Đề xuất cho V2: chỉ bán gói VIP thuê bao.** Một gói mở toàn bộ game.

| Gói | Thời hạn | Ghi chú |
| --- | --- | --- |
| VIP 1 tháng | 30 ngày | Gói chính |
| VIP 3 tháng | 90 ngày | Giảm giá nhẹ |
| VIP 12 tháng | 365 ngày | Giảm sâu nhất |

Lý do chọn VIP trước: chỉ một loại quyền (`vip_until`), dễ làm và dễ hỗ trợ. Người dùng cũng thấy giá trị tăng dần khi có thêm game.

**[Cần quyết định]**, có thể thêm ở V2.5:
- **Bán đứt từng game** (lifetime). Cần thêm bảng `entitlements` theo game. Nên định nghĩa "bán đứt" là được dùng phiên bản lớn hiện tại và các bản cập nhật trong 12 tháng.
- **Mã quà tặng / mã kích hoạt**, để bán qua kênh khác như Shopee hoặc nhắn tin trực tiếp.

### 3.1 Quy tắc gia hạn
- Mua thêm khi còn hạn: cộng nối vào `vip_until` hiện tại, không tính lại từ hôm nay.
- **Không tự động trừ tiền định kỳ** ở V2, vì cổng nội địa (VietQR, MoMo) làm thuê bao định kỳ phức tạp. Thay vào đó, nhắc gia hạn qua email hoặc thông báo trong app trước 3 ngày và trước 1 ngày.
- Hết hạn VIP: không tải hoặc cập nhật được bản full nữa. Bản full đã cài **ngừng chạy khi license token hết hạn** (tối đa 7 ngày offline, mục 6). Bản trial vẫn chơi bình thường. Mapping và cấu hình được giữ nguyên.

## 4. Tài khoản

### 4.1 Đăng ký và đăng nhập
- Đăng ký bằng **email + mật khẩu**, xác minh email bằng link có hạn 24 giờ.
- Tùy chọn **Đăng nhập bằng Google** (OAuth trên trình duyệt hệ thống theo chuẩn PKCE, không dùng webview trong app).
- Đăng nhập trong launcher: mở trình duyệt hệ thống → đăng nhập trên web → chuyển về app qua deep link `tiktool://auth?code=...` → app đổi code lấy token. App **không bao giờ** nhận mật khẩu trực tiếp.
- Liên kết TikTok username **chỉ để hiển thị và hỗ trợ**, không dùng làm định danh đăng nhập.

### 4.2 Mật khẩu và phiên
- Mật khẩu: tối thiểu 8 ký tự. Lưu bằng **Argon2id** (hoặc bcrypt cost ≥ 12). Không bao giờ log mật khẩu.
- Chặn dò mật khẩu: 5 lần sai trong 15 phút → khóa đăng nhập 15 phút theo email và theo IP. Có CAPTCHA (Cloudflare Turnstile) ở form web.
- Token: **access token** (JWT, hạn 15 phút) + **refresh token** (ngẫu nhiên, hạn 30 ngày, xoay vòng mỗi lần dùng, lưu dạng hash trên server).
- Trên máy người dùng, refresh token lưu bằng `safeStorage` của Electron (DPAPI trên Windows), không lưu file thường.
- Quên mật khẩu: link đặt lại hạn 30 phút, dùng một lần. Đặt lại thành công → thu hồi mọi phiên.
- **[Cần quyết định]** 2FA (TOTP): bắt buộc cho admin, tùy chọn cho người dùng.

### 4.3 Giới hạn thiết bị (chống chia sẻ tài khoản)
- Mỗi tài khoản VIP được kích hoạt tối đa **2 máy** cùng lúc. **[Cần quyết định]**
- Định danh máy: hash của (machine GUID của Windows + salt của app). Không gửi thông tin phần cứng thô.
- Người dùng tự gỡ máy cũ trong trang tài khoản, tối đa **3 lần gỡ / 30 ngày** để chống xoay vòng chia sẻ.
- Phát hiện bất thường: một tài khoản bị kích hoạt từ nhiều quốc gia hoặc IP trong thời gian ngắn → cảnh báo admin, chưa tự khóa.

### 4.4 Khóa và xóa tài khoản
- Admin có thể **tạm khóa** (gian lận thanh toán, chia sẻ tài khoản, chargeback). Người dùng được báo lý do qua email.
- Người dùng có quyền **yêu cầu xóa tài khoản và dữ liệu cá nhân**. Hoàn tất trong 30 ngày. Hóa đơn và giao dịch được giữ theo thời hạn kế toán, dưới dạng ẩn danh hóa phần không bắt buộc.

## 5. Thanh toán

### 5.1 Kênh
| Kênh | Cách làm | Ưu tiên |
| --- | --- | --- |
| **VietQR chuyển khoản** | Tạo đơn → hiện mã QR có số tiền + nội dung `TT<mã đơn>` → dịch vụ đối soát (SePay hoặc Casso) gọi webhook khi tiền vào | **V2, làm đầu tiên**: phí thấp, người Việt quen dùng |
| MoMo / ZaloPay | Cổng thanh toán chính thức, cần đăng ký merchant | V2 hoặc V2.5 |
| VNPay (thẻ ATM/Visa) | Cổng thanh toán | V2.5 |
| Visa quốc tế (Stripe/Paddle) | Nếu bán ra nước ngoài | V3 |

Cổng thanh toán chính thức thường yêu cầu đăng ký kinh doanh (hộ kinh doanh hoặc công ty).

### 5.2 Vòng đời đơn hàng

```
created ──(webhook xác nhận đủ tiền)──► paid ──► fulfilled (cộng vip_until)
   │                                      │
   ├─(quá 30 phút chưa trả)──► expired     └─(hoàn tiền)──► refunded (trừ lại vip_until)
   └─(người dùng hủy)──► cancelled
```

Quy tắc bắt buộc:
1. **Chỉ cấp VIP khi server nhận webhook** từ cổng hoặc dịch vụ đối soát. Không bao giờ tin việc client báo "đã thanh toán".
2. Webhook phải **xác thực chữ ký** (secret của SePay, Casso hoặc cổng) và **idempotent** theo mã giao dịch của ngân hàng hoặc cổng. Một giao dịch chỉ cộng VIP một lần.
3. Đối chiếu đúng **số tiền** và **mã đơn** trong nội dung chuyển khoản. Chuyển thiếu → đơn `underpaid`, admin xử lý. Chuyển thừa → vẫn cấp, ghi chú để hoàn phần thừa nếu khách yêu cầu.
4. Giá luôn lấy từ server theo mã gói. Client chỉ gửi `planId`.
5. Mọi thay đổi `vip_until` ghi vào `vip_ledger` (ai, khi nào, lý do, đơn nào) để tra soát.

### 5.3 Hoàn tiền **[Cần quyết định]**
Đề xuất:
- Hoàn 100% trong **3 ngày** kể từ khi mua nếu chưa tải bản full nào, hoặc nếu lỗi kỹ thuật không khắc phục được.
- Sau 3 ngày: không hoàn, trừ khi lỗi do TikTool.
- Hoàn tiền → trừ số ngày tương ứng khỏi `vip_until` và thu hồi license token ở lần kiểm tra online kế tiếp.

### 5.4 Hóa đơn và thuế
- Mỗi đơn `paid` có biên nhận (email + trang lịch sử).
- Hóa đơn điện tử: làm theo quy định cho loại hình kinh doanh đã đăng ký. **Hỏi kế toán** về thuế suất cho dịch vụ phần mềm và nội dung số trước khi mở bán.

## 6. License và tải bản full

### 6.1 License token
Server ký bằng **Ed25519**. Launcher kiểm tra bằng public key, không cần mạng:

```json
{
  "sub": "user_123",
  "device": "dev_hash_abc",
  "plan": "vip",
  "vipUntil": "2027-01-10T00:00:00Z",
  "games": "*",
  "iat": 1794200000,
  "exp": 1794804800
}
```

- `exp` = min(`vipUntil`, bây giờ + **7 ngày**). Launcher online tự lấy token mới mỗi lần mở app hoặc mỗi 24 giờ.
- Offline quá `exp` → bản full không chạy và báo "Hãy kết nối mạng để xác minh VIP". Bản trial vẫn chạy.
- Đồng hồ máy: lưu `lastSeenTime` đã ký. Nếu đồng hồ bị lùi quá 24 giờ thì bắt buộc kiểm tra online.

### 6.2 Tải bản full
1. Launcher gọi `POST /downloads/{gameId}` kèm access token và device id.
2. Server kiểm tra VIP còn hạn và thiết bị đã kích hoạt → trả **URL ký có hạn 5 phút** tới file `.ttgame` bản full trên R2/S3.
3. Launcher tải → kiểm tra chữ ký gói → cài.
4. Ghi log tải (người dùng, game, phiên bản, IP) để phát hiện chia sẻ.
5. Giới hạn: tối đa 20 lượt tải mỗi ngày mỗi tài khoản.

### 6.3 Watermark
Khi phát URL tải, server có thể ghép một file `license.bin` riêng cho người dùng (chứa `sub` đã mã hóa) vào gói. Gói bị phát tán lậu sẽ truy được về tài khoản nguồn.

## 7. Dữ liệu cá nhân

Dữ liệu thu thập (tối thiểu): email, tên hiển thị, hash mật khẩu, TikTok username (tùy chọn), device hash, IP đăng nhập, lịch sử đơn hàng.

Không thu thập: danh bạ, dữ liệu người xem LIVE (comment và tên người xem chỉ xử lý **trên máy streamer**, không gửi về server TikTool).

Yêu cầu:
- Có **Chính sách quyền riêng tư** và **Điều khoản sử dụng** trước khi mở đăng ký, tuân theo quy định Việt Nam về bảo vệ dữ liệu cá nhân hiện hành (Nghị định 13/2023/NĐ-CP và các văn bản mới hơn). Nên nhờ người có chuyên môn pháp lý rà soát.
- Người dùng xem, sửa, tải về và xóa được dữ liệu của mình.
- Mã hóa đường truyền (HTTPS), sao lưu DB mã hóa, phân quyền truy cập DB tối thiểu.

## 8. Điều khoản sử dụng: các điểm chính

1. TikTool là công cụ độc lập, **không liên kết hay được bảo trợ bởi TikTok**. Kết nối LIVE có thể gián đoạn khi TikTok thay đổi hệ thống. Đây không phải lý do hoàn tiền nếu đã thông báo trước.
2. Người dùng mua **quyền sử dụng**, không mua mã nguồn. Cấm dịch ngược, sửa, phân phối lại game và chia sẻ tài khoản.
3. Vi phạm → khóa tài khoản, không hoàn tiền.
4. Streamer tự chịu trách nhiệm tuân thủ chính sách cộng đồng và chính sách LIVE của TikTok khi dùng game.
5. Giới hạn trách nhiệm, luật áp dụng là luật Việt Nam, kênh hỗ trợ.

## 9. Mô hình dữ liệu (PostgreSQL)

```sql
users(id, email UNIQUE, email_verified_at, password_hash, display_name, tiktok_username,
      role CHECK (role IN ('user','admin')), status CHECK (status IN ('active','locked','deleted')),
      vip_until TIMESTAMPTZ, created_at)
sessions(id, user_id, device_id, refresh_token_hash, expires_at, revoked_at, created_ip, created_at)
devices(id, user_id, device_hash, name, activated_at, removed_at)
plans(id, code, name, days, price_vnd, active)
orders(id, user_id, plan_id, amount_vnd, status, payment_channel, payment_ref UNIQUE,
       transfer_code UNIQUE, created_at, paid_at, expires_at)
payment_events(id, channel, external_id UNIQUE, raw_payload JSONB, signature_ok, received_at, order_id)
vip_ledger(id, user_id, delta_days, reason, order_id, actor_id, created_at)
games(id, slug, name, description, status CHECK (status IN ('draft','published','hidden')))
game_releases(id, game_id, version, edition CHECK (edition IN ('trial','full')),
              file_key, sha256, size, signature, min_hub_version, released_at)
downloads(id, user_id, release_id, device_id, ip, created_at)
audit_log(id, actor_id, action, target, details JSONB, created_at)
```

## 10. API (tối thiểu cho V2)

| Method | Path | Mô tả |
| --- | --- | --- |
| POST | `/auth/register` · `/auth/verify-email` · `/auth/login` · `/auth/refresh` · `/auth/logout` | Tài khoản |
| POST | `/auth/forgot-password` · `/auth/reset-password` | Quên mật khẩu |
| GET | `/me` · `/me/devices` · DELETE `/me/devices/{id}` | Hồ sơ, thiết bị |
| POST | `/license/activate` (device) · `/license/refresh` | Cấp license token |
| GET | `/catalog` | Danh sách game và bản phát hành (đã ký) |
| POST | `/downloads/{gameId}` | URL tải bản full |
| GET | `/plans` · POST `/orders` · GET `/orders/{id}` | Mua VIP |
| POST | `/webhooks/sepay` · `/webhooks/momo` · ... | Xác nhận thanh toán (chỉ server gọi) |
| * | `/admin/*` | Quản trị (bắt buộc 2FA, giới hạn IP nếu được) |

## 11. Bảo mật server: checklist

- [ ] HTTPS, HSTS. Admin và API cùng domain thì phải có CSRF token cho form web
- [ ] Rate limit theo IP và theo tài khoản cho auth, orders, downloads
- [ ] Validate input bằng schema (zod hoặc ajv). Chỉ dùng câu SQL tham số hóa
- [ ] Bí mật (DB, webhook secret, private key ký license và ký game) nằm trong biến môi trường hoặc secret manager, **không commit**
- [ ] **Tách key**: khóa ký game (offline, chỉ trên máy build) khác khóa ký license (trên server)
- [ ] Log audit cho mọi hành động admin và mọi thay đổi VIP
- [ ] Sao lưu DB hằng ngày, thử khôi phục hằng tháng
- [ ] Theo dõi lỗi (Sentry) cho server và launcher, không gửi dữ liệu cá nhân
- [ ] Trước khi mở bán: tự pentest các luồng thanh toán (webhook giả, replay, sửa số tiền, đơn hết hạn)

## 12. Danh sách quyết định cần chốt trước khi làm V2

1. Giá các gói VIP và có làm gói 3 hoặc 12 tháng không.
2. Giới hạn bản trial cho từng game.
3. Số thiết bị mỗi tài khoản (đề xuất 2).
4. Chính sách hoàn tiền (đề xuất 3 ngày).
5. Có bán đứt từng game hay mã kích hoạt không.
6. Loại hình kinh doanh (hộ kinh doanh hay công ty) và kênh thanh toán đầu tiên.
7. Có cần 2FA cho người dùng thường không.
