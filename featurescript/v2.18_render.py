"""Offline renders for mudguard v2.18. No Onshape."""
import math
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

R = 393.5
TAN = math.tan(math.radians(19))
A_W, PAD, YK, YO, WT = 38.0, 1.5, 42.7, 48.5, 2.5
RF = 10.0
BETA = math.atan(TAN)
TT = RF * math.tan(BETA / 2)
DI = PAD + (YK - A_W) / TAN
LEN = math.sqrt(1 + TAN * TAN)
DFT = DI + TT
DWT = DI - TT / LEN
D1 = DFT + 22
D2 = D1 + 64
LO = 45.0
ANG = LO / R
XC = -R * math.sin(ANG)
ZT = R * math.cos(ANG)
ZB = ZT + 1.5
DEDGE = (97 - 76) / (2 * TAN)
ZEDGE = ZT - DEDGE


def y_face(D):
    if D <= DWT:
        return A_W + max(D - PAD, 0) * TAN
    if D < DFT:
        dd = D - DFT
        return (YK - RF) + math.sqrt(max(0, RF * RF - dd * dd))
    if D <= D1:
        return YK
    if D >= D2:
        return YO
    u = (D - D1) / (D2 - D1)
    sm = u * u * (3 - 2 * u)
    return YK + (YO - YK) * sm


def wing_mesh(sg):
    Ds = np.linspace(DWT - 2, 130, 60)
    xs = np.linspace(XC - 28, XC + 28, 16)
    outer, inner = [], []
    for D in Ds:
        yo = sg * y_face(float(D))
        z = ZB - D
        outer.append([np.array([x, yo, z]) for x in xs])
        inner.append([np.array([x, yo - sg * WT, z]) for x in xs])
    tris = []
    for grid in (outer, inner):
        for a, b in zip(grid, grid[1:]):
            for i in range(len(a) - 1):
                tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    # R10 blend, 2.4 mm toward the tyre
    h, Rb = 2.4, 10.0
    half = math.sqrt(2 * Rb * h - h * h)
    y_in = sg * (YK - WT)
    yc = y_in - sg * (h - Rb)
    tris_b = []
    angs = np.linspace(-math.asin(half / Rb), math.asin(half / Rb), 12)
    row0, row1 = [], []
    for th in angs:
        y = yc - sg * Rb * math.cos(th)
        z = ZEDGE + Rb * math.sin(th)
        row0.append(np.array([XC - 28, y, z]))
        row1.append(np.array([XC + 28, y, z]))
    for i in range(len(angs) - 1):
        tris_b += [[row0[i], row1[i], row1[i + 1]], [row0[i], row1[i + 1], row0[i + 1]]]
    return tris, tris_b


def arch():
    ss = np.linspace(-165, 200, 40)
    ys = np.linspace(-48.5, 48.5, 12)
    ring = []
    for s in ss:
        a = s / R
        rn = np.array([-math.sin(a), 0, math.cos(a)])
        o = rn * R
        ring.append([o + np.array([0, y, 0]) - rn * (8 + 18 * (abs(y) / 48.5) ** 2) for y in ys])
    tris = []
    for a, b in zip(ring, ring[1:]):
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
    light = np.array([0.25, 0.45, 0.85])
    light /= np.linalg.norm(light)
    shade = np.clip(np.abs((n / nn[:, None]) @ light), 0.35, 1)
    base = np.array(matplotlib.colors.to_rgb(color))
    poly.set_facecolor(base * shade[:, None])
    ax.add_collection3d(poly)
    pts = v.reshape(-1, 3)
    ax.auto_scale_xyz(pts[:, 0], pts[:, 1], pts[:, 2], had_data=True)


def save(path, elev, azim, title, lim=None, inside=False):
    fig = plt.figure(figsize=(11, 8), dpi=110)
    ax = fig.add_subplot(111, projection="3d")
    add(ax, arch(), "#5c6b62")
    for sg in (1, -1):
        wing, blend = wing_mesh(sg)
        add(ax, wing, "#c4b49a")
        add(ax, blend, "#8d6b4a")
    ax.view_init(elev=elev, azim=azim)
    if lim:
        ax.set_xlim(*lim[0]); ax.set_ylim(*lim[1]); ax.set_zlim(*lim[2])
    ax.set_title(title)
    fig.tight_layout()
    fig.savefig(path)
    plt.close()
    print(path)


def main():
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "out"))
    os.makedirs(out, exist_ok=True)
    save(os.path.join(out, "v2.18_top.png"), 88, -90, "v2.18 top")
    save(os.path.join(out, "v2.18_front.png"), 8, 180, "v2.18 front")
    save(os.path.join(out, "v2.18_left.png"), 6, 90, "v2.18 left")
    box = ((XC - 40, XC + 40), (20, 80), (ZB - 120, ZB + 5))
    save(os.path.join(out, "v2.18_leg_wing.png"), 18, -60, "v2.18 leg face", box)
    save(os.path.join(out, "v2.18_leg_wing_inside.png"), 12, 130, "v2.18 tyre side of the wing", box)


if __name__ == "__main__":
    main()
