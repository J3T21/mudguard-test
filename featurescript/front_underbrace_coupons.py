"""Front mudguard under-brace arch coupons (Fox AWL). Offline only — no Onshape.

A4 (rise 7.0 mm, four ID notches) fitted best. Shoulder outer TOP_W = 94 mm
(−1 mm/side from the 96 mm trial). Sides then angle out to FLOOR_W = 100 mm.
10 mm extrusion. Inner at shoulder ≈ 89 mm (wall 2.5).
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

# OUTER wall-to-wall where the R18 meets the side wall (shoulder).
TOP_W = 94.0  # was 96; −1 mm each side at the shoulder
FLOOR_W = 100.0  # sides angle out to this at y = -DEPTH
CORNER_R = 18.0
WALL = 2.5
DEPTH = 30.0
LENGTH = 10.0  # short verify extrusion
RISES = {
    "A1": 2.5,
    "A2": 4.0,
    "A3": 5.5,
    "A4": 7.0,
}
CHORD = TOP_W  # R18 → side junction at ±TOP_W/2; floor at ±FLOOR_W/2


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
    return Crown(
        label=label,
        rise=rise,
        radius=crown_radius_tangent_to_fillets(half, rise, CORNER_R),
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
    """Outer path: angled wall → R18 → crown → R18 → wall (G1 at R18/crown).

    Shoulder width TOP_W at the R18; floor flares to FLOOR_W.
    """
    half_s = c.half_chord  # shoulder half-width
    half_f = FLOOR_W / 2.0
    rC = c.corner_r
    fy = c.fillet_cy
    fx_l, fx_r = c.fillet_cx, -c.fillet_cx

    left_bottom = np.array([-half_f, -DEPTH])
    right_bottom = np.array([half_f, -DEPTH])
    left_wall_top = np.array([-half_s, -rC])
    right_wall_top = np.array([half_s, -rC])

    tp_l = _tangent_point_crown_fillet(c, left=True)
    tp_r = _tangent_point_crown_fillet(c, left=False)

    a_wall_l = math.pi
    a_crown_l = math.atan2(tp_l[1] - fy, tp_l[0] - fx_l)
    left_fillet = _arc_points(fx_l, fy, rC, a_wall_l, a_crown_l, n_fillet)
    left_fillet[0] = left_wall_top
    left_fillet[-1] = tp_l

    a_crown_r = math.atan2(tp_r[1] - fy, tp_r[0] - fx_r)
    right_fillet = _arc_points(fx_r, fy, rC, a_crown_r, 0.0, n_fillet)
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
    if abs(float(crown[:, 1].max()) - c.rise) > 0.05:
        raise RuntimeError(f"crown peak {crown[:, 1].max():.3f} != rise {c.rise}")

    return np.vstack(
        [
            left_bottom,
            left_fillet,
            crown[1:-1],
            right_fillet,
            right_bottom,
        ]
    )


def offset_inward(poly: np.ndarray, dist: float) -> np.ndarray:
    n = len(poly)
    out = np.zeros_like(poly)
    for i in range(n):
        if i == 0:
            t = poly[1] - poly[0]
        elif i == n - 1:
            t = poly[-1] - poly[-2]
        else:
            t = poly[i + 1] - poly[i - 1]
        ln = math.hypot(float(t[0]), float(t[1])) or 1.0
        # CCW path: inward is left of travel → (-ty, tx) after unitising? here (ny, -nx) with
        # nx,ny = (ty/ln, -tx/ln) was the prior vertical-wall convention; keep it.
        nx, ny = t[1] / ln, -t[0] / ln
        out[i] = poly[i] + dist * np.array([nx, ny])
    # Floor ends: keep constant horizontal wall thickness at the flared foot.
    out[0, 1] = -DEPTH + dist
    out[-1, 1] = -DEPTH + dist
    half_inner = FLOOR_W / 2.0 - dist
    out[0, 0] = -half_inner
    out[-1, 0] = half_inner
    return out


def solid_shell_tris(c: Crown) -> list[np.ndarray]:
    outer = outer_polyline(c)
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
        f.write(name.encode("ascii", "ignore")[:80].ljust(80, b"\0"))
        f.write(struct.pack("<I", len(tris)))
        for t in tris:
            n = np.cross(t[1] - t[0], t[2] - t[0])
            ln = np.linalg.norm(n)
            n = np.array([0.0, 0.0, 1.0]) if ln < 1e-18 else n / ln
            f.write(struct.pack("<3f", *n))
            for v in t:
                f.write(struct.pack("<3f", *v))
            f.write(struct.pack("<H", 0))


def label_bump_tris(label: str) -> list[np.ndarray]:
    """1..4 notches along +z, packed to fit in LENGTH."""
    idx = {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[label]
    tris = []
    y0 = -DEPTH + WALL
    y1 = y0 + 1.0
    # Pitch so four notches sit inside a 10 mm extrusion with a small margin.
    pitch = min(2.2, (LENGTH - 1.5) / max(idx, 1))
    bar = min(1.4, pitch * 0.65)
    for k in range(idx):
        z0 = 0.5 + k * pitch
        z1 = z0 + bar
        x0, x1 = -8.0, -3.0
        corners = [
            np.array([x0, y0, z0]),
            np.array([x1, y0, z0]),
            np.array([x1, y0, z1]),
            np.array([x0, y0, z1]),
            np.array([x0, y1, z0]),
            np.array([x1, y1, z0]),
            np.array([x1, y1, z1]),
            np.array([x0, y1, z1]),
        ]
        for a, b, d, e in (
            (0, 1, 2, 3),
            (4, 7, 6, 5),
            (0, 4, 5, 1),
            (1, 5, 6, 2),
            (2, 6, 7, 3),
            (3, 7, 4, 0),
        ):
            tris.append(np.vstack([corners[a], corners[b], corners[d]]))
            tris.append(np.vstack([corners[a], corners[d], corners[e]]))
    return tris


def section_plot(crowns: list[Crown], out_path: str) -> None:
    fig, ax = plt.subplots(figsize=(10, 6), dpi=120)
    colors = ["#1b9e77", "#d95f02", "#7570b3", "#e7298a"]
    for c, col in zip(crowns, colors):
        outer = outer_polyline(c)
        inner = offset_inward(outer, WALL)
        ax.plot(
            outer[:, 0],
            outer[:, 1],
            color=col,
            lw=2.5 if c.label == "A4" else 1.2,
            label=(
                f"{c.label} rise {c.rise} shoulder {TOP_W:.0f} → floor {FLOOR_W:.0f} "
                f"(inner ~{TOP_W - 2 * WALL:.0f})"
            ),
        )
        if c.label == "A4":
            ax.plot(inner[:, 0], inner[:, 1], color=col, lw=1, ls="--", alpha=0.7)
    ax.axhline(0, color="#888", lw=0.6)
    ax.axvline(-TOP_W / 2, color="#bbb", lw=0.5, ls=":")
    ax.axvline(TOP_W / 2, color="#bbb", lw=0.5, ls=":")
    ax.axvline(-FLOOR_W / 2, color="#ccc", lw=0.5, ls="--")
    ax.axvline(FLOOR_W / 2, color="#ccc", lw=0.5, ls="--")
    ax.set_aspect("equal")
    ax.set_xlabel("lateral (mm)")
    ax.set_ylabel("outward from chord (mm)")
    ax.set_title(
        f"Front A4 — shoulder {TOP_W:.0f} mm, sides out to floor {FLOOR_W:.0f} mm"
    )
    ax.legend(loc="lower center", fontsize=8)
    ax.grid(True, alpha=0.3)
    fig.tight_layout()
    fig.savefig(out_path)
    plt.close()
    print(out_path)


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out = os.path.join(root, "out")
    os.makedirs(out, exist_ok=True)
    print(
        f"SHOULDER outer = {TOP_W:.0f} mm (inner ≈ {TOP_W - 2 * WALL:.0f}); "
        f"FLOOR outer = {FLOOR_W:.0f} mm (wall {WALL})"
    )
    crowns = [crown_for(k, v) for k, v in RISES.items()]
    for c in crowns:
        outer = outer_polyline(c)
        inner = offset_inward(outer, WALL)
        print(
            f"{c.label}: rise={c.rise} R_crown={c.radius:.3f} "
            f"shoulder={TOP_W:.0f} inner≈{TOP_W - 2 * WALL:.0f} "
            f"floor_outer={outer[-1, 0] - outer[0, 0]:.1f} length={LENGTH}"
        )
        tris = solid_shell_tris(c) + label_bump_tris(c.label)
        path = os.path.join(out, f"front_brace_coupon_{c.label}.stl")
        write_stl(path, tris, f"front_brace_{c.label}")
        print("wrote", path, "tris", len(tris), "notches", {"A1": 1, "A2": 2, "A3": 3, "A4": 4}[c.label])
    a4 = next(c for c in crowns if c.label == "A4")
    write_stl(
        os.path.join(out, "brace_coupon_A4.stl"),
        solid_shell_tris(a4) + label_bump_tris("A4"),
        "brace_A4",
    )
    print("wrote", os.path.join(out, "brace_coupon_A4.stl"), "(A4 alias)")
    section_plot(crowns, os.path.join(out, "front_brace_coupon_sections.png"))


if __name__ == "__main__":
    main()
