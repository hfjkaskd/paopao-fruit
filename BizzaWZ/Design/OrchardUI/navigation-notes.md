# Orchard navigation authoring

Editor-only implementation: `Assets/OrchardUI/Editor/OrchardNavigationPass.cs`.

The caller copies the approved source `exec-c4fce03e-38b4-4e10-8fab-ddae96f01eb0.png` to `Assets/OrchardUI/Art/Navigation.png`, invokes `ImportArt()`, then invokes `Apply(root, assetPath)` after all other skin passes and before saving each prefab. The pass has no automatic entry point, runtime component, event assignment, or independent Unity command.

## Atlas

Source inspection confirmed **1254 × 1254 RGBA**, alpha range 0–255. The source contains sparse colored alpha noise in the large gaps. The nine rectangles are measured tightly around the primary connected shapes, excluding those gaps rather than using equally divided grid cells. Tiny edge halos belonging to the source remain; source pixels are not modified.

| Sprite | Source rectangle: x, top, width, height |
| --- | --- |
| Back | 70, 82, 326, 321 |
| History | 462, 83, 326, 320 |
| Help | 855, 83, 326, 320 |
| Close | 70, 471, 326, 320 |
| Settings | 462, 471, 326, 321 |
| Chat | 855, 471, 327, 321 |
| LeavesLeft | 110, 870, 252, 297 |
| LeavesRight | 508, 869, 254, 298 |
| FlowerLeaves | 885, 861, 297, 311 |

Importer converts these top-origin coordinates to Unity's bottom-origin rectangles. Existing sprite IDs are preserved by name through `ISpriteEditorDataProvider` and `ISpriteNameFileIdDataProvider`. Texture settings: multiple sprites, no mipmaps, 100 PPU, NPOT unchanged, bilinear/clamp, non-readable, transparent alpha, uncompressed editor import and ASTC 4×4 on Android/iOS. The same atlas is shared by all icons and leaf ornaments.

## Exact navigation mapping

Only these verified full source paths below the two existing `Z_ReplaceAssets/UI_Frame` directories are matched:

- `Final/.../RealWithdrawPanel/Icon_Back.png` → Back.
- `Final/.../RealWithdrawPanel/Icon_Historiy.png` → History (existing filename spelling preserved).
- `Final/.../RealWithdrawPanel/Icon_FAQ.png` → Help.
- `Common/.../Common/Btn_Close.png`, `Final/.../ServicePanel/Icon_CloseQuickPanel.png`, `Final/.../Withdrawal/Icon_CloseFillPanel.png` → Close.
- `Common/.../GamePanel/Icon_Setting.png` → Settings.
- `Final/.../ServicePanel/Icon_ServiceEnter.png` → Chat.

No filename substring matching is used. Music, sound, vibration, pause/play, reward, payment, send-arrow, and arbitrary glyph artwork are untouched by this pass.

When an old icon is a child of its existing Button, the complete new circular icon goes onto that Button's own Image. The old child GameObject/component/sprite stays in place for all existing serialized references and idempotent re-authoring, but its Image rendering and raycast flag are disabled to avoid double circles. Button size, transform, event bindings and interaction component stay intact. Existing Button sprite states are remapped only when their sprites match the same exact allowlist.

Chat is the one intended layout exception: its existing root becomes 100 × 100, right anchored with x = -18; vertical anchor and anchored y remain. The existing red-dot object is preserved.

## Leaf details

Pairs are added only to enabled, opaque Images using the shared Controls atlas's exact `Title` or `ButtonGreen` roles, with minimum width 260/240 and minimum height 70. Titles use at most 64-unit ornaments; green actions use at most 54. Most of each leaf sits outside the plate's edge, away from its label. Leaf children are first among label children, have raycasts disabled, ignore layout, and follow the existing plate hierarchy and visibility. Existing named leaf children are reused; an existing other atlas leaf decoration prevents duplicates. FlowerLeaves is available in the atlas but not automatically scattered across the interface.

## Verification

Source dimensions, alpha presence, principal connected-component bounds, and the eight exact original source paths were checked before implementation. The pass adds no event or runtime logic, does not modify original hierarchy or script fields, and does not save prefabs itself. Unity import/compilation, execution, and visual verification belong to the root authoring workflow.
