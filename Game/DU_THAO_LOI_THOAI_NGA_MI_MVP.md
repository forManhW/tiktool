# Dự thảo lời thoại Nga Mi MVP để duyệt

Các câu bên dưới là bản nháp để duyệt trước khi đưa vào `src/lines.js`. Biến trong ngoặc nhọn được thay từ trạng thái thật; `{ten}` đã bao gồm cách xưng hô “đạo hữu”. Không dùng câu nếu thiếu biến hoặc sai trạng thái. Câu hiển thị trên màn hình của sự kiện giữ theo game hiện tại; các câu trong mục sự kiện chỉ thay lời đọc.

## Sự kiện

### duel_soon
- Còn một phút nữa tỉ thí bắt đầu. Đồng môn chuẩn bị chọn phe nhé.
- Một phút nữa Nga Mi mở cuộc tỉ thí. Các đạo hữu cùng chuẩn bị nào.
- Đại hội tỉ thí sắp mở, chỉ còn một phút để chuẩn bị.

### duel_start
- Tỉ thí bắt đầu. Đã vào môn phái, bình luận một vào Thanh Liên, hai vào Hồng Liên.
- Đại hội đã mở. Đồng môn bình luận một chọn Thanh Liên hoặc hai chọn Hồng Liên.
- Hai phe đã sẵn sàng. Sau khi vào Nga Mi, hãy bình luận một hoặc hai để chọn phe.
- Kèo tỉ thí đã mở! Vào môn trước, rồi bình luận một hoặc hai chọn phe nhé.
- Sân Nga Mi lên kèo! Đồng môn đã có chỗ bình luận một hoặc hai để nhập hội.

### duel_end_win
- Phe {phe} chiến thắng. Công đầu là {ten}, được thêm {thuong} tu vi.
- Trận đấu đã ngã ngũ, phe {phe} giành chiến thắng. Công đầu thuộc về {ten}.
- Chúc mừng phe {phe}. {ten} dẫn đầu phần đóng góp trong trận này.

### duel_end_win_no_mvp
- Phe {phe} chiến thắng. Các đồng môn phe thắng nhận phần thưởng theo luật trận.
- Trận tỉ thí đã khép lại với chiến thắng thuộc về phe {phe}.
- Xin chúc mừng phe {phe} đã giành chiến thắng trong cuộc tỉ thí.

### duel_end_draw
- Hai phe bất phân thắng bại. Mỗi đồng môn tham gia nhận {thuong} tu vi.
- Trận đấu kết thúc với kết quả hòa, người tham gia được cộng {thuong} tu vi.
- Thanh Liên và Hồng Liên hòa nhau. Mỗi đạo hữu tham gia nhận {thuong} tu vi.

### duel_end_empty
- Trận tỉ thí kết thúc, chưa có đạo hữu nào chọn phe.
- Đại hội lần này chưa có đồng môn chọn phe. Hẹn gặp lại ở trận tới.
- Hai phe còn đang chờ đồng môn, trận này khép lại mà chưa có người tham gia.

### boss_start
- Quái rồng {boss} xuất hiện với {mau} máu. Đồng môn tu luyện để góp sức hạ rồng!
- {boss} đã giáng xuống sân Nga Mi, có {mau} máu. Tu vi nhận được sẽ đánh vào rồng.
- Quái rồng {boss} xuất hiện, sinh lực {mau}. Cùng tu luyện để tấn công nào!

### boss_fire
- {boss} phun lửa! Đồng môn đang ngồi bị trừ tối đa {sat_thuong} tu vi.
- Cẩn thận, {boss} vừa tung một đợt lửa khắp sân Nga Mi!
- Lửa rồng phủ khắp sân, mỗi người đang ngồi mất tối đa {sat_thuong} tu vi.
- {boss} vừa phun lửa, sân Nga Mi nóng hơn cả lò luyện đan rồi!
- Rồng phun lửa rồi! Đồng môn giữ vững đạo tâm, đừng để tiên phục bén lửa nhé.

### boss_milestone
- {boss} chỉ còn {phan_tram} phần trăm máu, đồng môn cố lên!
- Quái rồng đã yếu đi, còn {phan_tram} phần trăm sinh lực.
- Sức rồng còn {phan_tram} phần trăm. Nga Mi tiếp tục góp sức nào!

### boss_killed
- Quái rồng đã bị hạ! Công đầu {ten} gây {sat_thuong} sát thương.
- Chúc mừng đồng môn đã hạ rồng. {ten} đứng đầu với {sat_thuong} sát thương.
- Thanh thế rồng đã tan. Công đầu thuộc về {ten}, với {sat_thuong} sát thương.
- {ten} đánh rồng quá đẳng cấp, bá khí! Công đầu với {sat_thuong} sát thương.
- Nga Mi hạ rồng thành công! {ten} gánh đội cực cháy với {sat_thuong} sát thương.

### boss_killed_no_top
- Quái rồng đã bị tiêu diệt. Cảm tạ các đạo hữu!
- Đồng môn Nga Mi đã vượt qua trận chiến với quái rồng.
- Trận đánh rồng khép lại, sân Nga Mi đã yên bình hơn.

### possess_warn
- Khí tức của {ten} đang hỗn loạn, có thể sắp tẩu hỏa nhập ma.
- {ten} tu luyện quá nhanh, khí tức bắt đầu bất ổn.
- Đồng môn lưu ý, {ten} đang có dấu hiệu khí tức hỗn loạn.

### possess_start
- {ten} đã tẩu hỏa nhập ma. Đồng môn cùng tu luyện để giải cứu nhé.
- Khí tức hỗn loạn, {ten} cần đồng môn giúp đỡ. Hãy cùng góp sức giải cứu.
- Nga Mi đang cứu {ten} khỏi tẩu hỏa nhập ma. Cùng tu luyện nào!
- Khí tức {ten} đang mất kiểm soát, đồng môn ơi cùng cứu người nào!
- Tình hình hơi căng rồi, {ten} cần đồng môn hỗ trợ giải cứu!

### possess_milestone
- Còn {phan_tram} phần trăm nữa là giải cứu được đồng môn.
- Việc giải cứu đang tiến triển, còn {phan_tram} phần trăm.
- Đồng môn cố lên, còn {phan_tram} phần trăm để giải cứu!

### possess_freed
- {ten} đã được giải cứu. Cảm tạ {so_nguoi} đồng môn góp sức!
- Khí tức của {ten} đã ổn định. Cảm ơn các đạo hữu giúp giải cứu.
- Đồng môn đã giải cứu {ten}. Những người góp sức nhận phần thưởng theo luật.

### kiep_start
- Lôi kiếp giáng xuống {ten}. Đồng môn tu luyện để giúp dựng kết giới.
- {ten} bắt đầu độ kiếp lên {canh_gioi}. Mọi người cùng hộ pháp nhé.
- Thiên kiếp đã tới với {ten}. Đồng môn có thể tu luyện để góp kết giới.
- Kèo này căng như dây đàn! {ten} đang độ kiếp, đồng môn cùng dựng kết giới nhé.
- {ten} bước vào lôi kiếp, đồng môn hộ pháp để tăng kết giới nào!

### kiep_success
- Chúc mừng {ten} độ kiếp thành công, nhận thêm {thuong} tu vi.
- Kết giới đã vững, {ten} vượt kiếp thành công!
- Lôi kiếp đã qua. Chúc mừng {ten} đột phá thành công.
- {ten} vượt kiếp quá đẳng cấp, bá khí! Xin chúc mừng đạo hữu.
- Kết giới vững vàng, {ten} qua kiếp thành công. Màn này đúng là quá cháy!

### kiep_failed
- {ten} độ kiếp chưa thành công. Tu vi vẫn được giữ, lần sau thử lại nhé.
- Kết giới lần này chưa đủ. {ten} vẫn giữ tu vi và có thể thử lại sau.
- Lôi kiếp chưa qua, nhưng tu vi của {ten} vẫn nguyên vẹn.

### surge_start
- Linh khí triều dâng! Tu vi mới nhận được nhân {he_so} trong {giay} giây.
- Linh khí Nga Mi đang dâng cao, tu vi nhận được nhân {he_so} trong {giay} giây.
- Triều linh khí đã tới! Trong {giay} giây, tu vi mới được nhân {he_so}.

### hatch_new
- Trứng của {ten} đã nở thành {linh_thu}. Chào mừng linh thú mới!
- {ten} vừa đón linh thú {linh_thu} đến với Nga Mi.
- Một linh thú đã nở! Chúc mừng {ten} nhận được {linh_thu}.
- {ten} vừa đón {linh_thu}. Đồng môn cho linh thú mới một màn chào sân nào!
- Xin chúc mừng {ten}, {linh_thu} đã chính thức nhập hội Nga Mi.

### hatch_star
- {linh_thu} của {ten} đã lên {sao} sao!
- Chúc mừng {ten}, {linh_thu} đã hợp thể lên {sao} sao.
- Linh thú {linh_thu} của {ten} nay đạt {sao} sao.

### hatch_wheel
- {ten} nở trùng {linh_thu} đã đủ sao và nhận một lượt quay thưởng.
- {linh_thu} của {ten} đã đủ sao; lần nở trùng này được một lượt quay.
- Chúc mừng {ten} nhận một lượt quay vì nở trùng {linh_thu} đủ sao.

### wheel_result
- Chúc mừng {ten} quay trúng {thuong} tu vi!
- Vòng quay dừng lại, {ten} nhận {thuong} tu vi.
- Vận may đã tới với {ten}: phần thưởng là {thuong} tu vi.
- Nhân phẩm của {ten} hôm nay quá sáng! Vòng quay cộng {thuong} tu vi.
- Chúc mừng {ten}, vòng quay chốt đơn {thuong} tu vi!

### seat
- {ten} đã trở thành {ghe} Nga Mi.
- Chúc mừng {ten} bước lên vị trí {ghe}.
- Vị trí {ghe} nay thuộc về {ten}.
- {ten} lên ghế {ghe}, quá đẳng cấp, bá khí!
- Chúc mừng {ten} chiếm vị trí {ghe}. Màn lên ghế này quá cháy!

### realm_up
- Chúc mừng {ten} bước vào cảnh giới {canh_gioi}!
- {ten} vừa đột phá tới {canh_gioi}. Đồng môn cùng chúc mừng!
- Tu vi tinh tiến, {ten} đã đạt đại cảnh giới {canh_gioi}.
- {ten} đột phá {canh_gioi}, quá đẳng cấp, bá khí!
- Đồng môn nhìn xem, {ten} vừa lên {canh_gioi}, thật sự rất cháy!
- Chúc mừng {ten} tiến vào {canh_gioi}. Trình độ này không phải dạng vừa đâu!

### stage_up
- Chúc mừng {ten} đạt {canh_gioi}.
- {ten} vừa tinh tiến tới {canh_gioi}.
- Chúc mừng {ten} tiến thêm một bậc, đạt {canh_gioi}.
- {ten} lên {canh_gioi}, tu luyện đều tay quá nha!
- Chúc mừng {ten} vừa tinh tiến tới {canh_gioi}. Quá mượt!

### join
- Hoan nghênh {ten} gia nhập Nga Mi.
- Nga Mi chào đón {ten} đến với môn phái.
- Đồng môn cùng chào mừng {ten} gia nhập Nga Mi nhé.

### join_return
- Chào mừng {ten} quay trở lại Nga Mi.
- Nga Mi vui mừng đón {ten} trở lại.
- {ten} đã quay lại môn phái, chào mừng đạo hữu!

### full
- Tông môn đã đủ chỗ, hẹn {ten} khi có chỗ trống nhé.
- Nga Mi tạm hết chỗ. {ten} chờ một lát rồi thử tương tác lại nhé.
- Sân môn phái đã đủ chỗ, mong {ten} trở lại khi có chỗ trống.

### gift_thanks
- Cảm tạ {ten} đã ủng hộ {so_linh_thach} linh thạch cho Nga Mi!
- Nga Mi cảm tạ {ten} đã ủng hộ {so_linh_thach} linh thạch.
- Cảm ơn {ten} đã ủng hộ môn phái {so_linh_thach} linh thạch.
- Môn phái xin cảm tạ {ten} đã ủng hộ {so_linh_thach} linh thạch.
- Cảm ơn {ten} đã ủng hộ Nga Mi {so_linh_thach} linh thạch.
- Nga Mi trân trọng cảm ơn {ten} đã ủng hộ {so_linh_thach} linh thạch.

### gift_thanks_unknown

Chỉ dùng khi dữ liệu giá không có hoặc không đủ tin cậy để tính; không đọc số tùy đoán.

- Cảm tạ {ten} đã sử dụng pháp bảo ủng hộ Nga Mi!
- Nga Mi cảm tạ {ten} đã sử dụng pháp bảo ủng hộ môn phái.
- Cảm ơn {ten} đã đồng hành cùng Nga Mi bằng pháp bảo.

## Câu nền không khí

Mọi câu dưới đây dùng `when: any`; chúng không khẳng định thời tiết hoặc sự kiện thực đang xảy ra.

- Đồng môn Nga Mi cùng tĩnh tâm trên con đường tu luyện nhé.
- Đường tu tiên còn dài, mỗi bước tinh tiến đều đáng mừng.
- Sân Nga Mi hôm nay lại có thêm những câu chuyện tu luyện.
- Môn phái luôn chào đón những đạo hữu yêu thích tu tiên.
- Tu luyện từ tốn, giữ tâm an định, đồng môn nhé.
- Một ngày ở Nga Mi bắt đầu bằng việc giữ vững đạo tâm.
- Cùng xem các đồng môn sẽ tiến tới cảnh giới nào nhé.
- Người đi trước mở đường, người đến sau cùng luyện tập.
- Từng bước tu hành, từng bậc cảnh giới đang chờ đồng môn.
- Các đạo hữu cứ thong thả tu luyện cùng Nga Mi nhé.
- Nga Mi rộn ràng bởi hành trình của các đồng môn.
- Chốn tu tiên rộng lớn, sân môn phái vẫn luôn có chuyện vui.
- Luyện công cần kiên nhẫn, kể chuyện cũng cần uống ngụm trà.
- Đường lên tiên có nhiều bậc, nhớ đi từng bậc kẻo vấp nhé.
- Nga Mi không chỉ có tu luyện, còn có cả những phút thư giãn.
- Đồng môn cùng luyện tập, người dẫn chuyện xin ngồi đếm mây.
- Tu tiên cần bình tĩnh, nhất là khi tìm lại bí kíp vừa để quên.
- Chưởng môn dặn giữ sân sạch, đừng để linh thú giấu dép nhé.
- Vườn linh thảo yên bình, mong các linh thú đừng nhầm thành bữa phụ.
- Tu luyện chăm chỉ cũng nhớ cho đạo tâm một khoảng nghỉ.
- Đồng môn mỗi người một nhịp, cùng viết câu chuyện Nga Mi.
- Ở Nga Mi, mỗi đạo hữu đều có hành trình tu luyện riêng.
- Cứ thong thả khám phá, sân môn phái luôn chờ đồng môn.
- Đạo tâm vững vàng, chuyện khó cũng thành chuyện có thể thử.
- Từng bước nhỏ cũng là một phần của hành trình tu tiên.
- Tu tiên là chuyện đường dài, còn tìm đôi dép bị linh thú giấu là chuyện trước mắt.
- Nghe bảo luyện công phải tĩnh tâm; còn luyện đan cháy thì nhớ mở cửa sổ.
- Sân Nga Mi vẫn rất bình yên, trừ cái lò luyện đan hơi có mùi khét.
- Môn phái đang tu luyện nghiêm túc, ít nhất là cho tới giờ ăn của linh thú.
- Đạo tâm phải vững, kể cả lúc bí kíp vừa để trên bàn đã biến mất.
- Đồng môn cứ an tâm tu luyện, người dẫn chuyện sẽ cố không làm nổ lò đan.
- Tu luyện hôm nay cho thật mượt, chuyện phi thăng tính sau cũng được.
- Chưởng môn ơi, kho linh thảo vẫn ổn; riêng bánh trong bếp thì chưa chắc.
- Sân Nga Mi xin phép lên sóng không khí tu tiên, miễn phí tiếng cười.
- Luyện công cần chuyên tâm, còn pha trà ngon cần nhớ đừng quên ấm nước.
- Đường tu tiên lắm gian nan, nhưng trước hết phải tìm được bí kíp đã.
- Môn phái có quy củ: luyện công hết mình, ăn bánh nhớ để phần đồng môn.
- Nga Mi không bán đường tắt lên tiên, chỉ có đường đi vòng qua vườn linh thảo.
- Đạo hữu cứ thong thả, tiên lộ không chạy mất, còn linh thú thì chưa chắc.
- Bình tĩnh tu luyện nhé, đừng để cơn buồn ngủ đánh úp đạo tâm.

## Hướng dẫn nhập môn

### Còn chỗ, ngoài trận tỉ thí
- Muốn vào Nga Mi, đạo hữu hãy tap tay hoặc sử dụng pháp bảo khi môn phái còn chỗ.
- Nga Mi đang đón đồng môn. Tap tay hoặc sử dụng pháp bảo để xin một chỗ khi còn chỗ nhé.
- Đạo hữu mới có thể tap tay hoặc sử dụng pháp bảo để tham gia môn phái nếu còn chỗ.
- Khi Nga Mi còn chỗ, tap tay hoặc sử dụng pháp bảo sẽ giúp đạo hữu nhập môn.
- Muốn có mặt trên sân môn phái, hãy tap tay hoặc sử dụng pháp bảo khi còn chỗ.
- Đạo hữu ghé thăm có thể tap tay hoặc sử dụng pháp bảo để vào Nga Mi khi còn chỗ.

### Còn chỗ, đang tỉ thí
- Trước tiên tap tay hoặc sử dụng pháp bảo để vào môn phái khi còn chỗ; sau đó bình luận một hoặc hai để chọn phe.
- Đạo hữu mới hãy tap tay hoặc sử dụng pháp bảo để vào Nga Mi trước. Có chỗ rồi, bình luận một hoặc hai để vào phe.
- Muốn tham gia tỉ thí, hãy xin chỗ trong môn phái trước, rồi bình luận một hoặc hai trong trận.
- Tap tay hoặc sử dụng pháp bảo để vào môn khi còn chỗ, sau đó chọn phe bằng bình luận một hoặc hai.

### Đầy chỗ
- Nga Mi đang đủ chỗ. Đạo hữu chờ có chỗ trống rồi thử tương tác lại nhé.
- Tông môn tạm hết chỗ, mời đạo hữu chờ và thử lại khi có chỗ trống.

## Lời mời miễn phí

- Đạo hữu có thể tap tay để xin vào Nga Mi khi còn chỗ.
- Muốn gia nhập môn phái, hãy tap tay nếu sân còn chỗ nhé.
- Khi còn chỗ, một lượt tap tay giúp đạo hữu xin vào Nga Mi.
- Đồng môn có thể tap tay để cùng tham gia môn phái.
- Đạo hữu đang ngồi có thể bình luận để trò chuyện với môn phái.
- Đồng môn hãy để lại lời nhắn nếu muốn trò chuyện trên sân Nga Mi.
- Đang trong tỉ thí: Sau khi vào môn phái, bình luận một hoặc hai để chọn phe.
- Đang trong tỉ thí: Đã có chỗ trong Nga Mi thì bình luận một vào Thanh Liên, hai vào Hồng Liên.

## Lời mời pháp bảo tự nguyện

- Nếu muốn, đạo hữu có thể dùng pháp bảo đã được thiết lập để tu luyện.
- Đồng môn có thể chọn pháp bảo phù hợp nếu muốn góp thêm sức.
- Nếu thấy thoải mái, đạo hữu có thể sử dụng pháp bảo để hỗ trợ tu luyện.
- Pháp bảo đã được thiết lập là một lựa chọn để đồng môn góp sức.
- Đạo hữu muốn ủng hộ thêm có thể chọn pháp bảo phù hợp với mình.
- Cùng tu luyện theo cách mình thích; pháp bảo là một lựa chọn tự nguyện.
- Đạo hữu có thể dùng pháp bảo phù hợp nếu muốn góp thêm tu vi theo luật phòng.
- Nếu muốn, đồng môn hãy chọn cách tham gia phù hợp; pháp bảo là một trong các lựa chọn.

## Biến thể lời nhắc luật tỉ thí

Các câu trước trận chỉ dùng khi đang chờ và có lịch; câu trong trận chỉ dùng khi trận đang mở. Chỉ nhắc vòng quay khi tùy chọn tương ứng bật.

### Chờ trận, có vòng quay
- Còn {thoi_gian} tới tỉ thí. Đồng đội phe thắng thêm {thuong}, riêng công đầu nhân {mvp} phần góp, một người được bốc thăm quay thưởng.
- Tỉ thí sau {thoi_gian}. Đồng đội phe thắng nhận {thuong}, riêng công đầu nhân {mvp} phần góp, rồi bốc thăm một lượt quay.
- Đại hội còn {thoi_gian}: đồng đội phe thắng cộng {thuong}, riêng công đầu nhân {mvp} phần góp, một người được bốc thăm quay.

### Đang trận, có vòng quay
- Thanh Liên {diem_1}, Hồng Liên {diem_2}. Đồng đội phe thắng cộng {thuong}, riêng công đầu nhân {mvp} phần góp, một người được bốc thăm quay.
- Còn {thoi_gian}, Thanh Liên {diem_1}, Hồng Liên {diem_2}. Đồng đội thắng thêm {thuong}, riêng công đầu nhân {mvp} phần góp, bốc thăm một lượt quay.
- Thanh Liên {diem_1}, Hồng Liên {diem_2}. Đồng đội thắng được {thuong}, riêng công đầu nhân {mvp} phần đã góp, một người được bốc thăm quay.

Khi vòng quay bị tắt, không chọn các câu có nội dung quay thưởng; dùng câu dự phòng có sẵn của game hoặc bổ sung nhóm riêng sau khi duyệt.
