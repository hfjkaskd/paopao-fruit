# AI 换皮效果图提示词

使用内置 image_gen；仅供视觉确认，未导入 Unity。

Use case: style-transfer / ui-mockup.
Deliver ONE high fidelity AI VISUAL PREVIEW of a Unity mobile game withdrawal page. Portrait 9:16, 1080x1920 composition. No phone frame, no side-by-side, no added explanatory text in the picture.

INPUT ROLES:
Image 1 (runtime-20260917-103115-427.png) is the EDIT TARGET and ABSOLUTE GEOMETRY / CONTENT AUTHORITY: an actual current Unity running screenshot. Keep the pixel composition, all UI element rectangles, title position, text positions, text sizes, button counts, payment logos, currency meanings, and spacing from this image.
Image 2 (codex-clipboard jpg) is STYLE REFERENCE ONLY: glossy blue/lavender cloud-and-stars theme, pearl white lilac panels, azure button, small warm yellow stars. Do not use image 2's layout or numbers.

PRIMARY REQUEST:
Reskin image 1 in the aesthetic of image 2 by changing existing background artwork, border materials, colors and icon finish ONLY. It must look implementable by replacing the sprites and text colors already on the existing Unity Prefab, with ZERO layout redesign.

LOCKED GEOMETRY at 1080x1920:
- Existing top title plaque stays x309..773, y20..146, text Sacar stays centered at (540,82). Keep the plaque; restyle its wood into pearl white/lilac cloud glass, not remove it.
- Three top round nav buttons stay near centers (165,86), (815,86), (914,86), with original back / history / ? symbols.
- Single main panel outer rectangle stays x123..956, y162..1123. Keep its straight sides and rounded corners. Do not shrink panel to make room for cards.
- Balance labels stay around y245..277. Two GOLD COIN icons remain gold coins, at their existing sizes and positions; no currency conversion, no banknote replacement.
- Large balance inset stays x178..899, y290..425; largest amount stays centered near (540,360).
- Payment selection heading stays x176,y483. Two payment cards stay x191..527 and x560..893, y542..705. Preserve PagBank and pix logos accurately in their original white tile. Only reskin outer selection borders from green/gold to blue/lilac; preserve original logo brand colors and exact size/placement. Left card selected, right not selected.
- Minimum-withdrawal explanatory text stays at x180, two lines around y731..795, same original wrapping, no overlap with the button.
- Primary Retirar button stays x296..783, y798..933, same broad rounded rectangle silhouette (NOT a narrower pill), exact same text center. Reskin vivid green into polished cyan-to-cobalt blue with a clean white highlight.
- One under-button hint stays at y965, same position and size.
- Existing chat tab stays on right screen edge x975,y1047, original silhouette and bubble symbol, exactly one tab.
- Existing SIX tier cards retain all positions and sizes: left column x165..560, right column x582..976; rows y1141..1339, y1358..1555, y1576..1774. Do not compact or shift them. Row-major levels 1,50,120,250,500,1000; all amounts R$0,00; multiplier badges upper right with 1.0X,1.2X,1.4X,1.6X,1.8X,2.0X. First has check at bottom right; other five have locks at bottom right. Keep multiplier badges the same size with warm pastel gold finish. Small star relief may be embossed within their existing badge bounds, never extend over labels.
- Footer plaque stays x157..925, y1773..1893, same text position. Retain the existing gap and alignment.

STYLE:
Soft lilac / periwinkle sky background, much lower saturation, contrast and detail than original landscape; soft cream-white puffy clouds restricted to side margins and extreme bottom, 3-5 tiny warm stars only. NO trees, hills, flowers, houses. Keep main content visually foremost.
Change honey wood frames to luminous pearl-white and pale lavender polished gel / soft ceramic frames with tasteful blue/lilac edge highlights. Panel interiors quiet near-white with slight lavender hue; no see-through busy scenery under text. One small cloud relief along existing panel rim and a tiny crescent embossed in existing top title plaque edge are enough. Decorative art must remain WITHIN current sprite rectangles or in the full-screen background; never occupy text or button areas.
Use original chunky rounded game type shapes, same scale/line breaks, dark navy main text and deep plum secondary hint, very readable. Main amount deep navy, emphasized amounts vivid blue. White Sacar and Retirar lettering with clean restrained blue-violet edge, no changed typographic layout.
First selected tier has bright cyan edge and pale blue-white interior. Remaining five are light lavender-white with clearly legible navy/plum text, soft lavender locks, no dark gray blocking.
Polished rounded 2.5D mobile casual game art, restrained soft specular reflections and clean bevels, coherent upper-left lighting. Match image 2's cozy blue/purple airy feeling while STRICTLY retaining image 1 structure.

TEXT: All text/numbers from image 1 exactly, NOT image 2. Key strings:
"Sacar"
"Meu saldo" with "0,00"
"Passe do Nível :0"
"100≈R$2,50"
"≈R$0,00"
"Selecionar forma de saque"
"PagBank: O valor mínimo de saque é R$0,01. Você"
"ainda precisa de mais R$0,01."
"Retirar"
"Quanto maior o nível, maior o valor de troca!"
"Nível 1", "Passe do Nível 50", "Passe do Nível 120", "Passe do Nível 250", "Passe do Nível 500", "Passe do Nível 1000"
"1.0X", "1.2X", "1.4X", "1.6X", "1.8X", "2.0X"
"R$0,00" on all six cards
"Concluído: Valor a Receber:0,00"

Important: This is a visual design preview, not a flattened production texture. Text represents existing live text fields, payment logos stay existing assets. Do not add new UI nodes, extra buttons, separate decorative ornaments, extra reward symbols, remove elements, reorder, resize or shift controls. Do not copy the reference image's 25/100/300/600/1200 levels or R$0,30 threshold. No watermark. Image 1 geometry always wins over aesthetics.
