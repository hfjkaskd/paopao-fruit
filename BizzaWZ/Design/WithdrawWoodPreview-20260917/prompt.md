# 提现页木质田园预览提示词

生成方式：内置 image_gen。仅供单页方向确认，未实施到 Unity。

Use case: style-transfer.
Asset type: ONE implementation-constrained AI visual preview of the existing Unity RealWithdrawPanel, portrait 9:16, 1080x1920 composition. Full page only, no phone frame or comparison board.

INPUT ROLES
Image 1 is the EDIT TARGET: existing Unity runtime screenshot runtime-20260917-105727-627.png. It is the sole authority for UI geometry, text positions, typography sizes, content, quantities, selected/locked states, payment brands and currencies.
Image 2 is the user's STYLE REFERENCE ONLY: warm polished honey wood, cream panel interiors, cheerful glossy blue navigation, green CTA, gentle countryside. Never use Image 2 layout, amounts, level, balances or threshold.

PRIMARY REQUEST
Carefully art-direct and refine image 1 toward the attractive 2.5D casual game finish of image 2 using texture, palette, border finish, icon clarity and background painting changes ONLY. Preserve the exact original page composition. This should be implementable by restyling existing Image sprites and text colors without adding, removing, moving or resizing UI elements.

HARD GEOMETRY LOCK in the 1080x1920 composition (origin top-left)
Keep title wood plaque x308..772 y20..146 and title center x540 y82. Existing small leaves stay in their existing positions at its sides, not bigger.
Exactly three nav buttons keep existing centers near (164,89), (817,89), (913,89), diameter 81: back, history, question mark.
Main panel outer bounds x122..958 y158..1123. Keep full height, straight sides, rounded corners and original empty breathing room. Never compress to reference image.
Balance/level/exchange row and the TWO GOLD COIN icons stay around y235..280; preserve gold coin meaning.
Large amount inset x177..899 y290..424 and centered green amount near (540,360).
Selection heading at x176 y483.
Exactly two payment cards: x191..526 and x560..895, y542..705. Preserve original PagBank and pix logos, original sizes and white background. Left selected with green frame/check at bottom right, right unselected.
Two-line explanatory text x180, around y731..795, exact wrapping and content unchanged.
Green Retirar button x295..784 y797..935, same broad rounded rectangular shape and same text position. Keep its existing small side leaves. Do not inflate, move, turn into a capsule, or add a ribbon.
Under-button hint around y965, same position.
Exactly one existing round chat button at right edge, x984..1065 y1051..1133.
SIX cards keep exactly two columns and three rows: left x165..562, right x580..977; rows y1140..1339, y1356..1555, y1573..1771. Retain current card dimensions, level/text positions, top-right multiplier rounded rectangle badges, all amounts, one green check on first card, five locks on other cards.
Footer plaque x156..924 y1770..1897, same existing side leaves and text position.
Do not center the grid differently, align it to the main panel differently, add inset bands behind card amounts, expand the scene bottom, or change any relative placement.

STYLE REFINEMENT
Natural warm honey wood grain with restrained bevel and a softly shaded inner cream edge. More tactile refined warm wood like image 2; less flat orange. Main interior creamy ivory, clean and almost textureless under text; pale butter amount inset. Deep blue readable text, green main amount and emphasized value text, crisp white lettering on title and CTA with subtle brown edge. Keep existing rounded bold game font shapes and sizes.
Existing blue navigation buttons with warm thin gold rim and white original symbols, coherent upper-left soft highlights. Existing green CTA with smooth apple-green highlights and forest-green lower shading. Existing gold-orange multiplier badges remain rounded rectangles and same size.
First card creamy ivory; other five pale sage cream, readable blue-teal text and clear slate locks. Preserve business-state distinction.
Countryside backdrop uses the same sky/hill/trees/houses/grass/flowers/stream motifs and placement as image 1, but noticeably reduce saturation, contrast and tiny details (roughly 30-40%) so content is foreground. Painterly softly simplified distant rolling hills and gentle airy sky; no new large tree or extra buildings. Keep side/bottom background quiet, no bright flower clusters competing with the cards.
Additional ornament only as subtle carved leaf relief INSIDE existing wood rim pixels, with no encroachment into text, logo, amount or clickable areas. No new protruding ornaments or independent visual objects. Existing leaves stay modest.

CONTENT LOCK
Keep every number and all text from image 1. Text represents live localized fields, not production texture lettering.
"Sacar"
"Meu saldo" with "0,00"
"Passe do Nivel :0"
"100≈R$2,50"
"≈R$0,00"
"Selecionar forma de saque"
"PagBank: O valor mínimo de saque é R$0,01. Você"
"ainda precisa de mais R$0,01."
"Retirar"
"Quanto maior o nível, maior o valor de troca!"
Cards row-major: "Nível 1" / "Passe do Nivel 50" / "Passe do Nivel 120" / "Passe do Nivel 250" / "Passe do Nivel 500" / "Passe do Nivel 1000".
Badges row-major: "1.0X" / "1.2X" / "1.4X" / "1.6X" / "1.8X" / "2.0X".
All six card amounts "R$0,00".
Footer "Concluído: Valor a Receber:0,00".
Do not use reference values 0,02 / Nível 3 / R$0,30. Preserve payment logos accurately, do not redesign brand art. Never exchange cash reward meaning for coins; the two existing icons in this specific screenshot ARE gold coins so keep those.

Avoid: layout redesign, new controls, changed line breaks, missing chat, shifted title, new text, flattened real production UI, decorative overlaps, oversized foliage, overly busy scenery. Geometry and existing content always win over style. Output only the finished preview image; the AI-preview identity will be in its external caption.
