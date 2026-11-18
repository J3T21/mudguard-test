"""Front mudguard under-brace arch coupons (Fox AWL). Offline only — no Onshape.

Separate from the rear brace shell coupons in brace_arch_coupons.py.

A4 (rise 7.0 mm, four ID notches) fitted best. This revise keeps the A4 crown
and R18 shoulders at TOP_W = 88 mm, then continues the walls so the floor is
8 mm wider (4 mm each side): BOTTOM_W = 96 mm. Quick verify: 20 mm extrusion.
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

TOP_W = 88.0  # wall-to-wall at the R18 fillet — the A4 size that fitted
BOTTOM_W = 96.0  # floor width: +4 mm each side vs the top
CORNER_R = 18.0  # production C1 corner radius
WALL = 2.5
DEPTH = 30.0
LENGTH = 20.0  # quick verify extrusion
RISES = {
    "A1": 2.5,
    "A2": 4.0,
    "A3": 5.5,
    "A4": 7.0,
}
CHORD = TOP_W


@dataclass
class Crown:
    label: str
    rise: float
    radius: float
    half_chord: float
    corner_r: float

    @property
    def center_y(self) -> float:
        return self.rise - self.radius

    @property
    def fillet_cx(self) -> float:
        return -(self.half_chord - self.corner_r)

    @property
    def fillet_cy(self) -> float:
        return -self.corner_r


def crown_radius_tangent_to_fillets(half_chord: float, rise: float, corner_r: float) -> float:
    hx = half_chord - corner_r
    left = hx * hx + (rise + corner_r) ** 2 - corner_r * corner_r
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
    fx = c.fillet_cx if left else -c.fillet_cx
    fy = c.fillet_cy
    F = np.array([fx, fy])
    C = np.array([0.0, c.center_y])
    v = F - C
    return C + (c.radius / np.linalg.norm(v)) * v


def outer_polyline(c: Crown, n_fillet: int = 16, n_crown: int = 48) -> np.ndarray:
    """R18 + crown at TOP_W, then walls continue out to BOTTOM_W at the floor.

    Wall meets the fillet vertically (G1), then slants +4 mm/side down to the floor.
    """
    half_top = c.half_chord
    half_bot = BOTTOM_W / 2.0
    rC = c.corner_r
    fy = c.fillet_cy
    fx_l, fx_r = c.fillet_cx, -c.fillet_cx

    left_bottom = np.array([-half_bot, -DEPTH])
    right_bottom = np.array([half_bot, -DEPTH])
    # Vertical at the fillet so wall→R18 stays G1; flare starts below.
    left_wall_top = np.array([-half_top, -rC])
    right_wall_top = np.array([half_top, -rC])

    tp_l = _tangent_point_crown_fillet(c, left=True)
    tp_r = _tangent_point_crown_fillet(c, left=False)

    a_wall_l = math.pi
    a_crown_l = math.atan2(tp_l[1] - fy, tp_l[0] - fx_l)
    left_fillet = _arc_points(fx_l, fy, rC, a_wall_l, a_crown_l, n_fillet)
    left_fillet[0] = left_wall_top
    left_fillet[-1] = tp_l

    a_crown_r = math.atan2(tp_r[1] - fy, tp_r[0] - fx_r)
    a_wall_r = 0.0
    right_fillet = _arc_points(fx_r, fy, rC, a_crown_r, a_wall_r, n_fillet)
    right_fillet[0] = tp_r
    right_fillet[-1] = right_wall_top

    cy = c.center_y
    a_l = math.atan2(tp_l[1] - cy, tp_l[0])
    a_r = math.atan2(tp_r[1] - cy, tp_r[0])
    if a_l < a_r:
        a_l += 2 * math.pi
    mid = 0.5 * (a_l + a_r)
    if cy + c.radius * math.sin(mid) < c.rise * 0.5:
        a_r, a_l = a_l, a_r
    crown = _arc_points(0.0, cy, c.radius, a_l, a_r, n_crown)
    crown[0] = tp_l
    crown[-1] = tp_r

    peak_i = int(np.argmax(crown[:, 1]))
    if abs(crown[peak_i, 1] - c.rise) > 0.05:
        raise RuntimeError(
            f"crown peak y={crown[peak_i, 1]:.3f} != rise={c.rise} "
            f"(a_l={math.degrees(a_l):.1f} a_r={math.degrees(a_r):.1f})"
        )

    # Path: floor → flared wall → fillet → crown → fillet → flared wall → floor
    return np.vstack(
        [
            left_bottom,
            left_wall_top,
            left_fillet[1:],
            crown[1:-1],
            right_fillet[:-1],
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
        nx, ny = t[1] / ln, -t[0] / ln
        out[i] = poly[i] + dist * np.array([nx, ny])
    out[0, 1] = -DEPTH + dist
    out[-1, 1] = -DEPTH + dist
    half_inner = BOTTOM_W / 2.0 - dist
    out[0, 0] = -half_inner
    out[-1, 0] = half_inner
    return out


def solid_shell_tris(c: Crown) -> list[np.ndarray]:
    outer = outer_polyline(c, n_fillet=16, n_crown=48)
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
    """1..4 raised notches on the inner floor — A4 has four."""
    idx = {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[label]
    tris = []
    y0 = -DEPTH + WALL
    y1 = y0 + 1.0
    x0 = -8.0
    for k in range(idx):
        z0 = 2.0 + k * 4.0
        z1 = z0 + 2.5
        x1 = x0 + 5.0
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
            lw=2 if c.label == "A4" else 1.2,
            label=f"{c.label} rise {c.rise} top {TOP_W:.0f} bottom {BOTTOM_W:.0f}",
        )
        if c.label == "A4":
            inner = offset_inward(outer, WALL)
            ax.plot(inner[:, 0], inner[:, 1], color=col, lw=1, ls="--", alpha=0.7)
    ax.axhline(0, color="#888", lw=0.6)
    ax.axvline(-TOP_W / 2, color="#bbb", lw=0.5, ls=":")
    ax.axvline(TOP_W / 2, color="#bbb", lw=0.5, ls=":")
    ax.axvline(-BOTTOM_W / 2, color="#bbb", lw=0.5, ls="--")
    ax.axvline(BOTTOM_W / 2, color="#bbb", lw=0.5, ls="--")
    ax.set_aspect("equal")
    ax.set_xlabel("lateral (mm)")
    ax.set_ylabel("outward from chord (mm)")
    ax.set_title(f"Front under-brace A4 revise — top {TOP_W:.0f} → bottom {BOTTOM_W:.0f} (+4/side)")
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
    # Primary deliverable: A4 with flared bottom. Also write A1–A3 for reference.
    crowns = [crown_for(k, v) for k, v in RISES.items()]
    for c in crowns:
        tp = _tangent_point_crown_fillet(c, left=True)
        print(
            f"{c.label}: rise={c.rise} R={c.radius:.3f} topW={TOP_W} bottomW={BOTTOM_W} "
            f"length={LENGTH} filletC=({c.fillet_cx:.1f},{c.fillet_cy:.1f}) "
            f"tp=({tp[0]:.2f},{tp[1]:.2f})"
        )
        tris = solid_shell_tris(c) + label_bump_tris(c.label)
        path = os.path.join(out, f"front_brace_coupon_{c.label}.stl")
        write_stl(path, tris, f"front_brace_{c.label}")
        print("wrote", path, "tris", len(tris), "idNotches", {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[c.label])
    # Convenience copy of the verify piece under the old A4 name too.
    a4 = next(c for c in crowns if c.label == "A4")
    tris = solid_shell_tris(a4) + label_bump_tris("A4")
    alias = os.path.join(out, "brace_coupon_A4.stl")
    write_stl(alias, tris, "brace_A4")
    print("wrote", alias, "(A4 alias for the front verify print)")
    section_plot(crowns, os.path.join(out, "front_brace_coupon_sections.png"))


if __name__ == "__main__":
    main()
