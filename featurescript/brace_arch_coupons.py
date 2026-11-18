"""Printable under-brace arch coupons A1-A4. Offline only — no Onshape.

Outer width 88 mm (~1 mm/side vs a 90 mm cavity at tyre-top height).
Final-style C1 section: vertical walls at ±41 (82 mm), R18 top-corner
fillets (same as production C1), circular crown tangent to both fillets
with the given rise at x=0. Wall 2.5 mm. Length 50 mm. Depth 30 mm.
Rises: 2.5, 4.0, 5.5, 7.0 mm.
"""
from __future__ import annotations

import math
import os
import struct
from dataclasses import dataclass

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

OUTER_W = 88.0
CHORD = 82.0  # wall-to-wall at the fillet (matches production C1 width)
CORNER_R = 18.0  # production C1 corner radius
WALL = 2.5
DEPTH = 30.0
LENGTH = 50.0
RISES = {
    "A1": 2.5,
    "A2": 4.0,
    "A3": 5.5,
    "A4": 7.0,
}


@dataclass
class Crown:
    label: str
    rise: float
    radius: float  # crown arc radius (tangent to both R18 fillets)
    half_chord: float
    corner_r: float

    @property
    def center_y(self) -> float:
        # Peak at (0, rise); centre on the y-axis below the peak.
        return self.rise - self.radius

    @property
    def fillet_cx(self) -> float:
        # Left fillet centre x; right is -fillet_cx. C1-style: wall at -half_chord.
        return -(self.half_chord - self.corner_r)

    @property
    def fillet_cy(self) -> float:
        # Fillet centres sit r below the old flat (y=0), same as C1.
        return -self.corner_r


def crown_radius_tangent_to_fillets(half_chord: float, rise: float, corner_r: float) -> float:
    """Crown R so the arc through (0, rise) is internally tangent to both R-corner fillets.

    Fillet centres at (±(half_chord - corner_r), -corner_r), radius corner_r.
    Crown centre at (0, rise - R). Internal tangency: |C - F| = R - corner_r.
    """
    hx = half_chord - corner_r  # |fillet centre x|
    # (hx)^2 + (rise + corner_r - R)^2 = (R - corner_r)^2
    # hx^2 + (rise + corner_r)^2 - 2(rise+corner_r)R + R^2 = R^2 - 2 corner_r R + corner_r^2
    # hx^2 + (rise + corner_r)^2 - corner_r^2 = 2(rise + corner_r)R - 2 corner_r R
    # left = 2 R rise
    left = hx * hx + (rise + corner_r) ** 2 - corner_r * corner_r
    # left = 2*R*(rise + corner_r) - 2*corner_r*R = 2*R*rise
    if abs(rise) < 1e-12:
        raise ValueError("rise must be > 0")
    R = left / (2.0 * rise)
    if R <= corner_r:
        raise ValueError(f"degenerate crown R={R} <= corner_r={corner_r}")
    return R


def crown_for(label: str, rise: float) -> Crown:
    half = CHORD / 2.0
    radius = crown_radius_tangent_to_fillets(half, rise, CORNER_R)
    return Crown(
        label=label,
        rise=rise,
        radius=radius,
        half_chord=half,
        corner_r=CORNER_R,
    )


def _arc_points(cx: float, cy: float, r: float, a0: float, a1: float, n: int) -> np.ndarray:
    angs = np.linspace(a0, a1, n)
    return np.column_stack([cx + r * np.cos(angs), cy + r * np.sin(angs)])


def _tangent_point_crown_fillet(c: Crown, left: bool) -> np.ndarray:
    """Point of internal tangency between crown circle and one corner fillet."""
    fx = c.fillet_cx if left else -c.fillet_cx
    fy = c.fillet_cy
    F = np.array([fx, fy])
    C = np.array([0.0, c.center_y])
    # Point from C through F at distance R (internal: F is between C and the point).
    v = F - C
    return C + (c.radius / np.linalg.norm(v)) * v


def outer_polyline(c: Crown, n_fillet: int = 12, n_crown: int = 36) -> np.ndarray:
    """Outer path: floor flare → wall → R18 fillet → crown → fillet → wall → floor.

    Coordinates: x lateral, y outward (crown +y). Chord / old flat at y=0.
    """
    half_outer = OUTER_W / 2.0
    half_w = c.half_chord
    rC = c.corner_r

    left_bottom = np.array([-half_outer, -DEPTH])
    right_bottom = np.array([half_outer, -DEPTH])
    # Wall meets left fillet at (-half_w, -rC)
    left_wall_top = np.array([-half_w, -rC])
    right_wall_top = np.array([half_w, -rC])

    # Left fillet: centre (fillet_cx, -rC). From wall tangent (angle pi) to crown tangent.
    # Wall tangent: from centre, direction to wall point is (-1, 0) → angle pi.
    tp_l = _tangent_point_crown_fillet(c, left=True)
    tp_r = _tangent_point_crown_fillet(c, left=False)
    fx_l, fy = c.fillet_cx, c.fillet_cy
    fx_r = -c.fillet_cx

    a_wall_l = math.pi  # left wall attachment
    a_crown_l = math.atan2(tp_l[1] - fy, tp_l[0] - fx_l)
    # Sweep from wall to crown (decreasing angle: pi → a_crown_l, a_crown_l is in Q1-ish)
    left_fillet = _arc_points(fx_l, fy, rC, a_wall_l, a_crown_l, n_fillet)

    a_crown_r = math.atan2(tp_r[1] - fy, tp_r[0] - fx_r)
    a_wall_r = 0.0  # right wall attachment
    right_fillet = _arc_points(fx_r, fy, rC, a_crown_r, a_wall_r, n_fillet)

    # Crown arc from tp_l to tp_r about (0, center_y), through the peak.
    cy = c.center_y
    a0 = math.atan2(tp_l[1] - cy, tp_l[0] - 0.0)
    a1 = math.atan2(tp_r[1] - cy, tp_r[0] - 0.0)
    if a1 < a0:
        a1 += 2 * math.pi
    mid = 0.5 * (a0 + a1)
    mid_pt_y = cy + c.radius * math.sin(mid)
    if mid_pt_y < c.rise * 0.5:
        a0, a1 = a1 - 2 * math.pi, a0
    crown = _arc_points(0.0, cy, c.radius, a0, a1, n_crown)
    # Snap ends to exact tangent points.
    crown[0] = tp_l
    crown[-1] = tp_r
    left_fillet[0] = left_wall_top
    left_fillet[-1] = tp_l
    right_fillet[0] = tp_r
    right_fillet[-1] = right_wall_top

    return np.vstack(
        [
            left_bottom,
            left_wall_top,
            left_fillet[1:-1],
            crown,
            right_fillet[1:-1],
            right_wall_top,
            right_bottom,
        ]
    )


def offset_inward(poly: np.ndarray, dist: float) -> np.ndarray:
    """Offset a downward-opening U toward its interior."""
    n = len(poly)
    out = np.zeros_like(poly)
    for i in range(n):
        if i == 0:
            t = poly[1] - poly[0]
        elif i == n - 1:
            t = poly[-1] - poly[-2]
        else:
            t = poly[i + 1] - poly[i - 1]
        ln = math.hypot(t[0], t[1]) or 1.0
        # CW normal (ty, -tx): inward for left→right U path.
        nx, ny = t[1] / ln, -t[0] / ln
        out[i] = poly[i] + dist * np.array([nx, ny])
    out[0, 1] = -DEPTH + dist
    out[-1, 1] = -DEPTH + dist
    half_inner = OUTER_W / 2.0 - dist
    out[0, 0] = -half_inner
    out[-1, 0] = half_inner
    return out


def solid_shell_tris(c: Crown) -> list[np.ndarray]:
    """Triangle soup for a constant-thickness U extruded along z."""
    outer = outer_polyline(c, n_fillet=10, n_crown=32)
    inner = offset_inward(outer, WALL)
    assert len(outer) == len(inner)

    z0, z1 = 0.0, LENGTH
    tris: list[np.ndarray] = []

    def add_quad(p00, p10, p11, p01):
        tris.append(np.vstack([p00, p10, p11]))
        tris.append(np.vstack([p00, p11, p01]))

    for i in range(len(outer) - 1):
        o0, o1 = outer[i], outer[i + 1]
        i0, i1 = inner[i], inner[i + 1]
        add_quad(
            np.array([o0[0], o0[1], z0]),
            np.array([o1[0], o1[1], z0]),
            np.array([o1[0], o1[1], z1]),
            np.array([o0[0], o0[1], z1]),
        )
        add_quad(
            np.array([i0[0], i0[1], z0]),
            np.array([i0[0], i0[1], z1]),
            np.array([i1[0], i1[1], z1]),
            np.array([i1[0], i1[1], z0]),
        )
        add_quad(
            np.array([o0[0], o0[1], z0]),
            np.array([i0[0], i0[1], z0]),
            np.array([i1[0], i1[1], z0]),
            np.array([o1[0], o1[1], z0]),
        )
        add_quad(
            np.array([o0[0], o0[1], z1]),
            np.array([o1[0], o1[1], z1]),
            np.array([i1[0], i1[1], z1]),
            np.array([i0[0], i0[1], z1]),
        )

    oL, oR = outer[0], outer[-1]
    iL, iR = inner[0], inner[-1]
    add_quad(
        np.array([oL[0], -DEPTH, z0]),
        np.array([oR[0], -DEPTH, z0]),
        np.array([oR[0], -DEPTH, z1]),
        np.array([oL[0], -DEPTH, z1]),
    )
    add_quad(
        np.array([iL[0], -DEPTH + WALL, z0]),
        np.array([iL[0], -DEPTH + WALL, z1]),
        np.array([iR[0], -DEPTH + WALL, z1]),
        np.array([iR[0], -DEPTH + WALL, z0]),
    )
    add_quad(
        np.array([oL[0], -DEPTH, z0]),
        np.array([iL[0], -DEPTH + WALL, z0]),
        np.array([iR[0], -DEPTH + WALL, z0]),
        np.array([oR[0], -DEPTH, z0]),
    )
    add_quad(
        np.array([oL[0], -DEPTH, z1]),
        np.array([oR[0], -DEPTH, z1]),
        np.array([iR[0], -DEPTH + WALL, z1]),
        np.array([iL[0], -DEPTH + WALL, z1]),
    )
    return tris


def write_stl(path: str, tris: list[np.ndarray], name: str) -> None:
    with open(path, "wb") as f:
        header = name.encode("ascii", "ignore")[:80].ljust(80, b"\0")
        f.write(header)
        f.write(struct.pack("<I", len(tris)))
        for t in tris:
            n = np.cross(t[1] - t[0], t[2] - t[0])
            ln = np.linalg.norm(n)
            if ln < 1e-18:
                n = np.array([0.0, 0.0, 1.0])
            else:
                n = n / ln
            f.write(struct.pack("<3f", *n))
            for v in t:
                f.write(struct.pack("<3f", *v))
            f.write(struct.pack("<H", 0))


def label_bump_tris(label: str) -> list[np.ndarray]:
    """Raised nubs on the inner floor: 1..4 for A1..A4."""
    idx = {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[label]
    tris = []
    y0 = -DEPTH + WALL
    y1 = y0 + 0.6
    x0 = -6.0
    for k in range(idx):
        z0 = 8.0 + k * 8.0
        z1 = z0 + 4.0
        x1 = x0 + 4.0
        corners = [
            (x0, y0, z0),
            (x1, y0, z0),
            (x1, y0, z1),
            (x0, y0, z1),
            (x0, y1, z0),
            (x1, y1, z0),
            (x1, y1, z1),
            (x0, y1, z1),
        ]
        c = [np.array(p, dtype=float) for p in corners]
        faces = [
            (0, 1, 2, 3),
            (4, 7, 6, 5),
            (0, 4, 5, 1),
            (1, 5, 6, 2),
            (2, 6, 7, 3),
            (3, 7, 4, 0),
        ]
        for a, b, d, e in faces:
            tris.append(np.vstack([c[a], c[b], c[d]]))
            tris.append(np.vstack([c[a], c[d], c[e]]))
    return tris


def section_plot(crowns: list[Crown], out_path: str) -> None:
    fig, ax = plt.subplots(figsize=(10, 6), dpi=120)
    colors = ["#1b9e77", "#d95f02", "#7570b3", "#e7298a"]
    for c, col in zip(crowns, colors):
        outer = outer_polyline(c)
        ax.plot(
            outer[:, 0],
            outer[:, 1],
            color=col,
            lw=2,
            label=f"{c.label} rise {c.rise} R={c.radius:.0f} +R{c.corner_r:.0f}",
        )
        inner = offset_inward(outer, WALL)
        ax.plot(inner[:, 0], inner[:, 1], color=col, lw=1, ls="--", alpha=0.7)
        # Mark fillet centres.
        ax.plot(
            [c.fillet_cx, -c.fillet_cx],
            [c.fillet_cy, c.fillet_cy],
            "o",
            color=col,
            ms=3,
            alpha=0.5,
        )
    ax.axhline(0, color="#888", lw=0.6)
    ax.axvline(-45, color="#bbb", lw=0.5, ls=":")
    ax.axvline(45, color="#bbb", lw=0.5, ls=":")
    ax.set_aspect("equal")
    ax.set_xlabel("y lateral (mm)")
    ax.set_ylabel("outward from chord (mm)")
    ax.set_title("Under-brace coupons — R18 corners + arched crown, outer 88 mm")
    ax.legend(loc="lower center")
    ax.grid(True, alpha=0.3)
    fig.tight_layout()
    fig.savefig(out_path)
    plt.close()
    print(out_path)


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out = os.path.join(root, "out")
    os.makedirs(out, exist_ok=True)
    crowns = [crown_for(k, v) for k, v in RISES.items()]
    for c in crowns:
        tp = _tangent_point_crown_fillet(c, left=True)
        print(
            f"{c.label}: rise={c.rise} R={c.radius:.3f} cornerR={c.corner_r} "
            f"wall=±{c.half_chord} filletC=({c.fillet_cx:.1f},{c.fillet_cy:.1f}) "
            f"tp=({tp[0]:.2f},{tp[1]:.2f}) outerW={OUTER_W}"
        )
        tris = solid_shell_tris(c) + label_bump_tris(c.label)
        path = os.path.join(out, f"brace_coupon_{c.label}.stl")
        write_stl(path, tris, f"brace_{c.label}")
        print("wrote", path, "tris", len(tris))
    section_plot(crowns, os.path.join(out, "brace_coupon_sections.png"))


if __name__ == "__main__":
    main()
