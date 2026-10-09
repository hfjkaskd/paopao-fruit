"""Read-only image measurements. Does not rewrite or generate image pixels."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

out = Path(__file__).resolve().parent
project = out.parent.parent
paths = {
    'reference': project / 'Design/OrchardServiceSystem-20260927/images/11-cash-withdrawal.png',
    'before': out / 'Before/cash-withdraw.png',
    'preview': project / 'Design/OrchardImplementation-20260928/Previews/cash-withdraw.png',
}
regions = {
    'balance': (170, 360, 678, 497),
    'balance_title': (240, 268, 490, 333),
    'amount_section': (60, 530, 460, 591),
    'amount_20': (100, 785, 381, 874),
    'goal_title': (65, 1105, 330, 1170),
    'remaining_hint': (130, 1263, 730, 1323),
    'footer': (100, 1488, 755, 1540),
}
report = {'method': 'Read-only dark text pixel bounds in restricted regions; does not measure complete visual equivalence.', 'regions': {}}
for key, region in regions.items():
    x0, y0, x1, y1 = region
    measurements = {}
    for label, path in paths.items():
        pixels = np.asarray(Image.open(path).convert('RGB'))[y0:y1, x0:x1]
        ys, xs = np.where((pixels[:, :, 0] < 140) & (pixels[:, :, 1] < 125) & (pixels[:, :, 2] < 100))
        measurements[label] = [int(xs.min()) + x0, int(ys.min()) + y0, int(xs.max()) + x0 + 1, int(ys.max()) + y0 + 1] if len(xs) else None
    report['regions'][key] = measurements
(out / 'text-placement.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))
