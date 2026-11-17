"""Offline geometry for the Amflow PR Carbon rear mudguard, layout 1c.

No Onshape. Volumes, tyre gap, stay clearance and bed-fit come from this model.
The FeatureScript builds the same sections; the test bundle's volume window is
+/-8% around the volumes printed here.

Coordinates: origin at the rear axle, +X forward, +Z up, +Y to the rider's left.
phi is from +X toward +Z (90 deg at top dead centre). s is arc length on the
outer top, s=0 at the seatstay brace, +s toward TDC.
"""
from __future__ import annotations

import math
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.patches import Polygon

# ---------------------------------------------------------------- constants
# James measured the brace underside 30 mm off the tyre tread. That radius
# drives the guard: the 1.5 mm pad sits on the plate, so the plate outer is
# tyre + braceGap - pad.
TYRE_R = 350.0
BRACE_GAP = 30.0
PAD = 1.5
R_BRACE = TYRE_R + BRACE_GAP  # 380, brace underside
R = R_BRACE - PAD  # 378.5, outer lap plate and main guard top
WALL = 2.5
LAP_WALL = 3.0
TYRE_W = 63.5
TYRE_RAD = TYRE_W / 2.0
TYRE_C = TYRE_R - TYRE_RAD
PHI0 = math.radians(33.6)

S_A0, S_A1 = -135.0, 20.0
S_B0, S_B1 = -20.0, 330.0
NECK0, NECK1 = -20.0, 40.0
LAP0, LAP1 = -20.0, 20.0
SHOULDER_S = -20.0
SHOULDER_THK = 2.5

NECK_A = 26.0  # outer half-width, 52 mm
WIDE_A = 47.0  # 94 mm, inside the 90-95 band and 3 mm off a 100 mm gap
NECK_R = 4.0
WIDE_R = 12.0
# At R 378.5 a 22 mm wall bottom is inside the 15 mm tyre gap. 17 mm keeps the
# tongue's inner wall, the closest point, just over 15 mm.
SECTION_H = 17.0
LAP_CLEAR = 0.2

RIB = 3.0
RIB_Y = 22.0
RIB_EMBED = 0.6
TRIM_S = (-120.0, -100.0, -80.0, -60.0)
TRIM_W, TRIM_H = 0.6, 0.4
BRACE_RIB0, BRACE_RIB1 = -15.0, 15.0

SLOT_S = 14.0
SLOT_Y = 12.0
SLOT_L = 6.0
SLOT_A = 2.7
SLOT_B = 2.4
SLOT_R = 0.8
RECESS_W, RECESS_D = 6.2, 1.3
BOSS = 1.0
RAIL_Y, RAIL_W, RAIL_H = 20.0, 3.0, 1.2
GROOVE_W, GROOVE_D = 3.6, 1.5
RAIL_CHAMFER = 4.0

# Stay tabs. With the brace at R 380 the old d=40 / d=44 stations are inside
# the tyre clearance. The lower slot is the deepest 6 mm slot whose inner face
# stays at or above 15 mm; the tab ends just past that slot.
TAB_S = 16.0
TAB_D0 = 8.0
TAB_D1 = 27.5
TAB_THK = 3.0
TAB_SLOT_D = (20.0, 24.0)
SIDE_CLR = 3.0
LIP_A = 1.8
LIP_B = 2.0
TAIL_R = 5.0
BED = 250.0


def smooth(u: float) -> float:
    u = 0.0 if u < 0.0 else 1.0 if u > 1.0 else u
    return u * u * (3.0 - 2.0 * u)


def blend_down(s: float, s0: float, s1: float, v0: float, v1: float) -> float:
    if s <= s0:
        return v0
    if s >= s1:
        return v1
    return v0 + (v1 - v0) * smooth((s - s0) / (s1 - s0))


def section_params(s: float) -> tuple[float, float, float]:
    """Outer half-width a, corner radius, depth. Neck is exactly 52 wide, depth is constant."""
    h = SECTION_H
    if s < -55.0:
        return WIDE_A, WIDE_R, h
    if s < NECK0:
        return blend_down(s, -55.0, NECK0, WIDE_A, NECK_A), blend_down(s, -55.0, NECK0, WIDE_R, NECK_R), h
    if s <= NECK1:
        return NECK_A, NECK_R, h
    if s < 85.0:
        return blend_down(s, NECK1, 85.0, NECK_A, WIDE_A), blend_down(s, NECK1, 85.0, NECK_R, WIDE_R), h
    return WIDE_A, WIDE_R, h


def phi_of(s: float) -> float:
    return PHI0 + s / R


def rn(phi: float) -> np.ndarray:
    return np.array([math.cos(phi), 0.0, math.sin(phi)])


def tangent(phi: float) -> np.ndarray:
    return np.array([-math.sin(phi), 0.0, math.cos(phi)])


def world(s: float, y: float, depth: float) -> np.ndarray:
    p = phi_of(s)
    return rn(p) * (R - depth) + np.array([0.0, y, 0.0])


def stay_gap(d: float) -> float:
    if d <= 0.0:
        return 60.0
    if d <= 50.0:
        return 60.0 + 30.0 * d / 50.0
    if d <= 100.0:
        return 90.0 + 10.0 * (d - 50.0) / 50.0
    return 100.0


def tab_y_outer(d: float) -> float:
    return stay_gap(d) / 2.0 - SIDE_CLR


def tyre_gap(radius: float, y: float) -> float:
    return math.hypot(radius - TYRE_C, y) - TYRE_RAD


def rounded_rect_area(length: float, width: float, radius: float) -> float:
    return length * width - (4.0 - math.pi) * radius * radius


# ---------------------------------------------------------------- profiles
def _arc(cx: float, cy: float, radius: float, a0: float, a1: float, n: int = 8) -> list[tuple[float, float]]:
    if radius <= 0.05 or n < 1:
        return []
    return [
        (cx + radius * math.cos(a0 + (a1 - a0) * i / n), cy + radius * math.sin(a0 + (a1 - a0) * i / n))
        for i in range(n + 1)
    ]


def u_outer(a: float, rad: float, h: float) -> list[tuple[float, float]]:
    """Closed outer U. Sketch x = lateral, y = depth inward. Starts at the top centre."""
    c = a - rad
    pts = [(0.0, 0.0), (c, 0.0)]
    pts += _arc(c, rad, rad, -math.pi / 2.0, 0.0)[1:]
    pts += [(a, h), (-a, h), (-a, rad)]
    pts += _arc(-c, rad, rad, math.pi, math.pi / 2.0)[1:]
    pts += [(-c, 0.0), (0.0, 0.0)]
    return pts


def u_inset(a: float, rad: float, h: float, q: float) -> list[tuple[float, float]]:
    """Inward parallel of the U, closed across the wall ends. q < a and q < h."""
    if q >= rad:
        x = a - q
        return [(0.0, q), (x, q), (x, h), (-x, h), (-x, q), (0.0, q)]
    c = a - rad
    ri = rad - q
    pts = [(0.0, q), (c, q)]
    pts += _arc(c, rad, ri, -math.pi / 2.0, 0.0)[1:]
    pts += [(a - q, h), (-(a - q), h), (-(a - q), rad)]
    pts += _arc(-c, rad, ri, math.pi, math.pi / 2.0)[1:]
    pts += [(-c, q), (0.0, q)]
    return pts


def polygon_area(pts: list[tuple[float, float]]) -> float:
    acc = 0.0
    for i in range(len(pts) - 1):
        acc += pts[i][0] * pts[i + 1][1] - pts[i + 1][0] * pts[i][1]
    return 0.5 * acc


def _inside(x: float, y: float, pts: list[tuple[float, float]]) -> bool:
    n = len(pts)
    hit = False
    j = n - 1
    for i in range(n):
        yi, yj = pts[i][1], pts[j][1]
        xi, xj = pts[i][0], pts[j][0]
        if (yi > y) != (yj > y):
            xint = (xj - xi) * (y - yi) / (yj - yi + 1e-30) + xi
            if x < xint:
                hit = not hit
        j = i
    return hit


def shell_masks(a: float, rad: float, h: float, wall: float):
    return u_outer(a, rad, h), u_inset(a, rad, h, wall)


def _corner_cut_area(rad: float) -> float:
    if rad <= 0.0:
        return 0.0
    return rad * rad * (1.0 - math.pi / 4.0)


def _corner_cut_moment(rad: float) -> float:
    """First moment about the top plane of one square-minus-quarter-disk corner."""
    if rad <= 0.0:
        return 0.0
    square = rad ** 3 / 2.0
    disk = (math.pi * rad ** 2 / 4.0) * rad * (1.0 - 4.0 / (3.0 * math.pi))
    return square - disk


def _rounded_block(a: float, rad: float, h: float) -> tuple[float, float]:
    """Area and y-moment of a U-block of half-width a, top corner radius rad, depth h."""
    area = 2.0 * a * h - 2.0 * _corner_cut_area(rad)
    moment = a * h * h - 2.0 * _corner_cut_moment(rad)
    return area, moment


def _cavity(a: float, rad: float, h: float, q: float) -> tuple[float, float]:
    """Area and y-moment of the open interior, inset by q."""
    if q >= h or q >= a:
        return 0.0, 0.0
    aw = a - q
    hh = h - q
    ri = max(0.0, rad - q)
    area, moment = _rounded_block(aw, ri, hh)
    return area, moment + q * area


def section_factor(a: float, rad: float, h: float, wall: float) -> float:
    """Integral of (R-y)/R over the shell section. Times ds this is volume."""
    oa, om = _rounded_block(a, rad, h)
    ia, im = _cavity(a, rad, h, wall)
    area = oa - ia
    moment = om - im
    return area - moment / R


def cavity_factor(a: float, rad: float, h: float, wall: float) -> float:
    area, moment = _cavity(a, rad, h, wall)
    return area - moment / R


def tongue_factor(a: float, rad: float, h: float) -> float:
    q0 = LAP_WALL + LAP_CLEAR
    q1 = q0 + LAP_WALL
    return cavity_factor(a, rad, h, q0) - cavity_factor(a, rad, h, q1)


# ---------------------------------------------------------------- features
def rib_intervals(s0: float, s1: float, breaks: tuple[float, ...], gap: float) -> list[tuple[float, float]]:
    cuts = [(s0, s1)]
    blocked = [(b - gap, b + gap) for b in breaks]
    blocked.append((BRACE_RIB0, BRACE_RIB1))
    for c0, c1 in blocked:
        nxt = []
        for a, b in cuts:
            if c1 <= a or c0 >= b:
                nxt.append((a, b))
                continue
            if c0 > a:
                nxt.append((a, c0))
            if c1 < b:
                nxt.append((c1, b))
        cuts = [(a, b) for a, b in nxt if b - a > 0.5]
    return cuts


def integrate_shell(s0: float, s1: float, wall_of, step_s: float = 2.0) -> float:
    ss = np.arange(s0, s1 + 1e-6, step_s)
    if ss[-1] < s1 - 1e-6:
        ss = np.append(ss, s1)
    vals = []
    for s in ss:
        a, rad, h = section_params(float(s))
        vals.append(section_factor(a, rad, h, wall_of(float(s))))
    vol = 0.0
    for i in range(len(ss) - 1):
        ds = float(ss[i + 1] - ss[i])
        vol += 0.5 * (vals[i] + vals[i + 1]) * ds
    return vol


def wall_A(s: float) -> float:
    return LAP_WALL if s >= LAP0 else WALL


def wall_B(s: float) -> float:
    # s=20..26 is a thick bridge so the tongue (depth 3.2..6.2) joins the 2.5 mm shell
    if s < 26.0:
        return 6.6
    return WALL


def feature_volumes() -> dict:
    print("integrating shells...")
    vA = integrate_shell(S_A0, S_A1, wall_A, 2.0)
    vB = integrate_shell(20.0, S_B1, wall_B, 2.0)
    # tongue
    tongue = 0.0
    ss = np.linspace(LAP0, LAP1, 9)
    moms = []
    for s in ss:
        a, rad, h = section_params(float(s))
        moms.append(tongue_factor(a, rad, h))
    for i in range(len(ss) - 1):
        tongue += 0.5 * (moms[i] + moms[i + 1]) * float(ss[i + 1] - ss[i])
    # shoulder bulkhead fills A's cavity just ahead of the lap
    a, rad, h = section_params(SHOULDER_S)
    shoulder = cavity_factor(a, rad, h, WALL) * SHOULDER_THK
    # grooves, two of them, through the lap plate only
    groove = 2.0 * GROOVE_W * GROOVE_D * (LAP1 - LAP0) * (R - (LAP_WALL - GROOVE_D / 2.0)) / R
    # rails with a linear lead-in chamfer over RAIL_CHAMFER
    rail_len = (LAP1 - LAP0) - RAIL_CHAMFER
    rail = 2.0 * RAIL_W * RAIL_H * (rail_len + 0.5 * RAIL_CHAMFER) * (R - LAP_WALL) / R
    # brace slots
    area_A = rounded_rect_area(SLOT_L, SLOT_A, SLOT_R)
    area_B = rounded_rect_area(SLOT_L, SLOT_B, SLOT_R)
    slots_A = 4.0 * area_A * LAP_WALL
    slots_B = 4.0 * area_B * LAP_WALL
    # recess on the tongue underside, minus the part already removed by B's slots
    recess_len = 2.0 * SLOT_S
    recess = 2.0 * RECESS_W * RECESS_D * recess_len * (R - (LAP_WALL + LAP_CLEAR + LAP_WALL - RECESS_D / 2.0)) / R
    recess_overlap = 4.0 * min(SLOT_L, RECESS_W) * SLOT_B * RECESS_D
    # bosses, 8 x 4 x 1 on the +s side of each brace slot, plus 4 stay bosses
    bosses = (4.0 + 4.0) * 8.0 * 4.0 * BOSS
    # ribs. Proud 3 x 3; the embed is inside the shell and adds nothing.
    rib_A = rib_intervals(S_A0, S_A1, TRIM_S, 5.0)
    rib_B = rib_intervals(20.0, S_B1 - 5.0, (), 5.0)
    len_A = sum(b - a for a, b in rib_A)
    len_B = sum(b - a for a, b in rib_B)
    ribs_A = 2.0 * RIB * RIB * len_A * (R + RIB / 2.0) / R
    ribs_B = 2.0 * RIB * RIB * len_B * (R + RIB / 2.0) / R
    # trim guides, four bars across the 40 mm top flat
    trims = 4.0 * TRIM_W * TRIM_H * 40.0
    # lips: bead along the free end. Perimeter approximated from the outer profile.
    def peri(s, lip):
        a, rad, h = section_params(s)
        # top + two walls + two corner quarters
        return 2.0 * (a - rad) + 2.0 * (h - rad) + math.pi * rad

    lipA = peri(S_A0, LIP_A) * LIP_A * LIP_A
    # skirt hem, both wall bottoms, s -135..-20
    hem = 2.0 * (NECK0 - S_A0) * LIP_A * LIP_A * 0.5  # average, blend shortens one end
    lipB = peri(S_B1, LIP_B) * LIP_B * LIP_B
    # R5 corners remove two (r^2 - pi r^2/4) patches through the local depth
    corner_cut = 2.0 * (TAIL_R ** 2 * (1.0 - math.pi / 4.0)) * SECTION_H
    tabs, tab_slots = tab_volume()
    parts = {
        "shellA": vA,
        "shoulder": shoulder,
        "grooves": -groove,
        "slotsA": -slots_A,
        "ribsA": ribs_A,
        "trims": trims,
        "lipA": lipA + hem,
        "shellB": vB,
        "tongue": tongue,
        "rails": rail,
        "slotsB": -slots_B,
        "recess": -(recess - recess_overlap),
        "bosses": bosses,
        "ribsB": ribs_B,
        "lipB": lipB,
        "tailCorners": -corner_cut,
        "tabs": tabs,
        "tabSlots": -tab_slots,
    }
    # bosses split: 4 brace bosses are on A, 4 stay bosses on B
    brace_boss = 4.0 * 8.0 * 4.0 * BOSS
    stay_boss = brace_boss
    volA = (
        parts["shellA"]
        + parts["shoulder"]
        + parts["grooves"]
        + parts["slotsA"]
        + parts["ribsA"]
        + parts["trims"]
        + parts["lipA"]
        + brace_boss
    )
    volB = (
        parts["shellB"]
        + parts["tongue"]
        + parts["rails"]
        + parts["slotsB"]
        + parts["recess"]
        + parts["ribsB"]
        + parts["lipB"]
        + parts["tailCorners"]
        + parts["tabs"]
        + parts["tabSlots"]
        + stay_boss
    )
    return {"mm3": parts, "volA": volA, "volB": volB, "ribLenA": len_A, "ribLenB": len_B}


def tab_volume() -> tuple[float, float]:
    """Tab material that is not already inside the shell, plus the slot voids."""
    # sample s, d, y. Cell volume uses the radial jacobian.
    ds, dd, dy = 1.0, 1.0, 0.5
    extra = 0.0
    a, rad, h = section_params(0.0)
    outer, inner = shell_masks(a, rad, h, WALL)
    for s in np.arange(-TAB_S, TAB_S + 1e-6, ds):
        for d in np.arange(TAB_D0, TAB_D1 + 1e-6, dd):
            yo = tab_y_outer(float(d))
            y_lo = yo - TAB_THK
            # extend 4 mm into the shell so the union is solid; that part is not extra volume
            y_root = min(y_lo, 18.0)
            depth = R_BRACE - (R - 0.0) + (R - (R_BRACE - float(d)))
            # depth of a point at this radius: R - r = R - (R_BRACE - d) = d - (R_BRACE - R) = d - PAD
            depth = float(d) - PAD
            jac = (R_BRACE - float(d)) / R
            for y in np.arange(y_root, yo + 1e-6, dy):
                yv = float(y)
                in_shell = False
                if 0.0 <= depth <= h and _inside(yv, depth, outer) and not _inside(yv, depth, inner):
                    in_shell = True
                if _inside(-yv, depth, outer) and not _inside(-yv, depth, inner) and 0.0 <= depth <= h:
                    in_shell = True
                if not in_shell and yv >= y_lo:
                    # count both sides by symmetry: this loop is +y only, times 2 below
                    extra += ds * dd * dy * jac
    extra *= 2.0  # both stays
    slot_area = rounded_rect_area(6.0, SLOT_B, SLOT_R)
    # slots are entirely in the exposed tab (d=20 and d=40, |y| near the outer face)
    slots = 2.0 * 2.0 * slot_area * TAB_THK
    return extra, slots


# ---------------------------------------------------------------- clearance
def min_tyre_and_side() -> dict:
    worst = {"gap": 1e9, "where": ""}
    samples = []

    def consider(gap: float, where: str):
        samples.append((gap, where))
        if gap < worst["gap"]:
            worst["gap"] = gap
            worst["where"] = where

    for s in np.linspace(S_A0, S_B1, 181):
        a, rad, h = section_params(float(s))
        wall = LAP_WALL if LAP0 <= s <= LAP1 else WALL
        # A's shell covers s<=20, B's shell covers s>=20. The lap underside is the tongue.
        if s < LAP0 or s > LAP1:
            inner = u_inset(a, rad, h, wall)
            for x, y in inner[::2]:
                consider(tyre_gap(R - y, abs(x)), f"shell s={s:.0f}")
        else:
            q1 = LAP_WALL + LAP_CLEAR + LAP_WALL
            inner = u_inset(a, rad, h, q1)
            for x, y in inner[::2]:
                consider(tyre_gap(R - y, abs(x)), f"tongue s={s:.0f}")
            # recess floor is farther from the tyre than the tongue inner top
    # tabs, inner face and the tip edge
    for d in np.linspace(TAB_D0, TAB_D1, 37):
        r = R_BRACE - float(d)
        yi = tab_y_outer(float(d)) - TAB_THK
        consider(tyre_gap(r, yi), f"tab d={d:.1f}")
    side_min = 1e9
    side_at = ""
    for d in np.linspace(TAB_D0, TAB_D1, 37):
        clr = stay_gap(float(d)) / 2.0 - tab_y_outer(float(d))
        if clr < side_min:
            side_min = clr
            side_at = f"tab outer d={d:.1f}"
    # shell neck vs stay at the matching depth
    for s in np.linspace(NECK0, NECK1, 13):
        a, rad, h = section_params(float(s))
        d = h + PAD
        clr = stay_gap(d) / 2.0 - a
        if clr < side_min:
            side_min = clr
            side_at = f"neck wall s={s:.0f} d={d:.1f}"
    return {"tyre": worst, "side": side_min, "sideAt": side_at, "n": len(samples)}


def counterfactual_d55() -> float:
    d = 55.0
    r = R_BRACE - d
    yi = tab_y_outer(d) - TAB_THK
    return tyre_gap(r, yi)


# ---------------------------------------------------------------- bed
def _xz(s: float, radius: float) -> np.ndarray:
    p = phi_of(s)
    return rn(p) * radius


def silhouette(part: str) -> np.ndarray:
    """Side-view (X, Z) outline of the finished part, including ribs, lips and tabs."""
    pts = []
    if part == "A":
        spans = [(S_A0, S_A1, True)]
    else:
        # tongue is inset; the shell outer starts where A ends
        spans = [(-20.0, 20.0, False), (20.0, S_B1, True)]
    ribs = rib_intervals(S_A0, S_A1, TRIM_S, 5.0) if part == "A" else rib_intervals(20.0, S_B1 - 5.0, (), 5.0)
    for s0, s1, full_outer in spans:
        for s in np.linspace(s0, s1, 181):
            _a, _r, h = section_params(float(s))
            rib_here = any(a0 <= s <= b0 for a0, b0 in ribs)
            extra = RIB if (rib_here and full_outer) else 0.0
            if part == "A" and abs(s - S_A0) < 1.2:
                extra = max(extra, LIP_A)
            if part == "B" and full_outer and abs(s - S_B1) < 1.2:
                extra = max(extra, LIP_B)
            outer_r = (R + extra) if full_outer else (R - (LAP_WALL + LAP_CLEAR))
            pts.append(_xz(float(s), outer_r))
            pts.append(_xz(float(s), R - h))
    if part == "B":
        for s in (-TAB_S, TAB_S):
            for d in (TAB_D0, TAB_D1):
                pts.append(rn(phi_of(s)) * (R_BRACE - d))
    return np.array([[p[0], p[2]] for p in pts])


def rotate_box(pts: np.ndarray, ang_deg: float) -> tuple[float, float]:
    a = math.radians(ang_deg)
    c, s = math.cos(a), math.sin(a)
    # u = x cos + z sin, v = x sin - z cos   (matches coordSystem xAxis=(cos,0,sin), zAxis=+Y)
    u = pts[:, 0] * c + pts[:, 1] * s
    v = pts[:, 0] * s - pts[:, 1] * c
    return float(u.max() - u.min()), float(v.max() - v.min())


def best_bed(pts: np.ndarray) -> dict:
    best = None
    at22 = rotate_box(pts, 22.0)
    boxes = {}
    for ang in np.linspace(0.0, 180.0, 1801):
        L, W = rotate_box(pts, float(ang))
        m = max(L, W)
        if best is None or m < best["m"]:
            best = {"m": m, "ang": float(ang), "L": L, "W": W}
        if abs(ang - 22.0) < 0.051:
            boxes["22"] = (L, W)
    assert best is not None
    L22, W22 = boxes.get("22", at22)
    return {
        "at22": (max(L22, W22), min(L22, W22), L22, W22),
        "best": best,
        "axis": rotate_box(pts, 0.0),
    }


def neck_width() -> float:
    # max outer lateral extent over the neck, on the shell (not the tabs)
    w = 0.0
    for s in np.linspace(NECK0, NECK1, 25):
        a, _r, _h = section_params(float(s))
        w = max(w, 2.0 * a)
    return w


# ---------------------------------------------------------------- report
def build_report() -> dict:
    vols = feature_volumes()
    clr = min_tyre_and_side()
    bedA = best_bed(silhouette("A"))
    bedB = best_bed(silhouette("B"))
    s_tdc = (math.pi / 2.0 - PHI0) * R
    phi_tail = math.degrees(PHI0 + S_B1 / R)
    to_tdc = 90.0 - phi_tail
    d55 = counterfactual_d55()
    # slot alignment: both layers are cut at the same s in the assembled model
    align = 0.0
    volA = vols["volA"] / 1000.0
    volB = vols["volB"] / 1000.0
    L22, W22 = bedB["at22"][2], bedB["at22"][3]
    text = (
        f"REAR-R2 parts=2"
        f" volA={volA:.2f}"
        f" volB={volB:.2f}"
        f" minTyreGap={clr['tyre']['gap']:.2f}"
        f" neckW={neck_width():.2f}"
        f" bedA={bedA['best']['L']:.1f}x{bedA['best']['W']:.1f}@{bedA['best']['ang']:.1f}"
        f" bedB22={max(L22, W22):.1f}x{min(L22, W22):.1f}@22"
        f" bedBbest={bedB['best']['L']:.1f}x{bedB['best']['W']:.1f}@{bedB['best']['ang']:.1f}"
        f" slotAlignErr={align:.3f}"
        f" tailS={S_B1:.0f}"
        f" coverB={S_B1 - S_B0:.0f}"
        f" degToTDC={to_tdc:.2f}"
        f" sTDC={s_tdc:.1f}"
        f" outerR={R:.1f}"
    )
    return {
        "text": text,
        "vols": vols,
        "clr": clr,
        "bedA": bedA,
        "bedB": bedB,
        "sTdc": s_tdc,
        "phiTail": phi_tail,
        "toTdc": to_tdc,
        "d55": d55,
        "volA": volA,
        "volB": volB,
        "align": align,
    }


# ---------------------------------------------------------------- renders
def _save(fig, path: str):
    fig.tight_layout()
    fig.savefig(path, dpi=140)
    plt.close(fig)
    print(path)


def render_side(path: str):
    fig, ax = plt.subplots(figsize=(11.2, 7.2))
    th = np.linspace(math.radians(5), math.radians(100), 400)
    ax.plot(TYRE_R * np.cos(th), TYRE_R * np.sin(th), color="#9aa0a6", lw=1.0, label="tyre crown r350")
    for part, color, s0, s1 in (("A", "#c47b2b", S_A0, S_A1), ("B", "#2f6f6a", 20.0, S_B1)):
        ss = np.linspace(s0, s1, 200)
        outer = np.array([world(float(s), 0.0, 0.0) for s in ss])
        inner = np.array([world(float(s), 0.0, section_params(float(s))[2]) for s in ss])
        ax.fill(
            np.concatenate([outer[:, 0], inner[::-1, 0]]),
            np.concatenate([outer[:, 2], inner[::-1, 2]]),
            color=color,
            alpha=0.85,
            label=f"part {part}",
        )
    # tongue, crown only
    ss = np.linspace(LAP0, LAP1, 40)
    t_out = np.array([world(float(s), 0.0, LAP_WALL + LAP_CLEAR) for s in ss])
    t_in = np.array([world(float(s), 0.0, LAP_WALL + LAP_CLEAR + LAP_WALL) for s in ss])
    ax.plot(t_out[:, 0], t_out[:, 2], color="#143c3a", lw=1.0)
    ax.plot(t_in[:, 0], t_in[:, 2], color="#143c3a", lw=1.0)
    # tabs
    for s in (-TAB_S, TAB_S):
        dd = np.linspace(TAB_D0, TAB_D1, 40)
        arc = np.array([rn(phi_of(s)) * (R_BRACE - d) for d in dd])
        ax.plot(arc[:, 0], arc[:, 2], color="#1d4e4a", lw=1.2)
    d0 = np.array([rn(phi_of(s)) * (R_BRACE - TAB_D0) for s in np.linspace(-TAB_S, TAB_S, 20)])
    d1 = np.array([rn(phi_of(s)) * (R_BRACE - TAB_D1) for s in np.linspace(-TAB_S, TAB_S, 20)])
    ax.fill(
        np.concatenate([d0[:, 0], d1[::-1, 0]]),
        np.concatenate([d0[:, 2], d1[::-1, 2]]),
        color="#1d4e4a",
        alpha=0.35,
        label="stay tabs",
    )
    # brace ray
    b = rn(PHI0) * R
    ax.plot([0, b[0]], [0, b[2]], color="#a33", lw=0.8, ls="--")
    ax.scatter([b[0]], [b[2]], color="#a33", s=18, zorder=5)
    ax.annotate("brace s=0", (b[0], b[2]), textcoords="offset points", xytext=(8, -12), color="#a33", fontsize=8)
    ax.scatter([0], [0], color="k", s=12)
    ax.set_aspect("equal")
    ax.set_xlabel("X forward (mm)")
    ax.set_ylabel("Z up (mm)")
    ax.set_title("Rear mudguard r1 — side")
    ax.legend(loc="lower left", fontsize=8)
    ax.grid(True, lw=0.3, alpha=0.5)
    _save(fig, path)


def render_top(path: str):
    fig, ax = plt.subplots(figsize=(11.2, 5.6))
    ss = np.linspace(S_A0, S_B1, 400)
    left, right, cx, cz = [], [], [], []
    for s in ss:
        a, _r, _h = section_params(float(s))
        p = world(float(s), a, 0.0)
        n = world(float(s), -a, 0.0)
        c = world(float(s), 0.0, 0.0)
        right.append(p)
        left.append(n)
        cx.append(c[0])
        cz.append(c[2])
    right = np.array(right)
    left = np.array(left)
    # top view is X (forward) vs Y (lateral). Use the outer top edge.
    ax.fill(
        np.concatenate([right[:, 0], left[::-1, 0]]),
        np.concatenate([right[:, 1], left[::-1, 1]]),
        color="#d7c4a3",
        ec="#5c4a32",
        lw=0.6,
        label="outer top width",
    )
    # lap band
    for s, name in ((LAP0, "shoulder s=-20"), (LAP1, "A rear / B shell s=+20")):
        p = world(s, 0, 0)
        q = world(s, NECK_A, 0)
        ax.plot([world(s, -WIDE_A, 0)[0], world(s, WIDE_A, 0)[0]], [-WIDE_A, WIDE_A], color="#888", lw=0.0)
        ax.plot([p[0], p[0]], [-NECK_A, NECK_A], color="#a33", lw=0.8)
    # rails and slots
    for y in (RAIL_Y, -RAIL_Y):
        a = world(LAP0, y, 0)
        b = world(LAP1, y, 0)
        ax.plot([a[0], b[0]], [a[1], b[1]], color="#143c3a", lw=2.0, solid_capstyle="butt")
    for s in (SLOT_S, -SLOT_S):
        for y in (SLOT_Y, -SLOT_Y):
            c = world(s, y, 0)
            ax.scatter([c[0]], [c[1]], marker="s", s=18, color="#1a1a1a", zorder=4)
    for s in TRIM_S:
        c = world(s, 0, 0)
        ax.plot([c[0], c[0]], [-20, 20], color="#c47b2b", lw=0.7)
    ax.set_aspect("equal")
    ax.set_xlabel("X forward (mm)")
    ax.set_ylabel("Y lateral (mm)")
    ax.set_title("Rear mudguard r1 — top (outer width, rails, brace slots, trim ticks)")
    ax.grid(True, lw=0.3, alpha=0.5)
    _save(fig, path)


def _section_world(s: float, pts: list[tuple[float, float]]) -> np.ndarray:
    out = []
    for x, depth in pts:
        out.append(world(s, x, depth))
    return np.array(out)


def render_rear(path: str):
    fig, axes = plt.subplots(1, 2, figsize=(11.2, 6.4))
    circ = np.linspace(0, 2 * math.pi, 180)
    tyre_y = TYRE_RAD * np.sin(circ)
    tyre_r = TYRE_C + TYRE_RAD * np.cos(circ)
    for ax, s, title in (
        (axes[0], 0.0, "neck at the brace, s=0"),
        (axes[1], S_B1, f"tail, from behind, s={S_B1:+.0f}"),
    ):
        a, rad, h = section_params(s)
        wall = LAP_WALL if LAP0 <= s <= LAP1 else WALL
        outer = np.array(u_outer(a, rad, h))
        inner = np.array(u_inset(a, rad, h, wall))
        ax.fill(outer[:, 0], R - outer[:, 1], color="#d7c4a3", ec="#5c4a32", lw=0.8)
        ax.fill(inner[:, 0], R - inner[:, 1], color="#f4f1ea", ec="#5c4a32", lw=0.4)
        ax.plot(tyre_y, tyre_r, color="#6b7280", lw=1.0, label="DHR II 63.5")
        if s == 0.0:
            for d in (0.0, 50.0):
                y = stay_gap(d) / 2.0
                z = R_BRACE - d
                ax.plot([y, y], [z - 6, z + 6], color="#a33", lw=1.0)
                ax.plot([-y, -y], [z - 6, z + 6], color="#a33", lw=1.0)
        ax.set_aspect("equal")
        ax.set_xlabel("Y lateral (mm)")
        ax.set_ylabel("radius from the axle (mm)")
        ax.set_title(title)
        ax.grid(True, lw=0.3, alpha=0.4)
        ax.legend(fontsize=7, loc="lower right")
        ax.set_xlim(-70, 70)
        ax.set_ylim(300, 410)
    fig.suptitle("Rear mudguard r1 — sections, radial outward up")
    _save(fig, path)


def render_joint(path: str):
    fig, axes = plt.subplots(1, 2, figsize=(11.2, 5.2))
    ax = axes[0]
    # crown section: radius vs s. Draw A plate, tongue, shoulder, slots.
    ss = np.linspace(-40, 40, 400)
    a_out, a_in, b_out, b_in = [], [], [], []
    for s in ss:
        if S_A0 <= s <= S_A1:
            w = wall_A(float(s))
            a_out.append((s, R))
            a_in.append((s, R - w))
        if LAP0 <= s <= LAP1:
            b_out.append((s, R - (LAP_WALL + LAP_CLEAR)))
            b_in.append((s, R - (LAP_WALL + LAP_CLEAR + LAP_WALL)))
        elif 20.0 <= s <= 40:
            b_out.append((s, R))
            b_in.append((s, R - WALL))
    def band(ax, outer, inner, color, label):
        o = np.array(outer)
        i = np.array(inner)
        ax.fill(np.concatenate([o[:, 0], i[::-1, 0]]), np.concatenate([o[:, 1], i[::-1, 1]]), color=color, alpha=0.9, label=label)
    band(ax, a_out, a_in, "#c47b2b", "A outer plate")
    band(ax, b_out, b_in, "#2f6f6a", "B tongue / shell")
    # shoulder
    ax.fill([-22.5, -20, -20, -22.5], [R - WALL, R - WALL, R - 12, R - 12], color="#8d5a1c", label="A shoulder")
    for s in (SLOT_S, -SLOT_S):
        ax.plot([s, s], [R - 8, R + 0.4], color="#1a1a1a", lw=3.0, solid_capstyle="butt")
    ax.annotate("slots s=±14", (SLOT_S, R + 0.6), fontsize=8, ha="center")
    ax.set_xlim(-40, 40)
    ax.set_ylim(R - 14, R + 2)
    ax.set_xlabel("s along the guard (mm)")
    ax.set_ylabel("radius (mm)")
    ax.set_title("Joint, crown section")
    ax.legend(fontsize=7, loc="lower left")
    ax.grid(True, lw=0.3, alpha=0.4)

    ax = axes[1]
    # transverse section through the lap at s=0, showing rails and grooves
    a, rad, h = section_params(0.0)
    outer = u_outer(a, rad, h)
    inner = u_inset(a, rad, h, LAP_WALL)
    tongue_o = u_inset(a, rad, h, LAP_WALL + LAP_CLEAR)
    tongue_i = u_inset(a, rad, h, LAP_WALL + LAP_CLEAR + LAP_WALL)
    def poly(pts, **kw):
        arr = np.array(pts)
        ax.fill(arr[:, 0], -arr[:, 1], **kw)  # depth downward on the page
    poly(outer, color="#c47b2b", ec="#5c4a32", lw=0.6, label="A")
    poly(inner, color="#f7f1e6", ec="#5c4a32", lw=0.3)
    poly(tongue_o, color="#2f6f6a", ec="#143c3a", lw=0.5, label="B tongue")
    poly(tongue_i, color="#f4f1ea", ec="#143c3a", lw=0.3)
    for y in (RAIL_Y, -RAIL_Y):
        # rail sits on the tongue outer top, proud toward A (up on this inverted plot)
        top = LAP_WALL + LAP_CLEAR
        ax.add_patch(
            Polygon(
                [(y - RAIL_W / 2, -(top)), (y + RAIL_W / 2, -(top)), (y + RAIL_W / 2, -(top - RAIL_H)), (y - RAIL_W / 2, -(top - RAIL_H))],
                closed=True,
                color="#143c3a",
            )
        )
        g0 = LAP_WALL
        ax.add_patch(
            Polygon(
                [
                    (y - GROOVE_W / 2, -(g0)),
                    (y + GROOVE_W / 2, -(g0)),
                    (y + GROOVE_W / 2, -(g0 - GROOVE_D)),
                    (y - GROOVE_W / 2, -(g0 - GROOVE_D)),
                ],
                closed=True,
                facecolor="#f4f1ea",
                edgecolor="#a33",
                lw=0.8,
            )
        )
    ax.set_aspect("equal")
    ax.set_xlim(-34, 34)
    ax.set_ylim(-20, 4)
    ax.set_xlabel("Y lateral (mm)")
    ax.set_ylabel("depth inward, drawn downward (mm)")
    ax.set_title("Lap section at s=0, rails in grooves")
    ax.legend(fontsize=7, loc="lower right")
    ax.grid(True, lw=0.3, alpha=0.4)
    fig.suptitle("Rear mudguard r1 — joint close-up")
    _save(fig, path)


def render_bed(path: str, info: dict):
    pts = silhouette("B")
    ang = info["bedB"]["best"]["ang"]
    a = math.radians(ang)
    c, s = math.cos(a), math.sin(a)
    u = pts[:, 0] * c + pts[:, 1] * s
    v = pts[:, 0] * s - pts[:, 1] * c
    # shift onto the sheet with a 2.5 mm margin from the min corner
    L, W = rotate_box(pts, ang)
    u = u - u.min() + (250.0 - L) / 2.0
    v = v - v.min() + (250.0 - W) / 2.0
    fig, ax = plt.subplots(figsize=(8.2, 8.2))
    ax.add_patch(Polygon([(0, 0), (256, 0), (256, 256), (0, 256)], closed=True, fill=False, ls="--", ec="#888", lw=1.0, label="P1S 256"))
    ax.add_patch(Polygon([(0, 0), (250, 0), (250, 250), (0, 250)], closed=True, fill=False, ec="#222", lw=1.2, label="250 usable"))
    hull = _hull(np.column_stack([u, v]))
    ax.add_patch(Polygon(hull, closed=True, facecolor="#2f6f6a", edgecolor="#143c3a", alpha=0.85, lw=0.8, label=f"B at {ang:.1f} deg"))
    ax.set_aspect("equal")
    ax.set_xlim(-8, 270)
    ax.set_ylim(-8, 270)
    ax.set_xlabel("bed X (mm)")
    ax.set_ylabel("bed Y (mm)")
    ax.set_title(f"B on its side, chord on the diagonal, polygon bbox {L:.1f} x {W:.1f} mm at {ang:.1f} deg")
    ax.legend(loc="upper right", fontsize=8)
    ax.grid(True, lw=0.3, alpha=0.4)
    _save(fig, path)


def _hull(pts: np.ndarray) -> np.ndarray:
    pts = np.unique(np.round(pts, 5), axis=0)
    pts = pts[np.lexsort((pts[:, 1], pts[:, 0]))]

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower = []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(tuple(p))
    upper = []
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(tuple(p))
    return np.array(lower[:-1] + upper[:-1])


def write_verification(path: str, info: dict):
    v = info["vols"]["mm3"]
    bA = info["bedA"]
    bB = info["bedB"]
    L22, W22 = bB["at22"][2], bB["at22"][3]
    lines = [
        "Rear mudguard r2 verification (offline, no Onshape)",
        "source: featurescript/rear/rear_mudguard_r1.fs",
        "model: featurescript/rear/rear_r1_model.py",
        "bike: Amflow PR Carbon, 27.5 rear, Maxxis DHR II 27.5x2.5",
        "layout: Split & Join Planner 1c, brace gap measured at 30 mm",
        "",
        "COORDINATES",
        "  origin rear axle, +X forward, +Z up, +Y rider's left",
        "  phi from +X toward +Z, 90 deg at TDC",
        f"  phi(brace) = 33.6 deg, s = 0 there, +s toward TDC, R_top = {R:.1f}",
        f"  TDC is at s = {info['sTdc']:.1f}. Tail is s {S_B1:.0f}, phi {info['phiTail']:.2f} deg,",
        f"  {info['toTdc']:.2f} deg before TDC.",
        f"  B covers {S_B1 - S_B0:.0f} mm of arc, from s {S_B0:.0f} to s {S_B1:.0f}.",
        "",
        "PARTS",
        "  A  s -135 .. +20   outer lap plate, brace slots, front skirt, trim guides",
        f"  B  s -20 .. {S_B1:.0f}   inner tongue, brace slots, stay tabs, squared tail",
        "  lap s -20 .. +20, B inside A, tongue tip butts the shoulder at s -20",
        "  6 ties: 2 brace (the s=±14 pair at each y) + 4 stay (one per tab slot)",
        "",
        "RADIUS  braceGap drives the solid",
        f"  measured brace underside to tyre tread = {BRACE_GAP:.0f} mm.",
        f"  brace underside R = {TYRE_R:.0f} + {BRACE_GAP:.0f} = {R_BRACE:.1f}.",
        f"  outer lap plate = {R_BRACE:.1f} - pad {PAD:.1f} = {R:.1f}. The pad bears on the brace.",
        f"  lap plate inner = {R - LAP_WALL:.1f}. Tongue outer = {R - LAP_WALL - LAP_CLEAR:.1f}.",
        f"  tongue inner = {R - (LAP_WALL + LAP_CLEAR + LAP_WALL):.1f}. Main shell inner = {R - WALL:.1f}.",
        f"  The tie recess is {RECESS_D:.1f} mm outboard of the tongue inner face, so a flush",
        f"  tie strand sits at R {R - (LAP_WALL + LAP_CLEAR + LAP_WALL):.1f},",
        f"  {R - (LAP_WALL + LAP_CLEAR + LAP_WALL) - TYRE_R:.1f} mm off the tyre crown.",
        "",
        "SECTION",
        f"  rounded U, vertical walls, radial depth {SECTION_H:.0f} mm.",
        "  A 22 mm wall at this radius puts the neck inside the 15 mm tyre gap.",
        "  17 mm keeps the tongue inner wall, the closest point, above 15 mm.",
        "  Neck outer width 52.0 from s -20 to +40 (corner R4).",
        "  Skirt and tail outer width 94 (corner R12).",
        "  Wall 2.5, lap and tabs 3.0. Lap radial clearance 0.2. Rails 3.0 x 1.2 at y=±20",
        "  into grooves 3.6 x 1.5, open at s=+20, 4 mm lead-in chamfer.",
        "",
        "CLEARANCE",
        f"  min tyre gap {info['clr']['tyre']['gap']:.2f} mm at {info['clr']['tyre']['where']}",
        f"  min side clearance {info['clr']['side']:.2f} mm at {info['clr']['sideAt']}",
        f"  a 3 mm tab at d=55 is {info['d55']:.2f} mm off the tyre.",
        f"  Lower slot d={TAB_SLOT_D[1]:.0f} (spans {TAB_SLOT_D[1]-3:.0f} to {TAB_SLOT_D[1]+3:.0f}), tab ends at d={TAB_D1:.1f}.",
        "",
        "VOLUME cm3  (section integral + features, mm3 below)",
        f"  volA {info['volA']:.2f}    volB {info['volB']:.2f}",
        f"  test window +/-8%: A {info['volA']*0.92:.2f}..{info['volA']*1.08:.2f}"
        f"   B {info['volB']*0.92:.2f}..{info['volB']*1.08:.2f}",
    ]
    for k, val in v.items():
        lines.append(f"    {k:14s} {val/1000.0:8.2f}")
    lines += [
        "",
        "BED  polygon bbox of the side silhouette, rotation about Y",
        f"  A best {bA['best']['L']:.1f} x {bA['best']['W']:.1f} at {bA['best']['ang']:.1f} deg"
        f"   (axis-aligned {bA['axis'][0]:.1f} x {bA['axis'][1]:.1f})",
        f"  B at a 22 deg world rotation {max(L22,W22):.1f} x {min(L22,W22):.1f}",
        f"  B best {bB['best']['L']:.1f} x {bB['best']['W']:.1f} at {bB['best']['ang']:.1f} deg",
        "  Usable bed 250 mm. The tail stays at s +330 because the smaller radius",
        "  and the 17 mm depth both shrink the polygon.",
        "",
        "SLOT ALIGNMENT",
        "  tongue tip and shoulder are both at s=-20, slots of both layers are cut at s=±14.",
        f"  alignment error {info['align']:.3f} mm.",
        "",
        "RESULT",
        "  " + info["text"],
        "",
        "MEASURE ON THE BIKE before printing",
        "  1. Tyre crown radius. The brace gap is now the measured 30 mm; the radius still assumes a 350 mm tyre.",
        "  2. Brace centreline angle from the forward horizontal (assumed 33.6 deg) and where TDC sits in s.",
        "  3. Seatstay inner gap at the brace, at 50 mm down and at 100 mm down (assumed 60 / 90 / 100).",
        "  4. Whether the stay below the brace points at the axle. If it stays higher, d=55 may become usable.",
        "  5. Chain-stay and motor clearance ahead of the brace (the skirt opens to 94 mm by s=-55).",
        "  6. Brace width along the arc (contact zone is modelled as s -10..+10, ties at ±14).",
        "",
    ]
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    print(path)


def main():
    info = build_report()
    print(info["text"])
    print(f"tyre {info['clr']['tyre']}")
    print(f"side {info['clr']['side']:.2f} at {info['clr']['sideAt']}")
    print(f"sTDC {info['sTdc']:.2f}  d55 tyre {info['d55']:.2f}")
    for k, val in info["vols"]["mm3"].items():
        print(f"  {k:14s} {val/1000.0:8.2f}")
    out = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "out", "rear"))
    os.makedirs(out, exist_ok=True)
    render_side(os.path.join(out, "side.png"))
    render_top(os.path.join(out, "top.png"))
    render_rear(os.path.join(out, "rear.png"))
    render_joint(os.path.join(out, "joint.png"))
    render_bed(os.path.join(out, "bed_B.png"), info)
    ver = os.path.abspath(os.path.join(os.path.dirname(__file__), "verification_r1.txt"))
    write_verification(ver, info)


if __name__ == "__main__":
    main()
