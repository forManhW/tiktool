const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { gameSfxUrl, gameSfxGain } = require("../src/sound");

test("mỗi pháp bảo có âm riêng và file game khớp bản gốc", () => {
  const items = {
    linh_thach: "linh_thach.mp3", tu_khi_dan: "tu_khi_dan.mp3", chu_qua: "chu_qua.mp3",
    ban_dao: "ban_dao.mp3", linh_chi: "thanh_nien_linh_chi.mp3",
    ngoc_dich: "ngoc_dich_quynh_tuong.mp3", bi_tich: "bi_tich_cong_phap.mp3",
    phi_kiem: "phi_kiem_tran.mp3", long_chau: "ngoc_rong.mp3"
  };
  for (const [id, filename] of Object.entries(items)) {
    assert.equal(gameSfxGain("item_" + id), 6);
    assert.equal(gameSfxUrl("item_" + id, 1), "src/assets/game/" + filename);
    assert.equal(gameSfxUrl("item_" + id, 2), "src/assets/game/" + filename);
    assert.deepEqual(fs.readFileSync(path.join(__dirname, "../src/assets/game", filename)),
      fs.readFileSync(path.join(__dirname, "../src/assets/raw", filename)));
  }
  for (const sound of ["kiem", "va_cham", "trung", "ti_thi"]) {
    for (const variant of [1, 2]) assert.ok(fs.existsSync(path.join(__dirname, "..", gameSfxUrl(sound, variant))));
  }
  for (const sound of ["kiem", "va_cham", "trung", "ti_thi"]) assert.equal(gameSfxGain(sound), 1);
  assert.equal(gameSfxUrl("unknown", 1), null);
});
