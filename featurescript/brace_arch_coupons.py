"""Black-plastic shell that fits around the rear brace.

The cavity is the negative of the brace (60 mm tyre mouth, 50 mm seat-tube
mouth, 12 mm thick, 20 mm tall). The shell is 2.5 mm thick and flares about
10 mm past the cavity on each side, where the brace meets the seatstays.
Three coupons, extra clearance 0 / 0.4 / 0.8 mm. The script writes STLs only
when the side silhouette matches refs/brace_side_ref.png. Offline only.
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
DEPTH = 16.0  # brace height the cavity still wraps
WALL = 2.5
FLARE = 20.0  # how far the skirt reaches out over the stay, each side
SKIRT = 45.0  # how far the skirt runs down the stay
FLARE_TURN = math.radians(82)  # ends heading down the stay, not curled back under
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


def _unit(v: np.ndarray) -> np.ndarray:
    n = float(np.linalg.norm(v))
    return v / n if n else v


def cavity_curve(width: float, clearance: float) -> np.ndarray:
    """Inner face. This is the brace, scaled to the mouth, opened by clearance."""
    src = crown_profile()
    scale = width / TYRE_OPEN
    return np.column_stack([src[:, 0] * scale, src[:, 1] - clearance])


def _flare(origin: np.ndarray, tangent: np.ndarray, side: int, n: int = 16) -> np.ndarray:
    """Continue past a cavity end, down and out over the seatstay.

    side is -1 on the left and +1 on the right. The turn drops the tip
    about SKIRT millimetres and carries it FLARE millimetres further out.
    """
    heading = _unit(tangent)
    # Clockwise on the right, counterclockwise on the left: both pitch down.
    dth = (-1 if side > 0 else 1) * FLARE_TURN / n
    c, s = math.cos(dth), math.sin(dth)
    step = (FLARE + SKIRT) / n
    p = origin.astype(float).copy()
    pts = []
    for _ in range(n):
        heading = np.array([c * heading[0] - s * heading[1], s * heading[0] + c * heading[1]])
        p = p + step * heading
        pts.append(p.copy())
    return np.array(pts)


def contact_path(width: float, clearance: float) -> np.ndarray:
    """Cavity plus the two flares, ordered left tip to right tip."""
    cav = cavity_curve(width, clearance)
    left = _flare(cav[0], cav[0] - cav[1], side=-1)
    right = _flare(cav[-1], cav[-1] - cav[-2], side=1)
    return np.vstack([left[::-1], cav, right])


def offset_tyre(poly: np.ndarray, dist: float) -> np.ndarray:
    """Offset to the tyre side of a left-to-right path. That is the outer skin."""
    out = np.zeros_like(poly)
    for i in range(len(poly)):
        if i == 0:
            t = poly[1] - poly[0]
        elif i == len(poly) - 1:
            t = poly[-1] - poly[-2]
        else:
            t = poly[i + 1] - poly[i - 1]
        t = _unit(t)
        out[i] = poly[i] + dist * np.array([t[1], -t[0]])
    return out


def shell_section(width: float, clearance: float) -> tuple[np.ndarray, np.ndarray]:
    """Outer skin and the brace-facing skin. The gap between them is the plastic."""
    contact = contact_path(width, clearance)
    outer = offset_tyre(contact, WALL)
    return outer, contact


def _in_slot(x: float, z: float) -> bool:
    if abs(z - SLOT_Z) > SLOT_WID / 2.0:
        return False
    return any(abs(x - xc) <= SLOT_LEN / 2.0 for xc in SLOT_X)


def _on_crown(y: float) -> bool:
    return y > -1.0


def shell_tris(clearance: float) -> list[np.ndarray]:
    zs = np.linspace(0.0, LENGTH, 21)
    outers = []
    inners = []
    for z in zs:
        w = width_at(float(z), clearance)
        outer, inner = shell_section(w, clearance)
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
    # End caps only. The mouth between the flare tips stays open so the
    # shell can seat on the brace.
    for end, z, outer, inner in (
        (0, 0.0, outers[0], inners[0]),
        (1, LENGTH, outers[-1], inners[-1]),
    ):
        for i in range(n - 1):
            if end == 0:
                quad(P(outer[i], z), P(inner[i], z), P(inner[i + 1], z), P(outer[i + 1], z))
            else:
                quad(P(outer[i], z), P(outer[i + 1], z), P(inner[i + 1], z), P(inner[i], z))
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
    """1/2/3 bars on the tyre side of the handle, clear of the brace face."""
    n = {"C0": 1, "C1": 2, "C2": 3}[label]
    clr = CLEARANCES[label]
    outer, _ = shell_section(width_at(0.0, clr), clr)
    # Crest of the outer skin, near x = 0.
    i = int(np.argmin(np.abs(outer[:, 0])))
    y_out = float(outer[i, 1])
    tris: list[np.ndarray] = []
    for k in range(n):
        z0 = 1.2 + k * 2.2
        tris.extend(_box_tris(-6.0, 6.0, y_out - 1.2, y_out + 0.4, z0, z0 + 1.4))
    for k in range(n):
        x0 = -8.0 + k * 6.0
        tris.extend(_box_tris(x0, x0 + 3.0, y_out - 1.2, y_out + 0.6, -1.2, 0.0))
    return tris


def _photo_top() -> tuple[np.ndarray, np.ndarray]:
    """Smooth upper edge of the black plastic against the light background.

    A quadratic fit, because the raw edge has an 8 px step from a highlight
    and the holes. y is upward, in pixels.
    """
    ref = os.path.join(os.path.dirname(__file__), "..", "refs", "brace_side_ref.png")
    im = plt.imread(ref)
    g = im[..., :3].mean(-1)
    xs, ys = [], []
    for x in range(g.shape[1]):
        hit = np.where(g[:, x] < 0.70)[0]
        if len(hit):
            xs.append(float(x))
            ys.append(float(hit.min()))
    xs = np.array(xs)
    ys = np.array(ys)
    fit = np.polyval(np.polyfit(xs, -ys, 2), xs)
    return xs, fit


def _vertical_run(x: np.ndarray, y: np.ndarray) -> float:
    """Longest run that drops while barely moving sideways, in mm."""
    worst = 0.0
    run = 0.0
    for dx, dy in zip(np.diff(x), np.diff(y)):
        if abs(dx) < 0.4 and dy < 0:
            run += -dy
            worst = max(worst, run)
        else:
            run = 0.0
    return worst


def side_checks() -> tuple[list[str], dict]:
    """The gate is the black plastic, not the brace curve."""
    outer, contact = shell_section(width_at(0.0, 0.0), 0.0)
    x = outer[:, 0]
    y = outer[:, 1]
    order = np.argsort(x)
    x, y = x[order], y[order]
    # One sample per x so a flare that folds back does not confuse the crest.
    xs = np.linspace(float(x.min()), float(x.max()), 80)
    ys = np.interp(xs, x, y)
    i = int(np.argmax(ys))
    frac = i / (len(ys) - 1)
    fails: list[str] = []
    if not 0.33 <= frac <= 0.67:
        fails.append(f"crest at {frac:.2f} of the width, want the middle third")
    wall = _vertical_run(xs, ys)
    if wall > 3.0:
        fails.append(f"vertical run {wall:.1f} mm, want under 3")
    span = float(xs[-1] - xs[0])
    cavity = width_at(0.0, 0.0)
    if span < cavity + 2 * FLARE - 2:
        fails.append(f"shell span {span:.1f} mm, want flares past the {cavity:.0f} mm cavity")
    drop = float(ys.max() - ys.min())
    if drop < SKIRT:
        fails.append(f"skirt drop {drop:.0f} mm, want at least {SKIRT:.0f} down the stay")
    # The photo is a crop of the crown. Scale it to the cavity, not the whole skirt.
    px, py = _photo_top()
    crest_x = float(xs[i])
    photo_x = (px - px.mean()) / (px.max() - px.min()) * cavity + crest_x
    photo_y = (py - py.min()) / (px.max() - px.min()) * cavity
    photo_y = photo_y - photo_y.max() + ys.max()
    shell_y = np.interp(photo_x, xs, ys)
    band = (photo_x > crest_x - 0.35 * cavity) & (photo_x < crest_x + 0.35 * cavity)
    err = float(np.mean(np.abs(shell_y[band] - photo_y[band])))
    if err > 2.0:
        fails.append(f"side outline {err:.2f} mm from the photo, want under 2")
    crest_y = float(ys.max())
    for xc in SLOT_X:
        yy = float(np.interp(xc, xs, ys))
        if yy < crest_y - 2.5:
            fails.append(f"slot at x={xc:.0f} is off the crest")
    gaps = np.diff(np.sort(np.array(SLOT_X, dtype=float)))
    if not (gaps[0] < gaps[1] and gaps[2] < gaps[1]):
        fails.append("slots are not two pairs")
    return fails, {
        "frac": frac,
        "err": err,
        "span": span,
        "xs": xs,
        "ys": ys,
        "photo_x": photo_x,
        "photo_y": photo_y,
        "outer": outer,
        "contact": contact,
    }


def section_plot(out_path: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5), dpi=130)
    colors = {"C0": "#1b4f72", "C1": "#b9770e", "C2": "#196f3d"}
    for name, clr in CLEARANCES.items():
        outer, contact = shell_section(width_at(0.0, clr), clr)
        ax.plot(outer[:, 0], outer[:, 1], color=colors[name], lw=2, label=f"{name} shell")
        ax.plot(contact[:, 0], contact[:, 1], color=colors[name], lw=1, ls=":")
    if _RAW is not None:
        ax.plot(_RAW[:, 0], _RAW[:, 1], color="#922b21", lw=1.0, ls="--", label="brace underside")
    ax.set_aspect("equal")
    ax.set_xlabel("lateral (mm)")
    ax.set_ylabel("toward the brace (mm)")
    ax.set_title("Shell around the brace — cavity dotted, outer skin solid")
    ax.legend(fontsize=8)
    ax.grid(True, alpha=0.3)
    fig.tight_layout()
    fig.savefig(out_path)
    plt.close()
    print(out_path)


def side_plot(out_path: str, info: dict, ok: bool) -> None:
    fig, axes = plt.subplots(1, 2, figsize=(9, 4.2), dpi=130)
    axes[0].fill(info["outer"][:, 0], info["outer"][:, 1], color="#1b4f72", alpha=0.85)
    axes[0].plot(info["photo_x"], info["photo_y"], color="#b9770e", lw=1.5, label="photo")
    for xc in SLOT_X:
        yy = float(np.interp(xc, info["xs"], info["ys"]))
        axes[0].plot([xc - 3, xc + 3], [yy, yy], color="#f5d0a9", lw=2)
    axes[0].set_aspect("equal")
    axes[0].set_title(f"C0 shell  crest {info['frac']:.2f}  outline {info['err']:.2f} mm")
    axes[0].set_xlabel("lateral (mm)")
    axes[0].legend(fontsize=8)
    axes[0].grid(True, alpha=0.3)
    ref = os.path.join(os.path.dirname(__file__), "..", "refs", "brace_side_ref.png")
    axes[1].imshow(plt.imread(ref))
    axes[1].set_title("black plastic")
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
    print(f"PASS crest={info['frac']:.2f} outline={info['err']:.2f} span={info['span']:.1f}")
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
