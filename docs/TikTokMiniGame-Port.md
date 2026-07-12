# TikTok Mini Game — Tài liệu port (branch `developTikTok`)

Tài liệu tổng hợp từ docs chính thức của TikTok (07/2026) và phân tích gap so với hiện trạng project (nhánh gốc `developTV` — Unity WebGL cho Samsung Tizen). Đây là handbook cho việc đưa coin-pusher lên nền tảng **TikTok Mini Games**.

Nguồn:
- [Mini Games Overview](https://developers.tiktok.com/doc/mini-games-overview)
- [Technical Overview](https://developers.tiktok.com/doc/mini-games-technical-overview)
- [Integration Workflow](https://developers.tiktok.com/doc/mini-games-integration-workflow)
- [Build Mini Games with Unity](https://developers.tiktok.com/doc/build-mini-games-with-unity)
- [Mini Games SDK Overview](https://developers.tiktok.com/doc/mini-games-sdk-overview)

---

## 1. TikTok Mini Games là gì

- Game **chơi ngay trong app TikTok**, không cần cài đặt, chạy trên cả Android & iOS.
- Phân phối qua: search, Minis Center, "Recently Played" sidebar, video ngắn nhúng game, đề xuất thuật toán, share bạn bè/DM.
- Thị trường đã mở: US, Japan, Indonesia, Turkey, Saudi Arabia, Thailand, Brazil, Malaysia, Philippines, **Vietnam**.
- Kiếm tiền qua 2 kênh:
  - **IAA** — rewarded video + interstitial của TikTok.
  - **IAP** — vật phẩm ảo qua hệ thanh toán TikTok (BEANS balance / recharge / pay).
- Có dashboard analytics riêng của platform (retention, eCPM, ROI).

## 2. Kiến trúc runtime (phía TikTok)

4 tầng, game chạy trong **native runtime** của client TikTok (không phải webview thuần cho Unity):

```
Game Layer            ← code của mình: C# → IL2CPP → WebAssembly
Platform Capability   ← login, ads, pay, storage, share, lifecycle
Native Runtime        ← rendering, audio, tài nguyên hệ thống
Client Host (TikTok)  ← entry, container, account, phân phối
```

Điểm mấu chốt cho Unity:

| Hạng mục | Yêu cầu |
|---|---|
| Unity version | **2021–2022** (project đang dùng 2022.3.62f3 → hợp lệ) |
| Scripting backend | **IL2CPP → WebAssembly** (Mono/JIT không hỗ trợ) |
| Client TikTok tối thiểu | **>= 43.1.0** (Android & iOS) |
| Tổng package | **≤ 60 MB** (Unity; non-Unity là 30 MB) — chú ý file `data` và `wasm` |
| Subpackage | Build bật **bulk memory** thì CHƯA dùng được subpackage |
| Wasm | ≥ 80.000 functions sẽ bị cảnh báo, cần code splitting (wasm-split toolchain) |
| Không chấp nhận | WebGL build "thô" chưa adapt, native app package |

Cấu trúc output Unity sau khi build qua SDK (folder `tt-minigame/`):

```
tt-minigame/
├── data-package/        # resources
├── images/
├── wasmcode/            # wasm binaries
├── game.js              # entry
├── game.json            # config (subpackages khai ở đây)
├── plugin.js / plugin-config.js
├── unity-namespace.js
├── webgl-wasm-split.js
└── webgl.framework.js
```

## 3. TikTok SDK for Unity

- SDK dạng `.unitypackage`, **lấy qua operations representative của TikTok** (không public download): `com.tiktok.minigame@1.0.1-Release.unitypackage` (bản khuyến nghị — fix lỗi load AssetBundle).
- SDK là bridge C# → capability layer: gọi kiểu `TT.Login(...)` thay vì JS `TTMinis.game.*`.
- Build flow: **Import SDK → build bằng menu của SDK → lấy folder `tt-minigame/` → upload**.
- Upload: CLI `@ttmg/cli` (chạy `ttmg dev` trong folder `tt-minigame`) hoặc web uploader (upload nguyên folder).

### Nhóm API platform (tên JS — bản Unity có wrapper C# tương ứng)

| Nhóm | API chính | Ghi chú cho project |
|---|---|---|
| Login | `login()` (silent), `authorize()` | Bắt buộc tích hợp Silent Login |
| Ads | `createRewardedVideoAd()`, `createInterstitialAd()` | Thay chỗ AdsSettings cũ |
| IAP | `checkBalance()`, `recharge()`, `pay()` | Thay Samsung Checkout dự kiến của TV |
| Storage | `set/get/remove/clearStorage(Sync)` | Key-value — nơi map save PlayerPrefs |
| File | `readFile/writeFile/mkdir/unzip`, `env.USER_DATA_PATH` | |
| Lifecycle | `onShow/onHide`, `getLaunchOptionsSync()` | Hook Save() khi vào background |
| Network | `request()`, `connectSocket()` (WSS) | GA4 analytics phải đi qua đây |
| Share | `shareAppMessage`, `shareToStory` | Cơ hội growth loop |
| Retention | `addShortcut()`, `startEntranceMission()` + reward API | Nên tích hợp (doc liệt kê là "required integrations") |
| System | `getSystemInfoSync()`, `getNetworkType()` | |
| Media | `createInnerAudioContext()`, WebAudio | Unity tự lo qua runtime, không đụng trực tiếp |

Tài liệu con cần đọc tiếp khi vào việc: *TikTok SDK for Unity*, *Unity Packaging Optimization*, *Initialize SDK*, *Error Code Reference*, *Debug Your Mini Game*.

## 4. Quy trình tích hợp (8 phase)

1. **Planning** — chọn loại project: Unity (đã chọn).
2. **Đăng ký** — tạo Organization + tạo App trên Developer Portal → lấy app credentials.
3. **Compliance** — business verification (doanh nghiệp đăng ký hợp pháp), industry qualification cho publisher game; xin duyệt riêng nếu phát hành **US/EU**.
4. **Cấu hình app** — basic info, localization (game đã có I2 Localization → khai các ngôn ngữ), bật monetization (IAP/IAA).
5. **Develop & integrate** — tích hợp các capability bắt buộc: **Silent Login, In-App Ads, In-App Purchases, Home Screen Shortcut, Revisit From Profile** (+ Explicit Authorization nếu cần).
6. **Test** — debug bằng **TikTok DevTool** (local), upload code package, **preview trong app TikTok thật**, acceptance testing theo moderation guidelines.
7. **Publish** — submit version → chờ review → release.
8. **Vận hành** — dashboard analytics, revenue settlement, invoice/payout, xử lý user report.

> Việc admin (org, verification, xin SDK từ operations rep) nên khởi động **song song** với dev vì có thể mất thời gian chờ duyệt.

## 5. Gap analysis: `developTV` → `developTikTok`

Hiện trạng kế thừa từ TV port: WebGL build sẵn, mobile SDK (Chartboost/Admob/UnityAds/OpenIAB/Prime31) đã gỡ, UI đã reskin, save qua facade `Prefs`/`UserData`. Những việc phải làm:

### 5.1 Input — quay lại touch
- TV dùng remote D-pad qua **TVInputKit** (`TVInput.cs`, `TVUINavigator.cs`, `TizenRemoteBridge.cs`, `TizenRemote.jslib`). TikTok Mini Game chạy trên **điện thoại, touch**.
- Gameplay gốc (`GameScene.cs` raycast `TapArea`) vốn là tap-based → phần core dùng lại được.
- Việc cần làm: disable/gate TVInputKit + focus frame, bỏ jslib bắt key Tizen (10009), rà lại các popup điều hướng bằng D-pad để bấm chạm trực tiếp.

### 5.2 Build & size (rủi ro lớn nhất)
- Trần **60 MB** cho toàn package — cần audit ngay size build WebGL hiện tại (data + wasm).
- Build phải qua **SDK/toolchain của TikTok** (ra `tt-minigame/`), không dùng output WebGL thuần như Tizen `.wgt`. `BuildScript.cs` cần thêm đường build TikTok.
- Nếu chạm trần: dùng subpackage (lưu ý xung đột với bulk memory) + wasm-split; texture đã có nền tảng tốt (TV_UI_Atlas, TextureBuildSetEnforcer).
- Compression/template WebGL: theo hướng dẫn của SDK TikTok, không giữ nguyên setting Tizen.

### 5.3 Persistence
- TV đang dùng facade `Prefs` ghi **localStorage đồng bộ** (fix mất save khi tắt TV). Trên TikTok, storage chuẩn là `setStorage/getStorage` của platform.
- Cần verify PlayerPrefs của Unity map vào đâu trong runtime TikTok; nếu cần thì thêm backend TikTok cho facade `Prefs` — **không đổi tên key** (giữ guardrail migration).
- Hook `onShow/onHide` để gọi `Save()` — thay cho `OnApplicationPause/Quit` vốn không đáng tin trên WebGL.

### 5.4 Monetization
- Kế hoạch Samsung Checkout (TV) **không dùng** cho nhánh này.
- IAA: rewarded video (thưởng coin/gift) + interstitial → viết wrapper mới theo pattern `AdsSettings` cũ (một entry point, không gọi SDK rải rác).
- IAP: gói coin/vật phẩm qua `pay()`/`recharge()` — cần thiết kế danh mục hàng + server-side verify qua **TikTok Minis Server APIs** nếu cần.

### 5.5 Login & account
- Tích hợp **Silent Login** (bắt buộc). Hiện game hoàn toàn local-save; tối thiểu dùng login để định danh, cân nhắc đồng bộ save lên server sau.

### 5.6 Analytics
- Hệ GA4 MP hiện gửi qua HTTP (`Assets/_Analytics`). Network trong runtime TikTok đi qua `request()` và **domain phải được khai whitelist** trong app config — cần kiểm tra `google-analytics.com` có được chấp nhận không; nếu không, cân nhắc chuyển sang dashboard analytics của TikTok làm nguồn chính.

### 5.7 Retention hooks (yêu cầu tích hợp)
- `addShortcut()` + `getShortcutMissionReward()` — shortcut ra màn hình chính.
- `startEntranceMission()` + `getEntranceMissionReward()` — revisit từ profile.
- Gắn reward các mission này vào economy sẵn có (coin/gift) qua `Rewardmanager`.

### 5.8 Compliance & release
- Cần tài khoản Organization đã verify + industry qualification trước khi submit.
- Nếu nhắm US/EU phải xin approval riêng.
- Pass acceptance testing guidelines trước khi submit review.

## 6. Checklist việc cần làm (thứ tự đề xuất)

**Admin / song song:**
- [ ] Tạo Organization + App trên TikTok Developer Portal
- [ ] Business verification + industry qualification (+ US/EU nếu cần)
- [ ] Liên hệ operations rep xin `com.tiktok.minigame@1.0.1` unitypackage
- [ ] Cài `@ttmg/cli` + TikTok DevTool

**Kỹ thuật:**
- [x] Audit size build WebGL hiện tại so với trần 60 MB — build TV hiện tại ~36 MB (wasm 23.8 + data 11.8), còn ~24 MB headroom
- [ ] Import TikTok SDK, build thử ra `tt-minigame/`, chạy trên DevTool
- [x] Gate/tắt TVInputKit + Tizen bridge (`GamePlatform`) — *verify touch trong Editor/build còn pending*
- [x] Storage routing `Prefs` → `tt.set/getStorageSync` (fallback localStorage) + hook `onHide` → Save — *verify trên DevTool còn pending*
- [x] Silent Login (`TTLogin`, tự chạy lúc boot)
- [x] Wrapper Ads mới (`TTAds`, rewarded + interstitial) theo pattern AdsSettings — *chưa có call-site & ad unit id*
- [ ] IAP qua TikTok pay (thiết kế danh mục hàng)
- [ ] Shortcut + Entrance mission → cắm reward vào Rewardmanager
- [ ] Kiểm tra GA4 qua `request()`/domain whitelist, hoặc chuyển analytics platform
- [ ] Preview trong app TikTok thật + acceptance testing → submit review

## 8. Tiến độ kỹ thuật (07/2026)

Scaffold platform đã dựng xong trên branch `developTikTok`, chưa cần SDK trong tay:

| Mảng | File | Trạng thái |
|---|---|---|
| Platform gate | `Assets/_CoinPusher/Scripts/GamePlatform.cs` | `Target.TikTok` là hằng của branch; TVUINavigator / PopupNavigator / TizenRemoteBridge early-return trong `Bootstrap()` → stack TV không boot, gameplay rơi về đường tap gốc (`GameScene` raycast `TapArea`). |
| Bridge runtime | `Assets/_CoinPusher/Scripts/TikTok/TikTokBridge.cs` + `Assets/Plugins/WebGL/TikTokBridge.jslib` | Detect `tt`, đăng ký `tt.onHide` + `visibilitychange` → `PersistNow()`, pump kết quả ads/login qua **polling** (không dùng SendMessage vì TikTok bọc Unity instance trong namespace riêng), đảm bảo EventSystem cho touch. |
| Storage | `Assets/Plugins/WebGL/TizenStorage.jslib` | Route sang `tt.setStorageSync/getStorageSync/removeStorageSync/clearStorageSync` khi có `tt`, fallback localStorage. Tên extern + key save giữ nguyên → không cần migration. Caveat: `getStorageSync` trả `''` cho key vắng. |
| Ads | `Assets/_CoinPusher/Scripts/TikTok/TTAds.cs` | 1 entry-point (`ShowRewardedVideo(cb)` / `ShowInterstitial()`); Editor/browser stub auto-grant. Ad unit id đặt ở `GlobalConfig.TikTok` (đang rỗng — chờ Developer Portal). |
| Login | `Assets/_CoinPusher/Scripts/TikTok/TTLogin.cs` | Silent login lúc boot, giữ `Code`/`AnonymousCode` cho server-side sau này. |
| Build | `Assets/Editor/BuildScript.cs` | Menu `TikTok Build` → `Build/WebGL-TikTok` (uncompressed, no data caching) + audit size tự động so trần 60 MB. |

### UI portrait (07/2026 — đã làm, verify bằng screenshot trong Editor ở 1080×1920)

- **Khung 3D + 2 cột nút HUD đã hợp portrait sẵn** (canvas ref res vốn là 1080×1920, HUD anchor theo mép); camera giữ nguyên.
- **TopBar** (GameScene.unity): `StatusBar_Group` chuyển sang stretch ngang; Gold neo trái, Gem + Settings neo phải, XP giữ giữa, tất cả scale 0.9; Timer regen dời xuống dưới Gold. Trước đó Gold/Gem bị cắt, Settings văng khỏi màn hình.
- **Nút X đóng popup (touch)**: thêm `Btn_Close` (sprite `Btn_OtherButton_Circle01_n` + icon `Pictoicon_Close`) vào 6 popup thiếu đường đóng chạm: NoCoins, Setting, Prizes, DailyTask, Shop (X nằm góc TRÁI vì góc phải là counter gem), PusherSelect. Tên chứa "close" nên `UIPanel.EnsureWiring` tự wire → đi đúng đường `HandleBack`/animation. Đã test click-đóng cả 6.
- **Quan trọng**: popup tồn tại ở **2 nơi** — instance author sẵn trong GameScene.unity (UIManager adopt trước) VÀ prefab trong `Prefabs/UI/UIPopups` (fallback). Mọi sửa UI popup phải áp cả hai. Đã sync cả hai.
- **DailyTask**: row template 1400×1.12 (đồ TV) tràn màn → scale 0.72 (~1008px hiệu dụng); RefreshTimer dời xuống dòng riêng dưới title.
- **Shop**: title dời trái (pos.x −240) hết đè counter vàng.
- Scene có 4 popup bị saved-active (tự mở lúc boot) → đã deactivate + save.
- BuildScript TikTok: default canvas 1080×1920.

Chưa làm / chờ điều kiện ngoài: import SDK + build `tt-minigame/`, cắm call-site ads vào gameplay, IAP, shortcut/entrance mission, GA4 whitelist. UI còn ngoáy nhỏ: hàng đầu Prizes/scroll ngang hơi cắt card ở mép (scrollable, cosmetic); nút Claim của DailyTask sát mép phải.

## 7. Lưu ý còn mở (chưa verify được từ doc public)

- SDK unitypackage không public — chưa rõ chi tiết build settings cụ thể (compression, template, graphics API) cho đến khi có package trong tay.
- Chưa rõ PlayerPrefs của Unity trong runtime TikTok bền vững đến đâu (IndexedDB? TT storage?) — phải test thực tế trên DevTool/thiết bị, đây là điểm từng gây mất save trên TV.
- Doc không nói rõ thời gian review và giới hạn chi tiết của `request()` (whitelist domain) — cần đọc *Set Up Development Configuration* khi có portal access.
