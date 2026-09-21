# Guide finger sorting — 2026-09-10

The finger DynamicCanvasLayer auto OnEnable overwrote the ordering assigned by NewPlayerGuider.Awake, placing the hand below the tutorial mask. Removed competing automatic layer components from the tutorial mask/finger. Configured the prefab canvases: mask -900, finger/text -898; highlighted fruit remains -899. Removed runtime creation and reassignment of static guide canvas layers.

Unity Editor 2022.3.62f3 / Android target: compilation passed. Replayed the guide using the regular TryStartGuide/StartGuide implementation; the completed flag was restored synchronously without saving. Runtime canvas orders confirmed -900 < -899 < -898, and the hand appeared fully lit above the blueberry. Clicking the pointed blueberry collected it and moved the hand to the next blueberry. Temporary preview command removed. No device test.
