# Brief cho Antigravity: Giao diện (UI) + tài nguyên miễn phí

> Gửi nguyên file này cho Antigravity. Đọc kèm `Game/DaiChienXanhDo/KE_HOACH.md` (luật chơi đầy đủ).

## 1. Bối cảnh

- Game **Đại Chiến Xanh Đỏ**: game 3D Unity cho TikTok LIVE. Hai phe Xanh/Đỏ đấm nhau liên tục. Người xem vào trận bằng quà/tim/comment, mỗi người có một nhân vật mang tên mình. Lõi một phe vỡ thì phe kia +1 điểm, sân tự reset.
- Màn hình game **vuông 1080x1080**. Lõi Xanh ở góc dưới trái, Lõi Đỏ ở góc trên phải. **Tỉ số ở góc trên bên trái.**
- Repo: `https://github.com/forManhW/tiktool` (repo của TikTool, game nằm trong `Game/DaiChienXanhDo/`).
- Unity **6000.3.29f1** (Unity 6.3 LTS), URP 17.3, Input System 1.20.1 (**không** dùng Input Manager cũ), UGUI 2.0 + TextMeshPro, Newtonsoft JSON.
- Phần lõi (luật chơi, lính, hiệu ứng, camera, kết nối TikTool) **Claude đã làm xong** và đang chạy với một giao diện tạm bằng IMGUI (`Scripts/Debug/DebugHud.cs`). **Việc của bạn là làm giao diện thật để thay nó.**

## 2. Cài đặt môi trường (làm một lần)

Dùng **clone riêng** để không đụng thư mục Claude đang làm (một project Unity chỉ mở được bằng một Editor):

```bash
git clone https://github.com/forManhW/tiktool.git C:\MinhAnh\tiktool-ag
cd C:\MinhAnh\tiktool-ag
git checkout -b antigravity/ui
```

Unity Hub → **Add** → **Add project from disk** → `C:\MinhAnh\tiktool-ag\Game\DaiChienXanhDo\UnityProject`. Mở scene `Assets/_Game/Scenes/Main.unity`, bấm **Play**.

Thử bằng phím (không cần TikTool):

| Phím | Việc |
| --- | --- |
| F5 / F6 | Người mới vào phe Xanh / Đỏ |
| F7 / F8 | Quà phe Xanh / Đỏ, lần lượt bậc 1 → 7 (Shift = Robot ngay) |
| F9 | 30 người vào cùng lúc (thử tải) |
| F1 | Đổi chế độ Live tự động ↔ Thường |
| F2 | Ẩn/hiện giao diện |
| F10 | Mở menu cài đặt (bạn sẽ làm) |
| Ctrl+R / Ctrl+Shift+R | Reset ván / reset tỉ số |

## 3. Vùng file của bạn (QUAN TRỌNG)

**Được tạo/sửa:**

- `Assets/_Game/Scripts/UI/**` (code UI, namespace `DaiChienXanhDo.UI`)
- `Assets/_Game/UI/**` (prefab UI, sprite, font, icon)
- `Assets/_Game/Resources/UI/**` (prefab UI cần nạp lúc chạy)
- `Assets/_Game/Art/**`, `Assets/_Game/Audio/**` (tài nguyên tải về)
- `Game/DaiChienXanhDo/CREDITS.md`

**Không được sửa** (thuộc Claude): `Scripts/Core`, `Scripts/Fighters`, `Scripts/Effects`, `Scripts/Camera`, `Scripts/Net`, `Scripts/Debug`, `Scripts/Editor`, `Scenes/Main.unity`, `Resources/GameAssets.asset`, `Materials/`, `Settings/`, `manifest.json`, mọi file ngoài `Game/DaiChienXanhDo/`.

**Không sửa scene `Main.unity`.** UI phải **tự gắn vào lúc chạy**:

```csharp
namespace DaiChienXanhDo.UI
{
    public static class UIBootstrap
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var prefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("UI/UIRoot");
            if (prefab == null) return;
            UnityEngine.Object.Instantiate(prefab);
            var debugHud = UnityEngine.Object.FindFirstObjectByType<DaiChienXanhDo.DebugHud>();
            if (debugHud != null) debugHud.enabled = false;   // tắt giao diện tạm
        }
    }
}
```

Cần đổi gì trong vùng của Claude (thêm event, thêm dữ liệu): **ghi vào mô tả PR** mục "Cần Claude làm", đừng tự sửa.

## 4. Hợp đồng dữ liệu (chỉ ĐỌC, không đổi luật chơi)

**Event** (`DaiChienXanhDo.GameEvents`, nhớ hủy đăng ký trong `OnDisable`):

| Event | Tham số | Dùng cho |
| --- | --- | --- |
| `ScoreChanged` | `(int blue, int red)` | Tỉ số góc trên trái |
| `CoreHealthChanged` | `(Team, float hp, float max)` | Thanh máu lõi |
| `PlayerJoined` | `Viewer` | Thông báo người mới vào |
| `EffectStarted` | `EffectNotice { Team, EffectId, DisplayName, Tier 0..7, Amount, By (Viewer), Position, Target }` | Banner quà, icon hiệu ứng |
| `RoundWon` | `(Team winner, Viewer mvp)` (mvp có thể null) | Banner thắng ván + MVP |
| `RoundReset` | | Dọn banner |
| `ModeChanged` | `GameMode` (LiveAuto / Manual) | Ẩn/hiện thông báo |
| `HudVisibilityChanged` | `bool` | F2 ẩn toàn bộ UI |
| `ConnectionStatusChanged` | `string` (demo/connecting/connected/reconnecting/unauthorized/duplicate_game) | Chấm trạng thái nhỏ |
| `MatchEventStarted` / `MatchEventEnded` | `MatchEventNotice { Id: x2 / x2_weak / disaster_warning / disaster, DisplayName, Duration, HasTeam, Team }` | Banner sự kiện + đếm ngược |
| `FighterCountChanged` | `(int blue, int red)` | Số lính mỗi phe (tùy chọn) |
| `Notice` | `(string text, NoticeKind Info/Join/Gift/Warning/Guide)` | Dòng thông báo |
| `SettingsMenuRequested` | | Mở menu F10 (đăng ký event này thì F10 thôi mở file) |

**Đọc trạng thái hiện tại:**

| Cần | Gọi |
| --- | --- |
| Chế độ, ẩn HUD, kết nối | `GameState.Mode`, `GameState.HudVisible`, `GameState.ConnectionStatus` |
| Tỉ số | `MatchManager.I.ScoreBlue`, `MatchManager.I.ScoreRed` |
| Lõi | `MatchManager.I.Core(Team.Blue)` → `.Hp`, `.MaxHp`, `.Ratio`, `.transform` |
| Lính trên sân | `FighterManager.I.List(Team.Blue)` → mỗi `Fighter`: `.Owner.Nickname`, `.IsMain`, `.IsSuper`, `.IsAlive`, `.transform` |
| Top người chơi trong ván | `PlayerRegistry.Top(Team.Blue, 3)` → `Viewer.Nickname`, `.RoundPoints` |
| Top hôm nay | `DailyStore.Top(Team.Blue, 5)` → `DonorEntry.nickname`, `.points` |
| Camera đang theo ai | `CameraRig.I.Following` (Transform), camera: `CameraRig.I.cam` |
| Cài đặt | `GameSettings.Current` (sửa field rồi gọi `GameSettings.Save()`) |
| Màu phe | `TeamUtil.BlueColor`, `TeamUtil.RedColor`, `team.DisplayName()` ("XANH"/"ĐỎ") |

## 5. Việc cần làm (theo thứ tự)

### UI-1. Font tiếng Việt
- Tải **Be Vietnam Pro** (Google Fonts, giấy phép OFL), bản Bold + ExtraBold, đặt ở `Assets/_Game/UI/Fonts/`.
- Window → TextMeshPro → **Import TMP Essential Resources**.
- Tạo TMP Font Asset chế độ **Dynamic** (để hiện được mọi tên người xem có dấu). Ký tự không có trong font (emoji…) thì bỏ qua, không hiện ô vuông.

### UI-2. Khung UI gốc
- Prefab `Assets/_Game/Resources/UI/UIRoot.prefab`: Canvas **Screen Space - Camera** (camera = `CameraRig.I.cam`, gán lúc chạy), Canvas Scaler **Scale With Screen Size 1080x1080**, match 0.5. Như vậy UI nằm gọn trong khung vuông kể cả khi cửa sổ bị kéo méo.
- EventSystem dùng **InputSystemUIInputModule** (không dùng StandaloneInputModule).
- `UIBootstrap` như mục 3.

### UI-3. Luôn hiện (cả 2 chế độ)
1. **Tỉ số góc trên trái**: `XANH 3 : 5 ĐỎ`, số to, màu phe, nhún nhẹ khi đổi.
2. **Thanh máu 2 lõi**: đặt ngay trên mỗi lõi (đổi vị trí thế giới → màn hình mỗi frame), có số máu, nháy khi bị đánh, rung khi dưới 20%.
3. **Tên trên đầu nhân vật**:
   - Pool sẵn label (tối đa 200), **không tạo/xóa object mỗi frame**, không cấp phát bộ nhớ mỗi frame.
   - Nhân vật chính: chữ trắng to. Lính phụ (`IsMain == false`): chữ nhỏ, mờ hơn.
   - Sân hơn `GameSettings.Current.fullNameLimit` (40) lính: ẩn tên lính phụ. Nhân vật chính chỉ hiện tên đầy đủ nếu là người camera đang theo, top 3 mỗi phe, hoặc vừa hành động trong 5 giây (`Owner.LastActionTime`). Còn lại hiện chữ viết tắt ("Nguyễn An" → "NA").
   - `IsSuper` thì label cao hơn (nhân vật to và bay).
4. **Chấm trạng thái nhỏ góc dưới phải**: chế độ + trạng thái kết nối. Mờ, không gây chú ý.

### UI-4. Chỉ hiện ở chế độ **Live tự động** (ẩn ngay khi `GameState.Mode == Manual`)
1. **Dải hướng dẫn** ở đáy khung: chạy lần lượt các dòng trong `GameSettings.Current.guideLines`, mỗi dòng 6 giây, có hiệu ứng chuyển.
2. **Dòng thông báo** (`Notice`): tối đa 4 dòng, mỗi dòng 4 giây rồi mờ dần, màu theo `NoticeKind`.
3. **Banner quà lớn** (`EffectStarted` với `Tier >= 4`): tên người tặng + tên hiệu ứng + icon, màu phe, 2,5 giây, xếp hàng nếu nhiều.
4. **Banner sự kiện**: X2 (vàng), X2 phe yếu (màu phe), cảnh báo thảm họa (đỏ nhấp nháy), có đếm ngược theo `Duration`.
5. **Banner thắng ván** (`RoundWon`): "PHE XANH THẮNG!" + "MVP: <tên>", hiện trong `GameSettings.Current.celebrateSeconds` giây.

### UI-5. Menu cài đặt F10
- Đăng ký `GameEvents.SettingsMenuRequested` để bật/tắt panel.
- Sửa được: `coreMaxHp`, `hardCap`, `disasterThreshold`, `disasterKillPercent`, `x2IntervalMin`, `x2IntervalMax`, `x2Duration`, `weakX2Threshold`, `fullNameLimit`, `windowSize` (720/1080/1440), `guideLines` (danh sách, thêm/xóa dòng), chế độ (Live tự động / Thường → `GameState.SetMode`).
- Nút **Lưu** → `GameSettings.Save()`. Ghi chú trên panel: "Máu lõi áp dụng từ ván sau, kích thước cửa sổ áp dụng khi mở lại game".

### UI-6. Icon hiệu ứng
- ChatGPT sẽ làm icon (người dùng chép vào `Assets/_Game/UI/Icons/`): `icon_join`, `icon_spawn`, `icon_like_power`, `icon_punch`, `icon_combo_punch`, `icon_superhero`, `icon_lightning`, `icon_tank`, `icon_meteor`, `icon_giant_robot` (PNG 512x512, nền trong suốt).
- Banner quà tìm icon theo `EffectId`. Chưa có icon thì banner vẫn chạy, chỉ hiện chữ.

### A-1. Tìm tài nguyên miễn phí (làm song song)
Chỉ lấy **CC0** (hoặc Mixamo / gói **free** trên Unity Asset Store). Không lấy bản trả phí, dùng thử, hay "free for personal use".

| Cần | Gợi ý tìm | Đặt vào |
| --- | --- | --- |
| Nhân vật người low-poly có rig Humanoid (để gắn hoạt ảnh Mixamo) | Quaternius, Kenney (Character packs) | `Art/Characters/<Gói>/` |
| Xe tăng low-poly | Kenney, Quaternius, Poly Pizza (CC0) | `Art/Vehicles/<Gói>/` |
| Robot / mech lớn | Quaternius (Mech/Robot), Poly Pizza (CC0) | `Art/Robot/<Gói>/` |
| Đá, thiên thạch, cây, hàng rào, đồ trang trí sân | Kenney, Quaternius | `Art/Environment/<Gói>/` |
| Âm thanh: đấm, sấm, nổ, xe tăng, robot giẫm, kèn thắng, UI click, cảnh báo | Kenney Audio, Freesound (lọc CC0), OpenGameArt (lọc CC0) | `Audio/SFX/` |
| Nhạc nền chiến đấu lặp được | OpenGameArt (CC0) | `Audio/Music/` |

- Mỗi gói ghi một dòng vào `Game/DaiChienXanhDo/CREDITS.md`: tên, tác giả, link, giấy phép, ngày tải.
- **Chỉ nhập (import), không gắn vào game.** Không sửa `GameAssets.asset` và prefab nhân vật. Claude sẽ gắn model, hoạt ảnh và âm thanh vào luật chơi. Trong PR liệt kê: file nào là nhân vật, xe tăng, robot…

## 6. Tiêu chí xong

- Play scene Main: không có lỗi đỏ trong Console. DebugHud tự tắt, UI mới hiện.
- F9 bấm 7 lần (khoảng 200 lính): vẫn ≥ 60 FPS trên RTX 3060, tên không bị chồng thành đống.
- F1 sang Thường: mọi thông báo, banner, dải hướng dẫn biến mất ngay. F1 lại thì hiện lại.
- F2 ẩn/hiện toàn bộ UI.
- Kéo cửa sổ Game view méo (16:9): UI vẫn nằm trong khung vuông.
- F10: sửa `guideLines`, Lưu, thấy dải hướng dẫn đổi theo.
- Tên có dấu hiện đúng ("Nguyễn Thảo Uyên").

## 7. Cách nộp

- Commit cả file `.meta` đi kèm asset. Không commit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (đã có `.gitignore`).
- Mỗi việc lớn (UI-1..6, A-1) một commit rõ ràng. Đẩy nhánh và mở PR:
  ```bash
  git push -u origin antigravity/ui
  ```
  ```bash
  gh pr create --base main --title "UI thật cho Đại Chiến Xanh Đỏ" --body "..."
  ```
- Mô tả PR: đã làm gì, ảnh chụp màn hình (Live tự động + Thường), mục **"Cần Claude làm"** nếu có.
