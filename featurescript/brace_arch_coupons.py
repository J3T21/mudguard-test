"""Brace-fit saddle coupons from the cropped Kiri scan plus James's calipers.

The brace arch is 60 mm inside on the tyre face and 50 mm on the seat-tube
face, 12 mm thick, 20 mm tall. The plastic flap is ignored. The crown is a
smooth fit to the bridge underside in refs/rear_brace_edit2.glb, mapped onto
that 60 mm mouth. Three coupons, extra clearance 0 / 0.4 / 0.8 mm. Four zip
slots on the crest, in two pairs. The script refuses to write STLs unless
the side view is a single shallow centered arch. Offline only — no Onshape.
"""
from __future__ import annotations

import math
import os
import struct

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

# Calipers. Outer coupon is 1 mm per side inside those openings.
TYRE_OPEN = 60.0
SEAT_OPEN = 50.0
TYRE_W = TYRE_OPEN - 2.0
SEAT_W = SEAT_OPEN - 2.0
BRACE_THK = 12.0
HANDLE = 8.0  # sticks out of the tyre face so the coupon can be held
LENGTH = HANDLE + BRACE_THK  # 20 mm
DEPTH = 16.0  # inside the 20 mm arch height
WALL = 2.5
CORNER_R = 8.0
CLEARANCES = {"C0": 0.0, "C1": 0.4, "C2": 0.8}
# Two pairs along the crest, so a side view shows four holes. Long axis lateral.
SLOT_X = (-16.0, -8.0, 4.0, 11.0)
SLOT_Z = 14.0  # mid-thickness of the brace, past the handle
SLOT_LEN = 6.0
SLOT_WID = 2.4
_CROWN = None
_RAW = None


def _load_mm(path: str) -> np.ndarray:
    import json
    import DracoPy

    data = open(path, "rb").read()
    ln = struct.unpack_from("<I", data, 12)[0]
    js = json.loads(data[20 : 20 + ln])
    prim = js["meshes"][0]["primitives"][0]
    bv_i = prim["extensions"]["KHR_draco_mesh_compression"]["bufferView"]
    bin_off = 20 + ln
    bln = struct.unpack_from("<I", data, bin_off)[0]
    blob = data[bin_off + 8 : bin_off + 8 + bln]
    bv = js["bufferViews"][bv_i]
    chunk = blob[bv["byteOffset"] : bv["byteOffset"] + bv["byteLength"]]
    return np.asarray(DracoPy.decode(chunk).points, dtype=np.float64) * 1000.0


def fitted_crown(n: int = 25) -> np.ndarray:
    """Smooth arch from the bridge underside, mouth scaled to 60 mm.

    The crop is still the rear triangle. The brace is the lower skin of the
    bridge, X 135–158 mm in this scan, under the stays. Y is up in the scan,
    so the underside is the low edge; the crown is that edge flipped up.
    """
    global _RAW
    glb = os.path.join(os.path.dirname(__file__), "..", "refs", "rear_brace_edit2.glb")
    if not os.path.isfile(glb):
        raise SystemExit("missing refs/rear_brace_edit2.glb")
    mm = _load_mm(glb)
    mm = mm - mm.min(0)
    sl = mm[(mm[:, 0] > 135) & (mm[:, 0] < 158) & (mm[:, 1] < 100) & (mm[:, 2] > 40) & (mm[:, 2] < 250)]
    if len(sl) < 100:
        raise SystemExit(f"no brace arch in the crop (bridge points {len(sl)})")
    zs = np.linspace(50, 240, 28)
    rows = []
    for z in zs:
        p = sl[np.abs(sl[:, 2] - z) < 5]
        if len(p) < 4:
            continue
        rows.append((z, np.percentile(p[:, 1], 8)))
    if len(rows) < 12:
        raise SystemExit("brace underside is too broken to fit")
    env = np.array(rows)
    z, y = env[:, 0], env[:, 1]
    chord = np.interp(z, [z[0], z[-1]], [y[0], y[-1]])
    span = float(z[-1] - z[0])
    scale = TYRE_OPEN / span
    x = (z - z.mean()) * scale
    h = (chord - y) * scale
    med = h.copy()
    for i in range(1, len(h) - 1):
        med[i] = np.median(h[i - 1 : i + 2])
    keep = h > med - 1.2
    _RAW = np.column_stack([x, h - h.min()])
    coef = np.polyfit(x[keep], h[keep], 4)
    xs = np.linspace(-TYRE_OPEN / 2, TYRE_OPEN / 2, n)
    ys = np.polyval(coef, xs)
    ys = ys - ys.min()
    return np.column_stack([xs, ys])


def crown_profile() -> np.ndarray:
    global _CROWN
    if _CROWN is None:
        _CROWN = fitted_crown()
    return _CROWN


def width_at(z: float, clearance: float) -> float:
    tyre = TYRE_W - 2.0 * clearance
    seat = SEAT_W - 2.0 * clearance
    if z <= HANDLE:
        return tyre
    if z >= HANDLE + BRACE_THK:
        return seat
    u = (z - HANDLE) / BRACE_THK
    return tyre + (seat - tyre) * u


def outer_section(width: float, clearance: float, n_arc: int = 10) -> np.ndarray:
    """x lateral, y outward. Crown follows the scan. Walls drop to a fixed floor."""
    del n_arc
    scale = width / TYRE_OPEN
    src = crown_profile()
    crown = np.column_stack([src[:, 0] * scale, src[:, 1] - clearance])
    floor = float(crown[:, 1].min()) - DEPTH
    half = width / 2.0
    left = np.array([[-half, floor], [-half, crown[0, 1]]])
    right = np.array([[half, crown[-1, 1]], [half, floor]])
    return np.vstack([left, crown, right])


def offset_inward(poly: np.ndarray, dist: float, half: float) -> np.ndarray:
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
        nx, ny = t[1] / ln, -t[0] / ln
        out[i] = poly[i] + dist * np.array([nx, ny])
    out[0] = (-(half - dist), -DEPTH + dist)
    out[-1] = (half - dist, -DEPTH + dist)
    return out


def _in_slot(x: float, z: float) -> bool:
    if abs(z - SLOT_Z) > SLOT_WID / 2.0:
        return False
    return any(abs(x - xc) <= SLOT_LEN / 2.0 for xc in SLOT_X)


def _on_crown(y: float) -> bool:
    return y > -CORNER_R * 0.35


def shell_tris(clearance: float) -> list[np.ndarray]:
    zs = np.linspace(0.0, LENGTH, 21)
    outers = []
    inners = []
    for z in zs:
        w = width_at(float(z), clearance)
        outer = outer_section(w, clearance)
        inner = offset_inward(outer, WALL, w / 2.0)
        outers.append(outer)
        inners.append(inner)
    tris: list[np.ndarray] = []

    def quad(a, b, c, d):
        tris.append(np.vstack([a, b, c]))
        tris.append(np.vstack([a, c, d]))

    def P(xy, z):
        return np.array([xy[0], xy[1], z])

    n = len(outers[0])
    for s in range(len(zs) - 1):
        z0, z1 = float(zs[s]), float(zs[s + 1])
        zm = 0.5 * (z0 + z1)
        for i in range(n - 1):
            o0, o1 = outers[s][i], outers[s][i + 1]
            o2, o3 = outers[s + 1][i], outers[s + 1][i + 1]
            i0, i1 = inners[s][i], inners[s][i + 1]
            i2, i3 = inners[s + 1][i], inners[s + 1][i + 1]
            crown = _on_crown(float(o0[1])) and _on_crown(float(o1[1]))
            xm = 0.5 * (float(o0[0]) + float(o1[0]))
            punched = crown and _in_slot(xm, zm)
            if punched:
                # Tunnel wall: outer edge to inner edge, so the slot is open.
                quad(P(o0, z0), P(o1, z0), P(i1, z0), P(i0, z0))
                quad(P(o3, z1), P(o2, z1), P(i2, z1), P(i3, z1))
                quad(P(o0, z0), P(i0, z0), P(i2, z1), P(o2, z1))
                quad(P(o1, z0), P(o3, z1), P(i3, z1), P(i1, z0))
            else:
                quad(P(o0, z0), P(o1, z0), P(o3, z1), P(o2, z1))
                quad(P(i0, z0), P(i2, z1), P(i3, z1), P(i1, z0))
    # End caps and the floor.
    for end, z, outer, inner in (
        (0, 0.0, outers[0], inners[0]),
        (1, LENGTH, outers[-1], inners[-1]),
    ):
        for i in range(n - 1):
            if end == 0:
                quad(P(outer[i], z), P(inner[i], z), P(inner[i + 1], z), P(outer[i + 1], z))
            else:
                quad(P(outer[i], z), P(outer[i + 1], z), P(inner[i + 1], z), P(inner[i], z))
    # Floor between the wall bottoms.
    for s in range(len(zs) - 1):
        z0, z1 = float(zs[s]), float(zs[s + 1])
        for loop in (outers, inners):
            a, b = loop[s][0], loop[s][-1]
            c, d = loop[s + 1][0], loop[s + 1][-1]
            if loop is outers:
                quad(P(a, z0), P(d, z1), P(c, z1), P(b, z0))
            else:
                quad(P(a, z0), P(b, z0), P(d, z1), P(c, z1))
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
                f.write(struct.pack("<3f", float(v[0]), float(v[1]), float(v[2])))
            f.write(struct.pack("<H", 0))


def _box_tris(x0, x1, y0, y1, z0, z1) -> list[np.ndarray]:
    c = [
        np.array([x0, y0, z0]),
        np.array([x1, y0, z0]),
        np.array([x1, y0, z1]),
        np.array([x0, y0, z1]),
        np.array([x0, y1, z0]),
        np.array([x1, y1, z0]),
        np.array([x1, y1, z1]),
        np.array([x0, y1, z1]),
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    tris = []
    for a, b, d, e in faces:
        tris.append(np.vstack([c[a], c[b], c[d]]))
        tris.append(np.vstack([c[a], c[d], c[e]]))
    return tris


def id_mark_tris(label: str) -> list[np.ndarray]:
    """Raised ID marks so C0/C1/C2 are obvious after printing.

    Count = 1/2/3 bars (C0→1 … C2→3) on the inner handle floor, plus matching
    ridges on the outer tyre-face end (z≈0) so the coupon can be ID'd from outside.
    """
    n = {"C0": 1, "C1": 2, "C2": 3}[label]
    tris: list[np.ndarray] = []
    # Inner floor of the handle (z = 1..7), raised +y into the U.
    y0 = -DEPTH + WALL
    y1 = y0 + 1.2
    for k in range(n):
        z0 = 1.2 + k * 2.2
        z1 = z0 + 1.4
        tris.extend(_box_tris(-10.0, 10.0, y0, y1, z0, z1))
    # Outer end-face ridges on the tyre face (z = 0), sticking out -z.
    for k in range(n):
        x0 = -8.0 + k * 6.0
        x1 = x0 + 4.0
        tris.extend(_box_tris(x0, x1, -DEPTH, -DEPTH + 1.5, -1.2, 0.0))
    return tris


def side_checks() -> tuple[list[str], dict]:
    """Side view is the arch seen along the brace thickness. This is the gate."""
    outer = outer_section(width_at(0.0, 0.0), 0.0)
    crown = outer[2:-2]
    x = crown[:, 0]
    y = crown[:, 1]
    xs = np.linspace(float(x[0]), float(x[-1]), 40)
    ys = np.interp(xs, x, y)
    i = int(np.argmax(ys))
    frac = i / (len(ys) - 1)
    rise = float(ys.max() - min(ys[0], ys[-1]))
    fails: list[str] = []
    if not 0.40 <= frac <= 0.60:
        fails.append(f"crest at {frac:.2f} of the width, want 0.40-0.60")
    if not 3.0 <= rise <= 12.0:
        fails.append(f"rise {rise:.2f} mm, want 3-12")
    if np.any(np.diff(ys[: i + 1]) < -0.3):
        fails.append("left shoulder is not falling away from the crest")
    if np.any(np.diff(ys[i:]) > 0.3):
        fails.append("right shoulder has a second peak")
    step = np.diff(ys)
    signs = np.sign(step)
    signs = signs[np.abs(step) > 0.05]
    changes = int(np.sum(signs[1:] * signs[:-1] < 0)) if len(signs) > 1 else 0
    if changes != 1:
        fails.append(f"crown slope changes direction {changes} times, want once")
    floor = min(float(ys[0]), float(ys[-1]))
    for xc in SLOT_X:
        yy = float(np.interp(xc, x, y))
        if yy < floor + rise * (2.0 / 3.0):
            fails.append(f"slot at x={xc:.0f} sits below the upper third of the arch")
    gaps = np.diff(np.sort(np.array(SLOT_X, dtype=float)))
    if not (gaps[0] < gaps[1] and gaps[2] < gaps[1]):
        fails.append("slots are not two pairs")
    return fails, {"frac": frac, "rise": rise, "xs": xs, "ys": ys}


def section_plot(out_path: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5), dpi=130)
    colors = {"C0": "#1b4f72", "C1": "#b9770e", "C2": "#196f3d"}
    for name, clr in CLEARANCES.items():
        w = width_at(0.0, clr)
        outer = outer_section(w, clr)
        ax.plot(outer[:, 0], outer[:, 1], color=colors[name], lw=2, label=f"{name} clearance {clr:.1f} mm")
    if _RAW is not None:
        ax.plot(_RAW[:, 0], _RAW[:, 1], color="#922b21", lw=1.2, ls="--", label="scan underside, levelled")
    ax.axvline(-TYRE_OPEN / 2, color="#bbb", lw=0.6, ls=":")
    ax.axvline(TYRE_OPEN / 2, color="#bbb", lw=0.6, ls=":")
    ax.set_aspect("equal")
    ax.set_xlabel("lateral (mm)")
    ax.set_ylabel("outward (mm)")
    ax.set_title("Brace coupons — 58 mm tyre face, tapering to 48 mm")
    ax.legend(fontsize=8)
    ax.grid(True, alpha=0.3)
    fig.tight_layout()
    fig.savefig(out_path)
    plt.close()
    print(out_path)


def side_plot(out_path: str, info: dict, ok: bool) -> None:
    fig, axes = plt.subplots(1, 2, figsize=(9, 4.2), dpi=130)
    axes[0].plot(info["xs"], info["ys"], color="#1b4f72", lw=2.5)
    for xc in SLOT_X:
        yy = float(np.interp(xc, info["xs"], info["ys"]))
        axes[0].plot([xc - 3, xc + 3], [yy, yy], color="#922b21", lw=3)
    axes[0].set_aspect("equal")
    axes[0].set_title(f"C0 side  crest {info['frac']:.2f}  rise {info['rise']:.1f} mm")
    axes[0].set_xlabel("lateral (mm)")
    axes[0].set_ylabel("outward (mm)")
    axes[0].grid(True, alpha=0.3)
    ref = os.path.join(os.path.dirname(__file__), "..", "refs", "brace_side_ref.png")
    if os.path.isfile(ref):
        axes[1].imshow(plt.imread(ref))
        axes[1].set_title("reference")
    axes[1].axis("off")
    fig.suptitle("PASS" if ok else "FAIL")
    fig.tight_layout()
    fig.savefig(out_path)
    plt.close()
    print(out_path)


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out = os.path.join(root, "out")
    os.makedirs(out, exist_ok=True)
    crown_profile()
    fails, info = side_checks()
    side_plot(os.path.join(out, "brace_coupon_side.png"), info, ok=not fails)
    section_plot(os.path.join(out, "brace_coupon_sections.png"))
    src = crown_profile()
    print("CROWN_X", ", ".join(f"{v:.2f}" for v in src[:, 0]))
    print("CROWN_Y", ", ".join(f"{v:.2f}" for v in src[:, 1]))
    if fails:
        print("FAIL")
        for item in fails:
            print(" -", item)
        raise SystemExit(1)
    print(f"PASS crest={info['frac']:.2f} rise={info['rise']:.2f}")
    for name, clr in CLEARANCES.items():
        tris = shell_tris(clr) + id_mark_tris(name)
        path = os.path.join(out, f"brace_coupon_{name}.stl")
        write_stl(path, tris, f"brace_{name}")
        n_bars = {"C0": 1, "C1": 2, "C2": 3}[name]
        print(
            f"{name}: clearance {clr} tyreW {width_at(0, clr):.1f} seatW {width_at(LENGTH, clr):.1f} "
            f"length {LENGTH} idBars {n_bars} tris {len(tris)} -> {path}"
        )


if __name__ == "__main__":
    main()
