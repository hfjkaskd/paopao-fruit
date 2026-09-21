# AddPropPanel button and limit alignment

## Applied changes

- Centered the existing rewarded-ad icon and localized button label as a group.
- Reduced the existing cash corner badge and kept it clear of the label.
- Moved the dynamic limit text above the bottom frame with a 46-unit gap below the button rectangle.
- Updated the existing editor authoring pass so it retains these prefab settings.

Production files:

- `Assets/BizzaWZ/Final/Real/UI/AddPropPanel/AddPropPanel.prefab`
- `Assets/OrchardUI/Editor/OrchardCommonPass.cs`

The node hierarchy, component set, button bindings, localization keys, sprite references, reward logic and existing disabled button decorations are unchanged. `structural-verification.json` records the 27 serialized field edits.

## Visual verification

`00-user-reference.png` is the user-provided runtime screenshot.

`after-AddPropPanel.png` is a Unity-rendered isolated **Prefab static preview**, not a runtime screenshot or an AI mockup. Portuguese labels, the undo icon and the Brazil cash sprite were assigned only on a disposable preview clone to reproduce the reported display state. Production localization and country-based sprite selection remain intact. `before-AddPropPanel.png` is the prior static layout using serialized title and currency defaults.

`after-validation.json` reports four active text elements with no overflow or truncation. Unity reported zero compilation errors. The ad, claim and account flows were not executed. No desktop input or Play mode changes were made.

The temporary rendering helper is archived under `Validation` and removed from `Assets` after validation. Original files and exact diffs are retained in this directory.
