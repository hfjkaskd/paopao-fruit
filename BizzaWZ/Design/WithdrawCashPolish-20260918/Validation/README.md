# Withdrawal static preview

Retained tool: `WithdrawPreview.cs`. Explicitly copy into an `Assets/**/Editor/` directory and refresh to install; this Design copy is not compiled into the project.

Write `preview:label` into `Design/WithdrawCashPolish-20260918/preview.command`. It waits in Play mode without controlling Play or any UI. Status appears in `preview-state.json`. Native full-page 1080 × 1920 PNGs and JSON reports appear under `Previews/label/` for Image.fillAmount 1, 0.35 and 0. Do not overwrite a pending command.

This is **Prefab static preview with explicit sample text**, not a runtime screenshot or account verification. It uses production GameCanvas and FakeWithdrawPanel, including the six existing nested amount items and original layout components. It creates no replacement page structure. The world-space camera reproduces the formal MatchHeight CanvasScaler. Business scripts, Unity animations and audio are disabled while the hierarchy is still inactive. No wallet, account, saved profile, ad, withdrawal, localization service or gameplay method is called.

Only disposable clones receive the explicitly recorded samples: Portuguese labels read from the existing `tbllanguage.bytes`, balance R$200,76, six amount samples, first-item tag/selection, static progress percentage and fill. Existing OrchardBackdrop receives its real `Assets/OrchardUI/Resources/OrchardUI/Backdrop.png` Sprite directly because runtime loading is disabled. The three progress values are independent geometry stress samples, not a simulated withdrawal state machine.

Reports include track/fill Sprite, borders, image type/fill method/origin, RectTransform dimensions, fillAmount, actual native CanvasRenderer mesh bounds, TMP actual size and generated glyph overflow with a one-pixel tolerance. Before/after SHA-256 verifies the three input prefabs were unchanged. A blank-render detector rejects uniform images. Source compilation was checked against Unity 2022.3.62f3 plus the project's imported TMP/UI assemblies.
