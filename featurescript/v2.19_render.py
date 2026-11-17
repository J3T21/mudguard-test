"""Offline renders for mudguard v2.19. No Onshape.

The leg sheet leaves the wall plane, bows 2.8 mm away from the leg, then returns
to the same plane. Thickness is 2.5 mm measured normal to that face.
"""
import math
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

R = 393.5
TAN = math.tan(math.radians(19))
PAD, YO, WT = 1.5, 48.5, 2.5
A, L1, L2 = 2.8, 10.0, 22.0
DEDGE = (97 - 76) / (2 * TAN)
D0 = PAD + DEDGE
LO = 45.0
XC = -R * math.sin(LO / R)
ZT = R * math.cos(LO / R)
ZB = ZT + PAD
YLEG = 72.5


def s5(u):
    return u * u * u * (u * (u * 6 - 15) + 10)


def sp(u):
    return 30 * u * u * (u - 1) ** 2


def face(D):
    if D <= D0 or D >= D0 + L1 + L2:
        return YO, 0.0
    if D < D0 + L1:
        u = (D - D0) / L1
        return YO - A * s5(u), -A * sp(u) / L1
    u = (D0 + L1 + L2 - D) / L2
    return YO - A * s5(u), A * sp(u) / L2


def y_in(D):
    y, sl = face(D)
    return y - WT * math.sqrt(1 + sl * sl)


def grid(xs, Ds, y_of):
    rows = []
    for D in Ds:
        z = ZB - D
        y = y_of(float(D))
        rows.append([np.array([x, y, z]) for x in xs])
    tris = []
    for a, b in zip(rows, rows[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def wing(sg):
    Ds = np.linspace(D0 - 6, 130, 80)
    xs = np.linspace(XC - 28, XC + 28, 18)
    outer = grid(xs, Ds, lambda D: sg * face(D)[0])
    inner = grid(xs, Ds, lambda D: sg * y_in(D))
    cap = []
    top_o = [np.array([x, sg * face(float(Ds[0]))[0], ZB - Ds[0]]) for x in xs]
    top_i = [np.array([x, sg * y_in(float(Ds[0])), ZB - Ds[0]]) for x in xs]
    bot_o = [np.array([x, sg * face(float(Ds[-1]))[0], ZB - Ds[-1]]) for x in xs]
    bot_i = [np.array([x, sg * y_in(float(Ds[-1])), ZB - Ds[-1]]) for x in xs]
    for a, b in ((top_o, top_i), (bot_o, bot_i)):
        for i in range(len(a) - 1):
            cap += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return outer + inner + cap


def arch():
    ss = np.linspace(-165, 200, 36)
    ys = np.linspace(-48.5, 48.5, 10)
    ring = []
    for s in ss:
        a = s / R
        rn = np.array([-math.sin(a), 0.0, math.cos(a)])
        o = rn * R
        ring.append([o + np.array([0, y, 0]) - rn * (6 + 16 * (abs(y) / 48.5) ** 2) for y in ys])
    tris = []
    for a, b in zip(ring, ring[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def collar():
    """OD 55 collar, D = 0 to 20, axis at the leg. One side only, the +Y leg."""
    th = np.linspace(-0.55, 0.55, 16)
    Ds = np.linspace(0, 20, 8)
    rows = []
    for D in Ds:
        z = ZB - D
        rows.append([np.array([XC + 27.5 * math.sin(t), YLEG - 27.5 * math.cos(t), z]) for t in th])
    tris = []
    for a, b in zip(rows, rows[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def add(ax, tris, color, alpha=1):
    if not tris:
        return
    v = np.asarray(tris, dtype=float)
    poly = Poly3DCollection(v, linewidths=0, alpha=alpha)
    n = np.cross(v[:, 1] - v[:, 0], v[:, 2] - v[:, 0])
    nn = np.linalg.norm(n, axis=1)
    nn[nn == 0] = 1
    light = np.array([0.3, 0.5, 0.8])
    light /= np.linalg.norm(light)
    shade = np.clip(np.abs((n / nn[:, None]) @ light), 0.4, 1)
    base = np.array(matplotlib.colors.to_rgb(color))
    poly.set_facecolor(base * shade[:, None])
    ax.add_collection3d(poly)


def save(path, elev, azim, title, lim):
    fig = plt.figure(figsize=(11, 8), dpi=110)
    ax = fig.add_subplot(111, projection="3d")
    add(ax, arch(), "#5c6b62")
    add(ax, wing(1), "#c4b49a")
    add(ax, wing(-1), "#c4b49a")
    add(ax, collar(), "#8aa0b4", alpha=0.35)
    ax.view_init(elev=elev, azim=azim)
    ax.set_xlim(*lim[0])
    ax.set_ylim(*lim[1])
    ax.set_zlim(*lim[2])
    ax.set_box_aspect((
        lim[0][1] - lim[0][0],
        lim[1][1] - lim[1][0],
        lim[2][1] - lim[2][0],
    ))
    ax.set_title(title)
    fig.tight_layout()
    fig.savefig(path)
    plt.close()
    print(path)


def main():
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "out"))
    os.makedirs(out, exist_ok=True)
    whole = ((-180, 40), (-60, 80), (200, 420))
    root = ((XC - 36, XC + 36), (40, 78), (ZB - 80, ZB + 8))
    save(os.path.join(out, "v2.19_top.png"), 88, -90, "v2.19 top", whole)
    save(os.path.join(out, "v2.19_front.png"), 8, 180, "v2.19 front", whole)
    save(os.path.join(out, "v2.19_left.png"), 6, 90, "v2.19 left", whole)
    save(os.path.join(out, "v2.19_leg_wing.png"), 8, -70, "v2.19 wing root, leg side", root)
    save(os.path.join(out, "v2.19_leg_wing_inside.png"), 8, 110, "v2.19 wing root, tyre side", root)


if __name__ == "__main__":
    main()
