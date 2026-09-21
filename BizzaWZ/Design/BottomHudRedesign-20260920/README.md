# 777 progress and bottom tool icons — design proposal

Status: AI local visual preview, awaiting the user's design confirmation. No Unity production assets, Prefabs, gameplay code or Play-mode state were modified for this proposal.

`00-user-reference.png` is the user screenshot. `01-ai-bottom-hud-preview.png` was generated with the built-in imagegen editor using that screenshot as the edit target; `prompt.txt` contains the exact prompt. The output is copied without manual pixel edits. This is not a Unity Prefab static preview or a Unity runtime screenshot. AI redraw and added outer canvas padding are not instructions to move or resize production controls.

Visual direction: keep the 777 logo and slim progress footprint; use a dark teal recessed track, modest green fill and readable light text. Give the undo, magic and shuffle tools wood edges, cream faces and matching sculpted icons. Preserve the existing AD, padlock and inventory-count markers and their locations. Add no branches to buttons or bars.

## Inspected before generation

- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.prefab`
- `Assets/BizzaWZ/Final/Real/UI/SlotsPanel/SlotEnter/SlotEnter.cs`
- `Assets/BizzaWZ/Final/Real/Tools/CustomGameCfg/CustomGameSaveData.cs`
- `Assets/FruitsHarvest/Resources/Original/res/local/coreplay/CorePlayUI.prefab`
- `Assets/FruitsHarvest/Scripts/CorePlayItemBtn.cs`
- Existing `Undo`, `Magic`, `Shuffle` normal/locked sprite resources under `Assets/FruitsHarvest/Resources/Original/res/local/coreplay/sprite/item`.

## Implementation boundaries after confirmation

`SlotProgressUtil.Current` clamps the saved progress to 0..5. `SlotEnter.OnRefresh` sets both text and fill from this value. There is no identified numerical defect: 1/5 must remain exactly 20% in Unity. AI fill pixels are illustrative, not the numerical acceptance test. Use the existing horizontal Filled Image, left origin, with a straight clipped partial-fill edge. Keep the actual dynamic TMP label; do not bake 1/5 into a shipped texture. The existing artwork's bright capsule highlight and light empty track make the low-progress state and white text unclear.

`CorePlayItemBtn.Init` loads normal/locked icon sprites by their existing resource paths. `ShowState` reassigns these sprites and calls `SetNativeSize`; changing only the serialized Prefab image would be overwritten. Implement the approved glyphs through these real resources while preserving native dimensions/pivots. Keep the existing Button components, bindings, state objects and original click regions. Retain AD for the undo action in the shown state, the lock on magic, and stock count 1 for shuffle. Do not make locked tools appear usable.

`GameUiWidget.prefab` also overrides the nested SlotEnter progress/track sprites and TMP color/material. Applying an approved progress skin requires synchronizing those specific overrides as well as the source SlotEnter prefab; do not replace unrelated widget settings.

The currently running Unity session was left running. No desktop input, recompilation helper, account changes, ad calls or reward actions were used to make this preview.
