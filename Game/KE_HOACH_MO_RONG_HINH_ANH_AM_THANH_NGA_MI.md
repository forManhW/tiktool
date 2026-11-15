# KẾ HOẠCH MỞ RỘNG HÌNH ẢNH VÀ ÂM THANH GAME TU TIÊN NGA MI

Ngày lập: 15/11/2026  
Trạng thái: Đã cập nhật theo năm quyết định và ba điều chỉnh; chưa triển khai và chưa tạo ảnh hoặc âm thanh mới.  
**Ai làm gì:** ChatGPT chỉ tạo ảnh theo **mục 12 (Phần A)**. Claude viết mã, ghép ảnh, lo âm thanh theo **mục 13 (Phần B)**. Chỗ nào mục 12 khác mục 3 hoặc mục 6 thì làm theo mục 12.

## 1. Mục tiêu và phạm vi

- Biến tỉ thí thành một trận chiến có chuyển động, chiêu thức và âm thanh liên tục, dễ theo dõi khi live dài.
- Mỗi pháp bảo có màn thi triển riêng, nhận diện được món vừa sử dụng, tương xứng với việc người xem phải trả tiền; vẫn thấy rõ người dùng và tác dụng thực tế.
- Tăng tần suất giọng đọc lúc yên tĩnh; ưu tiên pháp bảo cao nhất, có ngoại lệ cho sự kiện quan trọng sau 3 câu pháp bảo liên tiếp.
- Làm tên người chơi và cảnh giới dễ đọc hơn, nhưng giữ phong cách tiên khí Nga Mi.
- Đề xuất thêm sự kiện nếu thực sự làm phiên live đa dạng; tách phần đề xuất khỏi luật đang vận hành.
- **Giữ nguyên cách bố trí, vị trí và vùng hiển thị thông báo hiện tại ở từng trạng thái game.** Không làm lại khung thông báo; hiệu ứng mới phải tránh che thông báo.
- Không tự ý thay đổi cách tính tu vi, điểm, sát thương, phần thưởng, tỉ lệ linh thú, chu kỳ, dữ liệu người chơi hoặc cấu hình mapping TikTool hiện có. Riêng cài đặt giọng đọc cũ được chuyển từ 30 sang 15 giây một lần theo mục 7.3.

## 2. Những gì game đang có

1. Tỉ thí hiện chạy theo chu kỳ 10 phút, trận kéo dài 3 phút; hai phe Thanh Liên và Hồng Liên. Người đã vào phe góp tu vi thì mới có đòn đánh thực tế sang một đối thủ ngẫu nhiên nếu phe bên kia có người. Người trúng mất tu vi theo luật hiện có; điểm phe không bị trừ. Hiệu ứng hiện tại chủ yếu là người đánh phóng lớn trong khoảng 0,5 giây, kiếm bay và người trúng rung.
2. Cỡ nhân vật đã vào phe hiện dựa trên đóng góp, nội suy từ 0,82 đến 1,6. Chưa có đóng góp vẫn là 0,82 và đang quá to theo yêu cầu mới. Người chưa chọn phe và các chế độ khác không dùng quy tắc phóng này.
3. Game có 10 loại pháp bảo trong cấu hình. Mỗi món đã có ảnh và chuyển động riêng ở mức độ khác nhau; 9 món dùng pháp trận riêng, Linh Thú Đản có màn nở. Hiệu ứng hiện nay kéo dài theo nhóm khoảng 1,6 / 2,2 / 3 / 5 giây. Cần nâng lên thành chuỗi thi triển có cao trào, không bỏ nhận diện đang có.
4. Tu vi và sát thương đã tính từ mapping, linh thú và trạng thái Linh Khí Triều Dâng. Một hành động có thể liên quan tới tỉ thí, rồng, tẩu hỏa hoặc độ kiếp tùy trạng thái thực tế. Không suy ra sát thương từ độ lớn của hiệu ứng hoặc giá pháp bảo.
5. Khung tên hiện nằm dưới chân, dùng 18 ảnh biến thể cho 10 đại cảnh giới; chữ tên đặt trong ảnh, phía trên đầu còn có chức vị và bóng thoại. Các ngọc hiển thị tiểu cảnh. Việc đưa tên ra ngoài khung sẽ giải phóng diện tích chữ, nhưng cần giải quyết va chạm giữa các hàng.
6. Giọng đọc chờ tối đa 4 câu; hiện pháp bảo có mức ưu tiên 1 hoặc 2, trong khi nhiều sự kiện khác ở mức 3. Mặc định lời thoại lúc yên tĩnh cách 30 giây; cài đặt hiện cho phép 0 hoặc 15–120 giây. Game đã có kho lời thoại được duyệt và cơ chế xoay câu; không thay thế hay mất các câu này.
7. Hiện có âm báo thăng cấp và giọng đọc; chưa nên xem là đã có bộ âm thanh chiến đấu đầy đủ. Các giá trị nêu trên là hiện trạng đọc từ mã, cần kiểm tra lại nếu game thay đổi trước khi triển khai.

## 3. Bảng tên: đề xuất và cách kiểm chứng

### 3.1. So sánh hai phương án

| | Phương án A: tên trên đầu, tag cảnh giới dưới chân | Phương án B: tên dưới chân, tag cảnh giới ngay dưới tên |
|---|---|---|
| Dễ nhận ra người chơi | Tên tách khỏi trang trí, nằm gần mặt nhân vật; dễ nhận biết khi khung đủ thoáng. | Tên và cảnh giới nằm cùng cụm, dễ hiểu nhưng cụm dưới chân cao hơn. |
| Rủi ro khi đông người | Tên có thể chạm chức vị, bóng thoại, hào quang hoặc chân người hàng trên. | Cụm tên + tag có thể chồng lên nhân vật, tên hoặc tag của hàng phía dưới. |
| Thẩm mỹ | Nhân vật thoáng, cảnh giới như một ấn tu vi đặt dưới chân; có thể rất hợp tiên khí nếu kích thước nhẹ nhàng. | Trật tự hơn theo cụm, nhưng có nguy cơ thành nhiều dòng chữ xếp sát nhau ở 53 người. |
| Khi đánh nhau | Tên dễ theo dõi người ra đòn nếu neo ổn định và không bị kéo giãn theo hiệu ứng. | Tên gần điểm va chạm và hiệu ứng dưới chân nên có thể khó đọc khi trận đông. |

**Đã chọn A:** tên trên đầu bằng chữ sáng, không có hộp nền và không trang trí; chỉ dùng bóng/viền chữ rất mảnh nếu cần tương phản. Tag dưới chân chỉ chứa tên cảnh giới và dấu chỉ tiểu cảnh. Chữ cần ưu tiên độ đọc, không làm font cầu kỳ tới mức khó nhận ra dấu tiếng Việt. Chân Tiên vẫn là tag rực rỡ nhất, chữ sáng, không màu đen. Tiếp tục thử mẫu A trong cảnh đông người để điều chỉnh, không cần làm mẫu B song song.

Không mặc định tên đang luôn nằm trên mọi nhân vật: hiện tên và đầu là lớp con của từng nhân vật, còn thứ tự giữa các nhân vật phụ thuộc `z-index`; trong tỉ thí mã còn có quy tắc riêng cho từng hàng và người đang ra đòn. Cần chủ động thiết kế lớp tên riêng hoặc cơ chế tương đương, đặt trên hình người nhưng dưới vùng thông báo chung, neo đúng tọa độ khi người di chuyển/phóng to. Kiểm tra cả sân thường lẫn tỉ thí: một `z-index` cao bên trong lớp con chưa chắc vượt được lớp của nhân vật khác. Tên không được che nhầm hoặc tách khỏi chủ nhân.

Chức vị trên đầu và bóng thoại hiện có cần được sắp xếp **trong vùng của riêng nhân vật**, tránh đè tên: ưu tiên tên khi bình thường; khi bóng thoại xuất hiện, đặt bóng thoại ở vị trí không che tên hoặc tạm ẩn chức vị phụ. Không dời thông báo chung của game. Tên quá dài rút gọn bằng dấu ba chấm theo không gian thực tế, không làm chữ biến mất; tên đầy đủ vẫn được sử dụng đúng trong lời thoại. Bảo đảm tên có dấu, ký tự đặc biệt và emoji không phá vỡ bố cục. Tên, chức vị và tag cần giữ cỡ đọc ổn định khi nhân vật phóng to/thu nhỏ hoặc ra đòn; cân chỉnh vị trí theo chiều cao thực tế của hình người.

**Tiểu cảnh không được mất thông tin:** tag hiện có biến thể Đại Viên Mãn và các ngọc tiểu cảnh; phiên bản mới vẫn phải phân biệt được Luyện Khí tầng 1–9, các tiểu cảnh của từng đại cảnh giới và Đại Viên Mãn. Có thể dùng chữ tiểu cảnh ngắn, cụm ngọc rõ ràng hoặc biến thể tag; chọn theo mẫu thử. Chân Tiên không tự tạo tiểu cảnh không tồn tại.

### 3.2. Không vội tạo lại toàn bộ ảnh

1. Tạo thử 3 tag: Luyện Khí, Nguyên Anh, Chân Tiên; thêm một biến thể Đại Viên Mãn để kiểm tra độ khác biệt. Thiết kế bảng chỉ thể hiện cấp, chữ lớn và rõ, chất liệu tiên khí, nền trong suốt, không in tên người chơi.
2. Đặt phương án A lên **ảnh chụp thực tế**: sân 53 người, tên ngắn/dài/có dấu, 3 ghế đài, người có bóng thoại, hào quang, linh thú; thêm cảnh tỉ thí hai phe đông người và người đã phóng lớn. Kiểm tra ở kích thước người xem điện thoại thực sự nhìn thấy.
3. Nếu A chồng lấn, điều chỉnh kích thước, vị trí và thứ tự lớp của A rồi kiểm thử lại trước khi sản xuất hàng loạt. Chỉ đổi phương án khi người dùng quyết định lại.
4. Sau khi duyệt mới quyết định số lượng ảnh cuối. Nếu giữ đủ cấu trúc hiện tại thì cần tối đa **18 biến thể**; cũng có thể dùng 10 tag cơ sở cộng chỉ báo tiểu cảnh bằng mã nếu đẹp và dễ đọc hơn. Không mặc định làm lại 18 ảnh khi chưa kiểm chứng.
5. Bản thử và ảnh mới đặt trong thư mục riêng, giữ lại bộ `src/assets/game/ban3/tag/` để có thể quay về; không ghi đè bộ cũ trong giai đoạn thử. Kiểm tra font, dấu, khoảng cách, tương phản, thứ tự lớp và bản đóng gói.

## 4. Tỉ thí: nhỏ người hơn và đánh nhau hấp dẫn hơn

### 4.1. Giảm kích thước chính xác theo yêu cầu

- Trong thời gian tỉ thí, **mỗi nhân vật đã vào một phe** hiển thị hình người bằng 0,8 lần cỡ đang được tính hiện tại, kể cả khi chưa có điểm. Giữ nguyên các mốc đóng góp và nội suy, chỉ nhân hệ số vẽ cuối cùng với 0,8. Ví dụ: chưa góp 0,82 × 0,8 = **0,656**; tối đa 1,6 × 0,8 = **1,28**, so với cỡ cơ sở của nhân vật trước quy tắc tỉ thí.
- Đây là tỉ lệ so với cỡ tỉ thí hiện tại, không phải giảm 0,2 điểm tuỳ ý. Không thu nhỏ người chưa vào phe hay người ở chế độ khác. Điểm, vị trí phe, điều kiện ra đòn và luật trận không đổi.
- Chỉ thu nhỏ phần hình người/những lớp thực sự đi cùng cơ thể. Tên và tag cần giữ kích thước đọc được, còn hào quang, linh thú, điểm xuất phát đòn và vùng va chạm phải căn lại cho đúng. Hiện `--grow` áp vào lớp thân và tag có phép bù tỉ lệ: khi triển khai phải kiểm tra riêng các lớp này để không vô tình thu nhỏ chữ hoặc lệch chân.
- Trong tỉ thí giảm đỉnh phóng ra đòn từ 1,5 xuống khoảng **1,15–1,2 lần** cỡ tỉ thí mới, thêm động tác lao ngắn, vệt kiếm và va chạm thay vì phóng cả cụm tên. Tại mốc lớn nhất, 1,28 × 1,2 = 1,536 lần cỡ gốc, thay vì 1,28 × 1,5 = 1,92; kiểm tra ảnh thực tế và chọn hệ số cuối trong khoảng đã nêu. Không vô tình thay đổi hiệu ứng đòn của rồng hay chế độ khác dùng chung lớp hoạt họa.

### 4.2. Nhịp hình ảnh

**Lớp 1 – không khí trận:** chỉ có kiếm quang và bụi sáng lướt ở khoảng giữa hai phe, nhịp gợi ý 2–4 giây và điều chỉnh theo độ đông. Không gắn với bất kỳ nhân vật cụ thể nào, không cho nhân vật vung kiếm/ra đòn giả. Diễn hoạt nền không hiển thị số sát thương, không trừ tu vi, không chọn người thắng và không giả vờ có người sử dụng pháp bảo.

**Lớp 2 – đòn thực tế:** lấy đúng người ra đòn, người trúng và số tu vi mất do game đã tính. Trình tự gợi ý: lấy đà 0,1–0,2 giây → kiếm/quang bay 0,25–0,5 giây → lóe va chạm và rung 0,2–0,3 giây → hiện con số thật nếu có. Không có mục tiêu hợp lệ thì chỉ hiện động tác phát chiêu tại chỗ, không hiện va chạm hay số mất giả.

**Lớp 3 – pháp bảo:** thay đòn thường bằng biến thể của đúng món ở mục 5 khi sự kiện thực sự tạo đòn. Nếu món không tạo đòn trong trạng thái đó, vẫn có màn thi triển riêng tại người dùng; không tự dựng nạn nhân hay công dụng. Ưu tiên hiển thị chuỗi món thay vì vẽ thêm một đòn thường trùng lặp.

Người dùng muốn “đánh nhau ì đùng” nhưng không nên biến mọi thứ thành chớp sáng che tên: tăng nhịp qua các lớp nhẹ và âm thanh ngắn; giới hạn độ sáng, tránh nhấp nháy toàn màn hình. Người chơi ít thì mỗi người có chỗ diễn; đông thì chọn ít hiệu ứng nền đồng thời, vẫn giữ đòn thực và điểm chính xác.

### 4.3. Giới hạn hiệu năng ngay ở mẫu đầu tiên

Game đang dùng phần tử trang và hoạt họa CSS; 53 người cộng hạt, vệt sáng và bóng đổ chạy 24/24 là rủi ro chính. Trước khi dựng mẫu, đo FPS, CPU, bộ nhớ và số phần tử của bản hiện tại trên máy live. Đặt ngân sách ban đầu cho **phần hiệu ứng mới**: tối đa 64 phần tử DOM đang tồn tại (tính cả phần tử con), không quá 24 hạt độc lập, không quá 4 chuỗi pháp bảo/đòn nổi bật đồng thời, tối đa 2 hiệu ứng nền giữa hai phe và 1 cao trào lớn; tối đa 4 âm hiệu ứng đồng thời. Đây là ngưỡng thử, hạ nếu máy không đạt mục tiêu; không cộng thêm giới hạn này cho mỗi người.

Dùng bộ đếm và nhóm phần tử tái sử dụng, tránh tạo 53 luồng hạt riêng, hạn chế blur/filter lớn và thay đổi bố cục; dọn phần tử, timer và âm khi kết thúc hoặc chuyển trạng thái. Khi đầy ngân sách: bỏ/giảm hiệu ứng nền trước, gộp hoặc đơn giản hóa hiệu ứng đang dồn, vẫn giữ nhận diện món và kết quả quan trọng trong khả năng hiển thị. Không chặn hành động, mất tu vi hay làm sai kết quả. Đo lại ngay trên mẫu 53 người và tình huống pháp bảo dồn; chưa đạt thì tối ưu trước khi làm tiếp 10 món, không đợi thử cuối.

### 4.4. Đồng bộ sự kiện

Hiện hiệu ứng món và danh sách đòn là hai luồng riêng; `cultivate` có thể tạo đòn trước khi `itemFx` được thêm vào. Khi phát triển cần bổ sung mã liên kết cho **cùng một lần hành động** và truyền dữ liệu kết quả đã tính sang lớp hiển thị, gồm người dùng, món, số lần, mục tiêu hợp lệ, loại mục tiêu, lượng thực nhận/sát thương hoặc tu vi mất khi có. Các chế độ có thể cùng được xử lý bởi logic hiện hữu: chỉ minh họa các kết quả thực sự có, không tự thêm đòn; tránh phát hai lần do trùng sự kiện. Nếu chưa liên kết được thì giữ đòn hiện tại và diễn hoạt món tại chỗ, không đoán mục tiêu.

## 5. Thiết kế hiệu ứng riêng cho 10 pháp bảo

Mỗi món tối thiểu có: **xuất hiện rõ → tích lực đặc trưng → cao trào → tan dần**, có âm thanh riêng hoặc biến thể riêng. Giữ ảnh vật phẩm và pháp trận hiện hữu; ảnh mới là lớp hiệu ứng bổ sung. Thời lượng gợi ý 1,8–2,4 giây cho nhóm 1; 2,4–3 giây nhóm 2; 3–4 giây nhóm 3; 4–5 giây nhóm 4. Có thể chỉnh sau thử nghiệm; không dùng giá xu chưa được cung cấp để suy diễn xếp hạng mới.

| Pháp bảo | Thi triển và cảm giác hình ảnh | Khi có mục tiêu thực tế / âm thanh |
|---|---|---|
| **Linh Thạch** | Viên thạch xoay, tách thành mảnh lam ngọc; vòng khí nén bật ra. Nhỏ nhưng vẫn có mở đầu và cú kết rõ. | Mảnh sáng lao theo đòn thật, lóe xanh tại điểm trúng; tiếng ngọc va và tiếng xé gió ngắn. |
| **Tụ Khí Đan** | Viên đan rung, khí vàng cam tụ thành vòng xoáy quanh người rồi phóng quyền khí. | Quyền khí hoặc luồng sáng tới mục tiêu thật; tiếng tụ khí rồi “bụp” trầm gọn. |
| **Chu Quả** | Quả đỏ xoay, lửa tiên bùng thành cánh lửa và tàn hồng. | Cánh lửa bay theo đường đòn thực, tóe lửa khi trúng; tiếng lửa rít và va chạm nhẹ. |
| **Bàn Đào** | Cánh đào xoay thành lốc hồng, ánh ngọc lan như làn gió núi. | Lốc cánh đào quét tới mục tiêu thực; tiếng gió và chuông ngân. Không nói hoặc thể hiện hồi máu nếu luật không có. |
| **Thiên Niên Linh Chi** | Linh chi đung đưa, dây linh khí xanh kết thành xoáy lá và bào tử sáng. | Luồng lá linh khí bắn theo đòn thật; tiếng lá xào xạc hòa tiếng vang huyền ảo. Không tự thêm trói hoặc hồi phục. |
| **Ngọc Dịch Quỳnh Tương** | Bình nghiêng, dòng ngọc dịch xanh bạc uốn thành dải nước tiên quanh người. | Dải nước biến thành thủy kích tới mục tiêu thật, vỡ thành giọt sáng; tiếng rót, dòng nước và va chạm. |
| **Bí Tịch Công Pháp** | Quyển sách mở, các phù văn hiện thành vòng; ánh vàng tụ lại thành kiếm khí. | Kiếm khí có vệt phù văn bay theo đòn thật; tiếng mở quyển và rung ngân. Ký tự phải đúng, không dùng chữ giả khó hiểu. |
| **Phi Kiếm Trận** | Tám thanh kiếm hiện theo vòng trận đang có, xoay nhanh, hợp thế rồi lao đi; vệt sáng trắng lam rõ đường bay. | Các kiếm hợp thành một chuỗi đánh thị giác vào mục tiêu thật, chỉ hiện kết quả một lần; tiếng rút kiếm, xé gió và va kim loại. |
| **Long Châu** | Long Châu xuất hiện lớn, khí vàng vẽ bóng rồng cuộn quanh viên ngọc, sóng linh lực mở rộng; cao trào nổi bật nhất nhưng không chớp trắng kín màn. | Bóng rồng hoặc long quang lao theo kết quả thật; tiếng rồng trầm, gió mạnh và cú nổ ấm. Không che bảng thông báo/tên. |
| **Linh Thú Đản** | Giữ chuỗi trứng rơi, rung, nứt, nở và kết quả linh thú như hiện tại; thêm bụi sao, vỏ trứng và ánh nở riêng. | Nếu cùng hành động có đòn thật, diễn hoạt đòn được liên kết riêng; màn nở chỉ báo đúng linh thú/kết quả đã tính, không hứa trước. Âm thanh nứt vỏ, chuông nở. |

Ở sân thường, món thi triển tại người dùng và tan vào không khí; khi đánh rồng, giải cứu hoặc độ kiếp, chỉ hướng vào đối tượng đang thực sự nhận tác dụng trong logic. Nếu người dùng đang ở vị trí độ kiếp thay vì ô ngồi, vẫn cần màn thi triển đủ nhận biết chứ không chỉ vòng sáng; kiểm tra đúng tọa độ và không làm sai kết quả. Tình huống không có chỗ/người không hợp lệ phải theo xử lý hiện tại, không giả báo dùng thành công.

## 6. Danh mục ảnh và âm thanh cần chuẩn bị

### 6.1. Ảnh

Tất cả ảnh hiệu ứng mới: phong cách tiên hiệp Nga Mi thống nhất với ảnh game; nền trong suốt, không viền khung, không chữ, không watermark, chủ thể nằm trọn khung có khoảng an toàn; tạo ảnh nguồn 1024 × 1024 PNG trong suốt, sau duyệt tối ưu thành WebP trong suốt đúng kích thước sử dụng. Chuyển động được dựng bằng mã hoặc nhiều khung đã duyệt, không mặc định một ảnh tĩnh tự tạo thành hoạt họa. Đặt nguồn dự kiến ở `src/assets/raw/vfx_morong/`, sản phẩm game ở thư mục riêng tương ứng; xác nhận quy trình build hiện có trước khi nối, không ghi đè tài nguyên đang dùng.

| Tên ảnh nguồn dự kiến | Mô tả cần tạo | Dùng cho |
|---|---|---|
| `vfx_kiem_quang.png` | Vệt kiếm cong trắng lam sắc nét, đầu sáng, đuôi mờ, cảm giác thanh kiếm vừa vung. | Tỉ thí, Phi Kiếm, Bí Tịch. |
| `vfx_va_cham.png` | Tia va chạm tiên khí vàng trắng, có mảnh sáng, trọng tâm rõ, không có chữ hay con số. | Điểm trúng đòn thật. |
| `vfx_song_linh_khi.png` | Vòng sóng linh lực mỏng, ánh ngọc trắng xanh, rìa tan nhẹ. | Tụ khí, Linh Thạch, cao trào. |
| `vfx_manh_linh_thach.png` | Một cụm mảnh ngọc lam trong, có phản quang, hình rõ ở kích thước nhỏ. | Linh Thạch. |
| `vfx_hoa_tien.png` | Cụm lửa tiên đỏ cam có tàn lửa tách lớp, không vẽ hình người. | Chu Quả. |
| `vfx_canh_dao.png` | Cụm ít cánh đào hồng trắng với viền đọc rõ, không có cành cây. | Bàn Đào và không khí Nga Mi. |
| `vfx_linh_chi.png` | Dải lá linh thảo xanh ngọc và hạt bào tử sáng uốn cong. | Thiên Niên Linh Chi. |
| `vfx_ngoc_dich.png` | Dải nước tiên xanh bạc có đầu và đuôi rõ, vài giọt tách rời. | Ngọc Dịch Quỳnh Tương. |
| `vfx_phu_van.png` | Các nét phù văn trừu tượng ánh vàng, không tạo chữ Hán có nghĩa sai hoặc chữ Latin. | Bí Tịch. |
| `vfx_long_khi.png` | Bóng rồng tiên bằng linh khí vàng, dáng lượn, đường nét rõ, không có nền hoặc vật thể khác. | Long Châu. |
| `vfx_trung_no.png` | Các mảnh vỏ trứng vàng nhạt và bụi sao tỏa từ tâm, không vẽ trước loài linh thú. | Linh Thú Đản. |
| `vfx_khoi_bui.png` | Bụi mây sáng nhẹ, có khoảng rỗng ở giữa, không phải khói đen dày. | Lao người, đáp xuống, nền chiến đấu. |

Tag mẫu thử: `tag_mau_luyen_khi.png`, `tag_mau_nguyen_anh.png`, `tag_mau_dai_vien_man.png`, `tag_mau_chan_tien.png`; ảnh tag ngang, nền trong, chữ tiếng Việt rõ và đúng dấu, chỉ có cấp tu vi, không có tên người. Chân Tiên sáng và rực rỡ nhất. Thông số xuất thực tế chốt sau khi thử trên sân đông người; không kéo dẹt chữ. Ảnh tạo bằng AI cần được kiểm tra và sửa dấu, không mặc định sinh chữ đúng.

### 6.2. Âm thanh

Chuẩn bị ít nhất các nhóm: (1) vung kiếm/xé gió; (2) va chạm kiếm/khí; (3) tụ khí, nổ phép; (4) ngọc/chuông; (5) lửa; (6) nước; (7) mở bí tịch/phù văn; (8) rồng và long châu; (9) trứng nứt/nở; (10) nhịp mở/kết thúc tỉ thí. Mỗi nhóm nên có 2–3 biến thể ngắn để tránh lặp. Nguồn âm phải có quyền sử dụng phù hợp phiên live; ghi nguồn và điều kiện sử dụng trong dự án. Giữ âm thăng cấp hiện tại, không thay nếu chưa nghe so sánh.

- Có thể tạo bằng code một bộ mẫu tiếng kiếm, tụ khí, chuông, ngọc và va chạm rồi nghe thử, chỉnh trước khi tích hợp. Tiếng phức tạp như rồng gầm hoặc nước tự nhiên có thể cần nguồn thu âm được phép dùng hoặc công cụ tạo âm chuyên dụng; phiên hiện tại chưa có công cụ AI tạo hiệu ứng âm thanh trực tiếp. Không mặc định mọi âm tổng hợp đều đạt chất lượng hoặc đã sẵn có.
- Dựng mixer riêng cho giọng, hiệu ứng và âm nền nếu có; giữ các nút bật/tắt và âm lượng hiện hữu, không tự bật lại âm đã tắt. Hỗ trợ định dạng trình duyệt/ứng dụng thực tế, kiểm tra đường dẫn và bản build.
- Khi giọng đọc đang phát, giảm âm hiệu ứng nền khoảng 6–10 dB; hiệu ứng món vẫn nghe thấy nhưng không lấn chữ. Hạn chế số âm đồng thời, chống chồng cùng mẫu, tránh méo và âm lượng nhảy bất thường. Âm tỉ thí nền thay đổi nhịp và có khoảng nghỉ; không phát tiếng nổ dồn dập suốt 24 giờ.
- Âm phải đi cùng hình đúng thời điểm. Nếu tải lỗi hoặc trình phát bị khóa, game và giọng vẫn chạy, không treo hàng đợi. Kiểm tra âm thanh qua đầu ra dùng để live.

## 7. Ưu tiên thông báo và giọng đọc

### 7.1. Thứ tự đề xuất

Thứ tự dưới đây là thứ tự **chọn câu tiếp theo**; không di chuyển khu thông báo. Nên tách điều độ giọng và điều độ chữ để thay đổi ưu tiên không vô tình thay đổi bố cục hoặc làm mất thông tin trên màn hình.

| Mức | Nội dung | Cách xử lý |
|---|---|---|
| **P4 – cao nhất** | Mọi lần sử dụng pháp bảo hợp lệ, đủ 10 món, không phân biệt rẻ/đắt. | Câu tiếp theo ưu tiên người dùng + tên món + số lần nếu có + tu vi thực nhận. Ví dụ: “Đạo hữu An vừa sử dụng pháp bảo Phi Kiếm Trận, cộng 100 tu vi!” Con số chỉ là ví dụ, lấy từ kết quả thật. |
| **P3** | Bắt đầu/kết thúc tỉ thí, rồng, tẩu hỏa, độ kiếp; kết quả quan trọng và hướng dẫn đang cần ngay. | Thông thường sau P4; sau 3 câu P4 liên tiếp, cho đúng 1 câu P3 còn hiệu lực xen vào. Không đọc chỉ dẫn đã sai trạng thái. |
| **P2** | Chào người mới/quay lại, vào phe, thay đổi chức vị/cảnh giới đáng chú ý. | Có thể gom ngắn theo thời gian; giữ đúng người và tình trạng. |
| **P1** | Mốc phụ, lời cảm tạ khác, cập nhật diễn biến không khẩn. | Gom khi dồn, bỏ câu cũ nếu không còn hữu ích. |
| **P0** | Lời thoại không khí, hướng dẫn chung, lời mời lúc rảnh. | Chỉ nói khi không có câu đang chờ; ngừng tạo câu rảnh ngay khi sự kiện đến. |

Không cắt ngang câu đang phát. Thông thường P4 được chọn tiếp theo; ngoại lệ duy nhất là 1 câu P3 hợp lệ sau chuỗi 3 câu P4. Cần giữ nguyên cách gọi đã duyệt: **“tap tay”** và **“sử dụng pháp bảo”**, tránh các cách gọi người dùng đã cấm trong lời đọc và ví dụ mới. Không nói sai: muốn tỉ thí phải tham gia môn phái trước, sau đó chọn phe theo hướng dẫn thực tế; không hứa chắc thắng hoặc hứa quyền lợi chưa có.

### 7.2. Khi nhiều người dùng pháp bảo liên tục

- **Gom câu pháp bảo là bắt buộc và phải hoàn thành, kiểm thử trước khi nâng ưu tiên.** Hàng chờ hiện chỉ 4 câu, câu quan trọng chờ khoảng 12 giây; nâng lên ngay khi chưa gom dễ khiến nhiều người không được nhắc tên. Một lần sự kiện vẫn phải xử lý tu vi và hình ảnh đúng như hiện tại dù giọng bị dồn; không để giọng chặn game.
- Gom trong khoảng 3–5 giây: các lần của **cùng người, cùng món** thành một câu, cộng số lần và tu vi thực nhận khi đủ dữ liệu; khóa lô trước khi chuẩn bị âm thanh để tránh đọc số cũ. Người/món khác nhau giữ đúng cặp và thứ tự đến; khi đông có thể gom nhiều cặp trong câu ngắn nếu vẫn đọc rõ và đúng. Đặt giới hạn số lô và tuổi tối đa, không chờ vô hạn. Nếu vượt ngưỡng, câu tổng hợp phải trung thực, không tự ghép nhầm tên–món hoặc bịa tổng. Hàng chờ hữu hạn không thể bảo đảm đọc riêng mọi người khi tốc độ sự kiện vượt tốc độ nói.
- Quy tắc điều độ: đếm các **câu P4 đã phát thực tế liên tiếp**; sau câu thứ ba, nếu có P3 còn hiệu lực thì chọn đúng một câu P3 trước P4 tiếp theo. Sau P3 hoặc bất kỳ câu không phải P4 đã phát, đặt lại bộ đếm. Nếu chưa có P3, giữ bộ đếm tối đa ở 3; khi P3 hợp lệ đến, chọn ở lượt kế tiếp. Không tính câu đã bị hủy/hết hạn/TTS lỗi là đã đọc; không cho P2/P1/P0 chen theo ngoại lệ. Cùng mức giữ FIFO sau khi gom.
- Dành ít nhất một chỗ chờ cho P3 trong khi có P4 dồn, hoặc cơ chế dự trữ tương đương có giới hạn, để P4 không đẩy hết P3 ra khỏi hàng trước khi đến lượt. Kiểm tra hiệu lực theo trạng thái thực trước khi phát: câu đã hết ý nghĩa phải cập nhật hoặc bỏ. Ngoại lệ giảm nguy cơ P3 bị đói nhưng không bảo đảm mọi câu được đọc nếu giọng chậm hơn thời hạn; theo dõi thời gian đợi, điều chỉnh thời hạn có giới hạn theo từng loại và không phát hướng dẫn đã sai.
- Giữ chống trùng giữa thông tin quà thô và action đã map; lời cảm tạ dạng “ủng hộ … linh thạch” chỉ đọc số xu quy đổi khi đủ dữ liệu và tính đúng giá đơn vị × số lượng. Không nhầm số xu với tu vi nhận được. Trường hợp thiếu dữ liệu dùng câu cảm tạ không nêu số theo kho đã duyệt.
- Theo dõi số câu được đọc/gom/hết hạn, độ trễ của P4 và P3, lỗi giọng; log không chứa dữ liệu thừa. Thử trường hợp TTS lỗi, tải chậm, tắt/bật hoặc khởi động lại để không đọc lại hàng cũ.

### 7.3. Lời thoại khi không có sự kiện

- Đổi mặc định thành **15 giây yên tĩnh** thay cho 30 giây; tính sau khi giọng và hàng đợi đã thực sự rảnh. Đây là khoảng im lặng giữa các câu, không phải cứ đúng 15 giây chèn một câu, nên không chồng lời.
- Cài đặt mới dùng 15 giây; dữ liệu cài đặt cũ có `infoVoiceEverySec` đúng bằng 30 được chuyển sang 15 **một lần**, trước khi áp dụng và lưu. Giữ nguyên 0 và mọi giá trị khác hợp lệ; không đụng các cài đặt khác. Đánh dấu đã chuyển bằng phiên bản/dấu riêng và không tái kích hoạt các migration cũ có tác dụng phụ. Sau khi chuyển, nếu người dùng tự chọn lại 30 thì phải giữ 30 qua các lần mở tiếp theo. Không có dữ liệu cũ thì dùng mặc định 15.
- Giới hạn của quy tắc: giá trị 30 do người dùng từng tự chọn không phân biệt được với 30 mặc định cũ, nên cũng sẽ được chuyển một lần theo quyết định đã chốt. Kiểm thử giá trị số/chuỗi theo định dạng lưu thực tế, dữ liệu lỗi và lưu không thành công; không đổi các cấu hình khác.
- Dùng kho lời thoại đã duyệt và cơ chế xoay câu; chia nhóm không khí, hướng dẫn nhập môn, hướng dẫn đang diễn ra, điểm nhấn hài và lời mời. Tránh lặp cùng câu, cùng chủ đề, cùng tên liên tiếp; chọn theo đúng trạng thái thực tế và không nói hướng dẫn đã hết hiệu lực.
- Giữ khoảng cách tối thiểu với lời mời sử dụng pháp bảo hiện có, không biến mọi câu rảnh thành lời mời. Có thể bổ sung lời hài mới trong một đợt riêng, xem duyệt nội dung trước khi đưa vào game. Ví dụ: “Kiếm chưa ra khỏi vỏ mà khí thế Nga Mi đã vang tới tận chân núi rồi!”

## 8. Nội dung bổ sung cho live

Đợt này chỉ bổ sung **Tiên duyên đồng môn** như một nhóm lời thoại khi rảnh. Hộ sơn tiên trận và Luận kiếm giao lưu được giữ lại làm ý tưởng tham khảo nhưng **hoãn triển khai**, không bổ sung luật, bộ đếm, phần thưởng hoặc lịch chạy cho hai ý tưởng đó trong đợt này.

### 8.1. Hộ sơn tiên trận – hoãn

Một đợt ngắn, ví dụ 60–90 giây, cả môn phái cùng tích linh lực theo tu vi thực sự nhận trong thời gian sự kiện; thanh tiến độ và pháp trận sáng dần. Đạt mốc thì cảnh sân có màn hoa đào và tiếng chuông, có thể ghi nhận thành tích tập thể. Bản đầu **chỉ thưởng hình ảnh/danh hiệu tạm**, không cộng tu vi, không trừ của ai. Người xem có thể tham gia bằng tap tay theo mapping hợp lệ hoặc sử dụng pháp bảo. Giọng đọc hướng dẫn rõ thời gian và việc cần làm, không ép mua.

### 8.2. Luận kiếm giao lưu – hoãn

Một mẫu trình diễn ngắn thay cho một trận tỉ thí đã lên lịch, thay đổi lời dẫn, màu chiêu và bối cảnh; dùng đúng luật tham gia và tính điểm của tỉ thí hiện hữu. Không chạy thêm một trận độc lập song song, không tự thay thưởng hay sát thương. Chỉ bật sau khi duyệt thời điểm và hình thức, tránh làm người xem tưởng đó là luật mới.

### 8.3. Tiên duyên đồng môn – khoảnh khắc xã hội

Thêm nhóm câu P0 vào vòng chọn lời thoại lúc rảnh. Thỉnh thoảng chọn một thành tích có thật và còn phù hợp trong dữ liệu hiện có (ví dụ một đồng môn vừa đạt cảnh giới) để kể lại bằng một câu hài có tên. Chỉ dùng khi xác định được người và dữ kiện; không có dữ kiện thì bỏ qua nhóm, không bịa hoặc lưu thêm lịch sử phức tạp. Có thể kèm cánh đào hoặc chuông rất nhẹ trong ngân sách hiệu ứng, nhưng không bắt buộc và không dựng thành sự kiện hay hệ thống thưởng riêng. Tránh lặp cùng người/cùng câu; không thêm lợi ích hoặc trừ tài nguyên; không cắt ngang pháp bảo hoặc thông báo quan trọng. Các câu mới được duyệt nội dung trước khi đưa vào game.

Nếu sau này mở lại hai ý tưởng đã hoãn, phải thiết kế riêng điều kiện bắt đầu/kết thúc, xử lý chạy trùng, mất kết nối, luật và phần thưởng; không xem nội dung tham khảo ở trên là quyết định triển khai.

## 9. Kế hoạch thực hiện theo đợt

1. **Mẫu A và hiệu năng:** đo bản cũ, áp ngân sách DOM/âm ở mục 4.3 ngay từ đầu; dựng mẫu tên A và 4 tag, thử sân thường và tỉ thí 53 người. Chốt chi tiết chỉ báo tiểu cảnh và phạm vi ảnh cần tạo, không chọn lại A/B.
2. **Kích thước và diễn hoạt tỉ thí:** áp dụng hệ số 0,8, giảm đỉnh ra đòn còn 1,15–1,2, căn tên/đòn, dựng hiệu ứng nền chỉ ở giữa hai phe và đòn thật; đo hiệu năng ngay trên mẫu, duyệt nhịp và âm lượng.
3. **Pháp bảo:** bổ sung liên kết hành động–kết quả; làm mẫu Linh Thạch, Phi Kiếm Trận, Long Châu và Linh Thú Đản để kiểm tra các nhóm; duyệt rồi hoàn thành đủ 10 món và các ngữ cảnh. Sản xuất ảnh/âm thanh theo mục 6 khi đã duyệt mẫu.
4. **Giọng đọc:** trước hết hoàn thành và kiểm thử gom pháp bảo; sau đó mới nâng P4 và thêm ngoại lệ 3 P4 → 1 P3, kiểm tra quá tải/hết hạn. Đổi mặc định idle và chuyển cài đặt 30 → 15 một lần như mục 7.3. Duy trì lời thoại đã duyệt và cách gọi nhất quán.
5. **Tiên duyên đồng môn:** thêm nhóm câu rảnh dùng dữ kiện có thật; hoãn Hộ sơn tiên trận và Luận kiếm.
6. **Chạy thử dài và đóng gói:** so sánh game trước/sau, kiểm tra tiếng trong môi trường live, thử máy thực tế, kiểm tra thư mục asset và bản đóng gói. Có công tắc cấu hình cho hiệu ứng mới và đường quay về bộ tag cũ nếu cần.

## 10. Kiểm thử và tiêu chí hoàn thành

### Hình ảnh và chức năng

- Tỉ thí: kiểm tra người chưa góp, các mốc 300/1.000/50.000 và điểm giữa mốc; kích thước mới đúng 0,8 lần cũ, đỉnh ra đòn 1,15–1,2 của cỡ mới; người chưa vào phe không đổi. Tên và tag không bé theo; không đè thông báo, không tụt khỏi khung hình.
- Trận có một phe trống, ít người, hai phe đông; đòn nền không thay tu vi/điểm và không hiện số giả. Đòn thật đúng người, đúng đối tượng, đúng lượng đã xử lý; tác động bằng 0 và trường hợp mục tiêu vắng được hiển thị trung thực.
- Chạy từng món trong sân thường, hai phe, đánh rồng, tẩu hỏa, độ kiếp và khi không có mục tiêu thích hợp; 10 món đều thấy rõ đặc trưng. Kiểm tra liên kết đúng sự kiện, nhiều món cùng lúc, số lượng lớn, kết quả nở, không vẽ thêm sát thương. Tu vi/điểm/phần thưởng trước và sau giữ nguyên với cùng đầu vào.
- Kiểm tra phương án A ở 53 người, ba ghế đài, 2 phe đông, các cỡ phát triển, tên dài/có dấu/emoji, chức vị, bóng thoại, đủ cảnh giới và Đại Viên Mãn. Tên nằm trên hình người dù khác lớp nhân vật, đọc được trên khung xem điện thoại, không lẫn tên–cấp–người.
- Chụp ảnh đối chiếu thông báo trong sân thường, tỉ thí, rồng và tẩu hỏa; vị trí/bố trí thông báo không thay đổi và không bị các lớp mới che.

### Âm thanh và độ bền

- Kiểm tra gom cùng người/món, nhiều người/món, lô đầy và hết hạn trước khi bật ưu tiên mới. Khi chưa đủ 3 P4 liên tiếp, pháp bảo được chọn trước P3/P2; sau 3 P4 và có P3 hợp lệ, đúng 1 P3 được chọn rồi tiếp tục P4. Thử P3 đến muộn, hết hạn, hàng đầy, câu TTS lỗi, FIFO và bộ đếm được đặt lại; kiểm tra gom không sai tên/món/số lần/tu vi.
- Kiểm tra TTS chậm/lỗi, tắt/bật tiếng, âm bị khóa, tài nguyên thất lạc; game không treo, không đọc câu cũ sau khi hết hạn. Khi rảnh đủ 15 giây mới có câu, khi đang có hàng chờ không chen câu rảnh.
- Kiểm tra chuyển cài đặt cũ 30 → 15 đúng một lần; giữ 0 và các giá trị khác, giữ cấu hình khác, không tự đổi lại 15 sau khi người dùng đã chọn 30; thử mở lại và lưu lỗi.
- Chạy mô phỏng liên tục ít nhất vài giờ, sau đó thử live dài gần điều kiện 24/24: quan sát FPS, CPU, bộ nhớ, số phần tử hiệu ứng, số âm đồng thời và hàng đợi. Mục tiêu đề xuất ít nhất 30 FPS ổn định trên máy live thực tế, không tăng bộ nhớ liên tục; mức cụ thể cần đo bằng máy đang dùng.
- Kiểm tra ngân sách phần tử, hạt và âm mục 4.3 **ngay từ mẫu đầu** ở 53 người, đồng thời so sánh số đo với bản gốc; nếu tụt khung hình hoặc vượt giới hạn thì tối ưu trước khi mở rộng. Khi quá tải, giảm hạt/ánh sáng và bỏ diễn hoạt nền trước; không bỏ xử lý tu vi hoặc đổi kết quả. Dọn timer, âm và phần tử khi hiệu ứng kết thúc, đổi cảnh hoặc dừng game.
- Kiểm tra đóng gói: các ảnh/âm thanh mới có mặt, đường dẫn đúng, không phụ thuộc file thử ngoài dự án; thử dừng/chạy lại và kiểm tra dữ liệu cũ vẫn đọc được.

## 11. Quyết định đã chốt và các mẫu cần xem

Đã chốt: A; cỡ tỉ thí ×0,8 và đỉnh ra đòn 1,15–1,2; mặc định 15 giây và chuyển giá trị 30 cũ một lần; 3 câu pháp bảo rồi tối đa 1 câu P3 còn hiệu lực; chỉ thêm Tiên duyên đồng môn, hoãn hai sự kiện còn lại. Gom câu trước khi tăng ưu tiên; không gắn hiệu ứng nền vào người; đặt ngân sách hiệu năng từ mẫu đầu.

Cần xem mẫu khi thực hiện: độ rõ của A, chỉ báo tiểu cảnh và số tag cần tạo; nhịp/âm lượng trận, hiệu ứng từng nhóm và mức rực rỡ của Long Châu; nội dung các câu Tiên duyên mới. Các chi tiết này không làm thay đổi những quyết định đã chốt.

---

# 12. PHẦN A · CHỈ THỊ TẠO ẢNH (cho ChatGPT)

## A.1 Việc của bạn

Tạo **16 ảnh PNG**, chia 3 đợt. Xong mỗi đợt thì **dừng lại, báo danh sách file, chờ anh duyệt** rồi mới làm đợt sau.

| Đợt | Số ảnh | Nội dung | Điều kiện bắt đầu |
|---|---|---|---|
| 1 | 4 | Bảng cảnh giới mẫu (T1–T4) | Làm ngay |
| 2 | 5 | Hiệu ứng mẫu (V1, V2, V4, V10, V11) | Anh duyệt đợt 1 |
| 3 | 7 | Hiệu ứng còn lại (V3, V5, V6, V7, V8, V9, V12) | Anh duyệt đợt 2 |

**Không làm:**
- Không tạo âm thanh.
- Không sửa mã, không sửa file `.js`, `.css`, `.html`, `.json`.
- Không lưu, sửa hay xóa gì trong `src/assets/game/` và `src/assets/raw/ban3/` (bộ đang dùng, phải giữ để quay về).
- Không tạo lại đủ bộ 18 bảng cảnh giới. Việc đó chỉ làm khi anh yêu cầu riêng sau khi xem mẫu.
- Không tự thêm ảnh ngoài danh sách, không đổi tên file.

## A.2 Lưu file ở đâu

Thư mục gốc dự án: `D:\DuAnCaNhan\TikTool\Game\NgaMi_TuTien\`

| Loại | Thư mục (tạo mới nếu chưa có) |
|---|---|
| Bảng cảnh giới mẫu T1–T4 | `src/assets/raw/tag_mau/` |
| Hiệu ứng V1–V12 | `src/assets/raw/vfx_morong/` |

- Đúng tên file ở cột "File", chữ thường, đuôi `.png`.
- Mỗi ảnh ghi 1 dòng vào `src/assets/raw/vfx_morong/GHI_CHU.txt` (ảnh bảng cảnh giới cũng ghi vào file này): `<tên file>: nền trong suốt` hoặc `nền đen` hoặc `nền xanh` hoặc `nền hồng`.
- Tạo lại một ảnh thì ghi đè file cùng tên, không thêm hậu tố `_v2`.

## A.3 Quy tắc nền (quan trọng, quyết định ảnh có dùng được hay không)

| Loại ảnh | Nền phải dùng | Lý do |
|---|---|---|
| **Ánh sáng, lửa, nước, khí** (V1, V2, V3, V5, V8, V9, V10, V12) | **Đen tuyền `#000000`** phủ kín ảnh | Phần sáng mờ không tách nền được bằng nền phông màu. Claude sẽ đổi độ sáng thành độ trong suốt. |
| **Vật thể đặc** (T1–T4, V4, V6, V11) | Trong suốt thật (PNG có kênh alpha). Nếu ra nền ô bàn cờ giả thì tạo lại trên nền **xanh lá tuyền `#00FF00`** | Vật đặc tách nền phông sạch. |
| **V7 Linh Chi** (màu xanh lá) | Trong suốt thật, hoặc nền **hồng tuyền `#FF00FF`** | Tránh trùng màu với nền xanh. |

Với ảnh nền đen:
- Nền phải đen đều tuyệt đối: không chuyển sắc, không sương mù, không tối góc, không sao, không sàn, không bóng đổ.
- Chủ thể phải **sáng và rực**. Không dùng màu tối hay viền đen trong chủ thể, vì chỗ tối sẽ thành trong suốt.

Với ảnh nền phông xanh / hồng: chủ thể nét đặc, viền rõ, **không có quầng sáng mờ** tỏa ra nền.

Câu dùng để tạo lại: *"Regenerate on a pure solid #000000 background, nothing else changed."* (đổi mã màu theo bảng trên).

## A.4 Quy tắc chung cho mọi ảnh

- Mỗi tin nhắn tạo **1 ảnh**.
- Chủ thể nằm trọn trong ảnh, cách mép ít nhất **6%** mỗi cạnh, không bị cắt.
- Không người, không mặt, không bàn tay, không nhân vật, không chùa, không tượng Phật.
- Không chữ, không số, không logo, không chữ ký, không watermark. Ngoại lệ duy nhất: tên cảnh giới trên T1–T4.
- Nét đậm, hình khối đơn giản. Ảnh sẽ bị thu rất nhỏ trong game (xem cỡ ở từng bảng).
- Hướng: ảnh có đầu và đuôi thì **đầu ở bên PHẢI, đuôi bên TRÁI, nằm ngang**. Game tự xoay theo hướng đòn.
- Ảnh hình tròn thì **tròn đều nhìn thẳng** (không nghiêng thành elip), tâm vòng trùng tâm ảnh.

## A.5 Đợt 1 · Bảng cảnh giới mẫu (4 ảnh)

Kiểu mới: **bảng chỉ có tên cảnh giới**, đặt dưới chân nhân vật. Tên người chơi do game ghi ở trên đầu nên bảng **không còn ô tên trống**.

Khổ khi tạo: **ngang 1536×1024**, bảng nằm giữa, rộng khoảng 95% bề ngang ảnh.

Hình dáng:
- Bảng **rất dẹt**, tỉ lệ khoảng **4,5 : 1** (rộng 1460 thì cao khoảng 320). Không cao hơn 1/4 bề ngang.
- Tên cảnh giới chiếm gần hết mặt bảng: chữ cao khoảng **60%** chiều cao bảng, dài khoảng **75%** bề ngang bảng.
- Hốc ngọc nằm **trên viền dưới**, chính giữa, cách đều, **để trống** (game tự bật sáng theo tiểu cảnh).
- Trang trí chỉ ở **hai đầu trái phải**. Viền trên dưới mảnh, trơn.
- Trong game bảng chỉ rộng khoảng **96 px**: thu ảnh còn 96 px bề ngang mà không đọc được chữ là phải tạo lại.

Chữ:
- Font không chân, nét rất đậm, tròn, kiểu tiêu đề game mobile.
- Đúng 1 dòng, đúng y cột "Chữ in", đủ dấu tiếng Việt. Sai một dấu là tạo lại.
- Không thêm "Sơ kỳ", "Đại viên mãn", số, chữ Hán hay bất kỳ chữ nào khác.

Khối phong cách (dán 1 lần ở đầu cuộc chat đợt 1):

```
You will create game UI art for a bright, cute chibi anime xianxia (Chinese fantasy cultivation) mobile game.
Shared style: clean bold outlines, soft cel shading, bright saturated but soft colors, gold ornamental accents,
glowing ethereal immortal feel, crisp and sharp, no blur.
Must stay readable when shrunk to 96 pixels wide: bold simple shapes, thin plain frame edges, almost no tiny details.
The ONLY text allowed is the exact rank title I give you, in Vietnamese with correct diacritics.
Never include any other text, numbers, watermark, logo, signature, faces, people, temples, Buddha, lotus flowers.
Isolated on a fully transparent background (PNG with real alpha channel), no shadow, nothing outside the plaque.
```

Mẫu prompt (thay [TITLE], [LETTERS], [GEMS], [THEME], [COLOR]):

```
A FLAT, WIDE rank plaque for a game UI, front view. Overall ratio about 4.5 : 1 (very flat),
filling 95% of the canvas width, centered.
The whole face of the plaque is one wide banner carrying the title text "[TITLE]":
Vietnamese, exact spelling with correct diacritics, very bold rounded sans-serif letters, [LETTERS].
Letters about 60% of the plaque height and about 75% of the plaque width.
Bottom rim: [GEMS], evenly spaced, centered.
Thin plain top and bottom edges; decoration only at the left and right ends.
No empty name panel, no second line of text.
Theme: elegant immortal / xianxia, [THEME], main color [COLOR].
No other text anywhere. Transparent background.
```

| Mã | File | Chữ in ([TITLE]) | [LETTERS] | [GEMS] | [THEME] | [COLOR] |
|---|---|---|---|---|---|---|
| T1 | `tag_mau_luyen_khi.png` | Luyện Khí | dark brown letters with a thin light outline on a light cream banner | nine small round EMPTY gem sockets | simple jade plaque with small cloud swirls at both ends | pale jade green and white |
| T2 | `tag_mau_nguyen_anh.png` | Nguyên Anh | dark navy letters with a thin light outline on a pale silver-blue banner | four small round EMPTY gem sockets | sapphire frame with flowing silk ribbon ends | sapphire blue with silver trim |
| T4 | `tag_mau_chan_tien.png` | Chân Tiên | bright glowing white-gold letters with a dark amber-brown outline, on a deep rich imperial amber-gold banner; the letters must be LIGHT, never black or dark | NO gem sockets; one single radiant jewel centered on the bottom rim | radiant celestial plaque with small cloud wings at both ends and fine light rays behind; the most luxurious of the set | white-gold with deep gold |

**T3 · `tag_mau_dai_vien_man.png`** là bản Đại viên mãn **của Nguyên Anh**. Chữ in vẫn là **"Nguyên Anh"**, không in chữ "Đại viên mãn". Gửi lại ảnh T2 rồi dán:

```
Create the "Great Perfection" upgraded version of this exact rank plaque.
Keep exactly the same flat shape and proportions, the same text "Nguyên Anh" (same spelling, same diacritics,
same lettering, same size) and the same four empty gem sockets on the bottom rim.
Upgrade it: richer gold filigree at both ends, short elegant wing flourishes on the left and right
(keep them short so the plaque stays flat), a small radiant jewel at the top center, a soft golden glow around the plaque.
No other text. Transparent background.
```

Thứ tự tạo: T2 → T3 → T1 → T4. Từ ảnh thứ 2 thêm câu: *"Same art style, same plaque shape, same proportions and same lettering style as the first image of this set."*

Kiểm tra đợt 1:
- [ ] 4 file đúng tên trong `src/assets/raw/tag_mau/`, đã ghi `GHI_CHU.txt`.
- [ ] Bảng dẹt: cao không quá 1/4 bề ngang.
- [ ] Chữ đúng từng dấu. Soi kỹ: **ệ** (Luyện), **ê** (Nguyên), **â** (Chân), **ê** (Tiên).
- [ ] Thu còn 96 px bề ngang vẫn đọc được tên cảnh giới.
- [ ] Không có ô tên trống, không có chữ nào khác.
- [ ] T1 đủ 9 hốc, T2 và T3 đủ 4 hốc, T4 không có hốc. Hốc đều trống.
- [ ] T3 cùng hình dáng và cùng chữ với T2, chỉ lộng lẫy hơn; nhìn ở cỡ nhỏ vẫn phân biệt được với T2.
- [ ] T4 rực rỡ nhất, chữ sáng, không đen.
- [ ] Nền trong suốt thật, hoặc nền xanh `#00FF00` đều màu.

## A.6 Đợt 2 và 3 · Ảnh hiệu ứng (12 ảnh)

Khối phong cách (dán 1 lần ở đầu cuộc chat hiệu ứng):

```
You will create 2D visual-effect sprites for a bright, cute chibi anime xianxia (Chinese fantasy cultivation) mobile game.
Shared style: clean stylized hand-painted anime VFX, bold simple shapes, bright saturated colors, crisp edges,
elegant immortal / ethereal feel. Not realistic, not photographic, no 3D render look.
One single effect per image, fully inside the canvas with a clear margin on every side, nothing cropped.
Never include text, numbers, runes that look like real letters, watermark, logo, people, faces, hands,
weapons held by anyone, ground, scenery or any background object.
Must stay recognizable when shrunk to about 120 pixels.
```

Mỗi ảnh dán: `[SUBJECT]. [BACKGROUND]. Canvas [KHỔ].`

Câu [BACKGROUND] theo cột "Nền":
- **Đen**: `On a pure solid #000000 black background covering the whole canvas, perfectly uniform, no gradient, no vignette, no fog; the effect itself is bright and luminous with no dark colors and no black outlines`
- **Trong suốt**: `Isolated on a fully transparent background (PNG with real alpha channel); solid opaque shapes with crisp edges, no soft glow bleeding outside the shapes`

| Mã | Đợt | File | Khổ | Nền | [SUBJECT] | Cỡ trong game |
|---|---|---|---|---|---|---|
| V1 | 2 | `vfx_kiem_quang.png` | 1536×1024 | Đen | A single curved crescent sword-slash energy trail, white-hot core with an icy blue edge, sharp bright leading tip on the RIGHT, tail fading out to the LEFT, lying horizontally, centered | dài khoảng 200 px |
| V2 | 2 | `vfx_va_cham.png` | 1024×1024 | Đen | A radial impact burst of golden-white immortal energy, sharp star-like rays of uneven length and a few small flying sparks, brightest point exactly at the canvas center | khoảng 120 px |
| V4 | 2 | `vfx_manh_linh_thach.png` | 1024×1024 | Trong suốt | Six separate faceted sky-blue spirit crystal shards of different sizes, solid and opaque with clear white highlights and bold outlines, scattered loosely around the center, not touching each other | mỗi mảnh khoảng 30 px |
| V10 | 2 | `vfx_long_khi.png` | 1536×1024 | Đen | An eastern dragon made entirely of flowing golden spirit energy, long serpentine body in one clear S-curve, head on the RIGHT facing right with open jaws, tail on the LEFT, strong clean silhouette, whole body inside the canvas, no pearl, no clouds | dài khoảng 420 px |
| V11 | 2 | `vfx_trung_no.png` | 1024×1024 | Trong suốt | Seven separate pale golden-cream eggshell fragments with jagged cracked edges and bold outlines, plus a few small solid four-point star sparkles, all flying outward from the center, center left empty, no creature, no whole egg | mỗi mảnh khoảng 40 px |
| V3 | 3 | `vfx_song_linh_khi.png` | 1024×1024 | Đen | One thin circular shockwave ring of jade white-cyan spirit energy, a perfect circle seen straight on, softly fading outer edge, completely empty center, ring centered on the canvas | 150–400 px |
| V5 | 3 | `vfx_hoa_tien.png` | 1536×1024 | Đen | A flying bolt of immortal fire, rounded bright golden head on the RIGHT, red-orange flame tail streaming to the LEFT with layered flame tongues and a few loose embers, lying horizontally | dài khoảng 200 px |
| V6 | 3 | `vfx_canh_dao.png` | 1024×1024 | Trong suốt | Nine separate peach blossom petals, pink fading to white, solid and opaque with bold clean outlines, seen from different angles, scattered loosely, not touching each other, no branch, no whole flower | mỗi cánh khoảng 24 px |
| V7 | 3 | `vfx_linh_chi.png` | 1536×1024 | Trong suốt (hoặc hồng) | A curved ribbon stream of jade-green spirit herb leaves with small solid bright yellow-green spore dots, flowing from LEFT to RIGHT in one gentle arc, pointed head on the RIGHT, solid opaque shapes | dài khoảng 220 px |
| V8 | 3 | `vfx_ngoc_dich.png` | 1536×1024 | Đen | A flowing ribbon of silver-cyan immortal water, rounded bright head on the RIGHT, tapering wavy tail to the LEFT, a few separate round droplets beside it, lying horizontally | dài khoảng 220 px |
| V9 | 3 | `vfx_phu_van.png` | 1024×1024 | Đen | A ring of abstract golden talisman brush strokes arranged in a perfect circle seen straight on, empty center, ring centered; purely abstract strokes that must NOT form real Chinese characters, Latin letters or numbers | khoảng 260 px |
| V12 | 3 | `vfx_khoi_bui.png` | 1024×1024 | Đen | A soft bright puff of pale white-gold spirit dust shaped like a loose ring with a hollow center, light and airy, a few tiny sparkles, not dark smoke | khoảng 160 px |

Với V4, V6, V11 (nhiều mảnh rời): các mảnh **phải tách hẳn nhau**, giữa các mảnh có khoảng hở rõ, để Claude cắt từng mảnh.

Kiểm tra mỗi ảnh hiệu ứng:
- [ ] Đúng tên file trong `src/assets/raw/vfx_morong/`, đã ghi loại nền vào `GHI_CHU.txt`.
- [ ] Đúng khổ, chủ thể nằm trọn, cách mép ít nhất 6%.
- [ ] Ảnh nền đen: nền đen đều, không chuyển sắc, không tối góc; chủ thể sáng, không viền đen.
- [ ] Ảnh nền trong suốt / nền phông: hình đặc, viền rõ, không quầng sáng mờ; các mảnh rời nhau.
- [ ] Đầu bên phải, đuôi bên trái (V1, V5, V7, V8, V10). Vòng tròn đều, đúng tâm (V2, V3, V9, V12).
- [ ] Không chữ, không số, không người, không vật thể thừa. V9 không thành chữ thật.
- [ ] Thu còn khoảng 120 px vẫn nhận ra là gì.
- [ ] Cả bộ cùng một phong cách với ảnh pháp bảo đang có trong `src/assets/raw/item_*.png` (chỉ mở xem để tham khảo, không sửa).

## A.7 Báo cáo cuối mỗi đợt

Gửi anh đúng 3 thứ rồi dừng:
1. Danh sách file đã lưu, kèm loại nền của từng file.
2. Ảnh nào chưa đạt mục kiểm tra nào và lý do (không giấu, không tự cho qua).
3. Ảnh nào đã phải tạo lại và tạo lại mấy lần.

---

# 13. PHẦN B · GHI CHÚ CHO CLAUDE (mã, ghép ảnh, âm thanh)

Thứ tự làm, mỗi bước xong chạy `node --test test/*.test.js` trong `Game/NgaMi_TuTien/` (kiểm thử game) và `npm test` ở thư mục gốc TikTool (kiểm thử hub):

1. **Commit trạng thái hiện tại trước** (bản 3 và bộ khung tên bản 2 chưa commit) để có đường quay về.
2. **Giọng đọc (mục 7), chỉ sửa mã:**
   - Gom câu pháp bảo trước: câu `kind: "item"` ở `src/game.js` (hàm `useItem`, hiện `priority: item.speak ? 2 : 1`, khóa `item:<người>:<món>`). Gom theo khóa đó trong 3–5 giây, cộng số lần và tu vi.
   - Sau khi gom chạy đúng mới nâng lên mức 4, thêm bộ đếm 3 câu pháp bảo → 1 câu mức 3 và một chỗ chờ dành riêng cho mức 3 trong `src/announcer.js` (`say` và `pump`).
   - Số liệu đúng trong mã: `voice.maxQueue` 4, `maxWaitMs` 10 giây, `maxWaitImportantMs` 12 giây (`src/config.js`).
   - 15 giây: đổi `voice.infoEverySec` trong `src/config.js`; chuyển `infoVoiceEverySec` 30 → 15 một lần bằng cách tăng `SETTINGS_VERSION` trong `src/game.js`. Nhánh nâng phiên bản hiện tại đang xóa `duelEveryMin` và `duelDurationMin`: tách điều kiện theo từng phiên bản để lần nâng mới không xóa lại hai giá trị đó.
   - Tiên duyên đồng môn: thêm nhóm câu vào `src/lines.js`, anh duyệt nội dung trước.
3. **Tỉ thí ×0,8 và đỉnh ra đòn (mục 4.1):** nhân 0,8 ở chỗ đặt `--grow` trong `src/render.js`; `@keyframes nm-strike` trong `src/style.css` đang dùng chung, tạo biến thể riêng cho tỉ thí thay vì sửa bản chung. `.nm-duel-layout .nm-tag` đang được khai báo hai lần với `transform-origin` khác nhau trong `src/style.css`: gộp lại khi sửa.
4. **Tên trên đầu (phương án A):** tên hiện là `.nm-tag-name` nằm trong `.nm-tag` (lớp con của thân, bị `--grow` kéo theo). Tách thành lớp tên riêng theo mục 3.1. Ghép 4 ảnh mẫu đợt 1 vào bản thử, chụp sân 53 người và tỉ thí cho anh duyệt; chưa đụng `src/assets/game/ban3/tag/`.
5. **Ghép ảnh hiệu ứng:** viết công cụ riêng (không sửa `tools/build-ban3.py` đang chạy cho bộ cũ) đọc `GHI_CHU.txt`: nền đen → đổi độ sáng thành alpha; nền xanh / hồng → tách phông; cắt rời từng mảnh của V4, V6, V11; xuất WebP vào `src/assets/game/vfx/`.
6. **Liên kết hành động với kết quả (mục 4.4):** `useItem` hiện đẩy `{ id, item, userId, count, amount }` vào `itemFx`, chưa có mục tiêu; `duelAttack` tạo đòn ở luồng khác. Thêm mã liên kết chung cho một lần hành động rồi mới làm 4 món mẫu.
7. **Ngân sách hiệu năng (mục 4.3):** `trimFx` trong `src/render.js` đang giới hạn 60 phần tử cho lớp hiệu ứng cũ; ngân sách 64 phần tử mới tính riêng và phải có bộ đếm.
8. **Âm thanh:** ChatGPT không làm. Mở rộng `src/sound.js` (`createSfx`) thành bộ phát có giới hạn 4 âm đồng thời và hạ âm khi giọng đang đọc. Nguồn âm phải cho phép dùng thương mại (ưu tiên CC0), ghi nguồn vào `src/assets/raw/vfx_morong/NGUON_AM_THANH.txt`. File âm đặt trong `src/assets/raw/`, `tools/build-assets.js` đã tự chép mp3 / ogg / wav.
