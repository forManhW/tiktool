/*
 * Nga Mi Tu Tiên: cấu hình cân bằng mặc định.
 * Tim/quà/comment đổi ra BAO NHIÊU tu vi KHÔNG nằm ở đây: streamer cài trong dashboard TikTool (mapping -> action "tu_luyen").
 * File này chỉ giữ ngưỡng cảnh giới, chức vị, luật tỉ thí, giọng đọc. Ngưỡng là đề xuất, chỉnh sau buổi live thử.
 */
(function (root, factory) {
  if (typeof module === "object" && module.exports) module.exports = factory();
  else root.NgaMi = Object.assign(root.NgaMi || {}, { DEFAULTS: factory() });
}(typeof self !== "undefined" ? self : this, function () {
  "use strict";

  // Đại cảnh giới. `stages` = ngưỡng tu vi TÍCH LŨY để đạt từng tiểu cảnh. `sprite` = bộ trang phục (src/assets/game/).
  // Cách thiết kế ngưỡng và bảng quy đổi tham chiếu: KE_HOACH.md mục 4.2. In lại bảng: node tools/realm-table.js
  var SMALL4 = ["Sơ kỳ", "Trung kỳ", "Hậu kỳ", "Đại viên mãn"];
  var REALMS = [
    { id: "luyen_khi", name: "Luyện Khí", sprite: "luyenKhi", stageNames: ["tầng 1", "tầng 2", "tầng 3", "tầng 4", "tầng 5", "tầng 6", "tầng 7", "tầng 8", "tầng 9"],
      stages: [0, 30, 70, 120, 190, 280, 400, 550, 750] },
    { id: "truc_co", name: "Trúc Cơ", sprite: "trucCo", stageNames: SMALL4, stages: [1000, 1500, 2200, 3000] },
    { id: "ket_dan", name: "Kết Đan", sprite: "ketDan", stageNames: SMALL4, stages: [4000, 5500, 7500, 10000] },
    { id: "nguyen_anh", name: "Nguyên Anh", sprite: "nguyenAnh", stageNames: SMALL4, stages: [13000, 17000, 22000, 28000] },
    { id: "hoa_than", name: "Hóa Thần", sprite: "hoaThan", stageNames: SMALL4, stages: [36000, 46000, 58000, 72000] },
    { id: "luyen_hu", name: "Luyện Hư", sprite: "luyenHu", stageNames: SMALL4, stages: [90000, 110000, 135000, 165000] },
    { id: "hop_the", name: "Hợp Thể", sprite: "hopThe", stageNames: SMALL4, stages: [200000, 250000, 310000, 380000] },
    { id: "dai_thua", name: "Đại Thừa", sprite: "daiThua", stageNames: SMALL4, stages: [460000, 560000, 680000, 820000] },
    { id: "do_kiep", name: "Độ Kiếp", sprite: "doKiep", stageNames: SMALL4, stages: [1000000, 1300000, 1700000, 2200000] },
    { id: "chan_tien", name: "Chân Tiên", sprite: "chanTien", stageNames: [""], stages: [3000000] }
  ];

  // Bố cục khung 1080x1920 (anh chốt 10/11): 1 Chưởng môn + 2 Hộ pháp + 5 hàng x 10 cột = 53 chỗ.
  // Chỗ ngồi điền theo thứ hạng: 3 ghế đài, rồi điền kín hàng 1 từ trái sang phải, đầy mới xuống hàng 2...
  // Vùng TikTok che: trên ~1/20 (y < 96), dưới ~1/4 (y > 1440). Hai bên không bị che, bố cục căn giữa x = 540.
  // `h` = chiều cao nhân vật, `y` = vị trí đáy đệm.
  var LAYOUT = {
    // Bản 3 (13/11): khung tên chuyển xuống dưới chân (cao khoảng 40 px ở hàng thường) nên nhân vật hàng thường nhỏ đi
    // khoảng 18% và các hàng giãn ra: chân hàng trên + khung tên vừa chạm đỉnh đầu hàng dưới, không che nhau.
    podium: [
      { seat: "Chưởng môn", x: 540, y: 725, h: 236 },
      { seat: "Tả hộ pháp", x: 190, y: 780, h: 176 },
      { seat: "Hữu hộ pháp", x: 890, y: 780, h: 176 }
    ],
    rows: [
      { count: 10, y: 950, h: 98 },     // khung tên ảnh mới (14/11) dẹt hơn: nhân vật hàng thường to lại thêm 6 px
      { count: 10, y: 1070, h: 94 },
      { count: 10, y: 1186, h: 90 },
      { count: 10, y: 1298, h: 86 },
      { count: 10, y: 1406, h: 82 }
    ],
    rowLeft: 40, rowRight: 1040,   // đối xứng quanh x = 540
    // Kiểu khung tên. "v3" (bản thử 15/11, phương án A): tên người chơi là chữ sáng trên đầu, dưới chân là bảng chỉ có
    // tên cảnh giới (src/assets/game/tag_v3). "v2": quay về khung tên dưới chân đang dùng (src/assets/game/ban3/tag).
    tagStyle: "v3",
    titleTop: 100, titleWidth: 440,
    statusTop: 252,
    bannerTop: 318
  };
  var SEATS = LAYOUT.podium.length + LAYOUT.rows.reduce(function (n, r) { return n + r.count; }, 0);

  // Chức vị trong môn phái theo THỨ HẠNG tu vi trong buổi live. Người tu vi cao nhất làm Chưởng môn.
  var SECT_RANKS = [
    { id: "chuong_mon", name: "Chưởng môn", fromRank: 1, toRank: 1 },
    { id: "ho_phap", name: "Hộ pháp", fromRank: 2, toRank: 3 },          // Tả hộ pháp (#2), Hữu hộ pháp (#3)
    { id: "truong_lao", name: "Trưởng lão", fromRank: 4, toRank: 13 },    // hàng 1
    { id: "chan_truyen", name: "Chân truyền đệ tử", fromRank: 14, toRank: 33 }, // hàng 2-3
    { id: "noi_mon", name: "Nội môn đệ tử", fromRank: 34, toRank: SEATS }      // hàng 4-5
  ];

  return {
    realms: REALMS,
    sectRanks: SECT_RANKS,
    layout: LAYOUT,
    capacity: SEATS,                // tối đa 53 người tham gia cùng lúc, ai cũng có chỗ trên màn hình
    idleMs: 120000,                 // im lặng 2 phút (không tim / quà / comment) thì bắt đầu mờ (anh chốt 10/11)
    fadeMs: 3000,                   // mờ dần tới 50% trong 3 giây rồi rời chỗ
    fullNoticeCooldownMs: 30000,    // cùng 1 người bị báo "hết chỗ" tối đa 1 lần / 30 giây
    seatAnnounceMinTuvi: 1000,      // dưới mức này thì đổi ghế top không đọc (tránh ồn lúc đầu live)

    duel: {
      enabled: true,
      everyMs: 10 * 60 * 1000,      // chu kỳ 10 phút: 7' tu luyện + 3' tỉ thí (anh chốt 10/11). Trận ở phút 7, 17, 27
      durationMs: 3 * 60 * 1000,
      resultMs: 12 * 1000,          // thời gian hiện kết quả
      // Điểm phe = tu vi mà thành viên phe nhận từ action "tu_luyen" trong lúc đấu (tức là theo mapping tim/quà của streamer).
      sides: [
        { id: 1, name: "Thanh Liên" },  // comment "1" (map trong TikTool -> action "vao_phe", side 1)
        { id: 2, name: "Hồng Liên" }    // comment "2"
      ],
      winBonus: 300,                // tu vi cho MỖI người phe thắng. Phe thua không bị trừ (Q5)
      mvpMultiplier: 2,             // công đầu phe thắng: được thêm đúng số tu vi đã góp trong trận (x2), thay cho +300 (anh chốt 11/11)
      drawBonus: 100,
      mvpWheel: true,               // bốc thăm 1 lượt quay trong phe thắng, công đầu có hai phần
      // Mỗi lần góp tu vi khi tỉ thí = 1 đòn sang 1 người ngẫu nhiên phe kia; người trúng mất hitLossRatio x tu vi của đòn (anh chốt 11/11).
      // Điểm phe không bị trừ (điểm = tu vi đã góp).
      hitLossRatio: 0.5,
      // Người vào phe: nhỏ lại (0,82) khi chưa gây sát thương, góp càng nhiều càng to dần: [tu vi đã góp, tỉ lệ], nội suy giữa các mốc.
      // Tối đa 1,6 để người to không đè kín người bên cạnh (anh chốt 13/11, trước là 1 -> 2,6).
      grow: [[0, 0.82], [300, 0.9], [1000, 1], [3000, 1.15], [8000, 1.3], [20000, 1.45], [50000, 1.6]],
      // Chỉ là cỡ VẼ (anh chốt 15/11): người đã vào phe vẽ bằng 0,8 lần cỡ tính từ bảng grow (0,82 -> 0,656; 1,6 -> 1,28).
      // Các mốc đóng góp, điểm và luật trận không đổi.
      drawScale: 0.8,
      strikePeak: 1.18              // đỉnh phóng khi ra đòn trong tỉ thí, so với cỡ tỉ thí mới (cho phép 1,15–1,2)
    },

    // Quái rồng (anh chốt 10/11): cứ 30 phút (tính từ đầu chu kỳ) rồng xuất hiện, đánh tới khi rồng chết mới tính lại chu kỳ.
    // Trong lúc đánh quái: không có tỉ thí. Sát thương = tu vi người chơi kiếm được trong lúc đánh (tim / quà theo mapping).
    boss: {
      enabled: true,
      everyMs: 30 * 60 * 1000,      // phút thứ 30 của chu kỳ (sau 3 lượt tỉ thí 7+3)
      hp: 30000,                    // máu rồng (chỉnh trong Cài đặt)
      name: "Thanh Long",           // tên hiện trên thanh máu (rồng xanh dragonBoss.png; chờ anh duyệt)
      mvpBonus: 2000,               // người gây sát thương nhiều nhất: + tu vi + 1 lượt vòng quay
      resultMs: 12 * 1000,          // thời gian hiện kết quả sau khi rồng chết
      // Rồng đánh người chơi (anh chốt 11/11): mỗi 5 giây 1 đòn thường trúng 1 người ngẫu nhiên -50 tu vi;
      // cứ đòn thứ 5 thì phun lửa trúng tất cả người đang ngồi, mỗi người -50 tu vi. Không trừ xuống dưới 0.
      attackEveryMs: 5000,
      attackDamage: 50,
      fireEvery: 5,
      fireDamage: 50
    },

    // Tẩu hỏa nhập ma (anh chốt 10/11): nhận >= thresholdTuvi tu vi (tim/quà) trong windowMs thì bị tẩu hỏa.
    // Máu giải cứu dùng chung ô "Máu" với rồng (boss.hp). Không giới hạn thời gian.
    possession: {
      enabled: true,
      thresholdTuvi: 5000,
      windowMs: 60 * 1000,
      resultMs: 10 * 1000,
      rewardMultiplier: 2          // cứu thành công: nhận thêm (x2 - 1) = đúng số tu vi đã góp
    },

    // Hào quang sau lưng khi đang tu luyện (anh chốt 12/11): được cộng tu vi thì sáng, 1 phút 30 giây không được cộng thì tắt.
    // (Im lặng idleMs = 2 phút thì mờ rồi rời chỗ như cũ.)
    aura: { ms: 90 * 1000 },

    // Pháp bảo (anh chốt 12/11): tặng quà = mua pháp bảo = cộng tu vi + hiệu ứng riêng. Số tu vi đặt trong mapping TikTool.
    // fx = kiểu hiệu ứng trong render.js (shake = đan dược rung lắc, sway = linh chi đung đưa như sóng, fall, pour, scroll, swords, orb, egg); img = ảnh trong src/assets/game/; speak = món đắt, đọc với ưu tiên cao hơn (mọi món đều được đọc).
    // tier = độ hoành tráng (món càng đắt hiệu ứng càng to, càng lâu): 1 = 1,6 giây, 2 = 2,2 giây, 3 = 3 giây, 4 = 5 giây.
    items: [
      { id: "linh_thach", tier: 1, name: "Linh Thạch", img: "item_linh_thach", fx: "fall", color: "#5fd0ff", speak: false },
      { id: "tu_khi_dan", tier: 1, name: "Tụ Khí Đan", img: "item_tu_khi_dan", fx: "shake", color: "#ffb347", speak: false },
      { id: "chu_qua", tier: 1, name: "Chu Quả", img: "item_chu_qua", fx: "fall", color: "#ff5a4a", speak: false },
      { id: "ban_dao", tier: 2, name: "Bàn Đào", img: "item_ban_dao", fx: "fall", color: "#ff9ac2", speak: false },
      { id: "linh_chi", tier: 2, name: "Thiên Niên Linh Chi", img: "item_linh_chi", fx: "sway", color: "#8fe36b", speak: false },
      { id: "ngoc_dich", tier: 3, name: "Ngọc Dịch Quỳnh Tương", img: "item_ngoc_dich", fx: "pour", color: "#7fd8ff", speak: true },
      { id: "bi_tich", tier: 3, name: "Bí Tịch Công Pháp", img: "item_bi_tich", fx: "scroll", color: "#ffd75e", speak: true },
      { id: "phi_kiem", tier: 3, name: "Phi Kiếm Trận", img: "fx_sword", fx: "swords", color: "#e8f4ff", speak: true },
      { id: "long_chau", tier: 4, name: "Long Châu", img: "item_long_chau", fx: "orb", color: "#fff0b0", speak: true },
      { id: "linh_thu_dan", tier: 3, name: "Linh Thú Đản", img: "egg", fx: "egg", color: "#ffe28a", speak: true }
    ],

    // Linh thú (anh chốt 12/11): trứng nở ngẫu nhiên theo weight. Mỗi loại tối đa 1 con, nở trùng thì +1 sao (5% / 10% / 15%).
    // Đã 3 sao mà nở trùng: được 1 lượt vòng quay. Linh thú reset cùng tu vi (mỗi 2 tuần).
    pets: {
      types: [
        { id: "ngoc_tho", name: "Ngọc Thố", img: "pet_ngoc_tho", tuviPct: 5, dmgPct: 0, weight: 45 },
        { id: "hoa_ky_lan", name: "Hỏa Kỳ Lân", img: "pet_hoa_ky_lan", tuviPct: 0, dmgPct: 5, weight: 40 },
        { id: "kim_phuong", name: "Kim Phượng", img: "pet_kim_phuong", tuviPct: 5, dmgPct: 5, weight: 15 }
      ],
      maxStars: 3,
      hatchMs: 3200,               // trứng rơi, rung, nứt rồi mới nở
      maxEggsPerEvent: 10          // tặng combo nhiều trứng: tối đa 10 lần nở mỗi sự kiện
    },

    // Thiên kiếp (anh chốt 12/11): lên đại cảnh giới từ Nguyên Anh trở lên. Đồng môn góp tu vi "hộ pháp" để dựng kết giới.
    tribulation: {
      enabled: true,
      fromRealm: 3,                // chỉ số trong realms: 3 = Nguyên Anh
      durationMs: 30000,           // 30 giây cho người xem kịp hộ pháp (anh chốt 14/11, trước 15 giây)
      strikes: { 3: 6, 4: 7, 5: 7, 6: 8, 7: 8, 8: 9, 9: 9 },   // số đạo sét trong 30 giây; mỗi đạo diễn khoảng 3 giây (nhiều tia nối nhau)
      shieldPct: 5,                // kết giới cần = 5% ngưỡng cảnh giới mới...
      shieldMax: 30000,            // ...tối đa 30.000 tu vi
      successBonusPct: 5,          // vượt kiếp: +5% tu vi đang có
      helperBonusPct: 50,          // thành công: mỗi người hộ pháp +50% số tu vi đã góp
      topHelperBonus: 1000,        // ...người hộ pháp nhiều nhất thêm +1.000
      selfShieldPct: 50,           // người độ kiếp tự tu luyện / dùng pháp bảo: 50% tu vi nhận được vào kết giới (tự hộ thân)
      retryMs: 3 * 60 * 1000,      // thất bại: kẹt ở Đại viên mãn, độ kiếp lại sau 3 phút...
      retryTuviPct: 10,            // ...hoặc khi tu vi tăng thêm 10% (cái nào tới trước)
      maxQueue: 2,
      resultMs: 4000
    },

    // Linh Khí Triều Dâng (anh chốt 12/11): ngẫu nhiên 15-25 phút 1 lần, 60 giây nhân đôi tu vi mọi người nhận từ tim / quà.
    surge: {
      enabled: true,
      minEveryMs: 15 * 60 * 1000,
      maxEveryMs: 25 * 60 * 1000,
      durationMs: 60 * 1000,
      multiplier: 2
    },

    // Vòng quay thưởng: công đầu đánh quái + 1 người bốc thăm trong phe thắng tỉ thí (công đầu 2 phần) + linh thú nở trùng khi đủ sao. weight = tỉ lệ (ô to = dễ trúng).
    wheel: {
      spinMs: 5000,
      prizes: [
        { tuvi: 200, weight: 30 },
        { tuvi: 500, weight: 25 },
        { tuvi: 1000, weight: 20 },
        { tuvi: 2000, weight: 13 },
        { tuvi: 5000, weight: 9 },
        { tuvi: 10000, weight: 3 }
      ]
    },

    // Âm thanh hiệu ứng (file trong src/assets/game/, chép từ raw bằng tools/build-assets.js).
    // Hiệu ứng mở rộng (ảnh src/assets/game/vfx). enabled: false = quay về hiệu ứng cũ hoàn toàn.
    // Ngân sách tính RIÊNG cho lớp mới (lớp cũ vẫn do trimFx giới hạn): quá ngân sách thì dùng vệt sáng cũ, kết quả vẫn hiện đủ.
    vfx: { enabled: true, maxElements: 64, maxSequences: 4, maxAmbient: 2, maxParticles: 24, maxHighlights: 1 },

    sounds: {
      levelUp: "magic-fairy.mp3",   // phát mỗi lần nhân vật thăng cấp (tiểu cảnh và đại cảnh giới)
      volume: 0.8
    },

    voice: {
      enabled: true,
      lang: "vi-VN",
      rate: 1.05,
      volume: 1,
      maxQueue: 4,
      maxWaitMs: 10000,             // câu thường chờ quá 10 giây thì bỏ (mỗi câu giọng AI đọc mất 3–5 giây)
      maxWaitImportantMs: 12000,    // câu tỉ thí / lên ghế được chờ tới 12 giây
      infoEverySec: 15,             // lời nền: đọc sau khi giọng và hàng chờ đã rảnh đủ 15 giây; 0 = chỉ hiện chữ
      itemBatchMs: 4000,            // gom các lần dùng pháp bảo của cùng người + cùng món trong 4 giây rồi đọc 1 câu
      itemBatchMax: 8,              // tối đa 8 lô đang gom; quá thì chốt lô cũ nhất sớm
      announceStageUps: true,       // false = chỉ đọc khi lên ĐẠI cảnh giới
      joinBatchMs: 10000,           // gom người mới nhập môn rồi đọc 1 câu
      sideJoinBatchMs: 3000         // gom người vừa vào phe tỉ thí (3 giây) rồi đọc 1 câu
    },

    // Tu vi + linh thú giữ periodWeeks tuần (anh chốt 13/11: 2 tuần), reset 0h thứ Hai đầu kỳ mới (giờ máy).
    // Đổi số này thì sửa cả "periodWeeks" trong manifest.json (hub dùng để xuất top kỳ này / kỳ trước).
    save: { key: "ngami.tutien.week", everyMs: 5000, periodWeeks: 2 }
  };
}));
