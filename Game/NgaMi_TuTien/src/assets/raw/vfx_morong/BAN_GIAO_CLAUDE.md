# Bàn giao cho Claude — ảnh mở rộng Nga Mi

Ngày: 15/11/2026

## Phạm vi và trạng thái

- Kế hoạch gốc: `D:/DuAnCaNhan/TikTool/Game/KE_HOACH_MO_RONG_HINH_ANH_AM_THANH_NGA_MI.md`. Làm theo mục 12 nếu khác mục 3 hoặc 6; thứ tự triển khai mã, ghép ảnh và âm thanh ở mục 13.
- Anh đã duyệt tiếp sau đợt 2 và đã duyệt đợt 3. Có đủ 18 ảnh mẫu cảnh giới trong `src/assets/raw/tag_mau/` và 12 ảnh hiệu ứng trong thư mục hiện tại. Việc duyệt không xóa các điểm chưa đạt được báo bên dưới.
- Claude đảm nhiệm phần mã, ghép ảnh, âm thanh và kiểm thử theo mục 13. Bản bàn giao này không xác nhận phần đó đã được thực hiện.
- Workspace có nhiều thay đổi và tệp chưa theo dõi từ trước. Mục 13 yêu cầu tạo điểm khôi phục trước; kiểm tra nội dung, bảo toàn thay đổi hiện có và thực hiện theo kế hoạch, không tự xóa hoặc ghi đè công việc khác.

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
