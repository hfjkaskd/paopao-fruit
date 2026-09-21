# HUD Prefab static validation

`HudSpacingPreview.cs` is a temporary Unity Editor verification tool. The file in this directory is the retained source; it only becomes active when explicitly copied to an `Assets/**/Editor/` directory and imported. It does not modify or save production assets.

Write one command into `Design/HudSpacing-20260918/preview.command`:

- `preview:label`: two samples at 1080 × 1920 (ordinary values and long values).
- `preview-all:label`: eight samples: both value sets at 1080 × 1920 and 1080 × 2340, each with normal and simultaneous 1.25 currency-parent scale.

Do not overwrite a pending command. During Play mode or a pending Play transition, the command stays pending. The tool never starts or stops Play mode, selects a window, changes the active user scene, invokes buttons, or opens gameplay pages.

State: `Design/HudSpacing-20260918/preview-state.json`.
Outputs: `Design/HudSpacing-20260918/Previews/label/`, containing native 1080 × 320 top-of-render PNGs, matching geometry JSONs, and a batch `report.json`.

The tool stages production GameCanvas → BaseLayer → RealGamePanel/Content → GameUiWidget/CurrencyBar in a disposable inactive PreviewScene hierarchy. Business components, animations and audio are disabled before activation. Sibling branches are hidden only in the disposable instances. The formal CanvasScaler reference resolution and MatchHeight setting determine world-space canvas size and render-camera projection. The serialized RealGamePanel/Content margins remain intact; device-specific safe-area adaptation is not simulated.

Production Image, TMP, Button and LayoutGroup components are retained. Each sample changes existing TMP text only: level, coin amount, cash amount, conversion price, approximate sign and localized withdrawal label. Two transient reward labels receive an explicitly empty resting sample. Extended cases scale the same currency parents used by the existing animation and rebuild the existing layout. No account, saved profile, economy, localization lookup or runtime business method is executed.

Every JSON is labeled **Prefab static preview**, not a Unity runtime capture. It records active/inactive component rectangles, resolved sprite paths, TMP overflow and actual font sizes, Button click-rectangle estimates, conservative visual cluster bounds, layout settings and before/after SHA-256 of the four input prefabs. A non-uniform-pixel check rejects blank renders. Geometric rectangle overlap is a conservative diagnostic, not a real input dispatch or alpha-silhouette test.

`glyphOverflowsRect` additionally compares generated glyph bounds against the text rectangle (one-pixel tolerance), with `glyphOverflowPixels` in left/bottom/right/top order. This independent check matters: the first successful long-value sample reported TMP `isTextOverflowing=false` despite conversion-price glyphs extending about 9.28 pixels beyond the text rectangle's right edge.

The source compiles against the project's Unity 2022.3.62f3 and imported TMP/UI assemblies. First-pass rendering exposed null textInfo on inactive TMP components; the retained source guards that state and never forces inactive business content to initialize.
