# Slot machine preview feasibility audit — 2026-09-18

Scope: read-only inspection of current project assets and scripts. This report does not change Assets, Prefabs, scenes, scripts, Unity state, rewards, or account data. It is input to an AI appearance preview, not a Unity render or runtime acceptance result.

## Why the current machine lost its slot-machine shape

1. `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/UiPrefab/SlotMachineGroup.prefab` still contains `Content/SkeletonGraphic (01LHJ)`, its machine artwork and original animation data. In the source prefab the GameObject is inactive. The `SlotPanel.prefab` instance overrides its GameObject active, but adds CanvasGroup `7378796012750160327`, with `m_Alpha: 0`. Thus the original machine silhouette, marquee, reel surround and decoration are hidden while the animated component/reference remains available to the existing logic.
2. The visible `SlotMachineGroup/Buttom` Image (`4300416282917706685`) now uses `Assets/OrchardUI/Art/Controls.png` / `Panel` (GUID `50c5208c19d79f44ebd86c010248d125`, sprite ID `2758570`). This is a 294 × 284 near-square generic panel slice, with 70-pixel borders, rendered as `m_Type: 0` (Simple), `m_PreserveAspect: 0`, over 997 × 1575.5061 units. That enlarges and vertically distorts the ornamental frame instead of providing a purpose-built slot cabinet. The three independent dynamic symbols remain, but their former window surround is absent, so they appear to float.
3. `SlotPanel/Content/Hint` is pale cream (`1, .9764706, .90588236`) on the almost identical cream panel. The localizable hint exists but is difficult to read. A dark body color or dark backing inside the existing art area can fix this without moving the text.

## Immutable geometry and controls for the preview

Paths below are within the current project; numbers are RectTransform UI units, not screenshot pixels. Keep the current SlotPanel instance overrides; do not restore all source prefab defaults.

| Element | Current geometry / important IDs | Constraint |
|---|---|---|
| `SlotPanel/Content` | Rect `617195395320269764`; center `(0, -43.600098)`; 1015.5996 × 2142.9707 | Keep root layout |
| Nested `SlotMachineGroup` | source Rect `2012189451324540617`; SlotPanel overrides position `(0, -57)` | Keep nested instance and transform |
| `SlotMachineGroup/Buttom` | Rect `2068490866458744859`; 997 × 1575.5061; SlotPanel override position `(0, -34)` | Existing single Image is sufficient for all static cabinet art |
| `Content/SkeletonGraphic (01LHJ)` | Rect `2465795100176753288`; `(0, 112)`; 1072.0096 × 1486.999; component `7504067701055899966` | Keep component and animation reference/lifecycle |
| `Content/RewardGroup` | Rect `5534619669886448873`; SlotPanel override `(0, 112)` | Keep hierarchy and result group |
| `Content/RewardGroup/SlotMask` | Rect `2182083781733928091`; SlotPanel override `(6, -96.958435)`, 760.764 × 353.0657 | Preserve mask and three slot entries |
| Three `Slot1` instances inside mask | source nested positions `(-268,0)`, `(8,0)`, `(282,0)`; each has `img1` and `img2` | Preview must preserve currently visible symbol centers; artwork supplies three windows around them |
| `SlotMachineGroup/Btn` | Rect `4290165058539130588`; 620.7744 × 148; SlotPanel position `(0,-189)`; Button `277644941966949787`; Image `991499807541595864` | One spin button only; keep click region and localized `Girar` plus ad indicator |
| `SlotPanel/Content/Hint` | Rect `8562796657324610475`; `(-.6093998,-558)`; 645.7647 × 230.2327; TMP `808687578231943894` | Keep dynamic/localized string; change visual contrast, not content |
| Top navigation / balances | Back and FQA 100 × 100, scale 1.2; currency/cash image and TMP nodes already separate | Preserve back/help count, cash vs coin semantics, dynamic amount fields |

The source sibling order draws Buttom before Content and before Btn. A custom cabinet PNG can therefore contain the three reel-window backgrounds, narrow mullions, marquee decoration, body fascia and base, while existing live symbols and the existing spin button draw above it. The final art must be mapped to the actual instance geometry and mask, not used to resize/reposition the nodes to match an arbitrary concept.

## Recommended preview / implementation direction

Use the existing Buttom Image for one purpose-built cabinet at its 997:1575.5061 aspect ratio: warm wood outer body, muted deep green enamel inset, restrained brass edging and small warm light bulbs. At the current three symbol centers, draw three cream reel windows with light top/bottom shading and slim separators. Keep the central symbols as their existing independent dynamic images. Use the current empty upper body area for a compact arched light marquee with a star/leaf emblem, not new words, balances, amounts, rewards, controls, or a lever. Keep the spin button and hint at their current locations; improve their contrast and integration with the cabinet.

This is feasible without new nodes or component changes. Export a sprite fitted to the cabinet's existing aspect ratio rather than stretch the shared square Panel slice. A single dedicated texture has lower implementation and verification cost than repainting or rebuilding the existing Spine atlas. Do not promise animated bulbs in the static proposal.

The original Spine remains relevant to the animation timing and completion callback. Keeping its present hidden visual state while changing the existing Buttom artwork is the lowest-risk route. Do not remove or disable the component or its animation data. If a future approved direction explicitly requires the original Spine machine to become visible, it needs a separate atlas-level reskin and overlap check; merely changing alpha to 1 would revive the old blue cabinet and may overlap the new art.

## Existing Spine resources and logic to preserve

Resources under `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/Assets/Anim/DC_LaoHuJi 2/`:

- `01LHJ_SkeletonData.asset`, GUID `6f7439dc46e493245b0b7b06f2960e90`, scale .01.
- `01LHJ_Atlas.asset`, `01LHJ.atlas.txt`, texture pages `01LHJ.png`, `01LHJ2.png`, `01LHJ3.png` and their existing materials.
- `01LHJ.json`, Spine 4.1.00, animations `A` and `C`. Machine attachments include `jiqi1` (971 × 866), `jiqi2` (1074 × 514), `deng1` / `deng2`, reel shading `di1`, `di2`, masks and separator artwork. The existing `jiqi1` art incorporates an old `SLOTS` heading; do not copy that baked title into a new shared UI asset when the requirement is to preserve dynamic/localized titles.

Runtime source paths:

- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotPanel/SlotPanel.cs`: OnAwake binds close, FAQ and spin Buttons. Refresh selects free/ad state and hint text via `LanguageUtils.GetText("SlotPanel_HaveSpin")` / `SlotPanel_AdSpin`. Leave ad/account/progress/reward behavior unchanged.
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/BingoAsset/AllScripts/Scripts/Manager/SlotMachineManager.cs`: `PlayAnim` sets three result sprites, invokes Spine animation `A`, starts three slot entries with existing stagger, and `SetAnimC` switches to `C` and invokes the reward completion callback. Do not replace this with a decorative static three-symbol result.
- `.../Manager/SlotEntry.cs`: each of the three existing entries animates `img1` and `img2`, randomizes interim symbols, then sets the requested final sprite. Preserve both image nodes, mask, Animation component and callbacks.

All combinations, cash/coin reward meanings, symbol mappings and callback timing are outside the art-only change scope. All Portuguese hints/button text and displayed currency values remain live text; none are baked into cabinet textures.

## Final AI preview review — `07-ai-slot-machine-v3.png`

Read-only visual comparison against `01-user-machine.png`. Original screenshot is 795 × 1476; the selected AI preview is 920 × 1708, almost exactly the same aspect ratio (scale approximately 1.1572). Positions below are visual estimates in preview pixels, not measured Unity geometry or runtime validation.

| Element | Original position scaled into preview | Selected v3 appearance | Assessment |
|---|---|---|---|
| Cabinet extent | x 89–817; y 393–1536 | approximately x 89–816; y 391–1533 | Main extent restored; prior large upward extension is corrected |
| Three symbol centers | (286,847), (450,847), (631,847) | approximately (286,844), (450,844), (629,845) | Prior roughly 91-pixel upward drift is corrected |
| Spin button center | (450,1080) | approximately (450,1076) | Prior roughly 40-pixel upward drift is corrected |
| Hint center | (458,1308) | approximately (450,1304) | Prior roughly 53-pixel upward drift is corrected; slight horizontal text difference remains |

The visible control set is preserved: three independent reel symbols (die, chips, crown), and exactly three buttons (back, help, spin). The preview has not introduced a lever, extra reward, new amount field, or extra interaction. Marquee bulbs, star/leaf emblem and reel surrounds can be painted into the existing cabinet image. The hint is now readable on the green surface.

Residual limits: AI has redrawn the symbol contours, text glyphs, button edge and scenic background, so this is not a pixel-perfect composite. Actual implementation must retain the original TMP/localization, original live symbol assets and positions, and exact button RectTransforms; the AI screenshot must never be imported as a single full UI replacement. Do not infer animated lights or working slot logic from this static image. Acceptance of actual behavior still requires Unity verification after implementation is authorized.
