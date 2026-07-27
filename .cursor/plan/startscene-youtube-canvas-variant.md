---
name: StartScene YouTube via canvas variant
overview: Backup StartScene, wire production StartScene to UI-Canvas-Start-A Variant + YoutubeWebView like TestStart; keep TestStart but remove it from Build Settings.
date: 2026-07-28
status: implemented
---

# StartScene YouTube (canvas variant)

## Task name

Bring YouTube trailer support into production StartScene using TestStart's canvas variant

## Date

2026-07-28

## Scope

- Keep `TestStart.unity` as reference/dev scene
- Copy `StartScene.unity` → `Assets/Scenes/Game/Backup/StartScene.unity`
- Update production `StartScene` to use `UI-Canvas-Start-A Variant.prefab` + scene `YoutubeWebView` (same pattern as TestStart)
- Remove `TestStart` from Editor Build Settings

## Out of scope

- Deleting TestStart scene asset
- Modifying base `UI-Canvas-Start.prefab`
- Prefabbing `YoutubeWebView` controller (scene object, same as TestStart)

## Approved implementation steps

1. Create `Assets/Scenes/Game/Backup/` and duplicate StartScene there
2. Open StartScene; retarget canvas A/B instances to `UI-Canvas-Start-A Variant.prefab` (or ensure anchors exist)
3. Add `YoutubeWebView` + Display2 mirror wiring matching TestStart; assign `YoutubeConfig`
4. Remove TestStart from Build Settings; leave StartScene in place
5. Compile / hierarchy check

## Testing checklist

- ✅ Backup scene exists under `Assets/Scenes/Game/Backup/`
- ✅ TestStart scene file still present
- ✅ TestStart not in Build Settings
- ⬜ StartScene Play Mode: trailer on Display 1 + mirror on Display 2; CTRL still works
- ⬜ Production StartScene Music / start flow unchanged aside from YouTube

## Implementation notes (2026-07-28)

- Backup: `Assets/Scenes/Game/Backup/StartScene.unity`
- StartScene canvases retargeted to `UI-Canvas-Start-A Variant.prefab`
- Scene `YoutubeWebView` + Display2 mirror wired like TestStart
- TestStart kept on disk; removed from Editor Build Settings

## Rollback notes

- Restore production StartScene from `Assets/Scenes/Game/Backup/StartScene.unity`
- Or git checkout `Assets/Scenes/Game/StartScene.unity` and `ProjectSettings/EditorBuildSettings.asset`
