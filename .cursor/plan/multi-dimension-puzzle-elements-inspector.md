---
name: Puzzle Element Inspector
status: validated
date: 2026-05-19
overview: Compact ReorderableList inspector for MultiDimensionPuzzleElement[] on MultiDimensionPuzzelManager (correctIndex left, element right). No runtime changes. Compile fix — inherit UnityEditor.Editor explicitly in WhoWiredThis.Editor namespace.
---

# Compact MultiDimension Puzzle Elements Inspector

## Task name

MultiDimension puzzle elements compact Inspector

## Date

2026-05-19

## Scope

- Add `MultiDimensionPuzzleElementListDrawer.cs` and `MultiDimensionPuzzelManagerEditor.cs` under `Assets/WhoWiredThis/Editor/`.
- One row per `puzzleElements` entry: `correctIndex` (left), `element` (right).
- Add `MultiDimensionSubjectListDrawer.cs` and `MultiDimensionEditor.cs` for `MultiDimension.subjects`: `displayName` (left), `subject` (right).
- Preserve add/remove/reorder via `ReorderableList`.
- Document reusable pattern for future serialized arrays.

## Out of scope

- Runtime / serialization changes to `MultiDimensionPuzzleElement`.
- Generic shared list drawer (optional follow-up).
- `.cursor/rules/` entry (optional follow-up).

## Approved implementation steps

1. Create list drawer static factory with `drawElementCallback` + header labels.
2. Create `CustomEditor` using property iterator; hook `puzzleElements` to `DoLayoutList()`.
3. Verify Unity compile and inspector UX.

## Compilation validation (2026-05-19)

Validated via Unity MCP `refresh_unity` (compile request) + `read_console` (errors only) — **0 errors** after fixes below.

### Fix 1 — `CS0118` namespace shadowing

`MultiDimensionPuzzelManagerEditor.cs(9,54): error CS0118: 'Editor' is a namespace but is used like a type`

**Cause:** `namespace WhoWiredThis.Editor` shadows `UnityEditor.Editor` when the base type is written as `: Editor`.

**Fix:** `public class MultiDimensionPuzzelManagerEditor : UnityEditor.Editor`

### Fix 2 — Unity 6 `ReorderableList` API

`MultiDimensionPuzzleElementListDrawer.cs(27,17): error CS0117: 'ReorderableList' does not contain a definition for 'headerContent'`

**Cause:** Unity 6 (`6000.3.6f1`) removed `headerContent` from `UnityEditorInternal.ReorderableList` (confirmed via `unity_reflect get_type`).

**Fix:** Remove object-initializer `headerContent` assignment; rely on `drawHeaderCallback` for column labels (`Index` / `MultiDimension`). The **Combination** `[Header]` still comes from the property iterator above the list.

**Pattern notes for future editor scripts:**

- `{Host}Editor` in `WhoWiredThis.Editor`: base class **`UnityEditor.Editor`**, not `Editor`.
- `ReorderableList`: no `headerContent`; use `drawHeaderCallback` (and `displayAdd` / `displayRemove` fields if customizing buttons).

## Testing checklist

- [x] Unity compiles with zero errors after `UnityEditor.Editor` fix (MCP `read_console`, errors only).
- [ ] Inspector shows one line per puzzle element (index | MultiDimension).
- [ ] Add / remove / reorder works; YAML keys unchanged (`element`, `correctIndex`).
- [ ] Other manager fields still draw (headers, RequireInterface on solve button).
- [ ] Play Mode solve/diagnostics unchanged.

## Rollback

Delete the editor scripts above (and `MultiDimensionSubjectListDrawer.cs` / `MultiDimensionEditor.cs` if rolling back subjects UI) plus their `.meta` files.

---

## Reusable pattern for future arrays (project standard)

**Use this same approach** whenever a `[Serializable]` struct/class array should show **one compact row per element** instead of Unity's default multi-row foldout — without changing serialized field names or YAML.

### Two-file split

1. **`{ElementType}ListDrawer.cs`** — `ReorderableList.Create(SerializedProperty)` with single-line `drawElementCallback`.
2. **`{HostType}Editor.cs`** — property iterator; `if (iterator.name == "arrayField")` → `list.DoLayoutList()`.

### File naming

| Piece | Pattern |
|-------|---------|
| List drawer | `{ElementType}ListDrawer.cs` |
| Custom editor | `{HostType}Editor.cs` |
| Namespace | `WhoWiredThis.Editor` (base class must be `UnityEditor.Editor`, not `Editor`) |

### What not to do (addendum)

- In `namespace WhoWiredThis.Editor`, do **not** write `: Editor` for `CustomEditor` subclasses — use `: UnityEditor.Editor`.
- Do **not** assign `ReorderableList.headerContent` on Unity 6 — use `drawHeaderCallback` instead.

### Reference tasks

- `MultiDimensionPuzzelManager.puzzleElements` — `MultiDimensionPuzzleElementListDrawer` + `MultiDimensionPuzzelManagerEditor`.
- `MultiDimension.subjects` — `MultiDimensionSubjectListDrawer` + `MultiDimensionEditor`.

See full checklist in the original plan (`puzzle_element_inspector_82826727.plan.md` in Cursor cache).
