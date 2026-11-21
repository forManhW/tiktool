# Kế hoạch: Đại Chiến Xanh Đỏ (game 3D tương tác TikTok LIVE)

> Tên tạm: **Đại Chiến Xanh Đỏ** (`red-blue-brawl`). Game 3D làm bằng Unity, build ra **.exe Windows**.
> **Bản 1 phụ thuộc TikTool**: TikTool nhận quà/tim/comment từ TikTok LIVE, đổi thành lệnh theo cài đặt của streamer, gửi cho game qua Game Gateway (`ws://127.0.0.1:8765`). Kiến trúc đã chừa chỗ để sau này tách thành bản riêng (mục 4.7).
>
> Tài liệu giao việc cho AI khác: [BRIEF_ANTIGRAVITY.md](BRIEF_ANTIGRAVITY.md), [BRIEF_CHATGPT.md](BRIEF_CHATGPT.md).

---

## 0. Môi trường và vị trí project

| Công cụ | Phiên bản | Ghi chú |
| --- | --- | --- |
| Unity Editor | **6000.3.29f1 (Unity 6.3 LTS)** + Windows Build Support | `C:\Program Files\Unity\Hub\Editor\6000.3.29f1`, license Personal đã kích hoạt |
| Unity Hub | 3.22.6 | |
| .NET SDK / VS Code | 10.0.404 / extension Unity, C# Dev Kit | |
| Git / GitHub CLI | 2.56.1 / 2.105.0 | Đã đăng nhập `forManhW` |
| Node.js / Python | 26.13.1 / 3.13.9 | Chạy TikTool hub và adapter TikTok |

**Repo:** dùng chung repo `tiktool`, clone ở **`C:\MinhAnh\tiktool`** (ổ B không đủ chỗ cho Unity). Không có repo riêng cho game.

```
C:\MinhAnh\tiktool\Game\DaiChienXanhDo\
├─ manifest.json            TikTool đọc file này: danh sách hàm, cách mở game (.exe)
├─ KE_HOACH.md              file này
├─ BRIEF_ANTIGRAVITY.md     giao việc cho Antigravity
├─ BRIEF_CHATGPT.md         giao việc cho ChatGPT
├─ CREDITS.md               nguồn + giấy phép từng tài nguyên miễn phí
├─ Build\                   bản build .exe (không commit, tạo lại bằng BuildScript)
└─ UnityProject\            project Unity (mở bằng Unity Hub → Add → Add project from disk)
   ├─ Assets\_Game\Scripts\{Core, Fighters, Effects, Camera, Net, Debug, Editor, UI}
   ├─ Assets\_Game\{Scenes, Materials, Resources, Settings, Prefabs, Art, Audio, VFX, UI}
   ├─ Packages\, ProjectSettings\
   └─ Library\ Temp\ Logs\ UserSettings\   (Unity tự tạo, không commit)
```

**Một project Unity chỉ mở được bằng một Unity Editor một lúc.** Khi bạn đang mở Unity Editor, Claude không build bằng dòng lệnh được, và ngược lại.

---

## 1. Ý tưởng trong một câu

Một trận đấu **không bao giờ dừng** giữa hai phe **Xanh** và **Đỏ**. Người xem nhảy vào lúc nào cũng được bằng quà, tim hoặc comment. Mỗi người có một nhân vật mang tên mình. Phe nào bị phá lõi thì thua, phe kia +1 điểm, sân reset và ván mới bắt đầu ngay.

---

## 2. Luật chơi

### 2.1 Trận đấu vô hạn

- **Không có lobby, không có đồng hồ.** Trận luôn diễn ra.
- Sân **56×56m**, mặt cỏ, đường đất nối 2 lâu đài, tường thành bao quanh. **Lâu đài Xanh ở góc dưới bên trái**, **lâu đài Đỏ ở góc trên bên phải**, cách nhau khoảng 62m. Mỗi lâu đài **1000 máu** (chỉnh trong cài đặt game).
- **Không có cách hồi máu lõi.** Lõi về 0 là thua.
- Lính hai phe tự tìm địch gần nhất và đấm nhau.
- **Khi một phe chết hết lính**, lõi phe đó không còn ai bảo vệ: lính phe kia kéo thẳng tới đánh lõi. Phe bị đánh chỉ còn cách gọi người vào cứu.
- Hiệu ứng lớn (sấm sét khi phe kia hết lính, xe tăng, thiên thạch, robot) đánh thẳng vào lõi được.
- Sân chưa có ai thì lõi không bị trừ máu.

### 2.2 Thắng ván, tỉ số, reset

1. Lõi một phe về 0 → **phe kia thắng ván**.
2. Hiệu ứng thắng 7 giây: lõi thua sụp xuống, banner "PHE XANH THẮNG!" kèm **MVP** (người đóng góp nhiều nhất phe thắng).
3. **Tỉ số ở góc trên bên trái** tự +1: `XANH 3 : 5 ĐỎ`.
4. **Tự reset**: xóa hết lính và vật thể hiệu ứng, hồi đầy 2 lõi, xóa danh sách người chơi của ván. Ai muốn xuất hiện lại phải gửi lệnh tiếp.
5. **Quà tặng trong lúc ăn mừng không mất**: game giữ lại và chạy ngay khi ván mới bắt đầu. Hiệu ứng lớn còn trong hàng đợi cũng được giữ.
6. Tỉ số **giữ theo ngày**, sang 0h thì về `0 : 0`.

### 2.3 Người chơi xuất hiện thế nào

- **Chưa gửi lệnh thì không có nhân vật.** Lệnh là quà/tim/comment mà streamer đã cài trong TikTool.
- Lệnh đầu tiên của một người trong ván tạo **nhân vật mang tên người đó** cạnh lõi phe của lệnh.
- **Quà cố định phe**: `blue_*` luôn cho Xanh, `red_*` luôn cho Đỏ.
- **Mỗi người một nhân vật chính trong một ván**, thuộc phe của lệnh đầu tiên. Nếu sau đó người đó gửi lệnh của phe kia, hiệu ứng vẫn chạy cho phe kia qua một lính tạm mang tên họ. Nhân vật chính không đổi phe.
- Nhân vật chết thì biến mất. Gửi lệnh tiếp (của phe mình) thì hồi sinh ở lõi.
- **Tên hiện trên đầu nhân vật.** Sân đông hơn 40 lính thì chỉ hiện tên đầy đủ cho người camera đang theo, người vừa gửi lệnh trong 5 giây và top 3 mỗi phe. Người khác hiện chữ viết tắt.

### 2.4 Bảng hiệu ứng (đã chốt)

Game chỉ viết **hàm**. Quà nào, comment gì, bao nhiêu tim ứng với hàm nào **đều cài trong TikTool**.

| Bậc | Hiệu ứng | Hàm Xanh | Hàm Đỏ | Tác dụng | Gợi ý cài trong TikTool |
| --- | --- | --- | --- | --- | --- |
| 0 | Vào phe | `blue_join` | `red_join` | Tạo nhân vật nếu chưa có | Comment `xanh` / `đỏ` (đã cài sẵn) |
| 0 | Tim tiếp sức | `blue_like_power` | `red_like_power` | +5%/đơn vị sát thương cả phe (tối đa +100%) trong 5 giây | Mỗi 50 tim |
| 1 | Triệu hồi lính | `blue_spawn` | `red_spawn` | Gọi ra `amount` lính, **mỗi lính mang tên người tặng**. Sân đầy (200) thì đổi thành tim tiếp sức | Quà 1 xu (Hoa hồng), follow |
| 3 | Triệu hồi Siêu Chiến Binh | `blue_superhero` | `red_superhero` | Gọi ra `amount` siêu nhân **mang tên người tặng**: to gấp 1,6, bay, giáp vàng, mạnh x5, chịu đòn tốt, có thanh máu riêng, sống tới khi bị hạ | Quà 20–99 xu |
| 4 | Sấm sét | `blue_lightning` | `red_lightning` | Mỗi đợt đánh chết 5 lính địch. Địch hết lính thì đánh lõi 30 | Quà 100–299 xu |
| 5 | Xe tăng | `blue_tank` | `red_tank` | Lăn về lõi địch 20 giây, nghiền lính, 2 giây bắn lõi một phát 20 | Quà 300–999 xu |
| 6 | Mưa thiên thạch | `blue_meteor` | `red_meteor` | Mỗi đợt 12 thiên thạch xuống nửa sân địch, trừ lõi địch 80 | Quà 1000–4999 xu |
| 7 | Robot khổng lồ | `blue_giant_robot` | `red_giant_robot` | Rơi xuống sân, đi tới lõi địch, giẫm chết lính, mỗi giây đập lõi 50, tồn tại 25 giây | Quà 5000+ xu |

**Hàm theo phe đã tham gia** (cho tim/follow/share). Người chưa có nhân vật thì game bỏ qua, TikTool ghi log `failed`:

| Hàm | Tác dụng |
| --- | --- |
| `ally_like_power` | Tim tiếp sức cho phe người đó đang ở |
| `ally_spawn` | Thêm lính cho phe người đó đang ở |

**Hàm sự kiện và quản trị:** `event_x2`, `event_disaster`, `admin_reset_round`, `admin_reset_score`.

> **Không có nhân vật vô danh.** Mọi lính, siêu nhân, xe tăng, robot đều mang tên người triệu hồi. Lệnh không kèm người gửi (vd. bấm thử trong Test Lab) bị game từ chối.

Quy tắc chung:

- **Combo quà** (Hoa hồng x10) nhân `amount` theo `repeat: multiply` của TikTool. Mapping không đặt `amount` thì game lấy số combo (`count`) làm số lượng.
- **Mọi sát thương nhân với sức mạnh của phe** = tim tiếp sức × X2.
- **Hàng đợi hiệu ứng**: bậc 4+ chạy lần lượt, cách nhau tối thiểu 3 giây. Bậc 0–3 chạy ngay.
- **Không có giọng đọc AI. Không có quà hồi máu lõi.**

> Các con số trên là bản đầu, chỉnh lại sau khi live thử.

### 2.5 Sự kiện giữa trận (game tự chạy, chỉnh trong `settings.json`)

| Sự kiện | Khi nào | Tác dụng |
| --- | --- | --- |
| **X2 Sức Mạnh** | Ngẫu nhiên mỗi 3–5 phút, báo trước 5 giây | 30 giây: cả hai phe mạnh gấp đôi |
| **Thảm họa** (động đất / lốc xoáy / núi lửa) | Tổng số lính vượt **120** | Cảnh báo 3 giây, sau đó giết **40% lính mỗi phe**, mỗi phe còn ít nhất 3 lính, không trừ máu lõi. Hồi chiêu 20 giây |

Giới hạn cứng: **200** lính trên sân. Đầy thì `*_spawn` đổi thành tim tiếp sức.

### 2.6 Lưu dữ liệu theo ngày

- `%USERPROFILE%\AppData\LocalLow\TikTool\DaiChienXanhDo\data\YYYY-MM-DD.json`: tỉ số, số ván, top người đóng góp hôm nay. Giữ 30 ngày.
- Điểm đóng góp = số xu của quà × combo nếu TikTool gửi kèm. Không có thì tính theo bậc: 1 / 5 / 20 / 100 / 300 / 1000 / 5000 cho bậc 1–7. Tim, comment, follow = 0.
- Cài đặt game: cùng thư mục, file `settings.json`.

> **Bản quyền:** không dùng tên, logo, trang phục của Superman hay nhân vật có thương hiệu. Gọi là "Siêu Chiến Binh".

---

## 3. Màn hình, camera và hai chế độ

### 3.1 Khung hình

Cửa sổ game **vuông 1080x1080** (đổi trong `settings.json` → `windowSize`). Cửa sổ bị kéo méo thì game tự thêm viền đen, giữ khung vuông. Trong OBS / LIVE Studio ghép trên khung dọc 1080x1920:

```
┌──────────────────────┐  0
│   CAMERA STREAMER    │
│      (~420 px)       │
├──────────────────────┤  420
│ XANH 3 : 5 ĐỎ   [Đỏ] │  ← tỉ số góc trên trái, Lõi Đỏ góc trên phải
│   GAME VUÔNG         │
│   1080 x 1080        │
│ [Xanh]  thông báo    │  ← Lõi Xanh góc dưới trái
├──────────────────────┤  1500
│   BÌNH LUẬN TIKTOK   │
│      (~420 px)       │
└──────────────────────┘  1920
```

Sân 36x36, hai lõi ở hai góc chéo. Camera nghiêng 55°, góc nhìn mặc định nằm dọc trục Z nên Lõi Xanh rơi vào góc dưới trái, Lõi Đỏ vào góc trên phải.

### 3.2 Chế độ **Live tự động** (mặc định)

- **Đạo diễn camera** theo ưu tiên: thắng ván > quà bậc 4+ (bám theo xe tăng/robot) > **người mới vào** > lõi sắp vỡ (dưới 20%) > quà bậc 1–3 > toàn cảnh.
- Mỗi cảnh giữ tối thiểu 1 giây. Cảnh chờ quá 6 giây thì bỏ. Hơn 3 người vào trong 2 giây thì gom thành một thông báo "+N chiến binh mới vào phe X".
- **Hiện thông báo**: dải hướng dẫn luân phiên mỗi 6 giây (nội dung trong `settings.json` → `guideLines`), thông báo người vào/quà lớn, đếm ngược sự kiện, banner thắng.

### 3.3 Chế độ **Thường**

Không hiện thông báo. Chỉ còn tỉ số, máu lõi, tên trên đầu nhân vật. Streamer điều khiển camera:

| Thao tác | Phím / chuột |
| --- | --- |
| Di chuyển | `W A S D` / mũi tên / giữ chuột giữa kéo |
| Xoay, nghiêng | Giữ chuột phải kéo, hoặc `Q` / `E` |
| Zoom | Lăn chuột |
| Theo một nhân vật | Click vào nhân vật, `Esc` để thôi |
| Lần lượt qua top người chơi | `Tab` |
| Toàn cảnh | `Space` |
| Lưu / gọi góc camera | `Ctrl+1..4` / `1..4` |

### 3.4 Phím chung

| Phím | Việc |
| --- | --- |
| `F1` | Đổi Live tự động ↔ Thường (game nhớ lựa chọn) |
| `F2` | Ẩn / hiện toàn bộ giao diện |
| `F10` | Menu cài đặt trong game (có bật/tắt nhạc nền, âm thanh hiệu ứng) |
| `M` / `N` | Bật/tắt nhạc nền / âm thanh hiệu ứng (game nhớ cho lần sau) |
| `Ctrl+R` | Reset ván (không tính điểm) |
| `Ctrl+Shift+R` | Reset tỉ số hôm nay |
| `F5` / `F6` | Test: người mới vào phe Xanh / Đỏ |
| `F7` / `F8` | Test: quà phe Xanh / Đỏ, lần lượt bậc 1 → 7. Giữ `Shift` = Robot ngay |
| `F9` | Test: 30 người vào ngẫu nhiên (thử tải, thử thảm họa) |

---

## 4. Ngôn ngữ, công nghệ, kiến trúc

| Phần | Công nghệ |
| --- | --- |
| Game 3D | **C#**, **Unity 6.3 LTS**, **URP 17.3** |
| Package | Input System 1.20.1, Newtonsoft JSON 3.2.2, UGUI 2.0 (có TextMeshPro). Không dùng Cinemachine: camera tự viết |
| Kết nối TikTool | `System.Net.WebSockets.ClientWebSocket` (có sẵn trong .NET) |
| Build | Windows Standalone .exe, backend **Mono** (IL2CPP cần cài thêm Visual Studio C++ Build Tools, để sau) |
| TikTool | JavaScript (Node.js, Electron). Đã thêm chế độ mở game `.exe` |

### 4.1 Mã nguồn (`Assets/_Game/Scripts`)

| Thư mục | File | Việc | Ai sở hữu |
| --- | --- | --- | --- |
| `Core/` | `GameEvents.cs` | **Hợp đồng** lõi ↔ UI: các event UI được đăng ký | Claude |
| | `CommandRouter.cs` | Tên hàm → phe + hiệu ứng, giữ lệnh khi đang ăn mừng | Claude |
| | `MatchManager.cs`, `CoreHealth.cs` | Thắng ván, tỉ số, reset; máu lõi | Claude |
| | `MatchEvents.cs`, `TeamPower.cs` | X2, X2 phe yếu, thảm họa; hệ số sức mạnh | Claude |
| | `PlayerRegistry.cs`, `DailyStore.cs` | Người xem ↔ nhân vật; lưu theo ngày | Claude |
| | `GameSettings.cs`, `GameState.cs`, `GameBootstrap.cs`, `HotkeyController.cs`, `Arena.cs`, `GameAssets.cs`, `Team.cs` | Cài đặt, chế độ, phím, hình học sân, chỗ gắn model | Claude |
| `Fighters/` | `Fighter.cs`, `FighterManager.cs` | AI lính, object pool | Claude |
| `Effects/` | `EffectManager.cs` | Mọi hiệu ứng bậc 0–7 (đang là greybox) | Claude |
| `Camera/` | `CameraRig.cs`, `CameraDirector.cs` | Camera tự do + đạo diễn tự động | Claude |
| `Net/` | `TikToolSource.cs`, `DebugKeySource.cs` | WebSocket TikTool; phím test F5–F9 | Claude |
| `Debug/` | `DebugHud.cs` | **Giao diện tạm** bằng IMGUI, tắt đi khi UI thật xong | Claude |
| | `AutoScreenshot.cs` | Test: chạy `.exe --screenshot a.png --screenshot-after 10` thì game tự chụp chính nó | Claude |
| `Editor/` | `ProjectSetup.cs`, `BuildScript.cs` | Dựng project/scene bằng code, build .exe | Claude |
| `UI/` | (chưa có) | Giao diện thật: tỉ số, máu lõi, tên, thông báo, menu F10 | **Antigravity** |

Luồng một lệnh:

```
TikTok LIVE → TikTool (mapping) → ws:8765 → TikToolSource → CommandRouter
   → PlayerRegistry (tạo người chơi) → FighterManager (sinh nhân vật) → EffectManager (hiệu ứng)
   → GameEvents (UI + CameraDirector nghe) → TikToolSource gửi command_result + game_state về TikTool
```

### 4.2 Giao thức với Game Gateway

1. Gửi `{"type":"game_hello","gameId":"red-blue-brawl","sessionId":"…","token":"…","role":"game"}`.
2. Nhận `game_hello_ack` → gửi `game_state`.
3. Nhận `game_action` `{ id, action, params:{amount, count}, source:{ eventType, user:{id,nickname,avatarUrl}, gift:{diamonds} } }`.
4. Trả `{"type":"command_result","commandId":"…","status":"success"|"failed"}` ngay khi nhận lệnh vào hàng đợi.
5. Gửi `game_state` khi có thay đổi, tối đa 5 lần/giây, kèm nhịp 2 giây: `{ score, cores, fighters, event, mode }`.
6. Mất kết nối thì thử lại sau 3 giây. Mã `4003` (token sai) thì dừng hẳn.

### 4.3 TikTool mở game thế nào

- `manifest.json`: `"launch": { "type": "native", "exe": "Build/DaiChienXanhDo.exe" }`.
- **Start Game** trong TikTool Desktop → `desktop.js` chạy `Build/DaiChienXanhDo.exe --session <id> --token <token> --ws ws://127.0.0.1:8765`. Start lại thì tắt bản cũ. Tắt TikTool thì tắt game.
- Chỉ chạy được file `.exe` nằm trong thư mục của game (`core/native-launch.js`, có test).
- Mở dashboard bằng trình duyệt thường thì TikTool báo câu lệnh để tự chạy.
- Đóng gói TikTool: bỏ `UnityProject/`, giữ `Build/` ngoài asar.

### 4.4 Chạy thử trong Unity Editor với TikTool thật

1. Chạy TikTool, chọn game, **Start Game** (game .exe sẽ mở, tắt nó đi).
2. Copy `session` và `token` trong Game URL, dán vào component **TikToolSource** (object `Game` trong scene).
3. Bấm Play trong Unity.

### 4.5 Build

- Trong Unity: menu **Dai Chien Xanh Do → Build Windows (.exe)**.
- Dòng lệnh (khi Unity Editor đang tắt):
  ```bash
  "C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe" -batchmode -quit -projectPath C:\MinhAnh\tiktool\Game\DaiChienXanhDo\UnityProject -executeMethod DaiChienXanhDo.EditorTools.BuildScript.BuildWindows -logFile build.log
  ```

### 4.6 Manifest

Sinh tự động: 20 hàm `blue_*`/`red_*` + `ally_like_power`, `ally_spawn`, `event_x2`, `event_disaster`, `admin_reset_round`, `admin_reset_score`. Tên hiển thị bắt đầu bằng `XANH:` / `ĐỎ:` để trong dashboard các hàm cùng phe nằm cạnh nhau. Mapping mặc định: comment `xanh`/`blue`/`1` → `blue_join`, `đỏ`/`do`/`red`/`2` → `red_join`. Mọi mapping quà streamer tự cài.

### 4.7 Tách khỏi TikTool về sau

Luật chơi chỉ nhận `GameCommand { Action, Amount, User }` qua `CommandRouter.Handle`. Tỉ số, dữ liệu ngày, cài đặt, camera nằm hết trong game. Khi tách: viết `TikTokDirectSource` (game tự đọc TikTok LIVE) + màn hình cài quà → hàm trong game, thay cho `TikToolSource`.

---

## 5. Hình ảnh, âm thanh: chỉ dùng tài nguyên miễn phí

- **3D low-poly, màu tươi, nét rõ.** Xanh `#2D7DFF`, Đỏ `#FF3B3B`, vàng nhấn `#FFD43B`.
- Độ "đã mắt" đến từ ánh sáng, hạt, rung camera, chứ không phải độ chi tiết của model.

| Loại | Nguồn | Giấy phép |
| --- | --- | --- |
| Nhân vật, xe tăng, robot, môi trường | Kenney.nl, Quaternius, Poly Pizza (chỉ mẫu CC0) | CC0 |
| Hoạt ảnh | Mixamo (tài khoản Adobe miễn phí) | Miễn phí, dùng thương mại được |
| Hiệu ứng | Particle System / VFX Graph tự làm, gói free trên Unity Asset Store | Asset Store EULA |
| Âm thanh, nhạc | Kenney Audio, Freesound (lọc CC0), OpenGameArt (lọc CC0) | CC0 |
| Font | Be Vietnam Pro (Google Fonts) | OFL |
| Icon, UI, logo | ChatGPT / Antigravity tạo, chỉnh trong Photopea | Tự sở hữu |

Mọi asset ghi vào `CREDITS.md`. Không dùng asset trả phí, bản dùng thử, hay nhạc có bản quyền.

**Thay greybox bằng model thật:** kéo prefab vào `Assets/_Game/Resources/GameAssets.asset` (ô `tankPrefab`, `robotPrefab`, `meteorPrefab`, `blueFighterPrefab`, `redFighterPrefab`). Để trống thì game dùng khối hình cơ bản. Prefab nhân vật cần gắn hoạt ảnh nên Claude làm.

---

## 6. Ai làm gì

Nguyên tắc: **mỗi bên sở hữu một vùng file riêng**, không sửa file của bên khác. Cần đổi gì ở vùng khác thì nhắn qua bạn.

### 6.1 Bạn

- **Quyết định**: luật, cân bằng, duyệt hình ảnh, merge PR.
- **Cài quà ↔ hàm trong TikTool** (dashboard → Hiệu ứng).
- **Việc trong Unity Editor**: chỉnh màu, ánh sáng, vị trí, chọn âm thanh. Thử game bằng F5–F9.
- **Chuyển việc**: gửi brief cho ChatGPT / Antigravity, đưa ảnh ChatGPT làm vào đúng thư mục, báo lỗi kèm log hoặc ảnh chụp.
- **Phát live** và ghi lại người xem thích gì.

### 6.2 Claude (Claude Code): lõi game + TikTool

- Vùng file: `Scripts/{Core, Fighters, Effects, Camera, Net, Debug, Editor}`, `GameAssets.asset`, `Scenes/Main.unity`, `manifest.json`, toàn bộ TikTool (`*.js`, `core/`, `test/`).
- Việc tiếp theo: gắn model + hoạt ảnh nhân vật, làm hiệu ứng đẹp hơn (particle, ánh sáng), âm thanh, tối ưu 200 lính, cân bằng, build và test chung với TikTool.
- Review PR của Antigravity, nhất là chỗ dùng `GameEvents`.

### 6.3 Antigravity: giao diện + tìm tài nguyên

- Vùng file: `Scripts/UI/**`, `Assets/_Game/UI/**`, `Assets/_Game/Art/**`, `Assets/_Game/Audio/**`, `CREDITS.md`.
- Việc: toàn bộ UI thật thay cho `DebugHud` (tỉ số, máu lõi, tên, thông báo, banner, menu F10), font tiếng Việt, tìm và nhập tài nguyên CC0.
- Làm trên **clone riêng** `C:\MinhAnh\tiktool-ag`, nhánh `antigravity/<việc>`, mở PR.
- Chi tiết: [BRIEF_ANTIGRAVITY.md](BRIEF_ANTIGRAVITY.md).

### 6.4 ChatGPT: hình ảnh 2D + chữ

- Logo, icon hiệu ứng, khung UI, banner, thumbnail, ảnh concept để làm mẫu khi tìm model 3D, câu hướng dẫn và thông báo, caption TikTok.
- Không viết code cho project.
- Chi tiết: [BRIEF_CHATGPT.md](BRIEF_CHATGPT.md).

### 6.5 Quy trình mỗi việc

1. Bạn giao việc cho đúng bên (theo vùng file ở trên).
2. AI làm trên nhánh riêng, mở PR (ChatGPT thì gửi file ảnh, bạn chép vào `Assets/_Game/UI/...`).
3. Bạn mở Unity, bấm Play, thử bằng F5–F9 hoặc Test Lab của TikTool.
4. Lỗi → gửi log/ảnh cho bên đã làm.
5. Ổn → merge. PR của Antigravity thì nhờ Claude review trước.

---

## 7. Lộ trình

| Giai đoạn | Kết quả | Trạng thái |
| --- | --- | --- |
| **0. Chuẩn bị** | Unity, license, git, project URP trong repo tiktool | Xong |
| **1. Đường ống + luật** | Toàn bộ luật chơi chạy bằng greybox: vào phe, đánh nhau, lõi, thắng ván, tỉ số theo ngày, reset, X2, thảm họa, 7 bậc hiệu ứng, 2 chế độ camera, phím test, kết nối TikTool, TikTool mở được .exe | Code xong |
| **2. Giao diện thật** | UI thay DebugHud, font tiếng Việt, menu F10 | Antigravity |
| **3. Hình ảnh thật** | Model low-poly + hoạt ảnh, xe tăng, robot, sân đấu, hiệu ứng particle, âm thanh | Claude + Antigravity (asset) + ChatGPT (icon) |
| **4. Tối ưu + đóng gói** | 200 lính ≥ 60 FPS khi đang mở OBS, gói vào bộ cài TikTool | Claude |
| **5. Live thử** | 2–3 buổi live nhỏ, chỉnh số liệu | Bạn |

---

## 8. Rủi ro

| Rủi ro | Cách xử lý |
| --- | --- |
| Thư viện đọc TikTok LIVE không chính thức | Game không phụ thuộc trực tiếp; luôn test được bằng F5–F9 |
| Đông lính, lag | Thảm họa theo ngưỡng, giới hạn 200, object pool, rút gọn tên |
| Một phe áp đảo | Khoảnh khắc "chết hết → lâu đài bị đánh" để kêu gọi vào cứu; lính địch đứng sát làm xe tăng/robot mất máu |
| Hai AI sửa đè nhau | Vùng file riêng, clone riêng, PR, hợp đồng `GameEvents` |
| Hai Unity cùng mở một project | Chỉ một Unity Editor/lúc; Antigravity dùng clone riêng |
| Bản quyền | Chỉ CC0/miễn phí rõ ràng, `CREDITS.md`, tên "Siêu Chiến Binh" |
| Chính sách TikTok LIVE | Không hứa thưởng tiền/vật phẩm thật cho người tặng quà |

---

## 9. Việc còn mở

- [ ] UI thật (Antigravity)
- [ ] Model + hoạt ảnh nhân vật, xe tăng, robot
- [ ] Hiệu ứng particle, âm thanh
- [ ] Đo FPS với 200 lính khi đang chạy OBS
- [ ] Đóng gói vào bộ cài TikTool, thử trên máy khác
