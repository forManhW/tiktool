# Giọng đọc AI (chạy trên máy, không cần mạng)

## Cách hoạt động

- Hub TikTool có `POST /api/tts { "text": "..." }`, trả về `audio/wav`, và `GET /api/tts/status`.
- Phía sau là **Piper**: một tiến trình chạy liên tục, nạp model giọng 1 lần (khoảng 1–2 giây), sau đó mỗi câu mất khoảng 0,5–1,3 giây. Câu đã đọc được lưu đệm, đọc lại câu giống hệt thì có ngay. Code ở `core/tts.js`.
- Game (ví dụ Nga Mi) tạo trước câu ngay khi câu vào hàng chờ đọc, nên tới lượt đọc là có tiếng luôn. Hub không có giọng AI thì game dùng giọng tiếng Việt của Windows (nếu máy có cài).
- File cần có: `vendor/tts/piper/piper.exe` (+ dll, `espeak-ng-data`) và `vendor/tts/voices/<giọng>.onnx` + `.onnx.json`. Lấy bằng `npm run build:tts`. Bản cài chép vào `resources/tts`.
- Đổi giọng: bỏ file `.onnx` + `.onnx.json` khác vào `voices/` (hub lấy file đầu tiên theo tên), hoặc đặt biến môi trường `TIKTOOL_TTS_VOICE=<tên file>`.

## Quyết định hiện tại (10/11/2026)

Anh chốt: **giữ giọng `vi_VN-vais1000-medium`, chỉ dùng cá nhân.** Bộ cài build từ máy này có kèm giọng, nên **không phát hoặc bán bộ cài đó cho người khác** khi chưa đổi giọng (xem bảng dưới).

## ⚠ Giấy phép: cần người quyết trước khi phát bộ cài

| Thành phần | Giấy phép | Dùng trong app bán / phát cho người khác |
|---|---|---|
| Piper (`piper.exe`) | MIT | Được, giữ thông báo bản quyền |
| espeak-ng (đi kèm Piper) | GPL-3 | Được phát kèm như chương trình riêng, nhưng phải kèm giấy phép GPL-3 và chỉ chỗ lấy mã nguồn |
| Giọng `vi_VN-vais1000-medium` (đang dùng) | Dữ liệu tiếng Việt CC BY 4.0, nhưng model **huấn luyện tiếp từ giọng "lessac"**; giấy phép lessac (Blizzard 2013) **cấm mọi mục đích thương mại**, kể cả model làm từ nó | **Không an toàn** cho app thương mại |
| `vi_VN-vivos-x_low` | CC BY-NC-SA 4.0 | **Không** (cấm thương mại) |
| `vi_VN-25hours_single-low` | Không rõ | Không rõ |

Kiểm tra ngày 10/11/2026 từ MODEL_CARD trên huggingface.co/rhasspy/piper-voices và trang giấy phép lessac. Tôi không phải luật sư: anh cần tự đọc lại hoặc hỏi người có chuyên môn.

Lựa chọn trước khi phát app có thu tiền:
1. Huấn luyện hoặc mua một giọng Piper tiếng Việt có giấy phép thương mại rõ ràng (huấn luyện từ dữ liệu CC BY / tự thu âm, không dựa trên lessac). Chỉ cần thay file trong `voices/`.
2. Dùng dịch vụ TTS trả phí (FPT.AI, Viettel AI, Google, Azure…) qua cùng endpoint `/api/tts`.
3. Không gói giọng trong bộ cài. Người dùng tự cài giọng tiếng Việt của Windows; game tự dùng giọng đó.
