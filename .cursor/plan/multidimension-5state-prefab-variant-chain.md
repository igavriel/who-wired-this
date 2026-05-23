---
task: MultiDimension 5-state prefab variant chain (parents → variants)
date: 2026-05-23
status: in_progress
phase_0: approved
phase_0_5: approved
phase_1: ready
overview: Re-implement 5-state MultiDimension prefabs incrementally (3State base → 4State → 5State) without repeating commit d41674b mistakes; MCP + editor validation each phase; user approval gate before next phase.
related_assets: Assets/WhoWiredThis/Prefabs/MultiDimension/, Assets/Scenes/Puzzel Pipes.unity, Assets/Scenes/Puzzles/Split Tutorial.unity
reference_commit: d41674b8f4b6a4dfa39c8d46be4094be592ab53c
---

# MultiDimension 5-state prefab variant chain

## Task name

Incremental 5-state MultiDimension prefabs (parents → 4State → 5State)

## Date

2026-05-23

## Scope

- Re-create **`MultiDimension_Knob_5State`**, **`MultiDimension_Slider_5State`**, **`MultiDimension_ButtonText_5State`** as proper prefab variants (not by cherry-picking commit `d41674b` YAML).
- Fix or avoid the mistakes in `d41674b`: editing **`Knob_3State` base**, re-parenting **`Knob_4State`** to chase base refactors, **skipping 4State** as parent for 5State assets, **GUID reuse** (`7516c9f…` moved from Knob_4 → Knob_5).
- Work **one family at a time** (Knob → Slider → ButtonText), **one phase at a time**, with **user approval** before the next phase.
- Use Unity MCP for compile/console checks and structural verification after each phase.
- Use commit `d41674b` only as a **reference** for intended 5-state vocabulary and layout hints (not as the apply patch).

## Out of scope

- Changing **Puzzel Pipes** to 5-state inputs (scene stays on **4-state** per [pipe-pressure-puzzle-puzzel-pipes.md](pipe-pressure-puzzle-puzzel-pipes.md) unless you explicitly approve a follow-up).
- **`MultiDimension_SwitchColor_3State`** — not touched in Phase 0.5 (visual pass covered the other eight prefabs).
- Runtime / C# changes to `MultiDimension`, `MultiDimensionPuzzelManager`, or validation tools (unless a phase uncovers a required compile fix).
- New editor validation menu for 5-state (optional follow-up); Phase 0–3 use MCP + manual checks documented below.
- Scene wiring for 5-state prefabs (deferred to **Phase 4** after all three prefab families exist).

---

## Reference: what commit `d41674b` did (do not replay)

| Asset | Commit action | Correct approach |
|-------|---------------|------------------|
| `Knob_3State` | Added `Common` parent; reparented children | Only if you approve structural base change; never bundle with 5State creation |
| `Knob_4State` | Updated variant overrides for new base IDs; **new meta GUID** | Leave stable; reconcile only if base intentionally changes |
| `Knob_5State` | New; parent = **3State**; labels MIN…MAX; stole Knob_4 GUID | New asset; parent = **4State** (default) or **3State** (if you choose); **new GUID** |
| `Slider_5State` | New; parent = **3State** | Parent = **4State** (default) |
| `ButtonText_5State` | New; parent = **3State** | Parent = **4State** (default) |
| `Puzzel Pipes.unity` | Minor 4State override trims | No change until Phase 4 and explicit approval |

### 5-state vocabulary (from commit — confirm per phase)

| Prefab | `displayName` set (commit) | Notes |
|--------|---------------------------|--------|
| Knob_5State | MIN, LOW, MID, HIGH, MAX | Differs from Knob_4 (SHUT, LOW, HALF, OPEN) |
| Slider_5State | MIN, LOW, MID, HIGH, MAX | Differs from Slider_4 (LOW, MID, HIGH, …) |
| ButtonText_5State | FLAT, SINE, PULS, TRNG, NOIS | May use symbolic TMP on control (like 4State LEFT vs `<<<`) |

### Intended variant trees

```
Knob:     Knob_3State → Knob_4State → Knob_5State
Slider:   Slider_3State → Slider_4State → Slider_5State
ButtonText: ButtonColor_3State → ButtonText_3State → ButtonText_4State → ButtonText_5State
```

**Default rule:** each N-state variant parents the **(N−1)-state** prefab in the same family, adding only the **Nth subject** (+ layout/TMP), not re-adding subjects 4 and 5 from the 3State base.

---

## Global workflow rules

1. **Git:** Commit or stash a clean baseline before Phase 0; do not reset/checkout user work.
2. **Implementation:** Unity Editor (Inspector) or Unity MCP `manage_prefabs` / `manage_gameobject` — prefer Inspector for variant creation when MCP is awkward.
3. **No YAML cherry-pick** from `d41674b`.
4. **GUID safety:** never reuse an existing prefab `.meta` GUID for a new file.
5. **4-state freeze:** do not modify `*_4State` prefabs while working on another family unless that phase explicitly requires it.
6. **User approval gate:** each phase ends with a checklist + your explicit **“approved — proceed”** before the next phase starts.

---

## MCP validation playbook (every phase)

Run with Unity Editor open on this project. If multiple instances are connected, call `set_active_instance` first.

| Step | MCP tool / action | Pass criteria |
|------|-------------------|---------------|
| V1 | `refresh_unity` (or wait for `editor_state` → `isCompiling: false`) | No compile errors |
| V2 | `read_console` (filter: Error) | 0 new errors after prefab save |
| V3 | `manage_asset` search prefab by name | Asset exists; GUID stable vs prior phase |
| V4 | `manage_prefabs` / open prefab in Editor | `m_SourcePrefab` points to correct parent (variants only) |
| V5 | Optional: `execute_menu_item` → `Who Wired This/Pipe Pressure/Validate Phase 1 (Puzzel Pipes)` | **PASS** on regression scenes (4-state unchanged) |

**Manual (required when MCP cannot read nested overrides):**

- Prefab mode: `MultiDimension.subjects` array size = expected N.
- Each subject has `displayName` + `subject` GameObject reference.
- Active subject cycles through N states in Play Mode on a **test instance** (Phase 4 or ad-hoc test scene).

---

## Phase 0 — Baseline & decisions

**Goal:** Confirm starting point; lock parent-chain and base-refactor choices. **No new prefabs.**

### Implementation steps

- [ ] Verify workspace has **no** `*_5State.prefab` files (or delete stray copies from experiments).
- [ ] Confirm `Knob_4State.meta` GUID is **`7516c9f35182548e18e591d5484dbc79`** (not assigned to another asset).
- [ ] Document current `Knob_3State` hierarchy: **no `Common` parent** (or note if already present).
- [x] Record decisions (locked 2026-05-23 — user approved Phase 0; visual work staged before variant chain):

| Decision | Options | Your choice |
|----------|---------|-------------|
| 5State parent | A) variant of **4State** (recommended) / B) variant of **3State** | **A — variant of 4State** |
| `Knob_3State` `Common` refactor | Yes / No / Defer to Knob-only 5State overrides | **Yes — done in Phase 0.5** (`Common` + `Common-Knob-OFF` hierarchy) |
| First scene to use 5State | None yet / Test scene / other: ___ | **None yet** (prefabs first; scenes in Phase 4) |

### MCP / validation checklist

- [x] V1–V2: project compiles; **0 compile errors** (`refresh_unity` + `read_console` errors)
- [x] `manage_asset` search: 9 MultiDimension prefabs; **no `*_5State`**; 4-state trio present with stable GUIDs
- [x] V5: **Validate Phase 1 (Puzzel Pipes)** — **PASS** (`# Phase 1 OK`; e.g. `VALVE has 4 subjects`)

### Phase 0 baseline snapshot (2026-05-23)

| Check | Result |
|-------|--------|
| `*_5State.prefab` in repo | **None** |
| `Knob_4State.meta` GUID | `7516c9f35182548e18e591d5484dbc79` (only on Knob_4 + scene refs) |
| `Knob_3State` has `Common` parent | **Yes** (after Phase 0.5) |
| `Knob_3State` subjects | 3 — LOW, MID, HIGH (`displayName` unchanged) |

### User approval gate

- [x] **Ilan approves Phase 0** — 2026-05-23

---

## Phase 0.5 — Visual changes + hierarchy naming (complete)

**Goal:** Layout/TMP/bezel tweaks and **consistent child naming** on existing MultiDimension prefabs before creating `*_5State` variants.

**Status:** **Approved 2026-05-23** — user completed visual + naming pass; prefabs **staged** in git (not yet committed with 5-state work).

### Prefabs touched (staged)

| Prefab | Visual / hierarchy | Notes |
|--------|-------------------|--------|
| `MultiDimension_Knob_3State` | Yes | Added `Common` parent; renamed children (see naming convention) |
| `MultiDimension_Knob_4State` | Yes | Variant reconciled with new base hierarchy |
| `MultiDimension_Slider_3State` | Yes | `Common-Slider-OFF` naming |
| `MultiDimension_Slider_4State` | Yes | Variant updated |
| `MultiDimension_ButtonColor_3State` | Yes | `Common-Button-OFF` naming |
| `MultiDimension_ButtonColor_1State` | Yes | Visual + **`.meta` GUID changed** — see risk below |
| `MultiDimension_ButtonText_3State` | Yes | Variant chain base |
| `MultiDimension_ButtonText_4State` | Yes | Variant updated |
| `MultiDimension_SwitchColor_3State` | — | Unchanged |

Still **no** `*_5State.prefab` files.

### Naming convention (Phase 0.5 — use for new subjects in Phase 1+)

Documented from authored hierarchy (extend index for 5th state):

| Pattern | Example | Use |
|---------|---------|-----|
| `Common` | Knob root grouping | Parent for shared bezel/labels under knob base |
| `Common-{Type}-OFF` | `Common-Knob-OFF`, `Common-Slider-OFF`, `Common-Button-OFF` | Inactive/off visual shell |
| `Text-{n}` | `Text-0`, `Text-1`, `Text-2` | TMP label objects (subject index) |
| `Sphere-{n}` | `Sphere-0`, `Sphere-1` | Knob indicator spheres |
| `Knob-{n}` | `Knob-1`, `Knob-2` | Knob mesh parts |
| `Mark-{n}` | `Mark-0`, `Mark-1` | Slider tick marks |

`MultiDimension.subjects[].displayName` (LOW, SHUT, etc.) is **unchanged** by naming pass — only **GameObject names** in hierarchy.

### Checklist

- [x] Visual edits on MultiDimension prefabs (8 of 9; SwitchColor excluded)
- [x] Hierarchy naming standardized per table above
- [x] Staged in git (`git add` on prefab paths above) — **commit separately** from Phase 1c+5State assets
- [ ] Play Mode smoke on Tutorial / Puzzel Pipes (recommended before visual commit)

### MCP / validation (2026-05-23)

- [x] V5: **Validate Phase 1 (Puzzel Pipes)** — **PASS** (`VALVE has 4 subjects`, etc.)
- [x] No `*_5State` prefabs in project

### Risks / follow-up before visual commit

| Risk | Detail | Action |
|------|--------|--------|
| **ButtonColor_1State GUID** | `.meta` changed `64fd3ce…` → `4cbef0a0…`; **6 scenes** still reference old GUID in YAML | Before committing visuals: open scenes in Unity and save, or run reference repair, so instances remap to new GUID |
| **Knob_4 variant overrides** | Base gained `Common` parent | Phase **1b** = verify variant applies cleanly (user already edited; agent verifies MCP + subject count) |

### User approval gate

- [x] **Ilan approves Phase 0.5** — 2026-05-23; proceed to **Phase 1** (5-state variant creation)

---

## Phase 1 — Knob family

**Goal:** Stable knob chain ending with **`MultiDimension_Knob_5State`** (if approved).

### Phase 1a — `MultiDimension_Knob_3State` (base)

**Done in Phase 0.5** (visual + `Common` hierarchy + naming).

- [x] Structural/layout: `Common` parent + renamed children
- [x] `subjects` count still **3** (LOW, MID, HIGH)

#### MCP / validation

- [x] Knob_3State base — guid `35a5599da05c343d38d430586d16dff3` (unchanged)
- [ ] V1–V2 on next agent pass before 1c

### Phase 1b — `MultiDimension_Knob_4State` (variant)

**Mostly done in Phase 0.5** — verify before creating 5State.

- [x] Parent = **`Knob_3State`** (guid `35a5599da05c343d38d430586d16dff3`)
- [x] Variant reconciled with `Common` base (user edit)
- [ ] **Verify** 4 subjects: **SHUT, LOW, HALF, OPEN** + 4th subject reference intact
- [x] `.meta` GUID unchanged (`7516c9f35182548e18e591d5484dbc79`)

#### MCP / validation

- [ ] V4: `m_SourcePrefab` → Knob_3State (agent check before 1c)
- [x] V5: Puzzel Pipes Phase 1 validation PASS (post–0.5)

### Phase 1c — `MultiDimension_Knob_5State` (new variant)

**Per your instructions for labels, layout, TMP, and 5th subject object.**

- [ ] Create prefab variant; parent = **choice from Phase 0** (default: `Knob_4State`)
- [ ] Add **only** 5th subject + positioning (do not duplicate 4State work if parenting 4State)
- [ ] Set `displayName` values per approved vocab (commit reference: MIN, LOW, MID, HIGH, MAX)
- [ ] New `.meta` with **new GUID**
- [ ] Prefab name: `MultiDimension_Knob_5State`

#### MCP / validation

- [ ] V3–V4: asset exists; correct parent guid
- [ ] Manual: `subjects.Array.size` == 5; cycle interact in prefab test instance
- [ ] V5: Puzzel Pipes Phase 1 still PASS (4-state scenes untouched)

### User approval gate

- [ ] **Ilan approves Phase 1** — Knob chain complete; proceed to Phase 2

---

## Phase 2 — Slider family

**Goal:** **`MultiDimension_Slider_5State`** with same discipline as Phase 1.

### Phase 2a — `MultiDimension_Slider_3State` (base)

- [ ] Only if approved in Phase 0 / your step instructions
- [ ] Else: skip (3-state base unchanged)

### Phase 2b — `MultiDimension_Slider_4State` (variant)

- [ ] Confirm parent = `Slider_3State`; 4 subjects intact (Pipes vocab)
- [ ] No GUID change

#### MCP / validation

- [ ] V4: parent guid `801858844e77f4d658e87a44bb15d01d`
- [ ] V5: Puzzel Pipes Phase 1 PASS

### Phase 2c — `MultiDimension_Slider_5State` (new variant)

- [ ] Create variant per Phase 0 parent choice (default: `Slider_4State`)
- [ ] 5th subject + layout per your instructions
- [ ] Labels per approved vocab (commit reference: MIN … MAX)
- [ ] New GUID in `.meta`

#### MCP / validation

- [ ] V3–V4 + manual 5-subject check
- [ ] V5: Puzzel Pipes Phase 1 PASS

### User approval gate

- [ ] **Ilan approves Phase 2** — Slider chain complete; proceed to Phase 3

---

## Phase 3 — ButtonText family

**Goal:** **`MultiDimension_ButtonText_5State`** (longest chain).

### Phase 3a — `MultiDimension_ButtonColor_3State` / `ButtonText_3State`

- [ ] Only if your instructions require base/3State changes
- [ ] Else: skip

### Phase 3b — `MultiDimension_ButtonText_4State` (variant)

- [ ] Parent = `ButtonText_3State`; 4 subjects (LEFT, MID, RGHT, LOOP)
- [ ] Symbolic TMP rule preserved (history ≠ control LCD text)
- [ ] No GUID change

#### MCP / validation

- [ ] V4: parent guid `f3ccf0763e84049748c040402a347187`
- [ ] V5: Puzzel Pipes Phase 1 PASS (FLOW/ROUTE symbolic rule still passes)

### Phase 3c — `MultiDimension_ButtonText_5State` (new variant)

- [ ] Create variant per Phase 0 parent choice (default: `ButtonText_4State`)
- [ ] 5th subject + TMP per your instructions
- [ ] Labels per approved vocab (commit reference: FLAT, SINE, PULS, TRNG, NOIS)
- [ ] New GUID in `.meta`

#### MCP / validation

- [ ] V3–V4 + manual 5-subject + symbolic TMP check if applicable
- [ ] V5: Puzzel Pipes Phase 1 PASS

### User approval gate

- [ ] **Ilan approves Phase 3** — all three 5-state prefabs exist; proceed to Phase 4

---

## Phase 4 — Scene wiring & integration (optional)

**Only after Phases 1–3 approved and you specify target scene(s).**

- [ ] Create or use test scene (e.g. `Assets/Scenes/Test/…`) — prefer **not** mutating Puzzel Pipes unless approved
- [ ] Place prefab instances; wire `MultiDimensionPuzzelManager.puzzleElements` if needed
- [ ] Play Mode: two-player cycle + SEND + history tokens (width 5 per [puzzle-input-labels-5char.md](puzzle-input-labels-5char.md))
- [ ] Run relevant scene validation tools if wired

### MCP / validation

- [ ] V1–V2 after scene save
- [ ] `manage_scene` get_hierarchy — instances reference correct prefab guids
- [ ] V5 on Puzzel Pipes if scene touched (must still PASS for 4-state inputs)

### User approval gate

- [ ] **Ilan approves Phase 4** — mark plan **implemented** / **validated**

---

## Rollback notes

| Phase | Rollback |
|-------|----------|
| 0 | No asset changes |
| 1c / 2c / 3c | Delete new `*_5State.prefab` + `.meta`; revert git for any accidental base/4State edits |
| 1a / 1b | `git checkout --` on `Knob_3State` / `Knob_4State` from last good commit |
| 4 | Revert scene YAML only |

**Critical:** If `Knob_4` GUID ever pointed at `Knob_5`, run `grep -r 7516c9f35182548e18e591d5484dbc79 Assets/` and fix scene references before shipping.

---

## Progress log

| Date | Phase | Status | Notes |
|------|-------|--------|-------|
| 2026-05-23 | Plan archived | planned | Analysis of `d41674b`; awaiting Phase 0 decisions + approval |
| 2026-05-23 | 0 | **approved** | Baseline verified; MCP Phase 1 Puzzel Pipes PASS; decisions locked (5State parents 4State) |
| 2026-05-23 | 0.5 | **approved** | Visual + naming on 8 prefabs staged; `Common` on Knob_3; naming convention documented; Pipes Phase 1 still PASS |
| | 1a–1b | done (verify) | Knob base + 4State visually complete; **1c** (`Knob_5State`) next — awaiting user instructions |
| | 1c | pending | Create `MultiDimension_Knob_5State` variant of `Knob_4State` |
| | 2 | | |
| | 3 | | |
| | 4 | | |

---

## Related plans

- [pipe-pressure-puzzle-puzzel-pipes.md](pipe-pressure-puzzle-puzzel-pipes.md) — **4-state** prefabs for Puzzel Pipes; do not regress
- [puzzle-input-labels-5char.md](puzzle-input-labels-5char.md) — 5-char history tokens + label width
- [multi-dimension-puzzle-elements-inspector.md](multi-dimension-puzzle-elements-inspector.md) — Inspector editing of `puzzleElements`
