---
name: StartScene trailer intro sequence
overview: Show Image-Name + instructions for 30s; hide YoutubeAnchor until play; loop trailer for configurable 56s; then restore UI.
date: 2026-07-28
status: implemented
---

# StartScene trailer intro → timed loop → instructions

## Task name

StartScene UI sequence around YouTube trailer

## Date

2026-07-28

## Scope

- For 30 seconds: show `Image-Name` + `UI_PopupMessagePanel_PerPlayer` (both canvases); video stopped; `YoutubeAnchor-16x9` hidden
- Then hide intro UI and play looping trailer for configurable `videoPlaySeconds` (56)
- When play window elapses: stop/hide WebView + anchors; show name + instructions again
- CTRL / start button still start the run at any time

## Out of scope

- Changing instruction copy
- Prefabbing the sequence controller

## Approved implementation steps

1. Config: `introSeconds`, `videoPlaySeconds=56`, `loop=true` on YouTube embed
2. `StartSceneTrailerSequence` drives show/hide + YouTube start/stop by wall-clock duration
3. `YoutubeWebViewController` hides/shows `YoutubeAnchor-16x9` (+ Display2 mirror) with playback
4. Wire refs on StartScene (and TestStart for parity)

## Testing checklist

- ⬜ 30s intro shows name + popup; YoutubeAnchor hidden
- ⬜ Then video loops with audio; intro UI hidden; anchors visible
- ⬜ After 56s, video/audio stop; anchors hidden; name + popup return
- ⬜ CTRL during intro or video still starts run and stops video

## Wiring notes

- `StartSceneTrailerSequence` on `YoutubeWebView` in `StartScene` and `TestStart`
- `introUiRoots`: Image-Name + UI_PopupMessagePanel_PerPlayer on canvases A and B
- `videoPlaySeconds`: 56
- `YoutubeConfig.asset`: `playOnAwake=0`, `loop=1`
