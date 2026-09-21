# Baseline script reference repairs

These repairs address pre-existing script faults that blocked Unity's `SaveAsPrefabAsset` during the orchard authoring pass. They do not add gameplay behavior or change the UI hierarchy. The original prefabs are retained under `Design/OrchardUI/Before/Assets/`.

## Proven type reference recovery

| Prefab | Old script GUID | Recovered script GUID | Evidence |
| --- | --- | --- | --- |
| `Assets/BizzaWZ/Common/BizzaGame/WhiteWinPanel/WhiteWinPanel.prefab` | `13d32c38f28b6be4f9dd96cc08e6a2b4` | `4a0d5d7a3cd02944ca6de4431fdbec8e` | The existing `SnakeEscape.Recovered.RecoveredVictoryPanel` MonoBehaviour matches the complete serialized payload: `m_pageId`, `canvasGroup`, `hero`, `card`, `titleText`, `bodyText`, `nextButton`, and `skin`. The page ID remains `WhiteWinPanel`. |
| `Assets/BizzaWZ/Common/UI/SettingPanel/PausePanel.prefab` | `5f4041fcb99834045898e5efd3407a77` | `15f5c58fe51ec5a459e200904f65a6bd` | The component on the existing `VersionLog` object serializes `tMP_Text`, matching `Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/VersionLog.cs`. |

Only the script GUID changed in each repair. Existing fields and object references were preserved. The victory page is the framework's existing compatibility branch; the real-mode flow continues using its existing reward page. The current `VersionLog` class has its old display logic commented out, so this repair does not introduce a new version display.

## Empty MonoScript cleanup

`Assets/BizzaWZ/Final/Framework/Runtime/Misc/HideInReviewMode.cs` has GUID `6437d74000d96514ba0ea70e74b9dcdf`, but its entire class is commented out. It therefore supplies no MonoBehaviour type even when `BIZZA_REAL_WITHDRAW` is defined. Unity could not save prefabs with this empty type reference.

The following component list entries and their corresponding serialized MonoBehaviour blocks were removed:

- `Assets/BizzaWZ/Final/MenuSystem/Common/CurrencyBar/CurrencyBar.prefab`: component IDs `1440285596579270237` and `2505454896082585396`.
- `Assets/BizzaWZ/Final/Real/UI/BadgeAndDaily/ItemForCountry.prefab`: component ID `2048194175437563492`.

Before removal, each component ID was verified to have no inbound serialized references except its own GameObject's `m_Component` list. No object, active state, visual component, business reference, or review-mode behavior was changed. The commented feature was not restored.

## Focus tutorial investigation

`Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/UITeach/UITeachMaskFocusPage.prefab` contains two additional missing scripts:

- `MaskParent/MaskClick`: component `1008440906262682425`, script GUID `04c6fab8f97d5c343aa9d757606c0ce5`, no serialized fields beyond the MonoBehaviour header.
- `MaskParent/MaskClick/Mask`: component `5487575077436927723`, script GUID `6751abf0e7fc90d42b21bcd31e21efae`, one `animation` reference to the existing Animation component `3619667077405012926`.

Neither GUID resolves to a current script/meta file or recoverable tracked implementation. Both component IDs have no inbound references except their own GameObject component lists. The current `UITeachMaskFocusPage` root script uses only its `maskParent` and `block` bindings to size, position, and follow the target; both remain intact. No production caller of `UI_TeachMaskFocus` was found. The main tutorial uses the separate `UITeachMaskPage`.

There is insufficient evidence to map either unknown component to another class. In particular, the existing `TransitionBlock` also has an `animation` field but deactivates itself in `Awake`; it is not a safe substitute. The two nonfunctional script entries and their own component list references were removed, preserving the complete hierarchy, existing Image/Animation components, and root page logic. Their previous behavior cannot be recovered from the available project. After removal, verified the two affected GameObjects, existing Image/Animation components, root page component, and its `maskParent`/`block` bindings are still present.

## Button target graphics

The baseline also reports 123 Buttons with no serialized `targetGraphic`. A safe visual repair is to use the existing meaningful non-text Graphic on the Button's own GameObject when present. A blanket selection of child Images is unsafe: several buttons have dynamic status images, masks, tutorial hit targets, or nested buttons. These warnings do not justify changing events or business hierarchy.

## Verification scope

Verified the two old script GUIDs no longer occur in their production prefabs and each replacement occurs once; verified the empty HideInReviewMode GUID is absent from the two repaired production prefabs; verified the Focus page's two isolated missing components are gone while its valid objects and bindings remain. Unity import/save and the final visual/business-reference audit are performed by the root authoring task. No runtime behavior or authoring scripts were executed by this repair investigation.

Compared every original GameObject ID, name, and active state against the retained baseline after these repairs: all were preserved in WhiteWinPanel (9), PausePanel (45), CurrencyBar (29), ItemForCountry (9), and UITeachMaskFocusPage (6).
