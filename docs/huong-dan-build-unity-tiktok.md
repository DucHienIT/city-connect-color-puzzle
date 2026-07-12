# Hướng dẫn Build Game Unity lên TikTok Mini Games

## Giai đoạn 1: Chuẩn bị tài khoản & app

1. Tạo tài khoản Developer tại `https://developers.tiktok.com/`
2. Đăng ký app tại `https://developers.tiktok.com/apps/` → lấy **Client Key**
3. Vào app vừa tạo → **Add products → Mini Games** (bắt buộc, nếu không sẽ không thấy các mục cấu hình/tài nguyên dành riêng cho Mini Games)
4. Thêm tài khoản TikTok test của bạn vào mục **Test Users** của app (tài khoản phải là tài khoản người lớn, không phải tài khoản trẻ em)

## Giai đoạn 2: Cấu hình project Unity

5. Kiểm tra **Unity version** thuộc khoảng 2021–2022 (ví dụ 2022.3.62f3 dùng được)
6. Đổi **Scripting Backend** sang **IL2CPP** (không dùng Mono/JIT) — vào `Project Settings → Player → Other Settings → Scripting Backend`
7. Import TikTok Unity plugin vào project (nếu chưa có link tải, kiểm tra mục Resources trên app page hoặc hỏi Support của TikTok, vì phần này không có link công khai rõ ràng trong docs)
8. Gọi API khởi tạo SDK đúng chỗ trong code:

```csharp
TT.InitSDK((code, env) => {
    if (code == 0) {
        Debug.Log("SDK initialized successfully");
        // Chỉ gọi các API khác SAU dòng này
    }
});
```

## Giai đoạn 3: Build ra WebGL

9. `File → Build Settings → chọn WebGL → Build`
10. Đặt tên thư mục output là **`tt-minigame`** (tên thư mục chuẩn TikTok yêu cầu cho Unity build, không phải tên tùy ý)
11. Kiểm tra dung lượng: tổng package Unity phải **≤ 60 MB** (chú ý file `.data` và `.wasm` thường nặng nhất)
12. Nếu DevTool báo `wasmFuncCount >= 80000` → chạy quy trình **Wasm splitting** để tối ưu tốc độ khởi động

## Giai đoạn 4: Cấu hình project như một Mini Game

13. Cài CLI:

```bash
npm install @ttmg/cli -g --registry=https://registry.npmjs.org/
```

14. `cd` vào đúng thư mục `tt-minigame` (thư mục chứa `index.html`, không phải thư mục Unity gốc)
15. Chạy `ttmg init` → sinh file `game.json`, `project.config.json`, đồng bộ `minigame.config.json`
16. Kiểm tra `project.config.json` có đúng `appid` = Client Key chưa
17. Nếu game nặng, khai `subpackages` trong `game.json` (chú ý viết thường toàn bộ chữ, không viết hoa chữ P)

## Giai đoạn 5: Test / Debug

18. Chạy `ttmg login` (bắt buộc nếu cần upload code hoặc dùng Unity Wasm subpackaging)
19. Chạy `ttmg dev` ngay tại thư mục `tt-minigame`
20. Quét QR code hiện ra bằng app TikTok (điện thoại và máy tính cùng mạng wifi)
21. Kiểm tra domain: mọi domain game gọi tới (API, ảnh...) phải được thêm vào **domain whitelist** trên Developer Portal, nếu không sẽ bị chặn khi lên Preview/Production dù local chạy được

## Giai đoạn 6: Upload & Publish

22. Dùng CLI upload code package lên Developer Portal
23. Vào tab **Code version → Preview** → tạo QR code preview cho người khác test
24. Cấu hình domain whitelist, Terms of Service, Privacy Policy trong phần Basic Settings
25. Với game (khác app thường), có thể cần qua **Industry Qualification Review** trước khi submit
26. Submit để TikTok review → sau khi duyệt → game xuất hiện công khai trên TikTok, theo dõi hiệu suất qua Analytics Dashboard

---

## Ghi chú quan trọng

- **Lỗi "not a Mini Game project entry directory"**: đảm bảo bạn đang chạy lệnh CLI ngay tại thư mục chứa `index.html`, `game.json`, `project.config.json`, `minigame.config.json` — không phải thư mục Unity gốc.
- File `minis.manifest.json` là dấu hiệu bạn chạy nhầm workflow của **TikTok Minis** (web app thường) thay vì **Mini Games**. Hai loại này có bộ file cấu hình khác nhau hoàn toàn.
- Field cấu hình subpackage trong `game.json` phải là `subpackages` (viết thường toàn bộ), không phải `subPackages`.
