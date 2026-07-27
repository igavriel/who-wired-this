---
name: TestStart YouTube loop
overview: Duplicate StartScene as TestStart; both canvases show a centered 16:9 looping YouTube embed via free gree/unity-webview (UPM), URL from shared config.
date: 2026-07-28
status: implemented
---

# TestStart scene — centered looping YouTube

## Task name

Duplicate StartScene → TestStart with configured looping YouTube (16:9, centered)

## Date

2026-07-28

## Locked decisions

| # | Choice |
|---|--------|
| 1 | **Option A** — real YouTube via WebView |
| 2 | **Free package:** [gree/unity-webview](https://github.com/gree/unity-webview) (`net.gree.unity-webview`, zlib license, UPM) |
| 3 | Video on **both** canvases (`UI-Canvas-Start-A` and `UI-Canvas-Start-B`) |
| 4 | **Scene-first** — wire on `TestStart` only; prefab only if user later approves |
| 5 | Trailer URL: `https://www.youtube.com/watch?v=K6HYGICvEaU` |

## Scope

- Duplicate [`Assets/Scenes/Game/StartScene.unity`](Assets/Scenes/Game/StartScene.unity) → **`Assets/Scenes/Game/TestStart.unity`** (do **not** change production StartScene).
- Add centered **16:9** YouTube panels on **both** start canvases; endless loop; URL from shared configuration.
- Keep CTRL / start button flow on TestStart.
- Add TestStart to Build Settings for manual Play Mode load only — **not** in `GameConfig` playtest chain.

## Out of scope

- Replacing production StartScene or GameConfig chain
- Paid WebViews (Vuplex / UniWebView Pro)
- Audio ducking of StartScene Music (follow-up)
- Mobile store / offline download pipeline

## Free package note

**gree/unity-webview** is free/open (zlib). It uses a **native OS WebView overlay** (WKWebView on macOS), not a Unity texture.

Implications for dual displays:

- Overlay is positioned with **screen margins**, not a normal UI `RawImage` child.
- Plan: drive **two** `WebViewObject` instances (or one shared controller spawning two), each margin-mapped to the **center 16:9** region of Display A / Display B (or left/right half if single-window dual viewport).
- If dual-monitor margin mapping is unreliable in Editor, fallback evaluation: [UnityWebBrowser](https://github.com/Voltstro-Studios/UnityWebBrowser) (MIT, CEF → texture → RawImage on each canvas) — still free, heavier install.

## Current StartScene layout (MCP)

- `UI-Canvas-Start-A` / `UI-Canvas-Start-B`
- `StartSceneController` + `SceneFlowBootstrapConfig`
- Dual cameras + `PlayerManager` / `ActivateDisplays`

## Approved approach

### 1. Package

Add to `Packages/manifest.json`:

```json
"net.gree.unity-webview": "https://github.com/gree/unity-webview.git?path=/dist/package"
```

Resolve / import; confirm Editor Play Mode can create a WebView on macOS.

### 2. Scene

1. Duplicate StartScene → `TestStart.unity`.
2. Add to Build Settings (enabled for load-by-name; not in GameConfig chain).
3. Keep bootstrap `sceneId` as StartScene for next-scene resolution (or leave as-is from duplicate).

### 3. UI + WebView (both canvases)

Shared config SO so A and B stay in sync:

- Asset: `Assets/WhoWiredThis/Data/Playtest/YoutubeConfig.asset`
  - `youtubeUrlOrVideoId`
  - `loop` / `autoplay` / `mute` (`mute=false` so Display 1 audio is heard; browsers may still block unmuted autoplay)
  - optional margin/size fraction for 16:9 fit (~0.8 of viewport)

Per canvas (or one controller managing both displays):

```
YouTubeAnchor (empty RectTransform, center, used to compute screen rect)
```

Display 1: WebView margins from Canvas A anchor (video + audio).  
Display 2: RawImage mirror of the same WebView texture (no second audio).

Embed URL:

`https://www.youtube.com/embed/{id}?autoplay=1&loop=1&playlist={id}&controls=0&rel=0&modestbranding=1`

### 4. Scripts

| File | Role |
|------|------|
| `YoutubeConfigSO.cs` | Shared URL + playback flags |
| `YoutubeWebViewController.cs` | Display 1 WebView + Display 2 texture mirror |
| Optional helper | Extract YouTube id from watch / youtu.be / embed URLs |

Do **not** put this into `StartSceneController`.

## Approved implementation steps

1. Add `net.gree.unity-webview` via UPM; verify compile + sample WebView in Editor.
2. Duplicate StartScene → TestStart; add to Build Settings.
3. Create `YoutubeConfigSO` + default asset.
4. Add center anchors under Canvas A and B; wire controller + config.
5. Implement WebView loop load; handle empty URL with warning.
6. Play Mode on dual layout: both displays show centered 16:9 loop; CTRL still starts run.
7. If dual-display margins fail: document and switch to UnityWebBrowser texture path (same config SO).

## Testing checklist

- ✅ `TestStart.unity` exists; StartScene unchanged
- ✅ Package resolves; project compiles
- ⬜ Both A and B show centered 16:9 YouTube (Play Mode manual)
- ⬜ Video loops endlessly
- ⬜ Empty URL → warning, no crash
- ⬜ CTRL / start still works
- ⬜ Leaving TestStart destroys/hides WebViews (no stuck overlay)

## Implementation notes (2026-07-28)

- Scene-first only — **no prefab** yet (awaiting user approval).
- Trailer: `https://www.youtube.com/watch?v=K6HYGICvEaU`
- Wired: `YoutubeWebView` + `YoutubeAnchor-16x9` under both canvases; `YoutubeConfig.asset`.
- Classes: `YoutubeConfigSO`, `YoutubeWebViewController` (renamed from `TestStartYoutube*`).
- Canvases remain Screen Space Overlay (A→display 0, B→display 1).
- **YouTube Error 153 fix:** `LoadURL` + `Referer` (`https://www.google.com/`).
- **Display 2:** RawImage mirror of Display 1 WebView texture (no second player / no second audio).
- **Audio:** single Display 1 WebView only; config `mute=false` (no on-screen volume UI). Start-scene Music may still mix under it.

## Risks

- **Native overlay vs dual Display** — primary technical risk; may need UnityWebBrowser fallback.
- **Autoplay policy** — unmuted autoplay may be blocked by the WebView; if silent, toggle `mute` on `YoutubeConfig`.
- **Embed-blocked videos** — trailer must allow embedding.
- **Music + YouTube audio** clash (Start scene Music may still play under the trailer).
- **WebView leftover** after scene unload if Destroy not called.

## Rollback notes

Remove TestStart scene, YouTube scripts/config, and `net.gree.unity-webview` from `manifest.json`. StartScene untouched.

## Ready to implement

~~Decisions locked. Say **go** to implement.~~ **Implemented on scene** (no prefab).
