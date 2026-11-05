# TikTool – Individual assignment 3

## Bài tập

Giải thích hoạch định nguồn lực và ước tính chi phí, kèm ví dụ tương ứng. Trình bày bằng slide trong 5 phút. Đề ghi thời gian chuẩn bị 20 phút.

## Giả định và phạm vi

Giả định học tập: phát triển MVP tương đương phạm vi minh họa từ đầu với hai demo Clock và Tháp PK. Đây không phải dự toán phần việc còn lại, báo giá thị trường hay chi phí thực tế đã phát sinh. Một người có sẵn kỹ năng JavaScript, Python cơ bản và WebSocket, sẵn laptop, Internet và tài khoản có thể dùng để kiểm thử LIVE. Công suất 20 giờ/tuần, đơn giá quy đổi 150.000 đồng/giờ. Tổng effort 80 giờ, lịch cơ sở 4 tuần. Không gồm mua máy mới, bảo trì sau MVP, phát hành thương mại, thuế hoặc lợi nhuận nhà cung cấp.

Phạm vi ví dụ: ứng dụng desktop chạy local, quản lý một phiên LIVE và game, mapping có cooldown, hai demo Clock và Tháp PK, overlay OBS, Test Lab/Event Injector và nhật ký. Kho mã có thêm các game khác nhưng bài chỉ dùng hai demo này.

Chi phí nhân lực là giá trị quy đổi thời gian. Nếu tự làm và không nhận lương, tiền mặt thực chi sẽ khác. Điện/mạng 200.000 đồng là phần phân bổ cho giai đoạn MVP, test LIVE 300.000 đồng là hạn mức giả định. Không sử dụng bảng giá coin, quà tặng hoặc giá dịch vụ thị trường.

## Phân bổ thời gian trình bày

| Slide | Nội dung | Mốc thời gian | Thời lượng |
| --- | --- | --- | --- |
| 1 | TikTool: Hoạch định nguồn lực và ước tính chi phí | 0:00–0:15 | 15 giây |
| 2 | Phạm vi ví dụ TikTool | 0:15–0:45 | 30 giây |
| 3 | Hoạch định nguồn lực | 0:45–1:25 | 40 giây |
| 4 | Kế hoạch 80 giờ trong 4 tuần | 1:25–2:10 | 45 giây |
| 5 | Ước tính chi phí | 2:10–2:55 | 45 giây |
| 6 | Chi phí nhân lực theo công việc | 2:55–3:35 | 40 giây |
| 7 | Ngân sách dự kiến 14.375.000 đồng | 3:35–4:15 | 40 giây |
| 8 | Rủi ro và điều chỉnh kế hoạch | 4:15–5:00 | 45 giây |

Các mốc là mục tiêu khi luyện nói, tổng 300 giây. Lời nói bên dưới tương ứng với speaker notes trong PowerPoint.

## Slide 1. TikTool: Hoạch định nguồn lực và ước tính chi phí

Em xin trình bày việc hoạch định nguồn lực và ước tính chi phí qua dự án TikTool. Bài sử dụng một kịch bản MVP để cho thấy cách chuyển phạm vi công việc thành lịch thực hiện và ngân sách.

## Slide 2. Phạm vi ví dụ TikTool

TikTool là ứng dụng desktop chạy trên máy người livestream. Hệ thống nhận sự kiện LIVE, áp dụng mapping và gửi lệnh cho game. Ví dụ, quà Rose có thể thêm thời gian vào Clock hoặc thêm khối vào Tháp PK. Phạm vi bài gồm hai demo, overlay OBS và công cụ kiểm thử. Đây là kịch bản xây dựng MVP từ đầu, không phải chi phí đã phát sinh hay phần việc còn lại của dự án.

## Slide 3. Hoạch định nguồn lực

Hoạch định nguồn lực là xác định cần ai, cần công cụ gì, bao nhiêu và dùng vào lúc nào. Ta bắt đầu từ phạm vi, chia nhỏ công việc, xác định kỹ năng và kiểm tra lịch khả dụng. Trong ví dụ, một người đảm nhiệm các vai trò nhưng chỉ làm 20 giờ mỗi tuần, nên các vai trò không tương đương nhiều nhân viên. Thiết bị có sẵn gồm laptop và Internet. Công nghệ theo mã nguồn hiện tại gồm Electron, Node.js, adapter Python, WebSocket và SQLite. Đầu ra là danh sách nguồn lực và lịch phân bổ.

## Slide 4. Kế hoạch 80 giờ trong 4 tuần

Kế hoạch chia MVP thành các gói công việc. Tuần một dành 8 giờ phân tích và 12 giờ giao diện. Tuần hai làm 20 giờ cho Hub, listener và rule engine. Tuần ba hoàn thành 4 giờ Hub còn lại rồi tích hợp game và OBS trong 16 giờ. Tuần bốn dành 16 giờ kiểm thử tích hợp và sửa lỗi, cùng 4 giờ tài liệu. Các kiểm tra nhỏ diễn ra ngay khi lập trình. Tổng effort là 80 giờ. Vì chỉ có 20 giờ khả dụng mỗi tuần, thời gian lịch là 4 tuần. Lịch này chưa có dự phòng tiến độ.

## Slide 5. Ước tính chi phí

Ước tính chi phí dự báo số tiền cần để thực hiện phạm vi công việc. Bài dùng bottom-up, nghĩa là tính chi phí từng gói rồi cộng lại. Ví dụ, game và OBS cần 16 giờ. Nhân với đơn giá giả định 150 nghìn đồng một giờ, ta được 2,4 triệu đồng. Với gói Hub có độ bất định cao hơn, em minh họa PERT bằng ba mức 16, 24 và 32 giờ. Công thức lấy mức thường gặp nhân bốn rồi chia tổng cho sáu, cho kết quả 24 giờ. 24 giờ này đã có trong kế hoạch, không cộng thêm lần nữa.

## Slide 6. Chi phí nhân lực theo công việc

Bảng này áp dụng cùng đơn giá giả định cho cả sáu gói công việc. Tổng 80 giờ nhân 150 nghìn đồng bằng 12 triệu đồng. Gói Hub là khoản lớn nhất với 3,6 triệu đồng do phải xử lý kết nối, mapping và điều phối lệnh. Kiểm thử vẫn có ngân sách riêng để kiểm tra combo quà, cooldown và đồng bộ overlay. Một người có thể tự làm mà không trả lương trực tiếp, nhưng 12 triệu vẫn phản ánh giá trị kinh tế của thời gian. Cần phân biệt giá trị này với tiền mặt thực chi.

## Slide 7. Ngân sách dự kiến 14.375.000 đồng

Ngoài nhân lực, bài giả định 300 nghìn đồng kiểm thử LIVE và 200 nghìn đồng điện, Internet. Laptop có sẵn và ứng dụng chạy local nên không phát sinh mua máy hay thuê máy chủ trong kịch bản này. Chi phí cơ sở là 12,5 triệu đồng. Dự phòng 15% tương đương 1,875 triệu, đưa ngân sách lên 14,375 triệu đồng. Tỷ lệ dự phòng là giả định minh họa, cần điều chỉnh khi có dữ liệu rủi ro thực tế. Ngân sách chưa bao gồm bảo trì sau MVP hoặc phát hành thương mại.

## Slide 8. Rủi ro và điều chỉnh kế hoạch

Rủi ro chính là kết nối LIVE và xử lý combo quà khiến gói Hub cần thêm thời gian. Giả sử phát sinh 8 giờ, chi phí tăng 1,2 triệu đồng, vẫn nằm trong khoản dự phòng 1,875 triệu. Nhưng ngân sách dự phòng không tạo thêm thời gian làm việc: với 20 giờ một tuần, 88 giờ sẽ cần 4,4 tuần. Cách kiểm soát là ghi giờ và chi phí thực tế hàng tuần, dùng Event Injector khi LIVE gián đoạn, rồi ước tính lại nếu mở rộng phạm vi. Kết luận, hoạch định nguồn lực tạo lịch khả thi, còn ước tính chi phí tạo cơ sở cho ngân sách. Ví dụ TikTool có một người, 80 giờ và ngân sách 14,375 triệu đồng.

## Bảng tính cơ sở

Đơn giá giả định: 150.000 đồng/giờ cho mọi vai trò của cùng một người.

| Gói công việc | Giờ | Chi phí (đồng) |
| --- | ---: | ---: |
| Phân tích yêu cầu và phạm vi | 8 | 1.200.000 |
| Thiết kế và xây dựng giao diện | 12 | 1.800.000 |
| Hub, listener và rule engine | 24 | 3.600.000 |
| Tích hợp hai game và OBS | 16 | 2.400.000 |
| Kiểm thử tích hợp và sửa lỗi | 16 | 2.400.000 |
| Tài liệu và bàn giao | 4 | 600.000 |
| **Tổng nhân lực** | **80** | **12.000.000** |

Chi phí cơ sở = 12.000.000 + 300.000 + 200.000 = 12.500.000 đồng.

Dự phòng = 12.500.000 × 15% = 1.875.000 đồng.

**Ngân sách có dự phòng = 14.375.000 đồng.**

PERT cho Hub: (16 + 4 × 24 + 32) ÷ 6 = 24 giờ. Đây là gói đã có trong 80 giờ. Không cộng lặp.

Nếu Hub cần thêm 8 giờ: chi phí tăng 1.200.000 đồng, chi phí cơ sở mới 13.700.000 đồng và khoản dự phòng còn 675.000 đồng, với giả định các khoản khác giữ nguyên. Thời gian lịch = 88 ÷ 20 = 4,4 tuần. Tiền dự phòng không thay thế dự phòng tiến độ.

## Câu hỏi có thể gặp

**Vì sao dùng 150.000 đồng/giờ?** Đây là đơn giá quy đổi giả định để minh họa phép tính. Dự toán thực tế cần đơn giá theo năng lực, hình thức thuê và dữ liệu phù hợp.

**Vì sao chỉ một người mà có nhiều vai trò?** Người làm dự án lần lượt đảm nhiệm các vai trò. Các vai trò không phải năm nhân viên. Lịch tuần không vượt 20 giờ.

**Vì sao ứng dụng miễn phí vẫn có chi phí?** Thời gian của người phát triển và các khoản vận hành có giá trị kinh tế. Không thuê server trong ví dụ local không có nghĩa toàn dự án không tốn chi phí.

**Vì sao dùng dự phòng 15%?** Đây là giả định phục vụ bài tập, không phải tỷ lệ bắt buộc từ PMI. Dự toán thật cần định lượng rủi ro và điều chỉnh theo dữ liệu.

**80 giờ là thời gian đã làm hay cần làm tiếp?** Cả hai đều không. Đó là kịch bản minh họa phát triển một MVP tương đương phạm vi đã chọn từ đầu. Không có timesheet thực tế để kết luận chi phí đã chi hoặc effort còn lại.

## Tài liệu tham khảo

- [PMI – Leveraging the new practice standard for project estimating (Kanabar & Warburton, 2011)](https://www.pmi.org/learning/library/leveraging-new-practice-standard-project-estimating-6222): lịch và kỹ năng nguồn lực, bottom-up, PERT, cơ sở và cập nhật ước tính.
- [PMI – Estimation is not an event, it's a process! (2005)](https://www.pmi.org/learning/library/estimation-event-process-7460): chia chi phí thành các thành phần và tổng hợp bottom-up.
- [README dự án](D:/DuAnCaNhan/TikTool/README.md), [package.json](D:/DuAnCaNhan/TikTool/package.json), [server.js](D:/DuAnCaNhan/TikTool/server.js), [live_adapter.py](D:/DuAnCaNhan/TikTool/live_adapter.py), [requirements-live.txt](D:/DuAnCaNhan/TikTool/requirements-live.txt), [Clock manifest](D:/DuAnCaNhan/TikTool/Game/Clock/manifest.json), [Tháp PK manifest](D:/DuAnCaNhan/TikTool/Game/TowerPK/manifest.json).

Phạm vi và công nghệ dựa vào tài liệu cùng mã nguồn trong workspace tại ngày 09/10/2026. Mã hiện tại sử dụng adapter Python TikTokLive. README còn nhắc connector Node từ triển khai trước, nên bài theo mã đang chạy. Giờ công, tiền phụ trợ và đơn giá là ví dụ do bài tập đặt ra, không phải số liệu do PMI hay mã nguồn cung cấp.
