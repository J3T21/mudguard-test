"""Offline renders for mudguard v2.19.1. The sheet is the validated three-arc profile."""
import os
import sys
import math
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

sys.path.insert(0, os.path.dirname(__file__))
from v2_19_1_profile import outer_path, offset_inner

R = 393.5
LO = 45.0
XC = -R * math.sin(LO / R)
ZT = R * math.cos(LO / R)
ZB = ZT + 1.5


def strip(pts, sg):
    xs = np.linspace(XC - 28, XC + 28, 14)
    rows = []
    for y, D in pts:
        z = ZB - D
        rows.append([np.array([x, sg * y, z]) for x in xs])
    tris = []
    for a, b in zip(rows, rows[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def wing(sg):
    outer, _ = outer_path()
    inner = offset_inner(outer)
    return strip(outer, sg) + strip(inner, sg)


def arch():
    ss = np.linspace(-165, 200, 36)
    ys = np.linspace(-48.5, 48.5, 10)
    ring = []
    for s in ss:
        a = s / R
        rn = np.array([-math.sin(a), 0.0, math.cos(a)])
        o = rn * R
        ring.append([o + np.array([0.0, y, 0.0]) - rn * (6 + 16 * (abs(y) / 48.5) ** 2) for y in ys])
    tris = []
    for a, b in zip(ring, ring[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def collar():
    th = np.linspace(-0.55, 0.55, 16)
    rows = []
    for D in np.linspace(0, 20, 8):
        z = ZB - D
        rows.append([np.array([XC + 27.5 * math.sin(t), 72.5 - 27.5 * math.cos(t), z]) for t in th])
    tris = []
    for a, b in zip(rows, rows[1:]):
        for i in range(len(a) - 1):
            tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def add(ax, tris, color, alpha=1):
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
    ax.set_box_aspect((lim[0][1] - lim[0][0], lim[1][1] - lim[1][0], lim[2][1] - lim[2][0]))
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
    save(os.path.join(out, "v2.19.1_top.png"), 88, -90, "v2.19.1 top", whole)
    save(os.path.join(out, "v2.19.1_front.png"), 8, 180, "v2.19.1 front", whole)
    save(os.path.join(out, "v2.19.1_left.png"), 6, 90, "v2.19.1 left", whole)
    save(os.path.join(out, "v2.19.1_leg_wing.png"), 8, -70, "v2.19.1 wing root, leg side", root)
    save(os.path.join(out, "v2.19.1_leg_wing_inside.png"), 8, 110, "v2.19.1 wing root, tyre side", root)


if __name__ == "__main__":
    main()
