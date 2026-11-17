"""r2.2 bed placement check for part B on a Bambu P1S (offline).

B prints on its side, wall face down. The plate contact is the flat wall face where the section
is full width (s >= 85, depth WIDE_R..h, so 11 mm wide with wideCorner 6), trimmed 3.5 mm at the
tail by the corner round. Checks, with mouse ears and no brim:
  - the whole side silhouette (convex hull) stays >= 1 mm inside the 256 x 256 plate;
  - nothing (hull, contact, ears) touches the P1S bed_exclude_area 0..18 x 0..28 (front-left) or
    comes within 0.5 mm of the nozzle load line (Y 1.0..1.5 from X 18 to 240, jog to Y 11 at X 240);
  - four ears, 5 mm diameter discs centred on the contact band corners (tail end and s 85 end),
    stay on the plate and clear of both.
Prints the best placement (largest worst-case margin) and renders it.
"""
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import rear_r2_2_model as m  # noqa: E402
from shapely import affinity  # noqa: E402
from shapely.geometry import LineString, MultiPoint, Point, Polygon, box  # noqa: E402
from shapely.ops import unary_union  # noqa: E402

BED = box(0, 0, 256, 256)
EXCL = box(0, 0, 18, 28)
LOAD = unary_union([LineString([(18, 1.0), (240, 1.0)]).buffer(0.5), LineString([(18, 1.5), (240, 1.5)]).buffer(0.5),
                    LineString([(240, 1.0), (240, 11.0)]).buffer(0.6)])
EAR_R = 2.5


def side_geometry():
    pts = m.silhouette("B")
    hull = MultiPoint([tuple(p) for p in pts]).convex_hull
    s_end = m.S_B1 - 3.5  # corner round crosses the wall 3.5 mm before the end
    band = []
    ss = np.linspace(85.0, s_end, 120)
    for s in ss:
        p = m._xz(float(s), m.R - m.WIDE_R)
        band.append((p[0], p[2]))
    for s in ss[::-1]:
        p = m._xz(float(s), m.R - m.SECTION_H)
        band.append((p[0], p[2]))
    contact = Polygon(band)
    corners = []
    for s in (85.0, s_end):
        for r in (m.R - m.WIDE_R, m.R - m.SECTION_H):
            p = m._xz(s, r)
            corners.append((p[0], p[2]))
    return hull, contact, corners


def placed(geom, ang, flip, dx, dy):
    g = affinity.rotate(geom, ang, origin=(0, 0))
    if flip:
        g = affinity.scale(g, xfact=-1, origin=(0, 0))
    return affinity.translate(g, dx, dy)


def margins(hull, contact, ears):
    inner = box(1, 1, 255, 255)
    if not inner.contains(hull):
        return -1.0, {}
    stuff = unary_union([hull] + ears)
    if not BED.contains(unary_union(ears)):
        return -1.0, {}
    m_ex = stuff.distance(EXCL) if not stuff.intersects(EXCL) else -1.0
    m_ld = stuff.distance(LOAD) - 0.5 if not stuff.intersects(LOAD) else -1.0
    bx = hull.bounds
    m_ed = min(bx[0], bx[1], 256 - bx[2], 256 - bx[3]) - 1.0
    return min(m_ex, m_ld, m_ed), {"keepout": m_ex, "loadline": m_ld + 0.5, "edge": m_ed + 1.0}


def search():
    hull0, contact0, corners0 = side_geometry()
    best = None
    for flip in (False, True):
        for base in (0.0, 90.0, 180.0, 270.0):
            for dang in np.arange(-14.0, 14.01, 1.0):
                ang = base + dang
                h = placed(hull0, ang, flip, 0, 0)
                bx = h.bounds
                if bx[2] - bx[0] > 254 or bx[3] - bx[1] > 254:
                    continue
                for tx in np.arange(1 - bx[0], 255 - bx[2] + 1e-9, 0.5):
                    for ty in np.arange(1 - bx[1], 255 - bx[3] + 1e-9, 0.5):
                        hh = affinity.translate(h, tx, ty)
                        c = placed(contact0, ang, flip, tx, ty)
                        ears = [placed(Point(p), ang, flip, tx, ty).buffer(EAR_R) for p in corners0]
                        mg, det = margins(hh, c, ears)
                        if mg < 0:
                            continue
                        if best is None or mg > best["margin"]:
                            best = {"margin": mg, "ang": ang, "flip": flip, "tx": tx, "ty": ty, "det": det,
                                    "hull": hh, "contact": c, "ears": ears, "bbox": hh.bounds}
    return best


def render(best, path):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import Polygon as MP
    fig, ax = plt.subplots(figsize=(7.5, 7.5))
    ax.add_patch(MP([(0, 0), (256, 0), (256, 256), (0, 256)], fc="#f4f4f4", ec="k", lw=1))
    ax.add_patch(MP(list(EXCL.exterior.coords), fc="#ff9a9a", ec="r", lw=1, label="P1S keep-out 18x28"))
    for g in getattr(LOAD, "geoms", [LOAD]):
        ax.add_patch(MP(list(g.exterior.coords), fc="#ffcc66", ec="#c08000", lw=0.6))
    ax.add_patch(MP(list(best["hull"].exterior.coords), fc="#9cc3e6", ec="#2c5d8a", alpha=0.6, label="B side silhouette (hull)"))
    ax.add_patch(MP(list(best["contact"].exterior.coords), fc="#2c5d8a", ec="#123", label="plate contact (wall face, 11 mm)"))
    for i, e in enumerate(best["ears"]):
        ax.add_patch(MP(list(e.exterior.coords), fc="#3a3", ec="#060", label="mouse ears 5 mm" if i == 0 else None))
    ax.set_xlim(-5, 261)
    ax.set_ylim(-5, 261)
    ax.set_aspect("equal")
    d = best["det"]
    ax.set_title(f"Rear r2.2 B on P1S: rot {best['ang']:.0f} deg{' flipped' if best['flip'] else ''}; keep-out {d['keepout']:.1f}, "
                 f"load line {d['loadline']:.1f}, edge {d['edge']:.1f} mm", fontsize=9)
    ax.set_xlabel("bed X (mm), front-left origin")
    ax.set_ylabel("bed Y (mm)")
    ax.legend(loc="upper left", fontsize=7)
    fig.tight_layout()
    fig.savefig(path, dpi=140)
    print(path)


if __name__ == "__main__":
    b = search()
    if b is None:
        print("BED FAIL: no placement")
        sys.exit(1)
    bx = b["bbox"]
    print(f"BED PASS rot={b['ang']:.0f} flip={b['flip']} hull bbox X {bx[0]:.1f}..{bx[2]:.1f} Y {bx[1]:.1f}..{bx[3]:.1f} "
          f"({bx[2]-bx[0]:.1f} x {bx[3]-bx[1]:.1f}); keep-out clear {b['det']['keepout']:.1f}, load line clear {b['det']['loadline']:.1f}, "
          f"plate edge {b['det']['edge']:.1f}; contact area {b['contact'].area/100:.1f} cm2")
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "out", "rear", "r2.2_bed_B_P1S.png"))
    render(b, out)
