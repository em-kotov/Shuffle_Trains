# Puzzle layout and capacity tuning

The original 30 layouts retain each cat's body color and length. Level 25
exchanges the balls of cats 10 and 11 so every parked cat starts with a ball
different from its body color. Ball color totals, cat counts, station masks,
rewards, and progression are unchanged.

The campaign now contains 50 layouts. Levels 31–50 add distinct parking
arrangements and balanced color inventories, alternating mixed-length boards,
10–12 single-cell cats, and eight two-cell cats. Each new board uses four colors
and four stations, starts every cat with different-color balls, and has a
verified winning order and a losing order. Rewards remain 1,000 per level and
100 per completed cat. Levels 1–30 are unchanged by this extension.

## Capacity and color choices

| Levels | Track slots | Intent |
| --- | --- | --- |
| 1 | 2 | Two-cat tutorial, no losing order |
| 2–15 | 2 | Find a useful color partner before spending the last slot |
| 16–28 | 3 | Short color chains with one spare slot for setup |
| 29 | 4 | Extra room on the largest, twelve-cat board |
| 30 | 3 | Two-cell-cat puzzle |
| 31–38, 40–48, 50 | 3 | Mixed boards and short color chains |
| 39, 49 | 4 | Extra room on twelve-cat boards |

Whole cats are reassigned to compatible same-size parking footprints. This keeps
the board shapes, direction variety, and short blocking chains while changing
which color a release exposes. Level 25 additionally uses the ball exchange
described above; its solution and inventory fingerprint have been updated.

Each level after the tutorial has both a winning order and a losing order even
when sorting is allowed to settle between releases. Exact uniform random-choice
completion probabilities in this settled model are about 49–81%, mostly 50–75%.
These probabilities are balancing diagnostics, not measured human difficulty.

## Loss detection

An empty station no longer automatically means the player can recover. When no
more cats can enter, loss detection checks actual pickup/drop possibilities for
cats physically on the track. A parked color partner cannot rescue a full track.
It defers during sorting, reserved entries, completed exits, and parking slides.
The existing six-second polling interval is retained; it does not impose a timer
on player decisions. A full track is safe as long as a useful exchange remains.

## Verification

- `python Tools/rebalance_puzzles.py`: inventory fingerprints, capacities,
  bounds, head/tail overlaps, blocking cycles, opening choices, facing settings,
  winning orders, losing orders, and exact settled random-choice probabilities.
  Also checks ordered sequence references, level numbers, report coverage,
  balanced color totals, and passenger counts across all 50 levels.
- `python Tools/check_sprite_facing.py`: all 16 actual prefab rotations in four
  parking directions, plus single-cell left/right track facings. This includes
  green's reversed artwork.
- `Tools/test_capacity.ps1`: compiles the production sorting and deadlock classes
  with inert rendering/transform stubs, replays all 50 winning and 49 losing
  orders, and checks empty stations, partial pickups, full stations, in-flight
  sorting, parked partners, reserved entries, and completed exits.
- `Tools/test_hints.ps1`: completes all 50 levels by following the production
  hint planner through the production sorting logic.

`puzzle_layout_report.json` records capacities and one-based `_cats` indices for
`solution` and `losing_order`. Wait for useful sorting to settle between releases
when reproducing these witnesses. `poses` are head X/Y and direction (0 right,
1 left, 2 up, 3 down). Inventory SHA-256 fingerprints cover every cat's ordered
body color, length, and balls.

The headless tests execute real sorting logic but do not simulate scanner
physics, traffic timing, partial blocked slides, or the displayed loss panel.
Unity Play Mode testing is still needed for those behaviors. Runtime scripts
were also compiled against this project's Unity references with Unity's compiler.

`--write` retunes capacities and color placement from the current board shapes
using a fixed search seed; it updates the report after verification. Review the
asset diff before accepting a newly generated pass.
