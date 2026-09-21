# HUD spacing analysis

This is a read-only Prefab layout analysis, not a Unity runtime capture. Production files were not changed by this audit.

The production chain is `RealGamePanel/Content` → dynamically loaded `GameUiWidget` → nested `CurrencyBar`. Restrict authoring changes to instance `461910548945330748` in `GameUiWidget.prefab`, with source GUID `b1f781d8c27eec54db58a4849081f7e5`. The shared CurrencyBar asset serves other pages and does not need to change.

The current horizontal layout has negative spacing (`-31.54`), a left padding override of `19`, and uses child scale when allocating width. CurrencyBar's existing rewards animation scales the entire corresponding currency group to 1.25 and back. Consequently, the horizontal layout moves the other currency group during the animation. The existing cash Button rectangle is also wider than its visible card and reaches the settings area.

`hud-layout-proposal.json` contains exact source component IDs, override property paths, text constraints, and measured analytical rectangles. `analyze-hud-layout.py` rebuilds the report without writing Assets or sending Unity commands.

The proposal keeps the same row order, hierarchy, buttons, events, amount text, localization and animation code. It fixes group allocation widths, makes the layout ignore animation scale, introduces space between groups, uses a smaller compact settings button and lighter level badge, and bounds both existing withdrawal buttons to their visible currency cards.

Analytical checks cover 1080×1920 and 1080×2340, normal scale, each currency group at 1.25 individually, both groups at 1.25 together, and the existing green button tween at 1.08. All proposed visual and hit rectangles remain separate; the minimum gap in the extreme state is 5.29 px at 1080×1920 and 6.44 px at 1080×2340. Sprite transparent margins can add visible separation. The level text is centered six units lower to align with the face of the new sprout badge.

Six TMP objects use no wrapping and zero margins. Serialized font advances estimate that `99.999,99` fits its 102-unit amount box at about 21 points, `9999` fits the 88-unit level box at about 35 points, and `R$0,00` fits its 76-unit box at about 22.6 points. U+2248 (`≈`) is supplied by an existing fallback, so actual TMP rendering must validate it; these estimates do not replace a Unity render.

Bindings to preserve are `CurrencyBar.curLevelTxt`, `coinTxt`, `dollarTxt`, `clashTxt`, `coinBtn`, `dollarBtn`, `settingBtn`, `coinImg`, `dollarImg`, `coinBox`, `dollarBox`, and `RealUiWidget.levelTxt`. CurrencyBar binds settings and both withdrawal entries in Awake, and RealUiWidget refreshes the level text on the existing game-start event. Neither implementation needs a code change.
