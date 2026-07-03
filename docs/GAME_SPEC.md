# City Connect – Color Puzzle (Unity Clone Spec)

> File đặc tả này dùng để đưa cho **Claude Code CLI** trong thư mục dự án Unity, để Claude Code tự đọc và triển khai từng phần theo checklist bên dưới.
> Đây là bản clone dựa trên **thể loại gameplay** (dạng "Flow Free" / nối các điểm cùng màu, chủ đề thành phố), không sao chép asset, tên thương hiệu, logo hay mã nguồn của bất kỳ app cụ thể nào. Hãy tự tạo asset gốc (hoặc dùng asset free/CC0) và đặt tên riêng cho sản phẩm.

---

## 1. Tổng quan game

- **Thể loại**: Puzzle giải đố, kéo/vẽ đường nối các **nhà** về **thành phố** cùng màu trên lưới ô vuông.
- **Chủ đề hình ảnh**: Mỗi màu có 1 **thành phố** (tòa nhà lớn, trên nóc hiển thị số pin = số nhà cần nối) và các **nhà** nhỏ (có xe đậu trên nóc). Mỗi nhà kéo một con đường riêng về thành phố; khi nối xong, chiếc xe của nhà chạy dọc đường vào thành phố.
- **Mục tiêu mỗi màn**:
  1. Mỗi thành phố yêu cầu 1→n nhà (n = số pin); nối đủ mọi nhà của mọi thành phố là thắng.
  2. Mỗi nhà = một nhánh đường riêng; các nhánh không được cắt/chồng lên nhau (kể cả nhánh cùng màu) — chúng chỉ gặp nhau tại ô thành phố.
  3. **Không** cần lấp đầy lưới; ô trống là bình thường.
- **Vòng lặp gameplay**: Chọn màn → giải đố bằng thao tác kéo (drag) → hoàn thành → nhận sao (1–3 sao dựa trên số nước đi/độ phủ kín) → mở khóa màn tiếp theo.

---

## 2. Core Mechanics chi tiết

### 2.1 Lưới, Thành phố & Nhà
- Bản đồ là lưới `N x M` (kích thước thay đổi theo độ khó, ví dụ 5x5 → 14x14).
- Mỗi màu có **1 thành phố + 1→4 nhà** đặt sẵn trên lưới. Nóc thành phố hiển thị số pin = số nhà nó yêu cầu; thành phố tối đa 4 nhà (4 hướng vào).
- Người chơi chạm vào 1 **nhà** (không chạm được thành phố) và kéo (drag) qua các ô liền kề (ngang/dọc, không chéo) để vẽ nhánh đường về thành phố cùng màu.

### 2.2 Luật vẽ đường
- Nhánh đường chỉ được đi qua ô trống hoặc đi ngược lại để "xóa" phần đường vừa vẽ.
- Mọi đường/nhà/thành phố khác đều chặn nhánh (kể cả nhánh khác cùng màu); chỉ ô **thành phố của chính màu** là điểm kết thúc hợp lệ.
- Nhánh chạm vào thành phố cùng màu → nhà đó được nối (xe chạy vào thành phố).
- Kéo lại lên chính nhánh của mình → nhánh tự rút ngắn tới điểm đó (giống Flow Free); chạm vào một ô đường có sẵn để tiếp tục vẽ từ đó.
- Hoàn thành màn khi: **mọi thành phố đã đủ số nhà yêu cầu** (không yêu cầu phủ kín lưới).

### 2.3 Hệ thống gợi ý (Hint)
- Người chơi bấm nút Hint → hệ thống tự động vẽ 1 bước đi đúng tiếp theo (dùng thuật toán giải từ trước, xem mục 4).
- Giới hạn số hint miễn phí/ngày, có thể xem quảng cáo hoặc dùng tiền trong game để mua thêm.

### 2.4 Tính sao & đánh giá
- 1 sao: hoàn thành đúng luật.
- 2 sao: hoàn thành trong số nước đi tối ưu + một khoảng dung sai.
- 3 sao: hoàn thành với số nước đi tối ưu (hoặc phủ kín 100% lưới, tùy thiết kế).

---

## 3. Cấu trúc dữ liệu màn chơi (Level Data)

Dùng file JSON hoặc ScriptableObject để định nghĩa từng màn:

```json
{
  "levelId": 1,
  "gridWidth": 7,
  "gridHeight": 7,
  "groups": [
    { "color": "red",    "city": {"x":4,"y":2}, "houses": [ {"x":0,"y":0}, {"x":2,"y":4} ] },
    { "color": "blue",   "city": {"x":1,"y":5}, "houses": [ {"x":6,"y":6} ] },
    { "color": "yellow", "city": {"x":2,"y":1}, "houses": [ {"x":5,"y":0}, {"x":0,"y":3}, {"x":6,"y":2} ] }
  ],
  "obstacles": [ {"x":3,"y":3} ]
}
```

- `groups`: mỗi màu = 1 `city` + danh sách `houses` (số nhà = demand hiển thị bằng pin trên nóc thành phố, tối đa 4).
- `obstacles`: các ô bị chặn, không thể đi qua (tùy chọn, tăng độ khó).
- Nên tổ chức các level theo `LevelPack` (ví dụ mỗi pack 20-50 màn, theo chủ đề thành phố khác nhau: phố cổ, khu công nghiệp, ven biển...).

---

## 4. Thuật toán sinh & giải màn (Level Generator / Solver)

Claude Code cần triển khai 2 công cụ Editor riêng (Unity Editor Tool hoặc script chạy ngoài game):

1. **Solver (bắt buộc)**: Thuật toán backtracking/DFS định tuyến từng nhánh nhà→thành phố (các nhánh chặn lẫn nhau, ô thành phố dùng chung làm điểm kết thúc), kiểm tra màn có lời giải hợp lệ hay không. Dùng để:
   - Validate level do người thiết kế/generator tạo ra.
   - Cung cấp dữ liệu cho hệ thống Hint trong game (mỗi hint = vẽ trọn 1 nhánh).
2. **Generator (tùy chọn, nên có)**: "Trồng" lần lượt các thành phố lên ô trống rồi mọc 1–4 nhánh random-walk không giao nhau từ mỗi thành phố; đầu mút mỗi nhánh là 1 nhà — màn hợp lệ theo cách dựng (walk chính là một lời giải). Solver kiểm tra lại lần cuối. (Không yêu cầu lời giải duy nhất — với luật không phủ kín, điều đó gần như không tồn tại.)

---

## 5. Kiến trúc Unity đề xuất

```
Assets/
  _Project/
    Scripts/
      Core/
        GridModel.cs          # Dữ liệu lưới, ô, trạng thái
        PathManager.cs        # Quản lý các đường nối đang vẽ
        LevelData.cs          # ScriptableObject / class parse JSON
        LevelLoader.cs        # Load level từ JSON/Resources hoặc Addressables
        Solver.cs             # Thuật toán giải/kiểm tra
        LevelGenerator.cs     # (Editor tool) sinh level tự động
      Gameplay/
        GridView.cs           # Render lưới, spawn ô
        NodeView.cs           # Hiển thị 1 tòa nhà/node
        PathDrawer.cs         # Xử lý input kéo/vẽ, vẽ line renderer
        InputController.cs    # Bắt sự kiện touch/mouse
        WinChecker.cs         # Kiểm tra điều kiện thắng
        HintController.cs     # Xử lý logic gợi ý
      UI/
        LevelSelectUI.cs
        GameplayHUD.cs
        WinPopup.cs
        SettingsMenu.cs
        StarRatingUI.cs
      Systems/
        SaveSystem.cs         # Lưu tiến trình (PlayerPrefs hoặc file JSON)
        AudioManager.cs
        AnalyticsManager.cs   # (tùy chọn)
        IAPManager.cs         # (tùy chọn, mua vật phẩm/gỡ quảng cáo)
        AdsManager.cs         # (tùy chọn)
      Utils/
        GridUtils.cs
        ColorPalette.cs
    Prefabs/
      Node.prefab
      GridCell.prefab
      PathSegment.prefab
      UI/...
    ScriptableObjects/
      LevelPacks/
      Themes/
    Art/
      Buildings/
      Backgrounds/
      UI/
    Audio/
    Resources/
      Levels/ (JSON files hoặc TextAsset)
```

### Công nghệ/gói đề xuất
- Unity 2022 LTS trở lên, render pipeline URP (2D).
- Input System package (mới) hoặc Input Manager cũ – chọn 1, ghi rõ trong README.
- DOTween (hoặc Unity Tween tự viết) cho animation UI/path.
- TextMeshPro cho UI text.
- (Tùy chọn) Addressables nếu số lượng level lớn cần tải động.

---

## 6. Danh sách màn hình (Screens)

1. **Splash / Loading**
2. **Main Menu** – Chơi tiếp, Chọn màn, Cài đặt, Cửa hàng (nếu có IAP)
3. **Level Select** – Danh sách theo Pack/Chương, hiển thị số sao đã đạt
4. **Gameplay Screen** – Lưới chơi, HUD (số move, timer nếu có, nút Hint/Reset/Undo)
5. **Win Popup** – Hiển thị số sao, nút Next Level / Replay / Về menu
6. **Settings** – Âm thanh, nhạc nền, ngôn ngữ, khôi phục giao dịch (nếu có IAP)

---

## 7. Checklist triển khai (đưa cho Claude Code làm theo thứ tự)

- [x] 1. Khởi tạo project Unity 2D (URP), cấu trúc thư mục như mục 5.
- [x] 2. Viết `GridModel`, `LevelData`, `LevelLoader` — load được 1 level mẫu từ JSON.
- [x] 3. Viết `GridView`, `NodeView` — render lưới và node theo dữ liệu level.
- [x] 4. Viết `InputController` + `PathDrawer` — cho phép kéo để vẽ đường nối giữa 2 node cùng màu, tuân thủ luật ở mục 2.2.
- [x] 5. Viết `WinChecker` — phát hiện khi hoàn thành màn (tất cả cặp nối + phủ kín nếu yêu cầu).
- [x] 6. Viết `WinPopup` UI cơ bản + chuyển màn tiếp theo.
- [x] 7. Viết `Solver.cs` (thuật toán backtracking) để kiểm tra lời giải & phục vụ Hint.
- [x] 8. Viết `HintController` dùng Solver để gợi ý bước tiếp theo.
- [x] 9. Viết `LevelSelectUI` hiển thị danh sách level + số sao đã đạt (dùng `SaveSystem`).
- [x] 10. Viết `SaveSystem` lưu tiến trình (PlayerPrefs hoặc JSON file trong `Application.persistentDataPath`).
- [x] 11. Tạo tối thiểu 20 level mẫu (JSON) tăng dần độ khó (5x5 → 9x9), dùng `LevelGenerator`/tự tay tạo rồi validate bằng `Solver`.
- [x] 12. Thêm âm thanh cơ bản (SFX vẽ đường, SFX thắng màn, nhạc nền) qua `AudioManager`.
- [x] 13. Polish UI/UX: animation khi hoàn thành đường, hiệu ứng sao, theme màu thành phố (nhà cao tầng, công viên...). *(bản đầu: node "tòa nhà" procedural, DOTween pulse/star pop; art thật có thể thay sau)*
- [ ] 14. (Tùy chọn) Tích hợp quảng cáo thưởng (rewarded ads) để đổi thêm Hint.
- [ ] 15. (Tùy chọn) Tích hợp IAP gỡ quảng cáo / mở khóa gói level.
- [ ] 16. Build thử trên Android/iOS, kiểm tra hiệu năng và UX cảm ứng.

---

## 8. Ghi chú pháp lý & bản quyền

- Không copy tên "City Connect Color Puzzle", logo, icon, hình ảnh, âm thanh gốc của app tham khảo.
- Không copy mã nguồn của bất kỳ app nào khác.
- Cơ chế "nối điểm cùng màu không giao nhau" là dạng gameplay phổ biến (nổi tiếng nhất là *Flow Free*) và không thuộc quyền sở hữu độc quyền của riêng app nào — nhưng vẫn nên tạo giao diện, tên gọi, bộ nhận diện thương hiệu, artwork hoàn toàn mới của riêng bạn để tránh nhầm lẫn/tranh chấp thương hiệu khi phát hành.

---

## 9. Gợi ý prompt để chạy với Claude Code CLI

Sau khi đặt file này vào thư mục gốc project (ví dụ `docs/GAME_SPEC.md`), bạn có thể chạy Claude Code và yêu cầu, ví dụ:

```
Đọc file docs/GAME_SPEC.md và bắt đầu triển khai theo checklist mục 7,
làm từng bước một, bắt đầu từ bước 1. Sau mỗi bước hãy dừng lại để tôi review.
```

hoặc yêu cầu làm từng phần cụ thể:

```
Dựa vào docs/GAME_SPEC.md, hãy viết GridModel.cs, LevelData.cs và LevelLoader.cs
theo đúng kiến trúc ở mục 5, kèm 1 level JSON mẫu để test.
```
