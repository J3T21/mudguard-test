"""Offline AWL under-brace section check for v2.19.4 (matches mudguard_v2.19.4.fs).

Maps coupon (x, y_up) -> FS (x, h=-y_up). Peak at h=-rise. Zero Onshape calls.
"""
from __future__ import annotations

import math
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

CHORD = 94.0
FLOOR_W = 100.0
RISE = 7.0
CORNER_R = 18.0
WALL = 2.5
DEPTH = 30.49
FLANGE_HALF = 23.0  # flangeWidth 46 / 2
NUT_AF = 6.15
NUT_THK = 4.2


def crown_r(half_s: float, rise: float, r: float) -> float:
    hx = half_s - r
    return (hx * hx + (rise + r) ** 2 - r * r) / (2.0 * rise)


def flare_phi(half_s: float, half_f: float, r: float, depth: float) -> float:
    delta = half_f - half_s
    if abs(delta) < 1e-12:
        return 0.0
    A, B = delta + r, depth - r
    M = math.hypot(A, B)
    return math.acos(r / M) - math.atan2(B, A)


def outer_fs(n: int = 64) -> np.ndarray:
    """Closed-ish open U polyline in FS (x, h) coords, outer t=0."""
    half_s, half_f = CHORD / 2.0, FLOOR_W / 2.0
    r = CORNER_R
    Rc = crown_r(half_s, RISE, r)
    ccy = Rc - RISE
    fx, fcy = half_s - r, r
    phi = flare_phi(half_s, half_f, r, DEPTH)
    lx = -(fx + r * math.cos(phi))
    lh = fcy - r * math.sin(phi)
    rx, rh = -lx, lh
    F = np.array([-fx, fcy])
    C = np.array([0.0, ccy])
    v = F - C
    tp = C + (Rc / np.linalg.norm(v)) * v
    tpR = np.array([-tp[0], tp[1]])

    def arc(cx, cy, rad, a0, a1, nseg):
        angs = np.linspace(a0, a1, nseg)
        return np.column_stack([cx + rad * np.cos(angs), cy + rad * np.sin(angs)])

    a_wall_l = math.pi - phi
    a_tp_l = math.atan2(tp[1] - fcy, tp[0] + fx)
    a_tp_r = math.atan2(tpR[1] - fcy, tpR[0] - fx)
    left = arc(-fx, fcy, r, a_wall_l, a_tp_l, 16)
    left[0] = [lx, lh]
    left[-1] = tp
    # crown through peak
    a_l = math.atan2(tp[1] - ccy, tp[0])
    a_r = math.atan2(tpR[1] - ccy, tpR[0])
    peak_a = -0.5 * math.pi  # FS peak at h=-rise
    left = arc(0.0, ccy, Rc, a_l, peak_a, n // 2)
    right = arc(0.0, ccy, Rc, peak_a, a_r, n // 2)
    crown = np.vstack([left, right[1:]])
    crown[0] = tp
    crown[-1] = tpR
    crown[len(left) - 1] = [0.0, -RISE]
    right = arc(fx, fcy, r, a_tp_r, phi, 16)
    right[0] = tpR
    right[-1] = [rx, rh]
    return np.vstack(
        [
            [-half_f, DEPTH],
            left,
            crown[1:-1],
            right,
            [half_f, DEPTH],
        ]
    )


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out = os.path.join(root, "out")
    os.makedirs(out, exist_ok=True)
    outer = outer_fs()
    Rc = crown_r(CHORD / 2, RISE, CORNER_R)
    phi = flare_phi(CHORD / 2, FLOOR_W / 2, CORNER_R, DEPTH)
    peak = float(outer[:, 1].min())
    floor_w = float(outer[-1, 0] - outer[0, 0])
    # Crown drop at flange edge
    ccy = Rc - RISE
    y_edge = ccy - math.sqrt(Rc * Rc - FLANGE_HALF * FLANGE_HALF)
    drop = y_edge - peak  # toward tyre (+)
    print(
        f"AWL FS section: peak h={peak:.3f} (want {-RISE:.3f}), "
        f"floor={floor_w:.2f}, Rc={Rc:.3f}, phi={math.degrees(phi):.2f} deg, "
        f"flange-edge drop from peak={drop:.2f} mm"
    )
    print(f"Nut pocket defaults: AF={NUT_AF} thk={NUT_THK}")

    fig, ax = plt.subplots(figsize=(10, 6), dpi=120)
    ax.plot(outer[:, 0], outer[:, 1], "b-", lw=2, label="AWL outer (FS h)")
    ax.axhline(0, color="#888", lw=0.6)
    ax.axvline(-FLANGE_HALF, color="#c60", ls="--", lw=0.8, label="flange ±23")
    ax.axvline(FLANGE_HALF, color="#c60", ls="--", lw=0.8)
    # tongue band sketch (offsets of crown)
    xs = np.linspace(-FLANGE_HALF, FLANGE_HALF, 80)
    h0 = ccy - np.sqrt((Rc + 0.3) ** 2 - xs**2)
    h1 = ccy - np.sqrt((Rc + 2.3) ** 2 - xs**2)
    ax.fill_between(xs, h1, h0, color="#2a2", alpha=0.25, label="tongue band 0.3..2.3")
    ax.set_aspect("equal")
    ax.set_xlabel("lateral x (mm)")
    ax.set_ylabel("h toward tyre (mm)")
    ax.set_title("v2.19.4 AWL under-brace section (offline)")
    ax.legend(loc="lower center", fontsize=8)
    ax.grid(True, alpha=0.3)
    path = os.path.join(out, "v2.19.4_awl_section.png")
    fig.tight_layout()
    fig.savefig(path)
    plt.close()
    print("wrote", path)
    assert abs(peak - (-RISE)) < 0.05, peak
    assert abs(floor_w - FLOOR_W) < 0.05, floor_w


if __name__ == "__main__":
    main()
