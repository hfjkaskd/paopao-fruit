# Fruit click sorting verification — 2026-09-10

Cause: the primary ItemImage had an independent override-sorting Canvas (via DynamicCanvasLayer). Visible/raycast order disagreed with CollectItem.Priority used by occlusion. Item_16_9 received down, up and click, but was rejected at 0.801–0.833 occlusion despite appearing in front.

Fix: remove the primary image Canvas, DynamicCanvasLayer and GraphicRaycaster from CollectItem.prefab. The image inherits the fruit root canvas and follows hierarchy order. Preserve effect canvases and the standard InputMono/Button. HarvestSetup enforces the same configuration on future setup runs.

Validation: Unity 2022.3.62f3, normal InitWZ startup, Android build target, 1080x1920 Editor Game view, level 5. Raycast order now places Item_55_35 and Item_36_52 ahead of Item_16_9. Clicked the exposed grape, orange, then revealed peach: each entered the tray, board 60 -> 59 -> 58 -> 57, basket 0 -> 1 -> 2 -> 3. Tray visuals remain visible. Prefab local references checked. Temporary pointer tracing removed after validation. No Android/iOS device run.
