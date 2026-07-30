# Luna Playable Ads — Tài liệu setup & troubleshooting

Tài liệu ghi lại toàn bộ quá trình đưa game lên **Unity Playworks Plugin (Luna) 7.2.0** để build playable ads: những gì đã sửa/thêm, các lỗi đã gặp kèm cách fix, và lưu ý cho dev khi tiếp tục phát triển.

> **Trạng thái:** playable đã chạy hoàn chỉnh trên Playground — boot → chơi → thắng → CTA mở store. Branch: `dev_playable_ads`.

---

## 1. Môi trường

| Thành phần | Giá trị |
|---|---|
| Unity Editor | **2022.3.62f3** (hạ từ Unity 6 — Luna không hỗ trợ Unity 6; URP đã gỡ, dùng Built-in RP) |
| Luna toolchain | `C:\Users\PC0129\Luna\7.2.0\` (đã chuyển từ `Downloads\` sang chỗ ổn định) |
| Package | `Packages/manifest.json` → `"com.unity.playworks.upp": "file:../../Luna/7.2.0/scripts"` |
| .NET | .NET Framework **4.7 Developer Pack** (bắt buộc — xem lỗi #1) |
| MSBuild | VS2022 Community (Luna tự dò) |
| Mở plugin | Menu **Tools → Unity Playworks Plugin** (Ctrl+E) |
| Cấu hình build | `luna.json` (root project, có commit) — `LunaTemp/` là cache, đã gitignore |

⚠️ **Toolchain Luna là CẢ thư mục `7.2.0`** (~600MB: `scripts` + `pipeline` + `tools` + `engine` + `config.json`). **Không được** copy riêng `scripts` đi nơi khác — plugin tìm các thư mục anh em theo đường dẫn tương đối. Thiếu chúng sẽ gặp `Version.Parse(null)` mỗi lần reload domain và `DirectoryNotFoundException: ...\pipeline\templates\html` khi mở panel. Vì cỡ đó nên không nhúng vào repo git.

---

## 2. Tích hợp playable ads (file mới / file sửa)

### 2.1. `Assets/_Project/Scripts/Systems/PlayableAds.cs` (MỚI)

Wrapper duy nhất cho mọi API Luna:

- `Enabled` (const) — bật/tắt chế độ playable. `true`: bỏ main menu, boot thẳng vào level. Tắt để quay về full game.
- `StartLevelIndex` — level mở đầu ad.
- `GooglePlayUrl` / `AppStoreUrl` — link store cho CTA.
- `NotifyLoaded/NotifyStarted/NotifyEnded` → `Luna.Unity.LifeCycle.GameLoaded/GameStarted/GameEnded`.
- `InstallFullGame()` → `Luna.Unity.Playable.InstallFullGame(googlePlay, appStore)`.
- Custom events qua `Luna.Unity.Analytics.LogEvent`: `level_start`, `level_complete`, `cta_clicked` — ad network dùng funnel này chấm điểm playable.

Các API `Luna.Unity.*` là **no-op stub** trong editor/build thường — gọi thoải mái, chỉ hoạt động thật khi build qua Luna.

### 2.2. Wiring vào game

| File | Thay đổi |
|---|---|
| `GameBootstrap.cs` | `NotifyLoaded()` cuối boot; `Enabled` → `StartLevel(StartLevelIndex)` thay vì `ShowMainMenu()`. Kèm **error overlay** (mục 4.1). |
| `GameManager.cs` | `NotifyStarted()` trong `StartLevel`; `NotifyEnded()` khi thắng (`CheckWin`). |
| `WinPopup.cs` | Chế độ playable: nút **DOWNLOAD** (CTA) chiếm vị trí chính, kèm NEXT LEVEL / REPLAY, bỏ nút LEVELS. |

### 2.3. Nguyên tắc VÀNG: asset phải đi qua scene

**Luna chỉ export asset mà scene tham chiếu.** Mọi thứ load bằng `Resources.Load*` lúc runtime sẽ **không có mặt** trong gói build → null/empty lúc chạy.

❌ **Đừng** vá bằng `unity.assets.includes` trong `luna.json` — panel Playworks sẽ ghi đè file bằng state trong bộ nhớ của nó và lặng lẽ xoá entries tự thêm (đã dính thực tế: build ngay sau khi thêm thì ăn, build kế tiếp thì mất).

✅ Giải pháp đã áp dụng — `GameBootstrap` (component trong scene) có field serialized và bơm vào hệ thống lúc boot:

```
[SerializeField] UITheme theme;          → UIFactory.SetTheme(theme)
[SerializeField] LevelAsset[] levels;    → LevelLoader.InjectedLevels  (level 1–5)
[SerializeField] Material meshMaterial;  → MeshFactory.SetMaterial(...)  (shader VertexColor)
```

- `Materials/VertexColor.mat` (MỚI) — material dùng shader `TinyTownRoads/VertexColor`; scene → material → shader nên Luna buộc phải convert + đóng gói shader ("Always Included Shaders" trong Graphics Settings **không có tác dụng** với Luna).
- `LevelLoader` còn phao cuối: nếu không có level nào, tự sinh 3 level 6x6 bằng `LevelGenerator` seed cố định (solvable by construction) — playable không bao giờ boot vào khoảng trống.
- Thêm level mới cho playable = kéo thêm `LevelAsset` vào mảng `levels` của GameBootstrap trong scene.

---

## 3. Các lỗi đã gặp & cách fix (theo thứ tự thời gian)

### #1 — MSBuild: `MSB3644 .NETFramework v4.7 not found`
- **Triệu chứng:** build fail ngay bước compile C#, panel báo "MSBuild completed with error".
- **Nguyên nhân:** csproj template của Luna target .NET Framework 4.7, máy chỉ có targeting pack 4.7.1/4.7.2/4.8.
- **Fix:** cài **.NET Framework 4.7 Developer Pack**: `winget install Microsoft.DotNet.Framework.DeveloperPack_4 --version 4.7`. (Không sửa template của Luna — update toolchain sẽ mất.)

### #2 — `UnityEngine.InputSystem does not exist`
- **Nguyên nhân:** Luna **không hỗ trợ new Input System** (compiler của Luna không reference package này).
- **Fix:** Luna define sẵn **`UNITY_LUNA`** khi compile → nhánh điều kiện:
  - `InputController.cs`: `#if UNITY_LUNA` dùng `Input.GetMouseButton(0)` + `Input.mousePosition` (engine Luna map touch → mouse nên mobile vẫn chạy).
  - `GameBootstrap.cs`: `StandaloneInputModule` thay cho `InputSystemUIInputModule`.

### #3 — DOTween Pro: `DOGotoAndPause/DOGotoAndPlay: no suitable method found to override`
- **Nguyên nhân:** khi build, Luna **tự thay DOTween core bằng bản 1.2.705 của họ**; file nguồn DOTween Pro trong project là đời 1.2.765 nên lệch API.
- **Fix:** thêm `**/DOTweenPro/` vào `scripts.excludes` trong `luna.json` (game không dùng component Pro nào — chỉ dùng API core `DOPath/DOFade/DOScale/DelayedCall`). Cảnh báo "DOTween version is not supported" trong diagnostics vì thế cũng vô hại.

### #4 — `OnGUI` unsupported
- **Nguyên nhân:** `TCP2_Demo.cs` trong Toony Colors Pro (asset không dùng) có `OnGUI`.
- **Fix:** exclude `**/JMO Assets/` trong cả `scripts.excludes` lẫn `assets.excludes`.

### #5 — Loạt API gap khi compile qua Luna
| Lỗi | Fix |
|---|---|
| `FindFirstObjectByType` không tồn tại | `#if UNITY_LUNA` → `FindObjectOfType` (GameBootstrap) |
| `Math.Sign(int)` ambiguous (Bridge.NET thiếu overload int) | Helper `Sign(int)` tự viết trong PathDrawer, dùng chung mọi platform |
| `Resources.GetBuiltinResource<T>` generic không có | Chuyển dạng non-generic `(Font)GetBuiltinResource(typeof(Font), ...)` — hợp lệ cả Unity thường |
| `AudioClip.Create` không tồn tại | `#if UNITY_LUNA` → `Render()` trả `null`, mọi chỗ phát guard null ⇒ **bản playable chạy im lặng** (network mặc định cũng mute; muốn có tiếng phải dùng file audio thật) |

### #6 — Runtime: màn hình chỉ còn màu nền, không UI, không board
- **Triệu chứng:** build thành công nhưng Draw Calls = 0; boot ném exception giữa chừng mà Playground không hiện lỗi.
- **Nguyên nhân:** `Resources.LoadAll` trả rỗng (asset không được export — xem mục 2.3), cộng bẫy `Get(0)` với list rỗng → `Clamp(0,0,-1)` = -1 → IndexOutOfRange.
- **Fix:** hệ thống inject qua scene (mục 2.3) + `LevelLoader.Get` ném message rõ ràng khi rỗng + xây bộ debug (mục 4).

### #7 — Runtime: `Cannot set property name which has only a getter`
- **Nguyên nhân:** `Object.name` trong bridge của Luna **chỉ có getter** — gán `.name =` cho `Texture2D/Sprite/Mesh` tạo runtime là chết.
- **Fix:** bọc `#if !UNITY_LUNA` cho cả 3 chỗ gán name (SpriteFactory ×2, MeshFactory ×1) — name chỉ để debug trong editor.

### #8 — Runtime: `NotSupportedException: Resources.GetBuiltinResource is not implemented`
- **Nguyên nhân:** cả bản non-generic Luna cũng **throw** (không phải trả null).
- **Fix:** try/catch quanh fallback font trong `UIFactory.Font` — theme mất thì degrade sang không chữ thay vì chết boot.

### #9 — Runtime: `TypeError: reading 'handle' of null — at set shader`
- **Nguyên nhân:** `Shader.Find("TinyTownRoads/VertexColor")` trả **null** trong Luna (shader chỉ được gọi tên runtime, không nằm trong dependency scene) → `new Material(null)`.
- **Fix:** tạo `Materials/VertexColor.mat` + scene-reference qua `GameBootstrap.meshMaterial` + `MeshFactory.SetMaterial()` (mục 2.3).

### #10 — Runtime: board/nhà/xe trắng bệch toàn bộ
- **Nguyên nhân:** bridge Luna **nuốt lặng lẽ** các overload `Mesh.SetVertices/SetColors/SetTriangles(List<>)` — đặc biệt `SetColors` → mesh mất vertex color, mà toàn bộ màu + "ánh sáng" của game bake vào vertex color.
- **Fix:** đổi sang gán array property: `mesh.vertices/colors/triangles = list.ToArray()` (kết quả y hệt ở Unity thường). Kèm theo: `MaterialPropertyBlock` không tin cậy trong Luna → bản Luna tint per-object bằng `renderer.material.SetColor(...)` (`#if UNITY_LUNA` trong `MeshFactory.SetTint`).

---

## 4. Bộ công cụ debug (giữ lại, đừng xoá)

### 4.1. Error overlay trên màn hình (`GameBootstrap.cs`, chỉ `#if UNITY_LUNA`)
Toàn bộ boot bọc try/catch. Khi có exception:
- **Kênh 1 — Events:** lỗi bắn qua `Analytics.LogEvent` → hiện trong panel **Events** của Playground (`ERR <type>: <msg>` + `ST0..ST4` stack) — hoạt động kể cả khi rendering hỏng hoàn toàn.
- **Kênh 2 — Overlay đỏ:** in tên lỗi + stack trace lên màn hình; font có 3 tầng fallback, từng bước tự chịu lỗi.

### 4.2. Tự verify playable không cần thao tác tay
Panel **Start server** (port 8000) serve build mới nhất. Chạy headless Chrome và đọc console (thấy đủ `Debug.Log/LogError`):

```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" `
  --headless=new --no-sandbox --no-proxy-server --disable-gpu --no-first-run `
  --enable-logging=stderr --v=0 --virtual-time-budget=30000 --dump-dom `
  --user-data-dir=<thư mục tạm> http://127.0.0.1:8000/index.html 2> console.log
# rồi grep "CONSOLE" console.log
```

### 4.3. Soi gói export
`LunaTemp/stage1/assets/` (resources / scriptable-objects / fonts / shaders...) cho biết **chính xác** asset nào được đóng gói — nghi ngờ thiếu asset thì nhìn vào đây trước.

### 4.4. Compile check nhánh Luna không cần build
Compile toàn bộ game code với define `UNITY_LUNA` bằng csc của Unity (bắt lỗi cú pháp cả 2 nhánh, vài giây thay vì vài phút build Luna):

```bash
UNITY="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
# tạo rsp từ Assembly-CSharp.csproj (xem CLAUDE.md mục compile check), rồi:
"$UNITY/NetCoreRuntime/dotnet.exe" "$UNITY/DotNetSdkRoslyn/csc.dll" -nologo -target:library \
  -nostdlib -noconfig -define:UNITY_LUNA -nowarn:0162,0649 -out:/tmp/RuntimeLuna.dll @/tmp/csc.rsp
```

---

## 5. Checklist cho dev khi viết code mới

**Tư duy chung:** Luna chạy game qua engine JS riêng với bridge UnityEngine **không đầy đủ** — API compile được (DLL stub có đủ) nhưng runtime có thể thiếu. Lỗi chỉ lộ khi chạy thật → sau thay đổi lớn nên build + verify headless (mục 4.2).

Tránh / cẩn thận với:

- ❌ `Resources.Load*` cho asset mới → thêm field serialized vào `GameBootstrap` + inject (mục 2.3)
- ❌ `Shader.Find` → tham chiếu material asset qua scene
- ❌ Gán `.name` cho object tạo runtime (Texture2D/Sprite/Mesh) → bọc `#if !UNITY_LUNA`
- ❌ `Mesh.Set*(List<>)` → dùng array property (`mesh.vertices = arr`)
- ❌ `MaterialPropertyBlock` → nhánh Luna dùng `renderer.material`
- ❌ `AudioClip.Create` → không có audio synthesized trên Luna
- ❌ `UnityEngine.InputSystem`, `OnGUI`, `FindFirstObjectByType`, `Resources.GetBuiltinResource`, `Math.Sign(int)`
- ✅ Code riêng cho bản playable: `#if UNITY_LUNA` (Luna define sẵn khi compile)
- ✅ uGUI + legacy `Text`, DOTween API core, `PlayerPrefs`, LINQ, mesh/texture procedural (với lưu ý trên) — đều OK

Quy trình build: mở **Tools → Unity Playworks Plugin** → tab Build → đặt creative name → **Build** → preview trên Playground → khi ship thật: **Runtime Analysis** (strip code) → kiểm size theo network → **Upload to Creative Library**.

**Còn lại / TODO:**
- Âm thanh bản playable đang tắt hoàn toàn — muốn có tiếng cần thêm file wav/mp3 thật (Luna hỗ trợ audio asset, sẽ nén theo `luna.json`).
- `PlayableAds.Enabled` là const — bản WebGL thường trên branch này cũng auto-skip menu; muốn build WebGL full game từ branch này thì đổi cờ về `false` trước.
