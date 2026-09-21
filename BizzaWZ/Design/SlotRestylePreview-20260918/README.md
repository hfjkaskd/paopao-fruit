# Slot UI restyle — AI appearance previews (2026-09-18)

## Selected previews for confirmation

- [Main slot page — AI preview](07-ai-slot-machine-v3.png)
- [Slot help / paytable — AI preview](09-ai-slot-help-final.png)

These are AI-edited appearance mockups, not Prefab static renders and not Unity runtime captures. The help mockup retains the source screenshot's Unity toolbar and sidebars solely to preserve the original coordinate framing; their presence does not indicate a runtime test.

This task created files only under this Design directory. No Assets, Prefabs, scene, runtime script, economy or reward configuration was changed. Existing work elsewhere in this already modified repository is unrelated to this preview pass. User confirmation of these designs is still required before implementation, as requested in the original brief.

## Appearance

A recognizable three-reel slot machine with deep green enamel, honey wood, fine brass and warm marquee bulbs. Reel window backgrounds surround the existing three live symbol positions. Help uses the same material family with an ivory reward table, dark readable row descriptions and readable cream footer text. Existing bottom confirmation icon is represented by a checkmark.

The original cash/coin meanings, six reward combinations, Portuguese strings, number of controls and hierarchy remain the implementation constraints. All shown balances, localized text and reel symbols stay separate live elements in production; no such content may be baked into new cabinet assets.

## Layout and implementation basis

Read-only current Prefab and script audits were completed before generation:

- [Slot machine audit](machine-audit.md)
- [Help page audit](help-audit.md)
- machine-structure.json and help-structure.json record inspected hierarchy.

Use a dedicated cabinet texture in the existing SlotMachineGroup/Buttom Image, matching its actual 997 × 1575.5061 Rect. Paint the marquee, window surrounds and ornament into existing backgrounds. Keep the existing Spine component and animation completion path that controls reward timing; the current hidden visuals are not a reason to remove it. No added UI nodes or controls are needed.

The selected AI images were visually compared with the originals. They are approximate previews, not pixel-perfect render proofs. Serialized Prefab RectTransforms remain authoritative; any residual image-generation shifts must be resolved by fitting artwork to the original Rects, never by moving controls to fit these mockups. Confirmed implementation must be checked in the actual Unity flow from InitWZ.

## Generation provenance

Mode: built-in image_gen image editing, using the local user screenshots as edit targets. No fallback CLI/API and no programmatic bitmap edits.

- 01-user-machine.png and 02-user-help.png: unchanged copies of user-provided inputs.
- OriginalReference/: original HEAD art extracted read-only to understand machine shape, not production changes.
- prompts.json: first-pass prompts.
- prompt-machine-v2.txt and prompt-help-v2.txt: normalized coordinate refinement.
- [prompts-v3.json](prompts-v3.json): selected main preview prompt and help layout-restoration prompt.
- [prompt-help-final.txt](prompt-help-final.txt): localized final help correction, removing unrequested decorative reels and recoloring the existing confirmation button.

03/04 first drafts and 05/06 second drafts were not selected because their layouts drifted; 04 also had malformed transparent edges. 08 was not selected because it added decorative cherry/7 reels that do not exist in the page. These are retained as iteration provenance only. Use only 07 and 09 for confirmation.

