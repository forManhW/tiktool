# Ghi chú cho AI

Repo TikTool (Node.js + Electron) chứa game Unity **Đại Chiến Xanh Đỏ** ở `Game/DaiChienXanhDo/`.

Trước khi làm gì với game, đọc:
1. `Game/DaiChienXanhDo/HUONG_DAN_CAI_DAT.md`: cài đặt máy mới, lỗi đã gặp, build, test tự động, quy ước.
2. `Game/DaiChienXanhDo/KE_HOACH.md`: luật chơi, kiến trúc, phân việc.

Quy ước bắt buộc:
- Commit đứng tên `forManhW`, không thêm `Co-Authored-By`, không ghi tên AI/công cụ.
- Trả lời chủ dự án bằng tiếng Việt; báo kết quả test bằng chữ, không gửi ảnh trừ khi được hỏi.
- AI phụ làm trên nhánh riêng, không push thẳng `main`; chủ dự án duyệt mới merge.
- Sửa xong: build game + `node Game/DaiChienXanhDo/tools/test-scenario.js . test-out` + `npm test`.
