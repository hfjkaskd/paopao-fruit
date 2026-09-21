# Difficulty rhythm update

The original 900-level table and the original group-100 boards are restored. Each attempt keeps the original tile count, positions, layers, IDs, map and retry variant. Difficulty is applied to an independent copy by merging original fruit types; no tile is removed. Existing triplets remain divisible by three.

Editable parameters: BizzaWZ/Assets/FruitsHarvest/Resources/HarvestDifficulty.json.

- Levels 1–5: maximum three types.
- Level 6 onward: repeat limits 3, 3, 6, 3, 8 every five logical levels. Original boards continue progressing; the five-level rhythm does not recycle the first five boards.
- Types are only reduced; a board with fewer original types is not expanded.
- 20%/40% are desired failure rates, not measured results. The six/eight-type limits are initial tuning and require player trials.
- Reward-close interstitials are suppressed for source levels 1–3, including level-three victory after the selected level advances. Protected closes clear the counter and do not carry into level four. Voluntary rewarded-ad behavior is unchanged.
- A saved board from the old tuning revision restarts the same level once. The level number, tutorial progress and inventory are retained; subsequent interrupted boards resume normally.

Validation outputs: difficulty-cycle-validation.txt compares Unity-loaded levels 1–15 with their original layouts/counts and verifies triplet totals; intro-interstitial-validation.txt tests the actual reward-close gate repeatedly with over-threshold counters. Desktop UI interaction is not covered by these checks.
