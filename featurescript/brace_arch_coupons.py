"""Brace-fit saddle coupons from the Kiri scan plus James's calipers.

The brace arch is about 60 mm inside on the tyre face and 50 mm on the
seat-tube face, 12 mm thick front to back, 20 mm tall. A plastic flap on
top of the brace is ignored. The scanned underside, once the chord is
levelled, is only a couple of millimetres off a straight line, so the
crown rise is 2 mm and the corners are R8.

Three coupons, extra clearance 0 / 0.4 / 0.8 mm per side, so one sits
snug and the others show a tight scan. Four zip-tie slots on the crown.
Offline only — no Onshape.
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
# Levelled Kiri underside across the 60 mm tyre-side mouth. y is outward.
# Heights are a 3-point smooth of the levelled scan, so 1 mm of mesh noise
# does not print as a ridge. The raw scan is drawn dashed on the section plot.
SCAN_CROWN = np.array([
    [-30.12, 4.30], [-27.41, 2.57], [-24.10, 1.45], [-21.82, 0.32],
    [-19.11, 0.64], [-16.83, 0.95], [-14.55, 1.56], [-12.04, 2.15],
    [-9.74, 2.62], [-7.38, 2.99], [-4.81, 3.35], [-2.50, 3.33],
    [0.14, 3.71], [2.42, 3.98], [4.80, 4.71], [7.12, 5.18],
    [9.58, 5.59], [12.01, 5.78], [14.48, 6.13], [16.83, 6.24],
    [19.44, 6.30], [21.94, 6.17], [24.42, 5.62], [27.33, 5.01],
    [29.88, 4.30],
])
CLEARANCES = {"C0": 0.0, "C1": 0.4, "C2": 0.8}
# Two pairs, as in the photo. Long axis lateral, short axis along the bike.
SLOT_X = 11.0
SLOT_Z = (5.0, 15.0)
SLOT_LEN = 6.0
SLOT_WID = 2.4


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
    crown = np.column_stack([SCAN_CROWN[:, 0] * scale, SCAN_CROWN[:, 1] - clearance])
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
    for zc in SLOT_Z:
        if abs(z - zc) <= SLOT_WID / 2.0 and abs(abs(x) - SLOT_X) <= SLOT_LEN / 2.0:
            return True
    return False


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


def scan_envelope() -> np.ndarray | None:
    """Levelled underside from the Kiri scan, if the GLB is on disk."""
    glb = os.path.join(os.path.dirname(__file__), "..", "refs", "rear_brace_edit1.glb")
    if not os.path.isfile(glb):
        return None
    try:
        import DracoPy
    except ImportError:
        return None
    data = open(glb, "rb").read()
    ln = struct.unpack_from("<I", data, 12)[0]
    import json

    js = json.loads(data[20 : 20 + ln])
    bin_off = 20 + ln
    bln = struct.unpack_from("<I", data, bin_off)[0]
    blob = data[bin_off + 8 : bin_off + 8 + bln]
    bv = js["bufferViews"][1]
    chunk = blob[bv["byteOffset"] : bv["byteOffset"] + bv["byteLength"]]
    mm = np.asarray(DracoPy.decode(chunk).points, dtype=np.float64) * 1000.0
    sl = mm[(np.abs(mm[:, 0] + 25) < 8) & (mm[:, 2] > 450) & (mm[:, 2] < 545) & (mm[:, 1] > -980) & (mm[:, 1] < -900)]
    zs = np.linspace(460, 535, 25)
    rows = []
    for z in zs:
        p = sl[np.abs(sl[:, 2] - z) < 4]
        if len(p) < 2:
            continue
        rows.append((z, p[:, 1].min()))
    if len(rows) < 4:
        return None
    env = np.array(rows)
    # Level the chord and put the mouth on 60 mm, crown up.
    z0, z1 = env[0, 0], env[-1, 0]
    y0, y1 = env[0, 1], env[-1, 1]
    chord = math.hypot(z1 - z0, y1 - y0)
    ang = math.atan2(y1 - y0, z1 - z0)
    c, s = math.cos(-ang), math.sin(-ang)
    rel = env - env[0]
    xy = np.column_stack([c * rel[:, 0] - s * rel[:, 1], s * rel[:, 0] + c * rel[:, 1]])
    xy[:, 0] = (xy[:, 0] - xy[:, 0].mean()) * (TYRE_OPEN / chord)
    xy[:, 1] -= xy[:, 1].min()
    return xy


def section_plot(out_path: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5), dpi=130)
    colors = {"C0": "#1b4f72", "C1": "#b9770e", "C2": "#196f3d"}
    for name, clr in CLEARANCES.items():
        w = width_at(0.0, clr)
        outer = outer_section(w, clr)
        ax.plot(outer[:, 0], outer[:, 1], color=colors[name], lw=2, label=f"{name} clearance {clr:.1f} mm")
    env = scan_envelope()
    if env is not None:
        ax.plot(env[:, 0], env[:, 1], color="#922b21", lw=1.2, ls="--", label="scan underside, levelled")
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


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out = os.path.join(root, "out")
    os.makedirs(out, exist_ok=True)
    for name, clr in CLEARANCES.items():
        tris = shell_tris(clr) + id_mark_tris(name)
        path = os.path.join(out, f"brace_coupon_{name}.stl")
        write_stl(path, tris, f"brace_{name}")
        n_bars = {"C0": 1, "C1": 2, "C2": 3}[name]
        print(
            f"{name}: clearance {clr} tyreW {width_at(0, clr):.1f} seatW {width_at(LENGTH, clr):.1f} "
            f"length {LENGTH} idBars {n_bars} tris {len(tris)} -> {path}"
        )
    section_plot(os.path.join(out, "brace_coupon_sections.png"))


if __name__ == "__main__":
    main()
