# Background production source

- Mode: built-in `image_gen` edit (`style-transfer`).
- Edit target: `Assets/OrchardUI/Resources/OrchardUI/Backdrop.png`.
- Style reference: `Design/WithdrawWoodPreview-20260917/04-ai-wood-preview-final.png`.
- Output: `Backdrop-soft.png`, PNG, **941 × 1672** pixels; copied losslessly from generated output with no scripted pixel modifications.
- Scope: design implementation source only; this subtask did not edit production Assets or Prefabs.
- Visual QA: inspected the saved PNG. No UI, text, labels, or icons. Three houses, meadow, left fence, sky/land framing, and bottom-right stream are retained. Saturation, contrast, and fine foliage detail are softened for the approved quiet background direction.

## Final prompt

```text
Use case: style-transfer
Asset type: production-ready portrait Unity game background only, no UI.
Input images: Image 1 is the EDIT TARGET (the landscape-only Backdrop.png); Image 2 is STYLE REFERENCE ONLY (the approved wood-themed withdraw screen).
Primary request: Edit image 1's background artwork to match the softer, quieter countryside appearance seen BEHIND the UI in image 2. Return ONLY the full background artwork from image 1, with absolutely no UI or text. Keep the exact portrait composition, proportions, framing, and existing scene landmarks of image 1, and original 941 x 1672 pixel dimensions if possible.
Change only the visual treatment: gently reduce saturation and contrast, simplify fine flower/grass details, soften distant edges with a mild atmospheric haze, replace the intense cyan sky with the gentle airy blue of image 2, and soften neon yellow-green foliage to calm sunlit meadow greens. Match image 2's blurred, unobtrusive supporting backdrop feeling, while remaining warm, pleasant and colorful rather than gray or washed out.
Invariants: Preserve every major existing scene feature and its placement: upper expansive blue sky and soft white clouds, original horizon height, left and right rolling fields and trees, the three small red-roof houses in their original positions, lower left wooden fence, grassy flower meadow, the small foreground stream curving along the bottom right. No new subjects. No added buildings or foreground objects. Keep the original amount of sky and land.
Avoid: UI, panels, wooden frames, buttons, labels, currency, coins, logos, lettering, symbols, watermarks, any typography. The reference UI is NOT part of the requested output. Do not paste the UI into the background. Do not crop, zoom, shift or redesign the scene. Do not overblur into a flat abstraction; preserve readable countryside landmarks.
```

