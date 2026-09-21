# Clean green loading fill

Built-in image_gen edit/extraction, 2026-09-18.

Input: `Assets/OrchardUI/Resources/OrchardUI/LoadingArtwork.png`.

Saved output: `Assets/OrchardUI/Art/LoadingFillClean.png`.

The PNG retains generated transparency without raster post-processing. The Unity Sprite rect clips transparent padding and faint stray alpha pixels outside the capsule. The lightweight fill texture is limited to 512 pixels, has no mipmaps or CPU-readable copy, and uses ASTC 4x4 on mobile. Only the large scene artwork continues to load asynchronously through Resources.

Original extraction prompt (superseded by the borderless revision below):

Use case: background-extraction. Asset type: production Unity UI Sprite, isolated glossy green loading-progress fill on a truly transparent alpha background. Input image is the edit target. Extract ONLY the green horizontal pill-shaped fill located near the bottom of the reference loading screen. Preserve its saturated bright green color, smooth glossy top highlight, subtly darker green shading underneath, and smooth semicircular rounded ends. Remove every bit of the orange/gold outer progress-bar frame, cream empty-track background, scenery, lettering, and shadows outside the green silhouette. No outline/border or rectangular band above the fill. Output only ONE long horizontal green capsule with soft antialiased transparent edges, front view, fully filled green from left round cap to right round cap, suitable for Unity nine-slicing. Keep the capsule around 8:1 width-to-height like the reference (reference green fill about 298x37 pixels), centered and large in a wide transparent image, small transparent padding. Absolutely no orange, gold, brown, cream, text, border, checkerboard artwork or background. Do not reproduce the complete loading screen.

## Borderless revision, 2026-09-18

Mode: built-in image_gen edit. Input: the previous transparent fill, backed up as `Design/OrchardLoading/LoadingFillClean.before-borderless.png`. Saved output: `Assets/OrchardUI/Art/LoadingFillClean.png`. The new PNG removes the dark green lower band and perimeter shading. Sprite metadata trims transparent padding without editing pixels; its GUID and sub-asset ID are preserved.

Exact edit prompt:

Use case: precise-object-edit. Edit this existing transparent green capsule UI sprite. CRITICAL CORRECTION: remove its thick dark-green perimeter, bevel, bottom dark band and all dark edge shading completely. The user does NOT want ANY outline or border. Keep the same long horizontal pill silhouette and true transparent background. Repaint the pill with a simple, uniform vivid lime-green fill (approximately #25DA19), optionally a very soft pale-green highlight confined to the upper interior. The green must stay bright all the way to its antialiased alpha edge, including the entire bottom edge and both rounded caps. NO dark green pixels along the bottom, NO dark contour, NO rim, NO stroke, NO embossed/inset look, NO bevel, NO shadow, NO bottom band. Make it a clean flat game progress fill with semicircular ends; a tiny soft upper sheen is fine but not an outline. Preserve the input's overall canvas and pill position/size if possible (wide pill centered horizontally, about 8.4:1 aspect ratio, input is 2095x750 with pill roughly x87..2009, y260..487). Output only the borderless capsule on real RGBA transparency. Do not add a progress track, orange frame, text, background, checkerboard, or extra shapes.
