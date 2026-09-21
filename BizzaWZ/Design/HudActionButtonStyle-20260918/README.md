# Top HUD action buttons — proposed style

Status: visual proposal only, pending the user's design confirmation. No production Prefab or shared sprite has been replaced.

Scope: the two existing CurrencyBar `ButtonView` elements shown inside the user's red markup: the approximate cash-value action and `Retirar`. Keep the existing 116 × 60 rectangles, button count, hierarchy, currency meaning, localization and click bindings.

Design: light honey-gold face, slim warm edge, restrained highlight, dark-blue live text. No leaves or branches. The cream currency counters and neighboring HUD controls retain their current design.

## Files and provenance

- `00-user-reference.png`: original user screenshot.
- `button-honey.png`: a new blank button background created with the built-in imagegen tool, not a screenshot and not a text-bearing UI image.
- `generation-prompt.txt`: the exact generation prompt.
- Static preview files produced by the temporary Unity preview helper are **Unity Prefab static previews with a proposed sprite on disposable clones**, not Play-mode screenshots. Production files remain unchanged.

## Implementation recipe after design confirmation

Generated texture: 1774 × 887 RGBA. The main visible alpha ≥ 8 rectangle is (32, 200)–(1740, 679), using top-left image coordinates. The proposed Unity sprite rect is (28, 204, 1718, 486), using Unity's bottom-left texture coordinates. Transparent canvas pixels are not button padding. Preserve the original alpha; no manual painting or compositing was used.

Use one shared sliced sprite for both existing Image components. Keep their original RectTransforms and interactable buttons, with dark-blue TMP text remaining separate. Synchronize `OrchardCommonPass.ApplyCurrency` when applying the approved style because it currently restores the old green appearance.

The preview renderer samples the original values and a long-value case only. It does not invoke ads, withdrawal, account or reward logic, and does not use desktop input.

## Verified result

`Previews/honey-v1/after-static-hud-1080x1920-sample.png` is the proposed visual at the existing HUD layout. The sample and long-value cases both report zero text/glyph overflows, zero button overlaps, and zero HUD cluster overlaps. The before previews retain the original green buttons for comparison. The proposed sprite uses borders (160, 60, 160, 60), 100 pixels per unit and Image pixels-per-unit multiplier 10. The three existing TMP elements use the existing amount-label navy material.

All four source Prefabs were unchanged during rendering. `production-unchanged.json` also confirms the CurrencyBar and GameUiWidget Prefabs, old green texture, and authoring script match their pre-preview hashes. The temporary helper is archived under `Validation` and removed from `Assets`.
