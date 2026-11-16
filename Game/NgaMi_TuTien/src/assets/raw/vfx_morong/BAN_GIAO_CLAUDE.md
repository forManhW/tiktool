# Bàn giao cho Claude — ảnh mở rộng Nga Mi

Ngày: 15/11/2026

## Cập nhật tiếp theo sau `ce3a740` — thay đổi chưa commit

- Theo yêu cầu tiếp tục trực tiếp, đã bổ sung hiệu ứng cho sáu món còn lại, đòn của người tẩu hỏa và hiệu ứng hộ kiếp dựa trên `effects` thật. Trứng giữ đòn thường nếu có mục tiêu và hiệu ứng nở theo kết quả thật. Đã thử sáu món trong tỉ thí, hộ kiếp `+100`, tẩu hỏa `-300` và phe đối thủ trống.
- Đã thêm hiệu ứng nền giữa hai phe; giới hạn lớp mới 64 phần tử, 24 mảnh, 4 chuỗi, 2 hiệu ứng nền, 1 cao trào; đòn vượt ngân sách quay về hiệu ứng cũ. Trong thử nghiệm 6 Long Châu đồng thời, 4 chuỗi được nhận, 2 dùng đòn dự phòng, đủ 6 lượt va chạm. Thử 4 trứng nở cùng lúc dừng ở 24 mảnh; sau khi dọn bộ đếm về 0. Khi thoát, các hẹn giờ của lớp mới và đòn dự phòng được hủy.
- Long Châu được đẩy vào trong khung ở mép, giảm vùng sáng và bỏ lóe toàn màn. Ảnh thử hiện tại: `C:/Users/Admin/.codex/visualizations/2026/11/15/01a1debd-bd63-7240-9d52-9f2732dd786a/dragon_flight_2.png`. Tăng viền chữ và kích thước tối thiểu của ngọc; khả năng đọc ở màn điện thoại vẫn cần anh xem.
- Đã thêm bộ trộn tối đa 4 âm và hạ âm khi giọng đọc; các âm mới là mẫu tổng hợp ngắn, có hai biến thể cho mỗi nhóm và không dùng nguồn ghi âm ngoài. Xem `NGUON_AM_THANH.txt`. Cần nghe cân âm trên thiết bị live; tiếng rồng/nước là âm gợi ý tổng hợp, chưa được duyệt chất lượng.
- Anh đã duyệt `DU_THAO_TIEN_DUYEN_DONG_MON.md`; tám câu đã được đưa vào `src/lines.js` và được chọn khi rảnh từ dữ kiện hiện có. Có kiểm tra dữ kiện còn hợp lệ, đổi người và giới hạn nhắc lại cùng người.
- Số liệu offscreen là phép thử sơ bộ, không thay thế đo FPS/CPU/bộ nhớ và chạy dài trên máy live. Bản đóng gói thử mới tại `D:/DuAnCaNhan/TikTool/data/qa-package-ngami-tien-duyen-20261115/win-unpacked/` đã có Tiên duyên; bảy file mã và 20 âm trong `app.asar` khớp byte với workspace. Kiểm thử game 84/84 và hub 21/21. Chưa chạy bản đóng gói trong điều kiện live và chưa phát hành.

## Cập nhật sau commit `ce3a740` và thử nghiệm

Các nhận xét dưới đây được ghi trước các chỉnh sửa mới ở trên; giữ lại để đối chiếu lịch sử.

- Đã dựng bảng so sánh 18 WebP trong `tag_v3` ở bề ngang 96 px trên nền sáng và tối: `C:/Users/Admin/.codex/visualizations/2026/11/15/01a1debd-bd63-7240-9d52-9f2732dd786a/canh_gioi_96px.png`. Đây là ảnh WebP thu nhỏ tĩnh, chưa áp CSS bóng/viền hoặc lớp ngọc trong game. Chữ sáng, nhất là Đại Thừa, Độ Kiếp và Chân Tiên, tương phản yếu trên nền sáng; nhiều dấu và nét mảnh khó phân biệt ở cỡ này. Cần đánh giá lại trong bản thử có đủ CSS trước khi quyết định sửa.
- Đã xem phóng to hai WebP Đại Thừa: `C:/Users/Admin/.codex/visualizations/2026/11/15/01a1debd-bd63-7240-9d52-9f2732dd786a/dai_thua_phong_to.png`. Tại bốn tâm hốc theo `TAG_LAYOUT_V3`, alpha đều gần 0 trong cả hai ảnh, tức tâm hốc có độ trong suốt; tuy nhiên hốc nhỏ và viền khó phân biệt khi thu 96 px. Cần kiểm tra thêm các mức tiểu cảnh với ngọc bật/tắt trong renderer, không kết luận là hốc bị tô kín.
- Đã bổ sung lượt thử renderer phương án A trên sân thường 53 người, tự tạo state để đặt bốn nhân vật Đại Thừa ở Sơ / Trung / Hậu / Đại viên mãn: `C:/Users/Admin/.codex/visualizations/2026/11/15/01a1debd-bd63-7240-9d52-9f2732dd786a/dai_thua_trong_san.png`. DOM có đúng 4 hốc và lần lượt 1 / 2 / 3 / 4 ngọc sáng; ba mức đầu dùng `tag_daiThua`, mức cuối dùng `tag_daiThua_dvm`. Bề ngang bố cục của mỗi bảng hàng thường là 87 px CSS trước khi scale toàn sân. Ở ảnh toàn sân, hốc và ngọc rất khó đếm bằng mắt, nên chưa xác nhận đạt khả năng đọc trên điện thoại. Đây là state mô phỏng, không đo live.
- Git HEAD `ce3a740` đã hoàn tất bước 6: liên kết hành động với kết quả, đòn riêng cho Linh Thạch, Phi Kiếm Trận, Long Châu và hiệu ứng nở Linh Thú Đản; giới hạn riêng 64 phần tử / 4 chuỗi đã có. Các commit `e15be4b`, `9ea7e77`, `fe6b3c1`, `fae4956`, `ee46d1e` là các bước đã triển khai trước đó. Không lặp lại các bước chỉ vì phần bàn giao ban đầu bên dưới mô tả thời điểm cũ.
- Theo báo cáo triển khai, kiểm thử game 76/76 và hub 21/21 đã qua. Bộ đếm hiện có mới là một phần bước 7; các giới hạn khác và kiểm tra hiệu năng còn cần đối chiếu mục 4.3.
- Ở lượt thử trước khi chỉnh, rồng hiện rõ và nền đen không xuất hiện, nhưng hiệu ứng lớn che một phần tên và Long Châu tại người dùng bên trái bị cắt bởi mép sân. Xem bản ảnh mới ở phần cập nhật đầu tệp.
- Thử riêng bằng Electron offscreen, 53 người chia hai phe, phát 40 yêu cầu Linh Thạch / Phi Kiếm / Long Châu cách nhau 250 ms trong khoảng 10 giây: bộ đếm mới đạt tối đa 10 phần tử, 4 chuỗi; sau khi chờ tan trả về 0. Đây chỉ là một kịch bản, chưa đo các giới hạn hạt, nền, cao trào và âm.
- Trong khoảng dồn đó có 248 nhịp `requestAnimationFrame` trong 9,97 giây (~25 nhịp/giây; khoảng cách khung hình p95 83,3 ms). Tiến trình thử tăng working set từ khoảng 1,04 lên 1,54 GiB trong các mẫu và có thông báo GPU `GPU state invalid after WaitForGetOffsetInRange` sau khi thử. Số này từ Electron offscreen, không phải đo FPS/CPU/bộ nhớ trên cửa sổ game và máy live; chưa có bản gốc để so sánh, chưa chứng minh rò bộ nhớ. Cần kiểm tra lại trên máy live và theo dõi lâu hơn.
- Còn chờ: đo live dài và so với bản gốc; hiệu ứng nền tỉ thí, âm thanh, hình riêng kết giới thiên kiếp và người tẩu hỏa; câu Tiên duyên đồng môn và ba điểm bản thử tên trên đầu đang chờ anh duyệt.

## Phạm vi và trạng thái

- Kế hoạch gốc: `D:/DuAnCaNhan/TikTool/Game/KE_HOACH_MO_RONG_HINH_ANH_AM_THANH_NGA_MI.md`. Làm theo mục 12 nếu khác mục 3 hoặc 6; thứ tự triển khai mã, ghép ảnh và âm thanh ở mục 13.
- Anh đã duyệt tiếp sau đợt 2 và đã duyệt đợt 3. Có đủ 18 ảnh mẫu cảnh giới trong `src/assets/raw/tag_mau/` và 12 ảnh hiệu ứng trong thư mục hiện tại. Việc duyệt không xóa các điểm chưa đạt được báo bên dưới.
- Claude đảm nhiệm phần mã, ghép ảnh, âm thanh và kiểm thử theo mục 13. Xem cập nhật commit và kết quả thử ở đầu tệp để biết phần đã làm và phần còn lại.
- Điểm khôi phục `e15be4b` đã được tạo. Workspace vẫn có một số tệp chưa theo dõi và thay đổi khác; kiểm tra nội dung, bảo toàn công việc hiện có, không tự xóa hoặc ghi đè.

## Ảnh hiệu ứng

| File | Nền gốc | Kích thước thực tế |
|---|---|---|
| `vfx_kiem_quang.png` | Đen | 1536 × 1024 |
| `vfx_va_cham.png` | Đen | 1254 × 1254 |
| `vfx_manh_linh_thach.png` | Alpha | 1254 × 1254 |
| `vfx_long_khi.png` | Đen | 1536 × 1024 |
| `vfx_trung_no.png` | Alpha | 1254 × 1254 |
| `vfx_song_linh_khi.png` | Đen | 1254 × 1254 |
| `vfx_hoa_tien.png` | Đen | 1536 × 1024 |
| `vfx_canh_dao.png` | Alpha | 1254 × 1254 |
| `vfx_linh_chi.png` | Alpha | 1536 × 1024 |
| `vfx_ngoc_dich.png` | Đen | 1536 × 1024 |
| `vfx_phu_van.png` | Đen | 1254 × 1254 |
| `vfx_khoi_bui.png` | Đen | 1254 × 1254 |

Các ảnh vuông nêu trên chưa đúng kích thước 1024 × 1024 trong kế hoạch. Loại nền cũng đã ghi trong `GHI_CHU.txt`; tệp này còn có các dòng ghi chú cho `tag_mau_*` và văn bản tự do, vì vậy công cụ đọc ghi chú cần xử lý phù hợp, không giả định mọi dòng là ảnh VFX trong cùng thư mục.

## Những điểm phải kiểm tra trước khi ghép

- Ảnh nền đen có các pixel nền lệch 1–2 mức khỏi `#000000`. Mục 13 yêu cầu công cụ riêng đổi độ sáng thành alpha và xuất WebP, không sửa `tools/build-ban3.py`. Kiểm tra trên nền sáng lẫn tối: nền không còn đen, không còn quầng; vùng đỏ hoặc xanh sẫm không bị mất quá nhiều khi chuyển alpha.
- Bốn ảnh alpha có điểm mờ ngoài hình hoặc hình chưa hoàn toàn đặc. `vfx_linh_chi.png` còn quầng mờ rộng; lá nhỏ khó nhận ở bản xem 120 px. Cần xem trên nền sáng và tối trước khi dùng.
- `vfx_manh_linh_thach.png`, `vfx_canh_dao.png`, `vfx_trung_no.png`: các mảnh chính rời nhau để cắt, nhưng pixel alpha mờ có thể gây nhiễu khi tách. Không nhầm pixel rời với một mảnh mới.
- Khoảng cách tới mép chưa đạt 6% ở `vfx_trung_no.png`, `vfx_hoa_tien.png`, `vfx_canh_dao.png`, `vfx_ngoc_dich.png` và `vfx_phu_van.png`. Các nét phù văn giống lửa hơn nét bút; `vfx_khoi_bui.png` có vùng xám tối.
- Trước khi đưa bộ cảnh giới vào game, kiểm tra chữ và dấu ở bề ngang 96 px; một số còn khó đọc. Hốc ngọc của cả hai mẫu Đại Thừa chưa trống rõ.

## Tiếp tục theo kế hoạch

Đọc toàn bộ mục 13 để giữ đúng thứ tự, nhất là phần giọng đọc và tỉ thí trước khi ghép ảnh; sau mỗi bước chạy `node --test test/*.test.js` trong `Game/NgaMi_TuTien/` và `npm test` tại gốc TikTool. Nội dung mới cho tiên duyên đồng môn cần anh duyệt theo mục 13. Âm thanh cần nguồn cho phép dùng thương mại và ghi nguồn theo kế hoạch.
