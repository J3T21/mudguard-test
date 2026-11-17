"""Offline geometry for mudguard v2.17.

No Onshape. Numbers match featurescript/mudguard_v2.17.fs:
solid leg sheets (no seal window), only the lowest wing-tie pair,
a collar blister at 58 mm effective OD, and a tyre-side junction rib.
"""
from __future__ import annotations

import math
import os

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

# ---- fork / guard constants (v2.16 defaults + owner's collar) ----
PH1, PAD = 395.0, 1.5
R = PH1 - PAD  # 393.5
PH2, PH6 = 100.0, 45.0
Y_LEG = PH2 / 2 + PH6 / 2  # 72.5
LEG_ZONE_W, TOP_FLAT = 97.0, 76.0
TAN19 = math.tan(math.radians(19))
D_EDGE = (LEG_ZONE_W - TOP_FLAT) / (2 * TAN19)  # wall-edge depth below guard top
A_WALL = LEG_ZONE_W / 2 - D_EDGE * TAN19  # ~38
YO, WT = LEG_ZONE_W / 2, 2.5  # sheet outer |y|, thickness
YI = YO - WT
HW = 28.0  # sheet half-width (wingWidth 56)
LO = 45.0  # leg tie s
COLLAR_OD, CLEAR = 55.0, 1.5
COLLAR_R = COLLAR_OD / 2
Y_FACE = Y_LEG - COLLAR_R - CLEAR  # 43.5; 58 mm effective OD
COLLAR_TOP, COLLAR_H = 0.0, 20.0
FLAT_LEN, TAPER = 22.0, 51.0
RIB_PROUD, RIB_DOWN, RIB_OV = 2.0, 3.0, 1.0
STANCHION_R = 18.0  # Fox 36
LOWER_R = PH6 / 2  # 22.5 at and below 90 mm
V216_A = 62.5
V216_B = 112.131  # 114212.6 - 2082 mm^3, from the v2.16 console


def d_cross() -> float:
    """D where the 19 deg leg-zone wall reaches the clearance face."""
    return PAD + (Y_FACE - A_WALL) / TAN19


D0 = max(COLLAR_TOP, d_cross())
D1 = D0 + FLAT_LEN
D2 = D1 + TAPER
D_EDGE_BRACE = D_EDGE + PAD  # ~32 mm below the brace underside


def wall_outer(D: float) -> float:
    """Straight 19 deg wall. Conservative (actual corner is more inboard) for h>4 mm."""
    h = D - PAD
    return A_WALL + max(h, 0.0) * TAN19


def leg_r_design(D: float) -> float:
    """Owner's collar, then a straight taper to the measured OD 45 at 90 mm."""
    if D <= COLLAR_TOP + COLLAR_H:
        return COLLAR_R
    if D >= 90.0:
        return LOWER_R
    u = (D - (COLLAR_TOP + COLLAR_H)) / (90.0 - (COLLAR_TOP + COLLAR_H))
    return COLLAR_R + (LOWER_R - COLLAR_R) * u


def leg_r_repo(D: float) -> float:
    """Older tape model: OD 60 to 40 mm, OD 45 at 90 mm. Superseded for the collar."""
    if D <= 40.0:
        return 30.0
    if D >= 90.0:
        return LOWER_R
    return 30.0 - 7.5 * (D - 40.0) / 50.0


def y_outer(D: float) -> float:
    """Leg-facing |y| of the wing/wall skin after the blister."""
    if D < D0:
        return min(wall_outer(D), YO)
    if D <= D1:
        return Y_FACE
    if D <= D2:
        u = (D - D1) / (D2 - D1)
        return Y_FACE + (YO - Y_FACE) * u
    if D <= D_EDGE_BRACE:
        return min(wall_outer(D), YO)
    return YO


def y_allow(D: float, r_of) -> float:
    return Y_LEG - r_of(D) - CLEAR


def clearance_at(D: float, dx: float, r_of) -> float:
    """Radial gap from a point on the flat face y=y_outer(D) to a leg of radius r(D)."""
    y = y_outer(D)
    return math.hypot(dx, Y_LEG - y) - r_of(D)


def sheet_exists(D: float) -> bool:
    return D_EDGE_BRACE - 6 <= D <= 135.0


def main() -> None:
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    out_dir = os.path.join(root, "out")
    os.makedirs(out_dir, exist_ok=True)

    # centreline margin against the design leg and the old tape leg
    Ds = np.linspace(0.5, 130.0, 800)
    rows = []
    min_des, min_des_at = 1e9, 0.0
    min_repo, min_repo_at = 1e9, 0.0
    for D in Ds:
        cd = clearance_at(float(D), 0.0, leg_r_design)
        cr = clearance_at(float(D), 0.0, leg_r_repo)
        rows.append((float(D), y_outer(float(D)), cd, cr))
        if cd < min_des:
            min_des, min_des_at = cd, float(D)
        if cr < min_repo:
            min_repo, min_repo_at = cr, float(D)

    # off-axis: the flat face is closest at dx=0, but check the sheet corners anyway
    min_corner = 1e9
    corner_at = (0.0, 0.0)
    for D in np.linspace(D0, D2, 80):
        for dx in np.linspace(-HW, HW, 15):
            c = clearance_at(float(D), float(dx), leg_r_design)
            if c < min_corner:
                min_corner, corner_at = c, (float(D), float(dx))

    # stanchion (r=18) anywhere on our skin
    min_st = min(clearance_at(float(D), 0.0, lambda _D: STANCHION_R) for D in Ds)

    # tyre clearance of the rib and of the blister inner face
    ang = LO / R
    x_c = -R * math.sin(ang)
    z_top = R * math.cos(ang)
    z_b = z_top + PAD

    def torus_clear(y, D, tube_r):
        z = z_b - D
        rho = math.hypot(x_c, z)
        zc = 375.0 - tube_r
        return math.hypot(rho - zc, y) - tube_r

    rib_inner_y = (Y_FACE - WT) - RIB_PROUD  # 39
    # sample the rib box and the blister inner edge
    min_tyre_max = 1e9
    min_tyre_asg = 1e9
    tyre_pts = []
    for D in (D_EDGE_BRACE, D_EDGE_BRACE + RIB_DOWN, (D0 + D1) / 2, D1, 100, 125, 135):
        for y in (y_outer(D), y_outer(D) - WT, rib_inner_y):
            if D > D_EDGE_BRACE + RIB_DOWN + 1 and abs(y - rib_inner_y) < 0.1:
                continue  # rib only at the junction
            for tube, bucket in ((34.6, "max"), (31.75, "asg")):
                c = torus_clear(y, D, tube)
                tyre_pts.append((D, y, bucket, c))
                if bucket == "max":
                    min_tyre_max = min(min_tyre_max, c)
                else:
                    min_tyre_asg = min(min_tyre_asg, c)
    # also the original lower sheet, which is the historical closest region
    for D in np.linspace(95, 135, 20):
        for y in (YO, YI):
            min_tyre_max = min(min_tyre_max, torus_clear(y, float(D), 34.6))
            min_tyre_asg = min(min_tyre_asg, torus_clear(y, float(D), 31.75))

    # volume delta vs v2.16, voxelised over the wing region only
    dv = voxel_delta(x_c, z_b)
    vol_b = V216_B + dv
    # tie hole kept at 125; upper rows 90 and 110 are filled
    slot_mm3 = restored_slot_volume()

    text = build_report(
        min_des, min_des_at, min_repo, min_repo_at, min_corner, corner_at,
        min_st, min_tyre_max, min_tyre_asg, dv, vol_b, slot_mm3, rows,
    )
    ver_path = os.path.join(os.path.dirname(__file__), "verification_v2.17.txt")
    with open(ver_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print(text)
    render(out_dir, x_c, z_b)
    print("wrote", ver_path)
    print("D0,D1,D2", round(D0, 3), round(D1, 3), round(D2, 3), "yFace", Y_FACE)


def restored_slot_volume() -> float:
    """Material returned by not cutting the 90 mm and 110 mm rows (both sides)."""
    # slot through the 2.5 mm sheet: 2.4 x 6 x 2.5
    slot = 2.4 * 6.0 * WT
    # channel 0.8 mm into the inner face, 22 x 5.5, minus the part shared with the slots
    # each slot overlaps the channel in x by 1.2 mm and in z by 5.5 mm
    channel = 22.0 * 5.5 * 0.8 - 2 * (1.2 * 5.5 * 0.8)
    return 2 * 2 * (slot + channel)  # 2 rows x 2 sides


def voxel_delta(x_c: float, z_b: float) -> float:
    """cm^3 added to part B relative to v2.16, in the wing neighbourhood."""
    pitch = 0.5
    # bounds cover both wings and the collar/taper
    xs = np.arange(x_c - HW - 1, x_c + HW + 1, pitch)
    ys = np.arange(30.0, 56.0, pitch)
    Ds = np.arange(0.0, 140.0, pitch)
    # index helpers
    def inside_window(dx, y, D):
        return abs(dx) <= 20.0 and 36.0 <= y <= 80.0 and 12.0 <= D <= 84.0

    def slot_row(dx, y, D, row):
        # two slots at dx = +/-11, 2.4 wide, D +/-3, through the sheet
        for c in (-11.0, 11.0):
            if abs(dx - c) <= 1.2 and abs(D - row) <= 3.0 and YI - 0.2 <= y <= YO + 0.2:
                return True
        # channel on the inner 0.8 mm, |dx|<=11, D +/-2.75
        if abs(dx) <= 11.0 and abs(D - row) <= 2.75 and YI - 0.2 <= y <= YI + 0.8:
            return True
        return False

    def base_skin(dx, y, D):
        if abs(dx) > HW:
            return False
        # sheet
        if sheet_exists(D) and YI - 0.05 <= y <= YO + 0.05:
            return True
        # wall skin, straight-wall approximation, D up to the wall edge
        if 1.0 <= D <= D_EDGE_BRACE:
            outer = min(wall_outer(D), YO)
            inner = outer - 3.0 / math.cos(math.radians(19))
            if inner <= y <= outer:
                return True
        return False

    def rib_here(dx, y, D):
        y0 = (Y_FACE - WT) - RIB_PROUD
        y1 = (Y_FACE - WT) + RIB_OV
        return abs(dx) <= HW - 0.5 and y0 <= y <= y1 and D_EDGE_BRACE <= D <= D_EDGE_BRACE + RIB_DOWN

    n_add = 0
    n_sub = 0
    for D in Ds:
        yo_d = y_outer(float(D))
        in_blister_z = D0 <= D <= D2
        for x in xs:
            dx = x - x_c
            for y in ys:
                before = base_skin(dx, y, D)
                if before and inside_window(dx, y, D):
                    before = False
                if before and (slot_row(dx, y, D, 90) or slot_row(dx, y, D, 110) or slot_row(dx, y, D, 125)):
                    before = False
                after = base_skin(dx, y, D)
                if after and (slot_row(dx, y, D, 125)):
                    after = False
                # blister replaces skin inside the sheet width over D0..D2
                if abs(dx) <= HW and in_blister_z:
                    # remove anything outboard of the new face
                    if y > yo_d + 0.05:
                        after = False
                    # ensure a 2.5 mm skin
                    if yo_d - WT - 0.05 <= y <= yo_d + 0.05:
                        after = True
                if rib_here(dx, y, D):
                    after = True
                if after and not before:
                    n_add += 1
                elif before and not after:
                    n_sub += 1
    cell = pitch ** 3 / 1000.0  # cm^3
    # both sides are mirrors; voxel loop was +Y only
    return 2 * (n_add - n_sub) * cell


def build_report(min_des, min_des_at, min_repo, min_repo_at, min_corner, corner_at,
                 min_st, min_tyre_max, min_tyre_asg, dv, vol_b, slot_mm3, rows) -> str:
    # a few sample rows for the table
    want = [5, 15, 17.5, 20, 28, 32, 40, 50, 70, 90, 110, 125]
    lines_tbl = []
    for D, y, cd, cr in rows:
        if any(abs(D - w) < 0.15 for w in want):
            lines_tbl.append(
                f"  D={D:6.1f}  |y|={y:5.2f}  design {cd:+6.2f} mm   repo-tape {cr:+6.2f} mm"
            )
    bed_b = "241.3 x 238.3 (unchanged: blister and rib stay inside |y|<=48.5; XZ box is the v2.16 box)"
    return f"""v2.17 offline verification (no Onshape)
source: featurescript/mudguard_v2.17.fs from v2.16 (be36587)
datum: brace underside. leg centre |y|={Y_LEG:.1f}. sheet plane |y|={YO:.1f} thickness {WT:.1f}.

COLLAR (owner, this round)
  OD {COLLAR_OD:.0f} mm, height {COLLAR_H:.0f} mm, top at the brace underside (D={COLLAR_TOP:.0f}).
  Brace blends into the collar; the wing uses a cylinder and the plastic can flex the rest.
  Radial clearance {CLEAR:.1f} mm -> effective OD {COLLAR_OD + 2*CLEAR:.0f} mm.
  Leg-facing face |y|={Y_FACE:.2f}  (72.5 - 27.5 - 1.5).

BLISTER (enabled)
  The 19 deg leg-zone wall reaches |y|={Y_FACE:.2f} at D={D0:.2f} (already >= {CLEAR:.1f} mm clear above that).
  Flat wrap: D {D0:.2f} .. {D1:.2f} ({FLAT_LEN:.0f} mm) at |y|={Y_FACE:.2f}, full sheet width {2*HW:.0f} mm, wall {WT:.1f} mm.
  Top transition: the existing wall, tangent onto the flat (cut depth starts at 0, no feather).
  Bottom transition: linear ramp D {D1:.2f} .. {D2:.2f} ({TAPER:.0f} mm) back to |y|={YO:.1f}.
  {TAPER:.0f} mm is the length that stays {CLEAR:.1f} mm off a straight taper from OD {COLLAR_OD:.0f} at D=20 to OD 45 at D=90.
  No seal window. Nothing left under 1 mm: skin {WT:.1f}, overlaps 2 mm (consumed or buried), rib end radius 1.5.

RIB
  Tyre side of the wing/guard junction, not proud of the leg-facing face.
  A bead on the leg face would eat the {CLEAR:.1f} mm collar gap (face is already on the 58 mm cylinder).
  Proud {RIB_PROUD:.0f} mm toward the tyre, {RIB_DOWN:.0f} mm down the sheet, stadium ends R1.5 (modelled, not an edge fillet).
  Sits on the blister inner face |y|={Y_FACE - WT:.1f}, so the rib's inner face is |y|={Y_FACE - WT - RIB_PROUD:.1f}.

TIE HOLES
  Wing rows were 90 / 110 / 125 mm. Kept the lowest pair only: D=125 mm, pitch 22 mm, both sides (4 holes).
  Rows at 90 and 110 are solid. Brace ties stay at s = +/-12.5 on both parts.

CLEARANCE (centreline unless noted), design leg = collar then linear to OD 45 at 90 mm
{chr(10).join(lines_tbl)}
  minimum vs design leg: {min_des:.2f} mm at D={min_des_at:.1f}
  minimum vs design leg anywhere on the flat face |dx|<={HW:.0f}: {min_corner:.2f} mm at D={corner_at[0]:.1f} dx={corner_at[1]:.1f}
  minimum vs stanchion OD 36: {min_st:.2f} mm
  tyre, sampled on blister inner face, rib and lower sheet: max-tyre {min_tyre_max:.2f} mm, Assegai {min_tyre_asg:.2f} mm
  lower sheet at D>=90 vs OD 45: {(Y_LEG - YO) - LOWER_R:.2f} mm (the original 1.5 mm flush gap)

REPO TAPE (OD 60 at 40 mm, OD 45 at 90 mm) — NOT what this blister is built to
  The owner's collar measurement (OD 55 at the top) replaces that 60 mm station for the seal.
  Against the old tape solid the skin still intersects: minimum {min_repo:.2f} mm at D={min_repo_at:.1f}.
  If the leg really is OD 60 at 40 mm, the face would need to be |y|<={Y_LEG - 30 - CLEAR:.1f}, not {Y_FACE:.1f}.

VOLUME (offline estimate, cm^3)
  v2.16 measured A {V216_A:.1f} / B {V216_B:.3f}
  A is unchanged (blister, rib and tie-hole edits are on B, aft of the stepped joint).
  B delta {dv:+.2f} cm^3  ->  B about {vol_b:.1f}
  of which restoring the 90/110 slots+channels is {slot_mm3/1000:.2f} cm^3; the rest is the closed window minus the blister cut plus the shell and rib.
  Test band used in the feature: A 62.2..62.8, B {vol_b-6:.0f}..{vol_b+6:.0f}.

BED
  B on its side still {bed_b}. Limit 250 mm. A footprint is the v2.16 footprint (about 182 x 69).

HOLES / WINDOW
  wing zip holes 4 (was 12). brace tie slots 4. bolt holes 3.
  old window centre (D=50, on the taper) is solid skin, not an opening.

FILLETS
  R4 join fillets still run before the blister, as in v2.16. The blister cut then removes that fillet
  where it crosses the flat (the fillet sat at |y|~46-48.5, the cut takes everything outboard of {Y_FACE:.1f}).
  No new edge fillet. R1 sheet-end rounds are not retried. New ends are a 1.5 mm stadium radius on a 3 mm rib.
"""


def render(out_dir: str, x_c: float, z_b: float) -> None:
    guard = guard_mesh()
    wing = wing_mesh(x_c, z_b)
    collar = cylinder_mesh(x_c, Y_LEG, z_b, z_b - COLLAR_H, COLLAR_R, n=28)
    views = {
        "v2.17_top.png": dict(elev=88, azim=-90, title="v2.17 top"),
        "v2.17_front.png": dict(elev=8, azim=180, title="v2.17 front"),
        "v2.17_left.png": dict(elev=6, azim=90, title="v2.17 left"),
    }
    for name, kw in views.items():
        save_view(os.path.join(out_dir, name), guard, wing, None, **kw)
    save_view(
        os.path.join(out_dir, "v2.17_leg_wing.png"),
        guard, wing, collar,
        elev=16, azim=-58, title="v2.17 leg wing (collar in red)",
        xlim=(x_c - 40, x_c + 40), ylim=(20, 80), zlim=(z_b - 130, z_b + 8),
    )


def save_view(path, guard, wing, collar, elev, azim, title, xlim=None, ylim=None, zlim=None):
    fig = plt.figure(figsize=(11, 8), dpi=120)
    ax = fig.add_subplot(111, projection="3d")
    add_mesh(ax, guard, "#5d6b62")
    add_mesh(ax, wing, "#c4b49a")
    if collar is not None:
        add_mesh(ax, collar, "#a33b3b", alpha=0.35)
    ax.view_init(elev=elev, azim=azim)
    if xlim:
        ax.set_xlim(*xlim)
        ax.set_ylim(*ylim)
        ax.set_zlim(*zlim)
    else:
        ax.set_box_aspect((1.6, 0.7, 1.0))
    ax.set_title(title)
    ax.set_xlabel("X")
    ax.set_ylabel("Y")
    ax.set_zlabel("Z")
    fig.tight_layout()
    fig.savefig(path)
    plt.close(fig)
    print("render", path)


def add_mesh(ax, tris, color, alpha=1.0):
    poly = Poly3DCollection(tris, alpha=alpha, linewidths=0)
    poly.set_facecolor(color)
    # cheap shade from face normal
    v = np.asarray(tris)
    n = np.cross(v[:, 1] - v[:, 0], v[:, 2] - v[:, 0])
    nn = np.linalg.norm(n, axis=1)
    nn[nn == 0] = 1
    n = n / nn[:, None]
    light = np.array([0.3, 0.5, 0.8])
    light = light / np.linalg.norm(light)
    shade = np.clip(np.abs(n @ light), 0.35, 1.0)
    base = np.array(matplotlib.colors.to_rgb(color))
    poly.set_facecolor(base * shade[:, None])
    poly.set_alpha(alpha)
    ax.add_collection3d(poly)
    pts = v.reshape(-1, 3)
    ax.auto_scale_xyz(pts[:, 0], pts[:, 1], pts[:, 2], had_data=True)


def section_pts(width, depth, alpha_deg, radius):
    """Outer U, open along the bottom. Sketch x = lateral, y = depth."""
    al = math.radians(alpha_deg)
    ca, sa = math.cos(al), math.sin(al)
    a = width / 2 - depth * math.tan(al) if abs(ca) > 1e-9 else width / 2
    r = max(radius, 0.2)
    cx = a - r * (1 - sa) / ca
    wtx = cx + r * ca
    wth = r * (1 - sa)
    xe = width / 2

    def arc(side):
        # centre (side*cx, r). theta=-pi/2 is the top; the other end is the wall tangent.
        th_top = -math.pi / 2
        th_wall = (-math.pi + al) if side < 0 else -al
        cxs = side * cx
        pts = []
        for i in range(9):
            theta = th_wall + (th_top - th_wall) * (i / 8.0)
            pts.append((cxs + r * math.cos(theta), r + r * math.sin(theta)))
        return pts

    return [(-xe, depth), (-wtx, wth)] + arc(-1) + arc(1)[1:] + [(wtx, wth), (xe, depth)]


def station_list():
    """Sculpted stations at the v2.16 default lengths. (s, width, depth, alpha deg, radius)."""
    W, WL, Wf, Wt = 82.0, 97.0, 97.0, 97.0
    dC, rC, aR, rR = 30.49, 18.0, 5.0, 20.0
    raw = [
        [-240, Wf, 28, aR, rR], [-227.5, Wf, 30, aR, rR], [-215, Wf, 32, aR, rR],
        [-192.5, Wf, 33, aR, rR], [-170, Wf, 34, aR, rR], [-145, Wf, 34, aR, rR],
        [-120, Wf, 34, aR, rR], [-95, Wf, 34, aR, rR], [-82.5, Wf, 34, aR, rR],
        [-70, Wf, 34, aR, rR], [-55, Wf, 34, aR, rR], [-35, 90, 30, 8, 14],
        [-15, W, dC, 0, rC], [0, W, dC, 0, rC], [10, W, dC, 0, rC],
        [LO - 23, WL, D_EDGE, 19, 6], [LO - 11, WL, D_EDGE, 19, 6], [LO, WL, D_EDGE, 19, 6],
        [LO + 11, WL, D_EDGE, 19, 6], [LO + 23, WL, D_EDGE, 19, 6],
        [LO + 40, Wt, 31, 19, 6], [100, Wt, 28, 19, 6], [135, Wt, 25, 19, 6], [165, Wt, 22, 19, 6],
    ]
    fL, tL = 165.0, 315.0
    out = []
    for s0, w, d, al, rad in raw:
        s = s0
        if s <= -170:
            s = -fL + (s + 240)
        elif s < -70:
            s = -70 + (s + 70) * (fL - 140) / 100
        elif s > LO + 15 and s0 >= 100:
            s = (LO + 15) + (s - 60) * (tL - (LO + 15)) / 105
        out.append((s, w, d, al, rad))
    return out


def arc_point(s, lateral, depth):
    ang = s / R
    rn = np.array([-math.sin(ang), 0.0, math.cos(ang)])
    origin = rn * R
    return origin + np.array([0.0, lateral, 0.0]) - rn * depth


def guard_mesh():
    sts = station_list()
    rings = []
    for s, w, d, al, rad in sts:
        pts = section_pts(w, d, al, rad)
        rings.append([arc_point(s, p[0], p[1]) for p in pts])
    tris = []
    for a, b in zip(rings, rings[1:]):
        n = min(len(a), len(b))
        for i in range(n - 1):
            tris.append([a[i], b[i], b[i + 1]])
            tris.append([a[i], b[i + 1], a[i + 1]])
    return tris


def wing_mesh(x_c, z_b):
    """Both wings, outer and inner skin, with the blister and a simple rib bar."""
    Ds = np.linspace(D0 - 2, 135, 50)
    xs = np.linspace(x_c - HW, x_c + HW, 18)
    tris = []
    for sg in (1.0, -1.0):
        outer = []
        inner = []
        for D in Ds:
            yo = sg * y_outer(float(D))
            row_o, row_i = [], []
            for x in xs:
                z = z_b - D
                row_o.append(np.array([x, yo, z]))
                row_i.append(np.array([x, yo - sg * WT, z]))
            outer.append(row_o)
            inner.append(row_i)
        for grid in (outer, inner):
            for r0, r1 in zip(grid, grid[1:]):
                for i in range(len(r0) - 1):
                    tris.append([r0[i], r1[i], r1[i + 1]])
                    tris.append([r0[i], r1[i + 1], r0[i + 1]])
        # rib bar on the tyre side of the junction
        y0 = sg * ((Y_FACE - WT) - RIB_PROUD)
        y1 = sg * ((Y_FACE - WT) + RIB_OV)
        z0 = z_b - D_EDGE_BRACE
        z1 = z0 - RIB_DOWN
        x0, x1 = x_c - HW + 2, x_c + HW - 2
        box = np.array([
            [x0, y0, z0], [x1, y0, z0], [x1, y1, z0], [x0, y1, z0],
            [x0, y0, z1], [x1, y0, z1], [x1, y1, z1], [x0, y1, z1],
        ])
        faces = [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (1, 2, 6, 5), (0, 3, 7, 4)]
        for f in faces:
            p = [box[i] for i in f]
            tris.append([p[0], p[1], p[2]])
            tris.append([p[0], p[2], p[3]])
        # bottom tie holes as gaps in a second, unused way: punch by not drawing is hard;
        # draw a dark slot marker by omitting two quads — skipped, the bar shows the rib.
    return tris


def cylinder_mesh(x, y, z0, z1, radius, n=24):
    tris = []
    ang = np.linspace(0, 2 * math.pi, n, endpoint=False)
    top = [np.array([x + radius * math.cos(a), y + radius * math.sin(a), z0]) for a in ang]
    bot = [np.array([x + radius * math.cos(a), y + radius * math.sin(a), z1]) for a in ang]
    for i in range(n):
        j = (i + 1) % n
        tris.append([top[i], bot[i], bot[j]])
        tris.append([top[i], bot[j], top[j]])
    return tris


if __name__ == "__main__":
    main()
