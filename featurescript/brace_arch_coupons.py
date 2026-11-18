"""Printable under-brace arch coupons A1-A4. Offline only — no Onshape.

Outer width 88 mm (~1 mm/side vs a 90 mm cavity at tyre-top height).
Crown = circular arc over an 82 mm chord; walls vertical below the arc ends.
Rises: 2.5, 4.0, 5.5, 7.0 mm. Wall 2.5 mm. Length 50 mm. Depth 30 mm.
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
CHORD = 82.0
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
    radius: float
    half_chord: float

    @property
    def center_y(self) -> float:
        # Arc centre below the chord: chord at y=0, crown peak at +rise.
        return self.rise - self.radius


def crown_for(label: str, rise: float) -> Crown:
    half = CHORD / 2.0
    # R = (c^2 + h^2) / (2h)
    radius = (half * half + rise * rise) / (2.0 * rise)
    return Crown(label=label, rise=rise, radius=radius, half_chord=half)


def outer_polyline(c: Crown, n_arc: int = 48) -> np.ndarray:
    """Closed? No — open outer path from bottom-left up, over crown, down to bottom-right.
    Coordinates: x lateral, y outward (crown +y), origin at chord centre."""
    half_outer = OUTER_W / 2.0
    # left wall bottom -> left wall top (chord end) -> arc -> right wall top -> right wall bottom
    left_bottom = np.array([-half_outer, -DEPTH])
    left_top = np.array([-c.half_chord, 0.0])
    right_top = np.array([c.half_chord, 0.0])
    right_bottom = np.array([half_outer, -DEPTH])

    # Arc from left chord end to right, through the peak.
    # Circle centre at (0, cy). Angles from left to right (CCW for +y up).
    cy = c.center_y
    a0 = math.atan2(0.0 - cy, -c.half_chord)
    a1 = math.atan2(0.0 - cy, c.half_chord)
    # Ensure we go the short way through the peak (positive y).
    if a1 < a0:
        a1 += 2 * math.pi
    # Peak should be near pi/2 from centre; if the long arc was chosen, flip.
    mid = 0.5 * (a0 + a1)
    mid_pt = np.array([c.radius * math.cos(mid), cy + c.radius * math.sin(mid)])
    if mid_pt[1] < c.rise * 0.5:
        a0, a1 = a1 - 2 * math.pi, a0
        mid = 0.5 * (a0 + a1)

    angs = np.linspace(a0, a1, n_arc)
    arc = np.column_stack(
        [c.radius * np.cos(angs), cy + c.radius * np.sin(angs)]
    )
    # Force exact ends onto the chord corners.
    arc[0] = left_top
    arc[-1] = right_top
    return np.vstack([left_bottom, left_top, arc[1:-1], right_top, right_bottom])


def offset_inward(poly: np.ndarray, dist: float) -> np.ndarray:
    """Offset a downward-opening U toward its interior (roughly +y for walls, -y at crown)."""
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
        # inward normal for a path that runs left->right along the U: rotate tangent 90 CW
        # for walls going up, tangent ~(0,1), CW normal ~(1,0) which is outward on left...
        # Left wall up: we want inward = +x. Tangent (0,+1), rotate CCW -> (-1,0) = outward.
        # So use CW: (ty, -tx) for left wall (0,1)->(1,0) = inward. Good.
        nx, ny = t[1] / ln, -t[0] / ln
        # At the crown, tangent is ~(+1,0), CW normal (0,-1) = downward = into the U. Good.
        out[i] = poly[i] + dist * np.array([nx, ny])
    # Keep ends at the same depth so the floor stays flat.
    out[0, 1] = -DEPTH + dist
    out[-1, 1] = -DEPTH + dist
    # Snap wall bottoms to half-outer - wall
    half_inner = OUTER_W / 2.0 - dist
    out[0, 0] = -half_inner
    out[-1, 0] = half_inner
    return out


def closed_section(c: Crown) -> np.ndarray:
    outer = outer_polyline(c)
    inner = offset_inward(outer, WALL)
    # close: outer L->R, then tip, then inner R->L, then tip
    floor_r = np.array([inner[-1, 0], -DEPTH])
    floor_l = np.array([inner[0, 0], -DEPTH])
    # Rebuild cleaner: outer path, then line to inner right bottom, reverse inner, close.
    # Simpler solid: extrude the region between outer and a flat floor, then subtract inner void.
    # For STL we build a solid shell by lofting outer+inner as a closed ring.
    ring = np.vstack(
        [
            outer,
            [outer[-1, 0], -DEPTH],
            [inner[-1, 0], -DEPTH],
            inner[::-1],
            [inner[0, 0], -DEPTH],
            [outer[0, 0], -DEPTH],
            outer[0],
        ]
    )
    # drop near-duplicates
    clean = [ring[0]]
    for p in ring[1:]:
        if math.hypot(p[0] - clean[-1][0], p[1] - clean[-1][1]) > 1e-4:
            clean.append(p)
    if math.hypot(clean[0][0] - clean[-1][0], clean[0][1] - clean[-1][1]) > 1e-4:
        clean.append(clean[0])
    return np.asarray(clean, dtype=float)


def triangulate_ring(ring: np.ndarray) -> list[tuple[np.ndarray, np.ndarray, np.ndarray]]:
    """Fan triangulation of a near-convex U ring in 2D (x,y)."""
    pts = ring[:-1] if np.allclose(ring[0], ring[-1]) else ring
    # Use ear-clip style for a simple polygon; U-with-hole is not a simple ring.
    # Build as outer solid minus inner void instead.
    return []


def solid_shell_tris(c: Crown) -> list[np.ndarray]:
    """Build triangle soup for a constant-thickness U extruded along z."""
    outer = outer_polyline(c, n_arc=40)
    # Cap outer with floor to make a solid U block, then hollow with inner.
    half_o = OUTER_W / 2.0
    outer_closed = np.vstack(
        [outer, [half_o, -DEPTH], [-half_o, -DEPTH], outer[0]]
    )
    inner = offset_inward(outer, WALL)
    half_i = OUTER_W / 2.0 - WALL
    # Inner void polygon (same winding as outer for consistent side faces).
    inner_closed = np.vstack(
        [inner, [half_i, -DEPTH + WALL], [-half_i, -DEPTH + WALL], inner[0]]
    )

    def extrude_polygon(poly: np.ndarray, z0: float, z1: float, flip: bool = False):
        """Return triangles for a prism of a closed 2D poly."""
        pts = poly[:-1] if np.allclose(poly[0], poly[-1]) else poly
        tris = []
        # bottom and top fans from centroid
        cen = pts.mean(axis=0)

        def fan(z, invert):
            c3 = np.array([cen[0], cen[1], z])
            for i in range(len(pts)):
                a = np.array([pts[i, 0], pts[i, 1], z])
                b = np.array([pts[(i + 1) % len(pts), 0], pts[(i + 1) % len(pts), 1], z])
                if invert:
                    tris.append(np.vstack([c3, b, a]))
                else:
                    tris.append(np.vstack([c3, a, b]))

        fan(z0, invert=not flip)
        fan(z1, invert=flip)
        # walls
        for i in range(len(pts)):
            a = pts[i]
            b = pts[(i + 1) % len(pts)]
            a0 = np.array([a[0], a[1], z0])
            a1 = np.array([a[0], a[1], z1])
            b0 = np.array([b[0], b[1], z0])
            b1 = np.array([b[0], b[1], z1])
            if flip:
                tris.append(np.vstack([a0, a1, b1]))
                tris.append(np.vstack([a0, b1, b0]))
            else:
                tris.append(np.vstack([a0, b1, a1]))
                tris.append(np.vstack([a0, b0, b1]))
        return tris

    # Solid outer prism minus we can't boolean easily — build shell from outer walls + inner walls + rims.
    # Approach: triangulate the annular region (outer-inner) as a 2D strip, then extrude.
    # Pair outer and inner by index (same count).
    n = min(len(outer), len(inner))
    outer = outer[:n]
    inner = inner[:n]
    # Add floor corners so walls and floor connect.
    # Rebuild matching polylines with identical point counts.
    outer = outer_polyline(c, n_arc=40)
    inner = offset_inward(outer, WALL)
    assert len(outer) == len(inner)

    # Append floor ends already present as first/last.
    z0, z1 = 0.0, LENGTH
    tris: list[np.ndarray] = []

    def add_quad(p00, p10, p11, p01):
        tris.append(np.vstack([p00, p10, p11]))
        tris.append(np.vstack([p00, p11, p01]))

    # Side walls of the shell (outer and inner) along the path, both z ends, plus annular caps.
    for i in range(len(outer) - 1):
        o0, o1 = outer[i], outer[i + 1]
        i0, i1 = inner[i], inner[i + 1]
        # outer wall
        add_quad(
            np.array([o0[0], o0[1], z0]),
            np.array([o1[0], o1[1], z0]),
            np.array([o1[0], o1[1], z1]),
            np.array([o0[0], o0[1], z1]),
        )
        # inner wall (flip winding)
        add_quad(
            np.array([i0[0], i0[1], z0]),
            np.array([i0[0], i0[1], z1]),
            np.array([i1[0], i1[1], z1]),
            np.array([i1[0], i1[1], z0]),
        )
        # front annular rim z0
        add_quad(
            np.array([o0[0], o0[1], z0]),
            np.array([i0[0], i0[1], z0]),
            np.array([i1[0], i1[1], z0]),
            np.array([o1[0], o1[1], z0]),
        )
        # rear annular rim z1
        add_quad(
            np.array([o0[0], o0[1], z1]),
            np.array([o1[0], o1[1], z1]),
            np.array([i1[0], i1[1], z1]),
            np.array([i0[0], i0[1], z1]),
        )

    # Floor strip between outer bottoms and inner bottoms (already included as path segments
    # if first/last span the floor — our polyline does NOT include the floor middle).
    # Add floor: outer bottoms are indices 0 and -1; connect across.
    # Left bottom outer/inner to right bottom outer/inner as two quads along z.
    # Actually bottoms are only the two ends; add a floor rectangle along the U opening.
    # Floor outer edge from outer[0] to outer[-1], inner from inner[0] to inner[-1].
    oL, oR = outer[0], outer[-1]
    iL, iR = inner[0], inner[-1]
    # floor underside (y = -DEPTH)
    for z_a, z_b, flip in ((z0, z1, False),):
        add_quad(
            np.array([oL[0], -DEPTH, z_a]),
            np.array([oR[0], -DEPTH, z_a]),
            np.array([oR[0], -DEPTH, z_b]),
            np.array([oL[0], -DEPTH, z_b]),
        )
        add_quad(
            np.array([iL[0], -DEPTH + WALL, z_a]),
            np.array([iL[0], -DEPTH + WALL, z_b]),
            np.array([iR[0], -DEPTH + WALL, z_b]),
            np.array([iR[0], -DEPTH + WALL, z_a]),
        )
        # front/rear floor rims already partly covered; add end caps for floor thickness
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
        # vertical edges at floor corners connecting outer bottom to floor already in side walls
        break

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
    """Tiny raised bar pattern on the inner floor so prints are identifiable without text glyphs."""
    # Encode A1..A4 as 1..4 nubs along +z on the inner floor.
    idx = {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[label]
    tris = []
    y0 = -DEPTH + WALL
    y1 = y0 + 0.6
    x0 = -6.0
    for k in range(idx):
        z0 = 8.0 + k * 8.0
        z1 = z0 + 4.0
        x1 = x0 + 4.0
        # box
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
        ax.plot(outer[:, 0], outer[:, 1], color=col, lw=2, label=f"{c.label} rise {c.rise} R={c.radius:.0f}")
        inner = offset_inward(outer, WALL)
        ax.plot(inner[:, 0], inner[:, 1], color=col, lw=1, ls="--", alpha=0.7)
    ax.axhline(0, color="#888", lw=0.6)
    ax.axvline(-45, color="#bbb", lw=0.5, ls=":")
    ax.axvline(45, color="#bbb", lw=0.5, ls=":")
    ax.set_aspect("equal")
    ax.set_xlabel("y lateral (mm)")
    ax.set_ylabel("outward from chord (mm)")
    ax.set_title("Under-brace coupons — outer width 88 mm, cavity ref 90 mm")
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
        print(f"{c.label}: rise={c.rise} R={c.radius:.3f} chord={CHORD} outerW={OUTER_W}")
        tris = solid_shell_tris(c) + label_bump_tris(c.label)
        path = os.path.join(out, f"brace_coupon_{c.label}.stl")
        write_stl(path, tris, f"brace_{c.label}")
        print("wrote", path, "tris", len(tris))
    section_plot(crowns, os.path.join(out, "brace_coupon_sections.png"))


if __name__ == "__main__":
    main()
