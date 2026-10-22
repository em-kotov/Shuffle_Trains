"""Check artwork axes against movement using the actual prefab transforms.

This is an offline geometry check, not a Unity Play Mode test.
"""
import math
import re
import sys
sys.dont_write_bytecode = True
import rebalance_puzzles as layouts


def vector(text):
    return tuple(float(v) for v in re.findall(r': (-?[\d.eE+]+)', text))


def multiply(a, b):
    x,y,z,w = a
    X,Y,Z,W = b
    return (w*X+x*W+y*Z-z*Y, w*Y-x*Z+y*W+z*X,
            w*Z+x*Y-y*X+z*W, w*W-x*X-y*Y-z*Z)


def rotate(q, v):
    return multiply(multiply(q, (*v,0)), (-q[0],-q[1],-q[2],q[3]))[:3]


def euler(x,y,z):
    x,y,z = [math.radians(a)/2 for a in (x,y,z)]
    return multiply(multiply((0,math.sin(y),0,math.cos(y)),
                             (math.sin(x),0,0,math.cos(x))), (0,0,math.sin(z),math.cos(z)))


def dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def prefab_sprite_rotation(path):
    blocks = dict(re.findall(r'--- !u!\d+ &(\d+)\n(.*?)(?=--- !u!|\Z)', path.read_text(), re.S))
    sprite_id = next(i for i,b in blocks.items() if re.search(r'm_Name: CatTexture\s*\n',b))
    transform_id = next(i for i,b in blocks.items() if b.startswith('Transform:') and
                        f'm_GameObject: {{fileID: {sprite_id}}}' in b)
    def world(i):
        b = blocks[i]
        local = vector(layouts.field(b, 'm_LocalRotation'))
        parent = re.search(r'm_Father: \{fileID: (\d+)\}',b)[1]
        return multiply(world(parent),local) if parent != '0' else local
    return world(transform_id)


def main():
    checked = 0
    for path in (layouts.ROOT/'Resources/Prefabs/Cats').glob('*/*.prefab'):
        text = path.read_text()
        length = int(layouts.field(text,'_length'))
        color = int(layouts.field(text,'_carColor'))
        local = prefab_sprite_rotation(path)
        # Inspected source art: short green faces +X, other short cats -X;
        # all long cats have their head toward the top of the texture (+Y).
        art = (0,1,0) if length == 2 else (1 if color == 1 else -1,0,0)
        for direction,(dx,dz) in enumerate(layouts.DIRS):
            config = layouts.facing(length,direction,color)
            root = euler(*vector(config['_eulerAngles']))
            rest = multiply(root,local)
            axis = rotate(rest,art)
            movement = (dx,0,dz)
            if length == 1:
                if dot(axis,movement) < 0:
                    axis = tuple(-a for a in axis)
            assert dot(axis,movement) > .999, (path.name,direction,'parked')
            if length == 1:
                if config['_applyRotationFix']:
                    rest = multiply(euler(0,90 if config['_rotateFixLeft'] else -90,0),rest)
                for heading in (-1,1):
                    invert = config['_invertFlipTrack'] if config['_trackFlipSeparate'] else config['_invertFlip']
                    flip = heading > 0 if invert else heading < 0
                    axis = rotate(rest,tuple(-a for a in art) if flip else art)
                    assert dot(axis,(heading,0,0)) > .999, (path.name,direction,heading,'track')
            checked += 1
    assert checked == 64, checked
    print('PASS: all 16 cat prefabs in four parking directions; all single-cat left/right track facings.')


if __name__ == '__main__':
    main()
