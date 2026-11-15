# Kế hoạch mở rộng lời thoại tự động cho Nga Mi Tu Tiên

**Mục tiêu:** Tạo người dẫn chuyện tự động cho game Nga Mi Tu Tiên, có thể vận hành nhiều giờ liên tục, phản ứng theo trạng thái thật của game, hướng dẫn người xem mới, cảm ơn quà và tạo không khí tiên hiệp. Lời thoại đa dạng, không tạo áp lực tặng quà và không tự bịa kết quả.

**Phạm vi:** Kế hoạch thiết kế và tiêu chí triển khai; chưa thay đổi mã nguồn hoặc luật chơi. Các tham số tần suất dưới đây là đề xuất để thử nghiệm, không phải bảo đảm đáp ứng chính sách TikTok. Cần kiểm tra lại chính sách áp dụng cho tài khoản, quốc gia và loại LIVE trước khi phát tự động.

## 1. Hiện trạng cần giải quyết

1. `src/game.js` có các câu cố định gắn trực tiếp vào sự kiện. Câu nhắc luật có thể hiện mỗi 3 giây nhưng mặc định chỉ được **đọc tối đa một lần mỗi 30 giây**; có lúc không đọc vì giọng đang bận hoặc đã có câu chờ.
2. `src/announcer.js` đang xếp hàng giọng theo độ ưu tiên, mặc định tối đa 4 câu chờ; câu quá hạn hoặc ưu tiên thấp có thể bị bỏ. Không có thư viện câu đủ phong phú, lịch sử chống lặp hay chính sách công bằng giữa những người tặng quà.
3. Sự kiện quà thô được TikTool chuyển cho game để vào/giữ chỗ. Mapping mới là đường kích hoạt `tu_luyen` hoặc pháp bảo và cộng tu vi. Do đó, **nhận quà thô không đồng nghĩa đã cộng tu vi hoặc tạo hiệu ứng**.
4. `server.js` chuyển quà thô trước khi đánh giá mapping. Game không thể chỉ nhìn quà thô để biết chắc quà không có mapping. Việc cảm ơn ngay cả từ quà thô lẫn action sẽ gây đọc trùng.
5. Tỉ thí yêu cầu người xem **đã có chỗ trong tông môn**, sau đó chỉ trong trận mới chọn phe bằng bình luận `1` hoặc `2`. Người mới cần được hướng dẫn theo đúng thứ tự.
6. Các mốc cảnh giới, hệ số, máu và lịch sự kiện trong lời thoại phải lấy từ trạng thái thực tế/cấu hình đang chạy, không chép cố định từ kế hoạch hoặc README.

Nguồn đối chiếu: `src/announcer.js`, `src/game.js`, `src/config.js`, `src/duel.js`, `index.html`, `manifest.json`, `core/live-events.js`, `server.js`, `sdk/tiktool-game.js`.

## 2. Ranh giới chính sách và cách nói

### 2.1 Nguyên tắc

- Ghi rõ trên màn hình: **“Game tương tác tự động · Giọng đọc tổng hợp”**; không giả làm streamer đang trực tiếp trả lời từng người.
- Cho phép mời tham gia bằng cách miễn phí theo đúng luật; khi nhắc quà/pháp bảo, nói theo hướng **tự nguyện**, đi cùng thông tin có ích về game. Không nhắm lời kêu gọi chi tiền đến trẻ em hoặc người dễ bị tổn thương.
- Không tạo cảm giác tội lỗi, sỉ nhục người không tặng, thúc ép gửi quà lớn, dọa người chơi bị phạt nếu không tặng, hoặc hứa điều game không thực hiện.
- Không hứa đổi quà TikTok thành tiền, lợi nhuận, giải thưởng ngoài game hoặc một số “linh thạch” cụ thể nếu không có nguồn dữ liệu và quy tắc công bố rõ ràng.
- Khen/chê hướng vào **diễn biến hư cấu trong game**, tông vui và công bằng; tránh công kích người xem, ngoại hình, giới tính, vùng miền, tài chính hoặc thân phận.
- Không đọc nguyên văn bình luận hoặc tên tùy ý chưa qua lọc; không để người xem tự biến tên/quà/comment thành nội dung điều khiển lời đọc.
- Nội dung phải gắn với trạng thái game; không phát lời ghi sẵn giả vờ có người tặng, có trận hay có streamer đang hiện diện.

### 2.2 Vấn đề LIVE tự động 24/24

Chưa thể kết luận rằng mọi LIVE game tự động không có streamer đều được TikTok cho phép. Các tài liệu TikTok Shop tại một số thị trường yêu cầu tương tác thực và có quy định riêng về giọng AI hoặc nội dung không tương tác; các quy định đó **không tự động được coi là áp dụng cho mọi LIVE game thông thường**. Nếu dùng TikTok Shop, cần xem đúng chính sách Shop theo thị trường và loại phiên; không triển khai phương án chỉ dựa vào giọng tổng hợp nếu quy định áp dụng không cho phép.

Ngay cả với LIVE game thông thường, cần thử nghiệm trong điều kiện thực tế, theo dõi cảnh báo tài khoản, thiết lập người phụ trách kiểm tra định kỳ và cơ chế dừng khi kết nối/âm thanh/game lỗi. Thiết kế lời thoại chỉ giúp giảm rủi ro nội dung lặp hoặc sai, **không bảo đảm** LIVE tự động được duy trì 24/24 hoặc không bị hạn chế.

TikTok khuyến khích minh bạch nội dung AI và yêu cầu gắn nhãn đối với nội dung AI có hình ảnh, âm thanh hoặc video chân thực theo điều kiện của họ. Kiểm tra cách gắn nhãn cụ thể cho LIVE tại thời điểm sử dụng; không suy diễn nút gắn nhãn của bài đăng video chắc chắn giống giao diện LIVE.

Nguồn chính thức: [TikTok về nội dung AI](https://support.tiktok.com/en/using-tiktok/creating-videos/ai-generated-content), [TikTok về quà](https://support.tiktok.com/en/live-gifts-wallet/gifts/gifts), [TikTok về áp lực gửi quà](https://newsroom.tiktok.com/updating-our-gifting-policies-to-protect-our-community?lang=en), [hướng dẫn nội dung không tương tác của TikTok Shop Malaysia](https://seller-my.tiktok.com/university/essay?knowledge_id=752633581209360&lang=en), [hướng dẫn LIVE của TikTok Shop Mỹ](https://seller-us.tiktok.com/university/course?content_id=5769635937191681&learning_id=4357891188557610). Hai nguồn Shop chỉ là ví dụ có phạm vi Shop/khu vực riêng.

## 3. Vai trò người dẫn chuyện

- **Nhân vật:** Người dẫn chuyện của môn phái Nga Mi trong thế giới game hư cấu. Giọng thân thiện, lanh lợi, hơi dí dỏm; nói rõ hơn khi có biến cố.
- **Xưng hô:** “đạo hữu”, “chư vị”, “đồng môn”, “Nga Mi”; gọi tên đã qua bộ lọc khi cần. Không tự nhận là người thật hay nhận xét về đời tư người chơi.
- **Nhịp câu:** Một câu thường khoảng 6–15 từ; câu hướng dẫn 12–25 từ, tách thành hai câu ngắn nếu dài. Chỉ ghép câu khi TTS nói rõ trong thời hạn sự kiện.
- **Tỉ lệ nội dung khi phòng yên, mục tiêu trung bình 15 phút:** khoảng 35% tường thuật/ngữ cảnh, 25% hướng dẫn, 20% không khí/khen vui, 20% lời mời tương tác. Đây là trần định hướng theo cửa sổ, **không buộc phát cho đủ** và không tính lời cảm ơn thật là lời mời mua quà.
- Tách hai nhóm CTA: **miễn phí** (tap tim/nhập phe theo trạng thái) và **pháp bảo** (nhắc tự nguyện, chỉ khi mapping phù hợp). Tối đa 1 câu mời dùng quà/pháp bảo chủ động mỗi 3 phút khi không có sự kiện, hoặc 90 giây trong sự kiện cần đóng góp; không áp dụng giới hạn này cho lời cảm ơn sự kiện có thật.

## 4. Nhịp đọc và giới hạn

### 4.1 Khi có hoạt động

- Sự kiện thật có thể tạo lời thoại ngay; không phải đợi 30 giây. Không nói chồng hai câu. Khoảng nghỉ đề xuất 0,8–1,5 giây giữa các câu, trừ khi âm thanh đang bị lỗi hoặc câu đã bị hủy.
- Câu thường nên dài không quá khoảng 6 giây; câu mở/đóng sự kiện có thể 8 giây. Thời lượng này là mục tiêu, phải đo bằng giọng Piper thực tế.
- Không cắt ngang câu đang nói ở bản đầu. Sự kiện khẩn được chọn ngay sau câu hiện tại; nếu cần ngắt, coi là tính năng riêng phải thử nghiệm vì dễ làm câu bị cụt và gây cảm giác khó nghe.
- Khi đông người: gom quà/tương tác và ưu tiên thông báo quan trọng, không cố đọc từng sự kiện. Khi phòng yên: dùng lời dẫn ngữ cảnh thay vì lặp lời kêu gọi tặng quà.

### 4.2 Khi không có sự kiện

- Sau khi lời đọc kết thúc và hàng chờ rỗng **10–15 giây**, chọn một câu nền phù hợp trạng thái. Nhịp này áp dụng cho phát sinh câu nền, không hạn chế sự kiện thật.
- Nếu không có câu đạt điều kiện, chờ thêm 5 giây và chọn lại; không cố lấp mọi khoảng im lặng. Khi TTS lỗi hoặc giọng bị tắt, không tạo vòng lặp gọi API liên tục.
- Nhắc cách nhập môn tối đa một lần mỗi 60–90 giây; nhắc chọn phe 30–45 giây trong lúc trận còn mở; mốc máu/kết giới 20–30 giây hoặc khi có mốc mới, tùy điều kiện nào hợp lý.
- Không đọc liên tục các con số thay đổi rất nhanh. Trạng thái phải được lấy lại trước lúc phát; câu đếm ngược quá hạn phải được thay hoặc bỏ.
- Cài đặt đề xuất: bật/tắt lời nền; nhịp 10–60 giây, mặc định 12 giây; bật/tắt lời mời pháp bảo; bật/tắt trêu vui; âm lượng, độ dài câu và mức chi tiết. Giữ ý nghĩa `0 = chỉ hiện chữ` đối với tùy chọn cũ, có phương án di trú cài đặt cũ rõ ràng.

## 5. Kiến trúc lời thoại

### 5.1 Dòng xử lý

`Sự kiện đã xác nhận / snapshot game → chuẩn hóa sự kiện → xác định ngữ cảnh → chọn loại câu → xếp hàng → kiểm tra còn đúng → chọn biến thể → TTS → ghi lịch sử`

Một mục hàng đợi nên có: `id`, `eventId`, `loại`, `priority`, `createdAt`, `expiresAt`, `mergeKey`, `userId`, `stateVersion`, `context`, `templateId` và điều kiện còn hợp lệ. Không lưu hoặc phát lại dữ liệu người xem thô ngoài mục đích cần thiết.

### 5.2 Quy tắc chọn câu

1. Bộ câu được duyệt sẵn, lưu trong tệp dữ liệu riêng theo nhóm; không sinh câu tự do trực tiếp từ bình luận khi đang live.
2. Mỗi câu có `id`, nhóm, văn bản, biến bắt buộc, trạng thái cho phép, sắc thái, độ dài, `cooldown`, phiên bản và trạng thái duyệt. Kịch bản không đủ biến hoặc sai ngữ cảnh bị loại.
3. Không lặp nguyên câu trong ít nhất **60 phút**, không lặp cùng mẫu mở đầu trong **5 câu gần nhất**, và không nhắc cùng người liên tiếp nếu còn người khác đủ điều kiện. Khi nguồn câu bị cạn, tăng khoảng nghỉ hoặc chọn nhóm khác; không cam kết không bao giờ lặp với kho câu hữu hạn.
4. Xáo trộn theo túi không hoàn lại trong từng nhóm, kết hợp lịch sử toàn cục. Ưu tiên câu chưa dùng lâu; không chỉ bốc ngẫu nhiên độc lập.
5. Khi khởi động lại, khôi phục lịch sử gần đây trong giới hạn hợp lý; không phát lại hàng đợi sự kiện cũ. Thông báo đã hết hạn phải bỏ.
6. Mỗi 2–4 giờ thay nhịp chủ đề nhẹ: luyện công, quan sát sân môn, tường thuật sự kiện, kiến thức luật; chỉ thay giọng văn, không tự bịa lịch sử hoặc cơ chế Nga Mi.

### 5.3 Quy mô thư viện ban đầu

| Nhóm | Số câu đề xuất tối thiểu | Điều kiện |
|---|---:|---|
| Không khí môn phái, tấu hài chung | 30 | Không bịa người hoặc sự kiện |
| Hướng dẫn nhập môn và tu luyện | 25 | Điều kiện vào/giữ chỗ đúng |
| Trước, trong và kết thúc tỉ thí | 45 | Gắn phase, điểm, thời gian, kết quả |
| Quái rồng | 30 | Gắn phase, máu và người góp thật |
| Tẩu hỏa và giải cứu | 25 | Không đổ lỗi hoặc nhục mạ |
| Thiên kiếp và hộ pháp | 30 | Lấy ngưỡng, thời gian, kết quả thật |
| Triều dâng | 15 | Chỉ đọc khi đang hoạt động |
| Pháp bảo và cảm ơn quà | 40 | Phân biệt đã cộng/chưa xác nhận |
| Linh thú và vòng quay | 25 | Dùng kết quả nở/quay thật |
| Thăng cấp, đổi ghế, chào người chơi | 35 | Theo sự kiện thật và quyền ưu tiên |
| **Tổng tối thiểu** | **300** | Rà soát nội dung, không tính biến tên thành câu mới |

Mỗi sự kiện quan trọng nên có ít nhất 4–6 biến thể thực sự khác ý. Số lượng lớn không thay thế việc kiểm tra tính đúng và thử nghe.

## 6. Ưu tiên và thời hạn hàng đọc

Điểm ưu tiên là đề xuất cho **lời đọc**, không làm thay đổi thứ tự/luật xử lý sự kiện trong game.

| Mức | Nhóm | Thời hạn đề xuất | Cách xử lý |
|---:|---|---:|---|
| 100 | Sự kiện bắt đầu/kết thúc, kết quả rồng, tẩu hỏa, thiên kiếp, tỉ thí | 15 giây; kiểm tra phase | Ưu tiên phát sau câu hiện tại; kết quả phải nói đúng và không thay bằng quảng bá |
| 90 | Hướng dẫn bắt buộc khi vừa mở sự kiện; cảnh báo hệ thống quan trọng có thể thông báo cho người vận hành | 12 giây | Nói cách tham gia ngắn gọn, không hứa đảm bảo |
| 80 | Cảm ơn pháp bảo/quà đã được xác nhận, nở linh thú, kết quả vòng quay | 20 giây | Gom theo người và sự kiện; chỉ nêu số đúng sau ACK thành công |
| 70 | Đại cảnh giới, ghế top, hộ pháp nổi bật | 15 giây | Gộp tiến triển liên tiếp của cùng người thành trạng thái mới nhất |
| 60 | Diễn biến nổi bật: mốc máu, kết giới, thay đổi thế trận | 8–12 giây | Thay mốc cũ bằng mốc mới, bỏ khi phase thay đổi |
| 50 | Chào nhập môn, vào phe, tiểu cảnh | 15 giây | Gom theo nhóm; không cần đọc từng lượt khi đông |
| 30 | Nhắc luật đúng ngữ cảnh, trả lời mẫu cho nhu cầu thường gặp | 20 giây | Chỉ phát khi không có nhóm cao hơn |
| 20 | Khen vui, tấu hài, bình luận sân môn | 15 giây | Bỏ trước khi đông; không làm chậm câu hướng dẫn |
| 10 | Không khí, lời mời tự nguyện | 10 giây | Chỉ phát khi hệ thống rảnh và đủ cooldown |

**Chính sách hàng đợi:** tối đa 12 mục sau khi gom; mục nền không xếp nếu đã có câu nền chờ. Khi đầy, loại mục ưu tiên thấp nhất và cũ nhất trong nhóm đó; không loại mất các hành động game, chỉ loại lời đọc. Với mức bằng nhau, dùng FIFO có điều chỉnh công bằng giữa người tặng. Kiểm tra `expiresAt` và điều kiện phase trước khi nói. Nếu sự kiện quan trọng cũ đã bị thay thế, ưu tiên câu tóm tắt đúng hiện tại.

**Giới hạn bắt buộc:** TTS chỉ phát được một câu tại một thời điểm. Không hứa sẽ đọc tên mọi quà nếu lượng quà vượt khả năng phát. Vẫn ghi nhận mọi hành động thành công theo luật game; chỉ gom hoặc bỏ phần lời đọc. Đo số sự kiện, câu được gom và câu hết hạn để điều chỉnh.

## 7. Cảm ơn quà và pháp bảo chính xác

### 7.1 Cần phân biệt bốn kết quả

| Trạng thái | Điều kiện thực | Lời được phép nói |
|---|---|---|
| Pháp bảo thành công | Action tương ứng đã thực thi thành công, có số tu vi thực nhận | Tên quà/pháp bảo, số lượng và tu vi thực nếu dữ liệu xác nhận |
| Quà có mapping tu luyện thành công | `tu_luyen` thành công | Cảm ơn và nêu tu vi thực; không gọi là pháp bảo cụ thể nếu mapping không phải pháp bảo |
| Quà không có mapping | Hub xác định không có quy tắc phù hợp | Cảm ơn quà, không khẳng định đã cộng tu vi, không khẳng định có hiệu ứng |
| Quà bị cooldown, game đầy, action lỗi hoặc chưa xác nhận | Hub/game trả lý do, hoặc ACK chưa đến | Cảm ơn quà theo dữ liệu thật; không nói đã cộng tu vi; ghi trạng thái cho người vận hành |

Ví dụ quà chưa có mapping: “Cảm tạ đạo hữu {tên} đã gửi {quà} đến Nga Mi!” Nếu không có tên quà hợp lệ: “Cảm tạ đạo hữu {tên} đã gửi quà ủng hộ môn phái!”

Cụm “đóng góp linh thạch” có thể là cách nói nhập vai, nhưng **không đọc một con số quy đổi** khi không có quy tắc rõ ràng. Không đọc “+X tu vi” khi game chưa cộng. Nếu sau này muốn số linh thạch, phải định nghĩa rõ đây là số quà, giá trị đơn vị hay một điểm hư cấu; có nguồn đáng tin và không gây hiểu lầm là số tiền hoặc tu vi nhận được.

### 7.2 Phối hợp TikTool và game

1. Giữ `eventId` xuyên suốt adapter → sự kiện thô → kết quả đối chiếu mapping → command → ACK. Chấp nhận chỉ số lượng tăng thêm của combo theo cách adapter thực sự phát; không cộng lặp tổng combo.
2. Bổ sung **thông báo kết quả mapping có cấu trúc** từ hub sau khi đánh giá: `eventId`, trạng thái `matched / no_match / accumulating / cooldown / safe_mode / game_offline`, thông tin quà và command liên quan. Một event có thể có nhiều kết quả, không giả định chỉ có một trong mọi trường hợp.
3. Với action đã gửi, game phát lời “đã nhận +X” ngay từ nơi tính `gained` thành công; ghép cùng `eventId`. Hub ACK có thể xác nhận thành công/thất bại, nhưng muốn đọc **số thực sau bonus** phải lấy đúng kết quả tính ở game, không suy từ `params.amount`.
4. Nếu `no_match` được hub xác nhận: phát lời cảm ơn trung tính từ một luồng duy nhất. Nếu action thất bại hoặc quá thời gian chờ: không suy thành `no_match`; chỉ cảm ơn quà trung tính nếu chính sách gộp cho phép.
5. Chống trùng theo `eventId` và `commandId`, lưu dấu vết ngắn hạn có TTL. Thiết kế cho thứ tự đến khác nhau, gửi lại, mất ACK và reconnect. Một quà đã cảm ơn trung tính không được đọc lại thành lời cảm ơn khác khi ACK đến muộn; nếu cần, thông báo kết quả game chỉ nói phần kết quả, không cảm ơn lần hai.
6. Đây là thay đổi giao thức giữa hub và game; phải giữ tương thích khi hub hoặc game chưa được nâng cấp. Ở chế độ cũ, nói cảm ơn chung khi có sự kiện phù hợp, không khẳng định “không có mapping” hay đã cộng.

### 7.3 Gom khi live đông

- Gom các quà cùng người/cùng loại trong 4–6 giây; cộng số lượng tăng thêm đã xác nhận, không cộng trùng. Gom người khác nhau theo từng đợt 8–12 giây khi quá tải; nêu tối đa 2–3 tên, sau đó “cùng các đạo hữu khác”.
- Xếp luân phiên theo người, có ngưỡng không để một người chiếm mọi câu; ưu tiên sự kiện mới và sự kiện đã thực sự được xử lý. Không công khai xếp hạng giá trị tiền tặng.
- Mẫu ưu tiên sự thật: “Cảm tạ đạo hữu {tên} đã dùng {pháp_bảo}, nhận {tu_vi} tu vi.” Khi game báo thất bại: “Cảm tạ {tên} đã gửi quà ủng hộ Nga Mi.”

## 8. Hướng dẫn theo từng tình huống

Các câu dưới đây là **mẫu đề xuất**, chỉ dùng khi điều kiện sau được xác nhận. Biến đặt trong `{}` lấy từ game hoặc dữ liệu quà hợp lệ.

### 8.1 Phòng yên và người mới

- “Nga Mi đang mở cửa đón đồng môn. Đạo hữu có thể tap tim để xin một chỗ trong môn phái.”
- “Muốn vào Nga Mi, hãy tap tim hoặc gửi quà; còn chỗ thì đạo hữu sẽ xuất hiện trên sân.”
- “Đã vào môn phái rồi, đạo hữu có thể tiếp tục tu luyện theo tương tác và các pháp bảo đang được thiết lập.”
- “Hôm nay sân Nga Mi đang yên ắng, xem vị đạo hữu nào sẽ bước vào trước nhé.”
- Khi đầy: “Tông môn đã đủ chỗ. Đạo hữu chờ một lát, khi có chỗ trống hãy thử tương tác lại.”
- Khi không chắc tim đang có mapping: chỉ nói tim giúp **xin chỗ**, không hứa mỗi tim đều cộng tu vi.

### 8.2 Trước và trong tỉ thí

- Trước trận: “Còn {thời_gian} nữa là tỉ thí. Đạo hữu mới hãy tap tim vào môn phái trước nhé.”
- Khi trận vừa mở: “Tỉ thí bắt đầu! Đã có chỗ trong Nga Mi thì bình luận một vào Thanh Liên, hai vào Hồng Liên.”
- Đầy đủ cho người mới: “Trước tiên tap tim hoặc gửi quà để vào môn phái. Khi đã có chỗ, bình luận một hoặc hai để chọn phe.”
- “Thanh Liên {điểm_1}, Hồng Liên {điểm_2}; trận còn {thời_gian}. Đạo hữu đã vào phe có thể tiếp tục tu luyện.”
- “Phe thắng có thưởng cho đồng môn; công đầu được tính theo phần tu vi đã góp trong trận.” Chỉ nêu con số khi lấy từ cấu hình hiện tại.
- Kết quả hòa: “Hai phe bất phân thắng bại! Đồng môn đã tham gia cùng nhận phần thưởng theo luật trận.”
- Tấu hài theo điểm thật: “Hai phe đuổi nhau sát quá, e rằng trọng tài Nga Mi phải lau kính rồi!” Chỉ dùng khi chênh lệch đáp ứng ngưỡng cấu hình.

**Phân nhánh bắt buộc:** nếu chưa bắt đầu trận, không bảo “bình luận 1/2 để vào ngay”; nếu đã hết trận, không đọc câu đếm ngược hoặc kêu gọi chọn phe. Với người chưa có chỗ, luôn nêu bước vào môn trước.

### 8.3 Quái rồng

- “Thanh Long đã xuất hiện! Đồng môn có thể tu luyện để góp sức tấn công.”
- “Rồng còn {máu_còn} trên {máu_tối_đa}. Đạo hữu mới hãy vào môn phái trước khi góp sức.”
- “Thanh Long vừa phun lửa, sân Nga Mi nóng hơn cả lò luyện đan!” Chỉ nói khi có sự kiện phun lửa thật.
- “Thanh Long đã bị hạ. Công đầu thuộc về {tên}, với {sát_thương} sát thương.” Chỉ nêu tên/số khi có kết quả.
- Mời tùy chọn: “Nếu muốn hỗ trợ thêm, đạo hữu có thể dùng pháp bảo đã được thiết lập; tap tim cũng giúp tham gia theo luật phòng.”

### 8.4 Tẩu hỏa

- “Khí tức của {tên} đang hỗn loạn. Đồng môn cùng góp sức giải cứu nhé!”
- “Còn {máu_còn} kết giới giải cứu. Đạo hữu mới hãy vào môn phái trước.”
- “Cả sân đang khẩn trương, mong lò luyện đan lần này đừng nổ thêm!”
- “{tên} đã được giải cứu. Cảm tạ các đồng môn đã góp sức!”
- Không nói người chơi “tham lam”, “ngu”, “phá game”; không kêu ép tặng quà để cứu người.

### 8.5 Thiên kiếp

- “Lôi kiếp đã tới với {tên}! Đồng môn có thể tu luyện để giúp dựng kết giới.”
- “Kết giới hiện {đã_góp} trên {cần_góp}; còn {thời_gian}.”
- “Người đang độ kiếp cũng có thể tự hộ thân; một phần tu vi nhận được sẽ được tính vào kết giới.” Chỉ nêu phần trăm nếu đúng cài đặt.
- Thành công: “Chúc mừng {tên} vượt kiếp thành công! Cảm tạ các đạo hữu đã hộ pháp.”
- Thất bại: “Lần này kết giới chưa đủ; tu vi vẫn được giữ, đạo hữu sẽ có dịp thử lại.”
- Không nói “sắp thắng” nếu chưa đạt điều kiện, không hứa thành công vì có một món quà.

### 8.6 Triều dâng, linh thú, vòng quay

- “Linh Khí Triều Dâng đã tới! Tu vi nhận từ tương tác theo luật hiện đang nhân {hệ_số}.”
- “Trứng của {tên} đã nở thành {linh_thú}. Chào mừng linh thú mới đến Nga Mi!”
- “{linh_thú} của {tên} đã lên {số_sao} sao; sức tu luyện của đồng môn lại được tăng cường.”
- “Vòng quay đã dừng: chúc mừng {tên} nhận {thưởng} tu vi!” Chỉ phát khi thưởng thực sự được áp dụng.
- “Ngọc Thố vừa xuất hiện, mong thỏ nhỏ đừng ăn hết vườn linh thảo của môn phái!” Chỉ khi đúng linh thú.

### 8.7 Thăng cấp và vị trí

- “Chúc mừng {tên} đã đạt {cảnh_giới}. Đường tu luyện còn dài, đồng môn cùng cố gắng!”
- “{tên} vừa bước lên vị trí {chức_vị}. Nga Mi lại có thêm một cao thủ trấn môn!”
- “Ghế Chưởng môn hôm nay có vẻ khó giữ, các đạo hữu bám đuổi rất sát!” Chỉ khi chênh lệch thứ hạng thật nhỏ.
- Trêu nhẹ: “Đạo hữu vừa đánh hụt mất một nhịp, nhưng khí thế vẫn rất đáng nể!” Chỉ dùng nếu game thực sự có sự kiện đánh hụt. Hiện tại không có thì bỏ câu này.

### 8.8 Mời dùng pháp bảo, không gây áp lực

- “Đạo hữu muốn tu luyện nhanh hơn có thể chọn pháp bảo phù hợp đang được thiết lập trong phòng.”
- “Tap tim để xin gia nhập Nga Mi; nếu muốn ủng hộ thêm, đạo hữu có thể dùng quà hoặc pháp bảo một cách tự nguyện.”
- “Có nhiều cách cùng góp sức với môn phái. Đạo hữu cứ tham gia theo cách mình thấy thoải mái nhé.”
- Không dùng: “Không tặng quà là không xứng làm đồng môn”, “phải nạp ngay”, “gửi quà chắc chắn thắng”, “gửi quà để nhận tiền thật”.

## 9. Những trường hợp lời thoại phải im hoặc đổi câu

- Game đang ở màn cài đặt, phiên đã dừng, game mất kết nối, trạng thái cũ hoặc TTS lỗi: không nói như thể sự kiện còn đang diễn ra.
- Không có người tham gia: không bịa tên, thứ hạng, điểm số, quà hay cuộc đấu.
- Không còn chỗ: không nói “tap tim là chắc chắn vào được”; phải nói điều kiện còn chỗ.
- Không có đối thủ hoặc chưa có phe: không nói “hai phe đang bám đuổi sát nút”.
- Chưa có quà mapping: không nói đã cộng tu vi, không gọi tên pháp bảo do tự suy đoán.
- Tỉ thí/thiên kiếp/rồng đã chuyển phase: bỏ câu hướng dẫn cũ. Nếu có một câu kết quả đang chờ, phải kiểm tra nguồn dữ liệu và độ tuổi sự kiện.
- Không đọc số từ ảnh, tên quà không rõ hoặc giá trị tính theo ngoại tệ/kim cương khi dữ liệu không xác nhận; dùng câu trung tính.

## 10. Thiết kế dữ liệu và tệp dự kiến

Các tên dưới đây là đề xuất, có thể điều chỉnh theo cấu trúc thực tế khi triển khai:

| Tệp/thành phần | Trách nhiệm |
|---|---|
| `src/dialogue/catalog.js` hoặc JSON | Bộ câu đã duyệt, id, điều kiện, nhóm, cooldown, biến bắt buộc |
| `src/dialogue/context.js` | Chuyển snapshot/event thành ngữ cảnh và kiểm tra điều kiện |
| `src/dialogue/selector.js` | Chọn câu, lịch sử, shuffle bag, giới hạn theo nhóm/người |
| `src/announcer.js` | Xếp hàng đọc, ưu tiên, gom, hết hạn, chuẩn bị và phát TTS |
| `src/game.js` | Phát sự kiện miền chuẩn, số liệu đã tính và điều kiện phase |
| `server.js`, `core/live-events.js`, SDK | Nếu được duyệt thay đổi: trả trạng thái mapping, liên kết eventId/action/ACK |
| `index.html` và cài đặt game | Chỉnh nhịp, chế độ lời nền, lời mời và thử nghe |
| `test/` | Kiểm thử chọn câu, ưu tiên, dữ liệu, kết nối và tình huống 24 giờ giả lập |

**Không chạm tới luật tu vi, sát thương, tỉ thí, tẩu hỏa, rồng, thiên kiếp hoặc mapping hiện có** trừ thay đổi giao thức cần thiết đã nêu. Thiết kế phải vận hành đúng với cả game độc lập và game qua TikTool; chế độ độc lập không tự nhận quà TikTok.

## 11. Chế độ âm thanh và vận hành dài

1. **Sẵn sàng:** kiểm tra khả dụng của Piper/giọng Windows; thời gian phản hồi; hàng đọc; trạng thái game và kết nối hub. Nếu không có TTS, vẫn hiện chữ và báo trạng thái cho người vận hành.
2. **TTS chậm:** giới hạn số tác vụ tạo âm thanh đồng thời, chuẩn bị trước có kiểm soát cho câu chắc sẽ phát; không tạo toàn bộ 300 câu cá nhân hóa bằng API cùng lúc.
3. **Timeout/hủy:** giải phóng audio object và URL sau khi phát hoặc lỗi; hết hạn thì bỏ câu, không giữ hàng chờ mãi; thử lại có giới hạn và backoff.
4. **Mất kết nối:** không tự bịa quà/sự kiện; hiển thị cảnh báo, dừng lời kêu gọi gắn với trạng thái không còn tin cậy. Sau reconnect, đồng bộ trạng thái mới và bỏ sự kiện hết hạn.
5. **Theo dõi:** trạng thái cuối cùng của game, hub, TTS, OBS/LIVE; cảnh báo cho người vận hành và hướng dẫn dừng phiên nếu lỗi kéo dài. Không tự khẳng định phiên 24/24 được TikTok cho phép.
6. **Log:** id câu, loại, độ ưu tiên, thời điểm, lý do chọn/gom/bỏ/lỗi và độ trễ; hạn chế lưu dữ liệu cá nhân và không ghi nội dung bình luận thô không cần thiết.

## 12. Lộ trình triển khai đề xuất

### Giai đoạn 1: Khảo sát và duyệt nội dung

- Chốt giọng điệu, cách cảm ơn, quy tắc mời dùng quà, các cụm không dùng và cấu hình mặc định.
- Viết/duyệt 300 câu theo bảng mục 5.3; kiểm tra theo từng tình huống và chính tả. Ghi người duyệt, phiên bản và ngày hiệu lực của thư viện.
- Kiểm tra chính sách LIVE thực tế theo quốc gia, tài khoản và có/không TikTok Shop; quyết định phương án giám sát phù hợp.

### Giai đoạn 2: Bộ chọn câu và lịch đọc

- Tạo cấu trúc catalog; chọn câu dựa trên trạng thái; chống lặp; hàng đợi mới; cài đặt nhịp đọc.
- Giữ các câu hiện tại làm phương án dự phòng; trạng thái và logic game không đổi.
- Thử sự kiện liên tiếp, không có người chơi, TTS lỗi, tắt tiếng, đổi setting và khởi động lại.

### Giai đoạn 3: Kết quả quà

- Thiết kế thông điệp kết quả mapping và ACK, đảm bảo tính tương thích với phiên bản cũ.
- Làm cơ chế cảm ơn cho mapped/unmapped/cooldown/offline/failed/timeout và combo. Đảm bảo tu vi thực chỉ được thông báo sau khi game thực sự cộng.
- Tạo thử nghiệm mô phỏng quà đến nhanh và đến sai thứ tự; kiểm tra không đọc trùng.

### Giai đoạn 4: Diễn biến và vận hành

- Bổ sung lời riêng theo phase, thay đổi điểm/máu, linh thú, kết quả và thiên kiếp; thêm lời dí dỏm có điều kiện.
- Chạy mô phỏng dài, theo dõi độ trễ, lặp, lỗi TTS, tài nguyên, số câu bị bỏ, sự công bằng và tính đúng.
- Thử riêng 30–60 phút có người quan sát trước; sau đó tăng dần thời lượng, kiểm tra cảnh báo nền tảng và tình trạng game theo từng đợt.

## 13. Bộ tiêu chí nghiệm thu

1. Người mới được hướng dẫn đúng thứ tự: tương tác để vào chỗ trước, sau đó mới chọn phe trong lúc trận mở; có câu phù hợp khi đầy chỗ.
2. Lời đọc nền mặc định bắt đầu sau 10–15 giây rảnh, không còn phụ thuộc cứng vào nhịp 30 giây; sự kiện thật không chờ hết nhịp nền. Tham số điều chỉnh được.
3. 100% mẫu đưa vào thư viện có `id` duy nhất, điều kiện rõ, biến hợp lệ và được duyệt nội dung; mỗi nhóm đạt tối thiểu mục tiêu trong bảng.
4. Mô phỏng 24 giờ với seed cố định và ít nhất ba tải (không ai, vừa, rất đông): không bịa sự kiện, không đọc sai trạng thái, không lặp nguyên mẫu trong 60 phút nếu còn câu đủ điều kiện; khi cạn câu được phép im lặng hoặc đổi nhóm.
5. Mô phỏng ít nhất 100 sự kiện quà trùng, trễ, combo, ACK lỗi, cooldown, no-match và game đầy: không cộng lại hoặc cảm ơn trùng do cùng `eventId`; không nói có tu vi khi chưa có kết quả thành công.
6. Kiểm thử ưu tiên có câu đang phát, hàng đầy, sự kiện đổi phase và TTS treo: không nói chồng; không phát nội dung hết hạn; sự kiện mức 100 được chọn ngay sau câu hiện tại khi vẫn hợp lệ.
7. Khen/chê không dùng mẫu công kích cá nhân; lời mời tặng quà theo cooldown và người xem có cách tham gia miễn phí đúng luật.
8. Log cho thấy trạng thái hoạt động, số câu gom/bỏ, lỗi TTS và độ trễ; không lưu dữ liệu nhạy cảm không cần thiết.
9. Các bài kiểm thử game/TikTool hiện có tiếp tục đạt; không đổi kết quả tu vi, xử lý quà, tỉ thí, phần thưởng hoặc lưu dữ liệu.

## 14. Các quyết định cần chốt trước khi triển khai

- Chọn tên và tính cách người dẫn chuyện; có cần giọng nữ, nam hay chọn trong cài đặt.
- Chốt nhịp mặc định 12 giây lúc yên và giới hạn lời mời pháp bảo; điều chỉnh sau thử nghe.
- “Linh thạch” khi cảm ơn quà là cách gọi hư cấu hay một chỉ số mới; mặc định **không thêm chỉ số mới và không đọc số quy đổi**.
- Xác nhận có cho phép thay đổi giao thức TikTool ↔ game để phân biệt `no_match` với lỗi/đang chờ ACK. Nếu không, chỉ có thể cảm ơn trung tính, không khẳng định trạng thái mapping.
- Xác nhận phạm vi LIVE thường hay TikTok Shop, khu vực tài khoản, cách gắn nhãn và quy trình người phụ trách kiểm tra định kỳ.
