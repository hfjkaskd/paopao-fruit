# Orchard Trio loading artwork

- Source: the user-provided 941 x 1672 PNG, copied byte-for-byte to `Assets/OrchardUI/Resources/OrchardUI/LoadingArtwork.png`.
- Startup remains `Assets/Game/Resources/Scenes/InitWZ.unity` and the existing framework `LoadingPanel`/loading events.
- The prefab's dedicated artwork Image uses Envelope Parent aspect fitting. Its overlay Images use normalized anchors, so the image and progress stay aligned across portrait aspect ratios.
- The PNG is imported as four sprites: Full, EmptyTrack, EmptyCap, Fill. The cream track samples cover the baked sample fill; the foreground Image uses the existing actual loading progress. Its 20 px left/right Sprite borders are sliced: only the center stretches, preserving both rounded ends. The PNG pixels are never repainted.
- `OrchardLoadingArt` asynchronously loads the single texture from Resources, resolves the four Sprite sub-assets, then reveals the authored CanvasGroup. No large texture is strongly referenced by the prefab, and the shared menu background cache remains unchanged.
- Android/iOS import settings use ASTC 6x6, no mipmaps, and no CPU-readable pixel copy.
- `LoadingPanel.before.prefab` captures the pre-task working copy, including earlier user changes. It is a review backup, not an alternate game entry point.

Verification artifacts are recorded in `preview-report.json` and `startup-report.json` when the explicit editor checks run. Static previews are isolated prefab renders and do not substitute for the formal startup observation.

Verified on 2026-09-18 in Unity 2022.3.62f3:
- 9 static prefab renders passed (941x1672, 1080x2400, 1536x2048; 0/50/100%). Active asset-reference checks reported zero errors.
- Formal InitWZ Play startup observed actual progress from 0 through 0.19, 0.33, 0.43, 0.54, 0.65, 0.77, 0.88, and 0.99. Artwork bound successfully; LoadingPanel closed normally, and HarvestBridge.Ready became true.
- Unity reported zero compilation errors after restoring the unrelated SDK import.
- No Android/iOS device build was performed. Envelope Parent crops image edges on differing screen ratios; it does not stretch the artwork.
- Source PNG SHA-256: 90DAF5BCFDE19DACF6E0D87540F43A39F2F9B67E228E8905DDED0EB64CB36CF7.
- Unrelated AppLovin import was fully undone: tracked SDK files restored to their initial clean state and all package-cache files verified against original 8.6.3. Recovery copies are outside the project at C:/Projects/paopao-validation-private/loading-import-recovery-20260918.


Rounded-fill correction (2026-09-18):
- Replaced horizontal clipping with a sliced Image inside a prefab-authored full-width track.
- Existing loading events call OrchardLoadingArt.SetProgress; normalized fill width follows progress and cap borders scale with artwork height.
- 0% stays empty. The first visible fill has the minimum round-cap width, avoiding a pointed/sliver shape at very small progress. fillAmount continues to store the actual progress.
- Static verification now includes 0%, 1%, 35%, 50%, 100% across all three aspect ratios (15 renders).
- configure_loading.py is the original import record, predating this correction; the current serialized prefab and metadata are authoritative.
- Rounded-fill runtime recheck passed at 09:24 UTC: actual InitWZ progress advanced through 19%, 32%, 44%, 55%, 67%, 78%, 89%; LoadingPanel closed normally, gameplay became ready, zero compilation errors.

Clean-fill correction (2026-09-18):
- Replaced the opaque screenshot-derived Fill binding with `Assets/OrchardUI/Art/LoadingFillClean.png`, extracted with built-in image_gen as an actual transparent capsule. Prompt and asset details are in `clean-fill-art-prompt.md`.
- The new fill is a lightweight prefab-referenced Sprite (512px texture limit); the large background remains asynchronous. No runtime C# logic changed in this correction.
- Removed the old Fill entry from the atlas binding list so it cannot overwrite the transparent fill. The full artwork and empty-track bindings remain unchanged.
- Matched the fill track to the clean 33px green silhouette; nine-sliced caps remain round.
- 15 Unity prefab previews passed at 0/1/35/50/100% for three aspect ratios, with zero missing active references and zero compilation errors. Runtime startup was not rerun for this asset-only correction.

Borderless-fill correction (2026-09-18, 10:02 UTC):
- The first clean fill still contained a dark green bevel and lower band. Replaced it with a bright green transparent capsule without dark perimeter shading; rounded caps and the orange outer track remain.
- Preserved the Sprite GUID and sub-asset ID. Updated only the fill texture and its crop/slice metadata; runtime progress logic is unchanged.
- Built-in image_gen edit prompt and saved asset path are recorded in `clean-fill-art-prompt.md`. The previous texture and metadata are backed up outside Assets as `LoadingFillClean.before-borderless.png` and its `.meta` file.
- The preview validator now also exports close-ups directly from the Unity render target. At 10:02 UTC, all 15 previews passed with zero reference errors, and Unity reported zero compilation errors. Inspected 35%, 100%, and minimum round-cap close-ups to confirm the lower band is absent.
- Runtime startup was not rerun for this texture-only visual correction.
