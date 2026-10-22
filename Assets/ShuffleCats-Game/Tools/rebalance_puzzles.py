"""Deterministic layout authoring and conservative release-order checks.

Run with --write to author assets, otherwise validate the current layouts.
Uses only the Python standard library. The sorting model settles complete laps
between releases; it does not certify animation timing or Unity physics.
"""
import argparse
from collections import Counter
import functools
import hashlib
import json
from pathlib import Path
import random
import re

ROOT = Path(__file__).resolve().parents[1]
DIRS = ((1, 0), (-1, 0), (0, 1), (0, -1))
BLOCK = r"  - _color:.*?(?=  - _color:|  _activeStations:)"
NAMES = ['Blue', 'Green', 'Pink', 'Yellow', 'Purple', 'Orange', 'Brown', 'Iridiscent']

def field(text, name):
    return re.search(r'(?m)^\s*(?:- )?' + name + r': (.*)$', text)[1].strip()

def read(path):
    text = path.read_text()
    cats = []
    for block in re.findall(BLOCK, text, re.S):
        cell = tuple(map(int, re.findall(r'-?\d+', field(block, '_cell'))))
        d = (0 if int(field(block, '_orientation')) == 0 else 2)
        d += float(field(block, '_signDirection')) < 0
        balls = bytes.fromhex(field(block, '_passengers'))
        cats.append(dict(color=int(field(block, '_color')), length=int(field(block, '_length')),
                         balls=tuple(int.from_bytes(balls[i:i+4], 'little') for i in range(0, len(balls), 4)),
                         pose=(*cell, d)))
    return text, cats, sum(bytes.fromhex(field(text, '_activeStations'))), int(field(text, '_maxCarsOnTrackCount'))

def inventory(cats):
    return [(c['color'], c['length'], c['balls']) for c in cats]

def footprint(cat, pose):
    x, y, d = pose
    dx, dy = DIRS[d]
    return [(x-dx*k, y-dy*k) for k in range(cat['length'])]

def dependencies(cats, poses):
    occupied = {}
    for i, (cat, pose) in enumerate(zip(cats, poses)):
        for p in footprint(cat, pose):
            if p in occupied or not all(0 <= v < 4 for v in p):
                return None
            occupied[p] = i
    deps = []
    for i, (x, y, d) in enumerate(poses):
        dx, dy = DIRS[d]
        mask = 0
        x, y = x+dx, y+dy
        while 0 <= x < 4 and 0 <= y < 4:
            if (x, y) in occupied and occupied[x, y] != i:
                mask |= 1 << occupied[x, y]
            x, y = x+dx, y+dy
        deps.append(mask)
    return deps

def layers(deps):
    remaining = (1 << len(deps))-1
    result = []
    while remaining:
        free = [i for i, d in enumerate(deps) if remaining >> i & 1 and not d & remaining]
        if not free:
            return []
        result.append(free)
        for i in free:
            remaining &= ~(1 << i)
    return result

def settle(cats, active, stations):
    # Mirrors SorterCar/PassengerTracker: one color per station, four seats;
    # drop wrong balls before collecting body-color balls. Empty stations skip
    # a cat if every wrong color already has a station elsewhere.
    active = [(i, list(wrong), right) for i, wrong, right in active]
    stations = [list(s) for s in stations]
    for _ in range(100):
        changed = False
        for k, (i, wrong, right) in enumerate(active):
            for station in stations:
                colors = {s[0] for s in stations if s}
                drop = station[0] if station else (wrong[0] if any(c not in colors for c in wrong) else -1)
                for ball in wrong[:]:
                    if ball == drop and len(station) < 4:
                        wrong.remove(ball)
                        station.append(ball)
                        changed = True
                while station and station[0] == cats[i]['color'] and len(wrong)+right < cats[i]['length']:
                    station.pop()
                    right += 1
                    changed = True
            active[k] = (i, wrong, right)
        active = [a for a in active if a[1] or a[2] < cats[a[0]]['length']]
        if not changed:
            break
    return tuple((i, tuple(w), r) for i, w, r in active), tuple(tuple(s) for s in stations)

def solve(cats, deps, station_count, limit):
    full = (1 << len(cats))-1
    @functools.lru_cache(None)
    def search(mask, active, stations):
        if mask == full and not active:
            return ()
        if len(active) >= limit:
            return None
        choices = [i for i, d in enumerate(deps) if not mask >> i & 1 and d & ~mask == 0]
        choices.sort(key=lambda i: -sum(len(s) for s in stations if s and s[0] == cats[i]['color']))
        for i in choices:
            a, s = settle(cats, active+((i, cats[i]['balls'], 0),), stations)
            result = search(mask | 1 << i, a, s)
            if result is not None:
                return (i,)+result
        return None
    return search(0, (), ((),)*station_count)

def random_success(cats, deps, stations, limit, rng, count=48):
    successes = 0
    for _ in range(count):
        mask, active, storage = 0, (), ((),)*stations
        for step in range(len(cats)):
            options = [i for i, d in enumerate(deps) if not mask >> i & 1 and not d & ~mask]
            if not options or len(active) >= limit:
                break
            i = rng.choice(options)
            mask |= 1 << i
            active, storage = settle(cats, active+((i, cats[i]['balls'], 0),), storage)
        else:
            successes += not active
    return successes/count

def track_limit(number):
    return 2 if number <= 15 else 4 if number in (29, 39, 49) else 3

def target_success(number):
    return 1.0 if number == 1 else .75 if number <= 6 else .65 if number <= 15 else .55

def release_audit(cats, deps, station_count, limit):
    """Exact uniform-choice probability and a losing witness, with settled laps.

    A failure here cannot be rescued just by waiting longer. This deliberately
    gives random play maximum sorting time; it is not a real-time click test.
    """
    full = (1 << len(cats))-1
    @functools.lru_cache(None)
    def visit(mask, active, stations):
        if mask == full and not active:
            return 1.0, None
        choices = [i for i,d in enumerate(deps) if not mask >> i & 1 and not d & ~mask]
        if len(active) >= limit or not choices:
            return 0.0, ()
        total, failure = 0.0, None
        for i in choices:
            a,s = settle(cats, active+((i,cats[i]['balls'],0),), stations)
            probability, suffix = visit(mask | 1 << i,a,s)
            total += probability
            if failure is None and suffix is not None:
                failure = (i,)+suffix
        return total/len(choices), failure
    return visit(0,(),((),)*station_count)

def tune_colors(cats, number, stations, limit):
    rng = random.Random(260923+number)
    original = [c['pose'] for c in cats]
    groups = [[i for i,c in enumerate(cats) if c['length']==length] for length in (1,2)]
    best, seen = None, set()
    for attempt in range(1500):
        poses = original[:]
        for indices in groups:
            shuffled = [original[i] for i in indices]
            if attempt:
                rng.shuffle(shuffled)
            for i,pose in zip(indices,shuffled):
                poses[i] = pose
        key = tuple(poses)
        if key in seen:
            continue
        seen.add(key)
        deps = dependencies(cats,poses)
        exits = layers(deps)[0]
        if len(cats)>2 and len({cats[i]['color'] for i in exits})<2:
            continue
        solution = solve(cats,deps,stations,limit)
        if solution is None:
            continue
        rate = random_success(cats,deps,stations,limit,rng,64)
        if number>1 and not .35 <= rate <= .90:
            continue
        rate, _ = release_audit(cats,deps,stations,limit)
        if number>1 and not .45 <= rate <= .85:
            continue
        score = abs(rate-target_success(number))
        if best is None or score<best[0]:
            best = score,poses,solution,rate
            if score < .015:
                break
    if best is None:
        return generate(cats,number,stations,limit)
    return best[1:]

def generate(cats, number, stations, limit):
    rng = random.Random(230926 + number)
    n = len(cats)
    target_free = 1 if n == 2 else 2 if n <= 6 else 3
    best = None
    valid = 0
    # Place long cats first. Full domino boards need many packing attempts.
    order = sorted(range(n), key=lambda i: -cats[i]['length'])
    for attempt in range(100000):
        poses, occupied = [None]*n, set()
        for i in order:
            options = []
            for x in range(4):
                for y in range(4):
                    for d in range(4):
                        cells = footprint(cats[i], (x, y, d))
                        if all(0 <= a < 4 and 0 <= b < 4 and (a,b) not in occupied for a,b in cells):
                            options.append((x,y,d))
            if not options:
                break
            poses[i] = rng.choice(options)
            occupied.update(footprint(cats[i], poses[i]))
        if None in poses:
            continue
        deps = dependencies(cats, poses)
        waves = layers(deps)
        if not waves or len(waves[0]) != target_free or not 2 <= len(waves) <= (3 if n <= 6 else 4):
            continue
        if len({p[2] for p in poses}) < min(3, n):
            continue
        if n > 2 and len({cats[i]['color'] for i in waves[0]}) < 2:
            continue
        if n > 3 and len({cats[i]['balls'] for i in waves[0]}) < 2:
            continue
        solution = solve(cats, deps, stations, limit)
        if solution is None:
            continue
        rate = random_success(cats, deps, stations, limit, rng)
        desired_rate = target_success(number)
        cross = sum(any(poses[i][2]//2 != poses[j][2]//2 for j in range(n) if deps[i] >> j & 1) for i in range(n))
        color_unlocks = sum(any(deps[j] == 1 << i and cats[j]['color'] in cats[i]['balls']
                                for j in range(n)) for i in waves[0])
        center = sum(abs(x-1.5)+abs(y-1.5) for x,y,d in poses)
        score = (-abs(rate-desired_rate)*40 + min(cross, 3) + .15*len(waves)
                 + .6*min(color_unlocks, 2) - (.15*center if n <= 4 else 0))
        if best is None or score > best[0]:
            best = score, poses, solution, rate
        valid += 1
        if valid >= 60:
            break
    if best is None:
        raise RuntimeError(f'No suitable layout for level {number}')
    return best[1:]

def facing(length, d, color):
    if length == 2:
        return dict(_eulerAngles=f'{{x: 0, y: {[90,270,0,180][d]}, z: 0}}', _invertFlip=0,
                    _trackFlipSeparate=0, _invertFlipTrack=0, _applyRotationFix=0, _rotateFixLeft=0)
    # Green's one-cell artwork faces right; the other textures face left.
    # Horizontal parked sprites retain the prefab flip, while track movement
    # and vertical parking use SpriteFacingLock's configured flip convention.
    green = color == 1
    return dict(_eulerAngles=f'{{x: 0, y: {180 if d < 2 else 90}, z: {180 if d == 1 else 0}}}',
                _invertFlip=int((d in (1,3)) != green), _trackFlipSeparate=int(d == 3), _invertFlipTrack=int(green),
                _applyRotationFix=int(d >= 2), _rotateFixLeft=int(d >= 2))

def replace(text, cats, poses):
    blocks = iter(zip(cats, poses))
    def update(match):
        cat, (x,y,d) = next(blocks)
        block = match[0]
        changes = dict(_cell=f'{{x: {x}, y: {y}}}', _orientation=d//2, _signDirection=1 if d%2 == 0 else -1)
        changes.update(facing(cat['length'], d, cat['color']))
        for key, value in changes.items():
            pattern = r'(?m)^    '+key+r': .*$'
            line = f'    {key}: {value}'
            if re.search(pattern, block):
                block = re.sub(pattern, lambda _: line, block)
            else:
                block = block.replace('    _passengers:', line+'\n    _passengers:')
        return block
    return re.sub(BLOCK, update, text, flags=re.S)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args()
    baseline = json.loads((ROOT/'Tools/puzzle_layout_report.json').read_text())
    rows = []
    sequence = (ROOT/'Resources/Levels/LevelSequence.asset').read_text()
    guids = re.findall(r'guid: ([0-9a-f]{32}), type: 2', sequence)
    assert len(guids) == len(set(guids)), 'Duplicate sequence entries'
    assert len(guids) == len(baseline), 'Sequence/report count mismatch'
    assert [r['level'] for r in baseline] == list(range(1, len(guids)+1)), 'Report level order mismatch'
    for number, guid in enumerate(guids, 1):
        path = ROOT/f'Resources/Levels/Level-{number}-Layout.asset'
        assert field(path.with_suffix('.asset.meta').read_text(), 'guid') == guid, f'Wrong sequence entry: {number}'
        original, cats, stations, limit = read(path)
        assert int(field(original, '_levelNumber')) == number, f'Wrong level number: {number}'
        assert all(len(c['balls']) == c['length'] for c in cats), f'Wrong ball count: {number}'
        demand = Counter(c['color'] for c in cats for _ in range(c['length']))
        supply = Counter(ball for c in cats for ball in c['balls'])
        assert demand == supply, f'Unbalanced colors: {number}'
        assert all(ball != c['color'] for c in cats for ball in c['balls']), f'Already matched ball: {number}'
        signature = hashlib.sha256(json.dumps(inventory(cats)).encode()).hexdigest()
        assert signature == baseline[number-1]['inventory_sha256'], f'Inventory changed: {number}'
        if args.write:
            limit = track_limit(number)
            poses, solution, rate = tune_colors(cats, number, stations, limit)
            updated = replace(original, cats, poses)
            updated = re.sub(r'(?m)^  _maxCarsOnTrackCount: .*$', f'  _maxCarsOnTrackCount: {limit}', updated)
            path.write_text(updated, newline='\n')
            _, after, s, lim = read(path)
            assert inventory(after) == inventory(cats) and (s,lim) == (stations,limit)
        else:
            poses = [c['pose'] for c in cats]
        deps = dependencies(cats, poses)
        assert limit == track_limit(number), f'Unexpected track capacity: {number}'
        assert deps is not None, f'Overlap/out of bounds: {number}'
        waves = layers(deps)
        assert waves, f'Blocking cycle: {number}'
        assert 2 <= len(waves) <= 4, f'Unexpected blocking depth: {number}'
        assert len({p[2] for p in poses}) >= min(3, len(cats)), f'Insufficient direction variety: {number}'
        if len(cats) > 2:
            assert 2 <= len(waves[0]) <= 3
            assert len({cats[i]['color'] for i in waves[0]}) >= 2
        current_text = path.read_text()
        for block, cat, pose in zip(re.findall(BLOCK, current_text, re.S), cats, poses):
            for key, value in facing(cat['length'], pose[2], cat['color']).items():
                assert field(block, key) == str(value), f'Facing mismatch: {number}, {pose}, {key}'
        solution = solve(cats, deps, stations, limit)
        assert solution is not None, f'No modeled solution: {number}'
        rate, failure = release_audit(cats, deps, stations, limit)
        assert number == 1 or .40 <= rate < .95, f'Out of difficulty range: {number}: {rate}'
        row = dict(level=number, cats=len(cats), initial_exits=len(waves[0]), blocking_layers=len(waves),
                   track_capacity=limit, random_order_success=rate,
                   losing_order=None if failure is None else [i+1 for i in failure],
                   solution=[i+1 for i in solution], inventory_sha256=signature,
                   poses=poses)
        rows.append(row)
        print(f'{number:2}: {len(cats):2} cats; {len(waves[0])} exits; {len(waves)} layers; random {rate:.0%}; solution {row["solution"]}', flush=True)
    if args.write:
        (ROOT/'Tools/puzzle_layout_report.json').write_text(json.dumps(rows, indent=2)+'\n')
    else:
        report = json.loads((ROOT/'Tools/puzzle_layout_report.json').read_text())
        assert [r['inventory_sha256'] for r in rows] == [r['inventory_sha256'] for r in report], 'Inventory changed'

if __name__ == '__main__':
    main()
