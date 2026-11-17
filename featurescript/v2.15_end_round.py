"""v2.15 end-round analysis (offline). Replicates v2.14 stations + profSketch,
projects the square ends, and checks a plan-view corner round + 1 mm lip chamfer.

Chosen shape: R32 corner radii, tangent to the end plane and to y = +/-48.5
(the side-wall lip). Straight land between the corners is 33 mm. That is the
rounded-rectangle end used by Mudhugger, Marsh Guard, RRP and Sico: large
corners, short flat, not a semicircular nose (R48.5 would pull the side
coverage back by 49 mm). A 1 mm 45 deg chamfer is applied only on the
straight lip (outer and inner top), clear of the arcs.

Nothing here calls Onshape. Lengths are millimetres.
"""
import math
import os
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Arc

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "renders")
os.makedirs(OUT, exist_ok=True)

R = 393.5  # ph1 - padThk
FL, TL = 165.0, 315.0
LO = 45.0
WF, WT, W = 97.0, 97.0, 82.0
WL = 97.0
WALL, WW = 2.5, 3.0
KICK_LEN, KICK_ANG = 25.0, 12.0
RC = 32.0          # plan corner radius
CHAM = 1.0         # end-lip chamfer leg
ALPHA, CR = 5.0, 20.0

def kick(s):
    sK = -FL + KICK_LEN
    if s >= sK:
        return 0.0
    u = sK - s
    return u * u * math.tan(math.radians(KICK_ANG)) / (2 * KICK_LEN)

def build_stations():
    tA = math.tan(math.radians(19))
    dB = (W - 60) / (2 * tA)
    dL = (WL - 76) / (2 * tA)
    aR, rR, dC, rC = ALPHA, CR, 30.49, 18.0
    raw = [
        [-240, WF, 28, aR, rR, WALL],
        [-227.5, WF, 30, aR, rR, WALL],
        [-215, WF, 32, aR, rR, WALL],
        [-192.5, WF, 33, aR, rR, WALL],
        [-170, WF, 34, aR, rR, WALL],
        [-145, WF, 34, aR, rR, WALL],
        [-120, WF, 34, aR, rR, WALL],
        [-95, WF, 34, aR, rR, WALL],
        [-82.5, WF, 34, aR, rR, WALL],
        [-70, WF, 34, aR, rR, WALL],
        [-55, WF, 34, aR, rR, WALL],
        [-35, 90, 30, 8, 14, (WALL + WW) / 2],
        [-15, W, dC, 0, rC, WW],
        [0, W, dC, 0, rC, WW],
        [10, W, dC, 0, rC, WW],
        [LO - 23, WL, dL, 19, 6, WW],
        [LO - 11, WL, dL, 19, 6, WW],
        [LO, WL, dL, 19, 6, WW],
        [LO + 11, WL, dL, 19, 6, WW],
        [LO + 23, WL, dL, 19, 6, WW],
        [LO + 40, WT, 31, 19, 6, WALL],
        [100, WT, 28, 19, 6, WALL],
        [135, WT, 25, 19, 6, WALL],
        [165, WT, 22, 19, 6, WALL],
    ]
    out = []
    for row in raw:
        s = row[0]
        if s <= -170:
            s = -FL + (s + 240)
        elif s < -70:
            s = -70 + (s + 70) * (FL - 140) / 100
        elif s > LO + 15 and row[0] >= 100:
            s = (LO + 15) + (s - 60) * (TL - (LO + 15)) / 105
        if out and s - out[-1]["s"] < 8:
            continue
        a = row[1] / 2 - row[2] * math.tan(math.radians(row[3]))
        out.append(dict(s=s, w=row[1], d=row[2], alpha=row[3], r=row[4], wall=row[5], a=a, k=kick(s)))
    return out

def frame(s, k=0.0):
    ang = s / R
    rn = np.array([-math.sin(ang), 0.0, math.cos(ang)])
    t = np.array([-math.cos(ang), 0.0, -math.sin(ang)])
    origin = rn * (R + k)
    return origin, rn, t

def world_point(s, y, h, k):
    o, rn, t = frame(s, k)
    return o + np.array([0.0, y, 0.0]) - h * rn

def prof_params(a, alpha, r, t, hEnd):
    ca, sa = math.cos(math.radians(alpha)), math.sin(math.radians(alpha))
    cx = a - r * (1 - sa) / ca
    rr = r - t
    wtx = cx + rr * ca
    wth = r - rr * sa
    xe = a + hEnd * sa / ca - t / ca
    # arc mid (same construction as profSketch)
    md = np.array([ca, -1 - sa])
    md = md / np.linalg.norm(md)
    mx = cx + rr * md[0]
    mh = r + rr * md[1]
    return dict(cx=cx, rr=rr, wtx=wtx, wth=wth, xe=xe, mx=mx, mh=mh, ca=ca, sa=sa, t=t, hEnd=hEnd, a=a, alpha=alpha, r=r)

def prof_poly(p, n=48):
    """Closed outer/offset profile, sketch (y, h), CCW starting at left wall bottom."""
    cx, rr, wtx, wth, xe = p["cx"], p["rr"], p["wtx"], p["wth"], p["xe"]
    t, hEnd, r = p["t"], p["hEnd"], p["r"]
    # right wall top -> right arc -> top -> left arc -> left wall -> close
    pts = [(-xe, hEnd), (-wtx, wth)]
    # left arc from (-wtx,wth) through (-mx,mh) to (-cx, t). Sample the true circle.
    # centre (-cx, r), from the wall tangent to the top tangent.
    c = np.array([-cx, r])
    a0 = math.atan2(wth - r, -wtx - (-cx))
    a1 = math.atan2(t - r, -cx - (-cx))
    # walk the short way that passes near the mid
    for th in np.linspace(a0, a0 + _ang(a0, a1, np.array([-p["mx"], p["mh"]]) - c), n):
        pts.append(tuple(c + rr * np.array([math.cos(th), math.sin(th)])))
    pts.append((-cx, t))
    pts.append((cx, t))
    c = np.array([cx, r])
    a0 = math.atan2(t - r, cx - cx)
    a1 = math.atan2(wth - r, wtx - cx)
    for th in np.linspace(a0, a0 + _ang(a0, a1, np.array([p["mx"], p["mh"]]) - c), n):
        pts.append(tuple(c + rr * np.array([math.cos(th), math.sin(th)])))
    pts.append((wtx, wth))
    pts.append((xe, hEnd))
    pts.append((-xe, hEnd))
    return np.array(pts)

def _ang(a0, a1, toward):
    """Signed delta from a0 toward a1, the one whose mid points near `toward`."""
    d = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
    mid = a0 + d / 2
    if np.dot(np.array([math.cos(mid), math.sin(mid)]), toward) < 0:
        d = d - math.copysign(2 * math.pi, d) if d != 0 else math.pi
    return d

def normal_offset_error(p0, p1):
    """Max |distance from offset polyline to original| minus t, on the right half."""
    A = prof_poly(p0, 80)
    B = prof_poly(p1, 80)
    # compare B points with x>0 to A
    errs = []
    for q in B:
        if q[0] < 1:
            continue
        best = 1e9
        for i in range(len(A) - 1):
            v = A[i + 1] - A[i]
            L2 = np.dot(v, v)
            if L2 < 1e-12:
                continue
            tt = np.clip(np.dot(q - A[i], v) / L2, 0, 1)
            best = min(best, np.linalg.norm(q - (A[i] + tt * v)))
        errs.append(best)
    return float(np.max(np.abs(np.array(errs) - p1["t"]))) if errs else None

def end_station(sts, which):
    return sts[0] if which == "front" else sts[-1]

def corner_keep(u, y, y_side, rc=RC):
    """True if local (u into the part, y lateral) is INSIDE the rounded outline.
    u = 0 on the end plane, u > 0 into the part. y_side > 0.
    """
    yc = y_side - rc
    ay = abs(y)
    if ay <= yc:
        return u >= -1e-9
    # outside the side: nothing of the part, treat as not-keep for the outline
    du = u - rc
    dy = ay - yc
    return du * du + dy * dy >= rc * rc - 1e-8 and u <= rc + 1e-8 or u > rc

def outline_u(y, y_side, rc=RC):
    """Minimum u (into the part) of the rounded plan outline at lateral y."""
    yc = y_side - rc
    ay = abs(y)
    if ay <= yc:
        return 0.0
    dy = ay - yc
    if dy >= rc:
        return rc  # past the side tangent; side wall ends here
    return rc - math.sqrt(rc * rc - dy * dy)

def in_corner_cutter(u, y, h, y_side, h_lo=-6, h_hi=42, rc=RC):
    """Cutter occupies the square corner outside the arc, plus a skirt outside the part."""
    if h < h_lo or h > h_hi:
        return False
    if u < -6 or u > rc + 0.1:
        return False
    ay = abs(y)
    yc = y_side - rc
    if ay < yc - 0.05:
        return False
    if ay > y_side + 10:
        return False
    # outside the circle (the corner side), and not past the side-tangent into the wall
    du = u - rc
    dy = ay - yc
    outside = du * du + dy * dy >= rc * rc - 1e-6
    return outside and u <= rc + 1e-8

def shell_points(st, nu=1, nh=2):
    """Sample the solid wall of one station: points between outer and inner, both sides."""
    p_out = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
    p_in = prof_params(st["a"], st["alpha"], st["r"], st["wall"], st["d"] + 10)
    outer = prof_poly(p_out, 36)
    pts = []
    # grid in (y,h) inside outer and outside inner
    ys = np.linspace(-p_out["xe"] - 0.2, p_out["xe"] + 0.2, 97)
    hs = np.linspace(-0.5, st["d"] + 0.8, 40)
    for y in ys:
        for h in hs:
            if _inside(outer, y, h) and not _inside_inner(p_in, y, h, st["d"]):
                pts.append((y, h))
    return np.array(pts)

def _inside(poly, x, y):
    # even-odd on closed polyline
    c = False
    n = len(poly)
    j = n - 1
    for i in range(n):
        yi, yj = poly[i][1], poly[j][1]
        xi, xj = poly[i][0], poly[j][0]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-15) + xi):
            c = not c
        j = i
    return c

def _inside_inner(p_in, y, h, d_outer):
    """Inner void: inside the offset profile AND above its closing line, but only where
    it actually carves the outer solid (h <= d_outer + 0.05). The void profile extends
    to d+10; below the outer wall it does not remove material."""
    if h > d_outer + 0.05:
        return False
    poly = prof_poly(p_in, 28)
    return _inside(poly, y, h)

def main():
    sts = build_stations()
    print("stations", [(round(s["s"], 2), s["w"], s["d"], round(s["k"], 3)) for s in sts])
    front, rear = sts[0], sts[-1]
    print(f"FRONT s={front['s']} k={front['k']:.4f} d={front['d']} a={front['a']:.4f}")
    print(f"REAR  s={rear['s']} k={rear['k']:.4f} d={rear['d']} a={rear['a']:.4f}")
    for name, st in ("front", front), ("rear", rear):
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        pi = prof_params(st["a"], st["alpha"], st["r"], st["wall"], st["d"] + 10)
        print(f"  {name} outer xe={po['xe']:.4f} cx={po['cx']:.4f} wtx={po['wtx']:.3f} wth={po['wth']:.3f}")
        print(f"  {name} inner xe={pi['xe']:.4f} cx={pi['cx']:.4f} top h={pi['t']}")
        err = normal_offset_error(po, prof_params(st["a"], st["alpha"], st["r"], 1.0, st["d"]))
        print(f"  {name} t=1 normal-offset error vs profSketch {err:.4f} mm")

    # --- plan silhouette of the square-cut end (world XY, +X forward) ---
    def silhouette(st, s_span, n_s=25):
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        poly = prof_poly(po, 40)
        pts = []
        for s in np.linspace(st["s"], st["s"] + s_span, n_s):
            # section is not constant; use this station's profile only within 40 mm (tip zone is nearly constant)
            k = kick(s)
            for y, h in poly[::2]:
                pts.append(world_point(s, y, h, k))
        return np.array(pts)

    # Use actual stations near each end for a truer silhouette
    def cloud_from_stations(stations, side_sign=1):
        pts = []
        for st in stations:
            po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
            for y, h in prof_poly(po, 24)[::1]:
                pts.append(world_point(st["s"], y, max(h, 0), st["k"]))
        return np.array(pts)

    front_sts = [s for s in sts if s["s"] <= -100]
    rear_sts = [s for s in sts if s["s"] >= 200]
    print("front stations used", [round(s["s"], 1) for s in front_sts])
    print("rear stations used", [round(s["s"], 1) for s in rear_sts])

    # Local (u,y) outline comparison and world-XY plan of the END FACE only
    fig, axes = plt.subplots(1, 2, figsize=(11.5, 5.2))
    for ax, st, title, s_dir in (
        (axes[0], front, "Front tip  (s = -165)", 1),
        (axes[1], rear, "Rear end  (s = +315)", -1),
    ):
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        y_side = po["xe"]
        # end-face outer curve in plan: each profile point's world XY
        poly = prof_poly(po, 80)
        end_xy = np.array([world_point(st["s"], y, h, st["k"]) for y, h in poly])
        # side lip (wall bottom) along s
        lip = []
        for s in np.linspace(st["s"], st["s"] + s_dir * 80, 40):
            lip.append(world_point(s, y_side, st["d"], kick(s)))
            lip.append(world_point(s, -y_side, st["d"], kick(s)))
        lip = np.array(lip)
        # top edge along s
        top = []
        cx = po["cx"]
        for s in np.linspace(st["s"], st["s"] + s_dir * 80, 20):
            top.append(world_point(s, cx, 0, kick(s)))
            top.append(world_point(s, -cx, 0, kick(s)))
        top = np.array(top)
        ax.plot(end_xy[:, 0], end_xy[:, 1], "k-", lw=1.6, label="square end face (outer)")
        ax.scatter(lip[:, 0], lip[:, 1], s=6, c="tab:orange", label="side lip ±xe along 80 mm")
        ax.scatter(top[:, 0], top[:, 1], s=8, c="tab:blue", label="top-flat corner ±cx")
        ax.set_aspect("equal")
        ax.grid(alpha=0.3)
        ax.set_title(title)
        ax.set_xlabel("X (mm, +forward)")
        ax.set_ylabel("Y (mm)")
        ax.legend(fontsize=7, loc="best")
    fig.suptitle("v2.14 square ends in world plan (XY). Frontmost metal is the top; the wide lip sits behind it.", fontsize=10)
    fig.tight_layout()
    fig.savefig(os.path.join(OUT, "v2.15_plan_square_ends.png"), dpi=130)
    plt.close()

    # --- unwrapped (u, y) outline, the plan of the guard surface ---
    fig, axes = plt.subplots(1, 2, figsize=(11.2, 4.6))
    for ax, st, title in ((axes[0], front, "Front tip, unwrapped plan"), (axes[1], rear, "Rear end, unwrapped plan")):
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        ys = np.linspace(-po["xe"], po["xe"], 400)
        u_sq = np.zeros_like(ys)
        u_rd = np.array([outline_u(y, po["xe"]) for y in ys])
        ax.fill_betweenx(ys, 0, 70, color="#d9e6f2", label="kept (round)")
        ax.plot(u_sq, ys, "k--", lw=1, label="v2.14 square end")
        ax.plot(u_rd, ys, "C3-", lw=2.2, label=f"R{RC:.0f} corners")
        # mark tangent points and straight land
        yc = po["xe"] - RC
        ax.plot([0, 0], [-yc, yc], "C3-", lw=2.2)
        ax.scatter([0, 0, RC, RC], [yc, -yc, po["xe"], -po["xe"]], c="k", s=18, zorder=3)
        ax.annotate(f"straight end {2*yc:.1f} mm", (1.5, 0), fontsize=8)
        ax.annotate(f"R{RC:.0f}", (RC * 0.45, po["xe"] * 0.72), fontsize=8, color="C3")
        ax.set_xlim(-8, 78)
        ax.set_ylim(-po["xe"] - 8, po["xe"] + 8)
        ax.set_aspect("equal")
        ax.grid(alpha=0.3)
        ax.set_xlabel("u into the part along the end tangent (mm)")
        ax.set_ylabel("Y (mm)")
        ax.set_title(title)
        ax.legend(fontsize=7, loc="lower right")
    fig.suptitle("Plan outline on the guard (looking along the local radial). R32 tangent to the end and to y = ±48.5.", fontsize=10)
    fig.tight_layout()
    fig.savefig(os.path.join(OUT, "v2.15_plan_outline_R32.png"), dpi=140)
    plt.close()

    # --- removed shell volume + interference with stations away from the ends ---
    def removed_volume(st):
        """Vectorised wall grid at the end section. The tip zone holds this section for the cut depth."""
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        y_side = po["xe"]
        cell = 0.5
        us = np.arange(0.0, RC + cell, cell)
        ys = np.arange(-y_side - 0.5, y_side + 0.5 + cell, cell)
        hs = np.arange(0.0, st["d"] + cell, cell)
        uu, yy, hh = np.meshgrid(us, ys, hs, indexing="ij")
        yc = y_side - RC
        ay = np.abs(yy)
        du = uu - RC
        dy = ay - yc
        cutter = (ay >= yc - 0.05) & (du * du + dy * dy >= RC * RC - 1e-6) & (uu <= RC + 1e-8)
        outer = prof_poly(po, 24)
        p_in = prof_params(st["a"], st["alpha"], st["r"], st["wall"], st["d"] + 10)
        inner = prof_poly(p_in, 16)
        # point-in-polygon on the unique (y,h) pairs that the cutter can hit
        yh = np.stack([yy[cutter], hh[cutter]], axis=1)
        if len(yh) == 0:
            return 0.0, 0.0
        # unique rows
        yh_u = np.unique(np.round(yh / cell) * cell, axis=0)
        inside = np.array([_inside(outer, y, h) and not _inside_inner(p_in, y, h, st["d"]) for y, h in yh_u])
        key = { (round(y / cell), round(h / cell)): b for (y, h), b in zip(yh_u, inside) }
        n = 0
        n_far = 0
        idx = np.argwhere(cutter)
        for i, j, k in idx:
            if key.get((round(yy[i, j, k] / cell), round(hh[i, j, k] / cell))):
                n += 1
                if uu[i, j, k] > 8:
                    n_far += 1
        return n * cell ** 3, n_far * cell ** 3

    # chamfer volume (two ends, outer+inner top, |y|<=yc-0.5)
    def chamfer_volume(st):
        po = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
        yc = po["xe"] - RC
        y_lim = yc - 0.5
        # outer: triangle area 0.5, length 2*y_lim; inner the same. They don't overlap.
        return 2 * (0.5 * CHAM * CHAM) * (2 * y_lim)

    print("gridding removed volume...", flush=True)
    vf, far_f = removed_volume(front)
    vr, far_r = removed_volume(rear)
    print(f"corner-cut shell volume estimate front {vf:.1f} mm3 (of which u>8: {far_f:.1f}) rear {vr:.1f} (u>8 {far_r:.1f})")
    print(f"per end that is two corners; chamfer both lips one end {chamfer_volume(front):.2f} mm3")
    print(f"A volume delta ~ -{(vf + chamfer_volume(front))/1000:.2f} cm3   B ~ -{(vr + chamfer_volume(rear))/1000:.2f} cm3")
    print(f"v2.14 A 65.3 B 114.2 -> approx A {65.3 - (vf+chamfer_volume(front))/1000:.2f} B {114.2 - (vr+chamfer_volume(rear))/1000:.2f}")

    # Interference: any station more than RC+15 mm from the end whose wall points fall in the cutter,
    # measured in the END's local (u,y,h). u = dot(world - p0, into).
    def local_of(st_end, into, s, y, h, k):
        p0, rn, t = frame(st_end["s"], st_end["k"])
        p = world_point(s, y, h, k)
        u = np.dot(p - p0, into)
        # h along -rn, but the point's own frame differs. Use coordinates in the end frame:
        y_l = p[1]
        h_l = -np.dot(p - p0, rn)  # 0 on the end-plane outer radius; increases inward
        return u, y_l, h_l

    hits = []
    for st_end, into_sign, label in ((front, 1, "front"), (rear, -1, "rear")):
        _, rn, t = frame(st_end["s"], st_end["k"])
        into = into_sign * t  # +u into the part. Front: +t is +s. Rear: into the part is -t.
        # Wait: frame().t is direction of INCREASING s. Front into-part = +t. Rear into-part = -t.
        po = prof_params(st_end["a"], st_end["alpha"], st_end["r"], 0.0, st_end["d"])
        for st in sts:
            ds = (st["s"] - st_end["s"]) * into_sign
            if ds < RC + 12:
                continue  # near the end, intersection is the cut we want
            p_out = prof_params(st["a"], st["alpha"], st["r"], 0.0, st["d"])
            for y, h in prof_poly(p_out, 12)[::3]:
                for hh in (0.0, h, st["wall"]):
                    u, yl, hl = local_of(st_end, into, st["s"], y, hh, st["k"])
                    if in_corner_cutter(u, yl, hl, po["xe"]):
                        hits.append((label, round(st["s"], 1), round(u, 1), round(yl, 1), round(hl, 1)))
    print("cutter hits on stations beyond Rc+12 mm:", len(hits))
    if hits[:8]:
        print("  examples", hits[:8])

    # Clearance closest-point check (v2.14 reported points)
    for label, pt in ("A", np.array([-5.9, -37.8, 363.0])), ("B", np.array([-6.5, 37.8, 363.0])):
        print(f"v2.14 closest {label} {pt.tolist()}  |y|={abs(pt[1]):.1f} (cutters only |y|>16.5 near the ends)")

    # Radius check: sample the outline, fit the corner arc
    po = prof_params(front["a"], front["alpha"], front["r"], 0.0, front["d"])
    yc = po["xe"] - RC
    samples = []
    for y in np.linspace(yc + 0.5, po["xe"] - 0.5, 12):
        u = outline_u(y, po["xe"])
        samples.append((u - RC) ** 2 + (y - yc) ** 2)
    print(f"corner radius check (should be {RC**2:.1f}): min {min(samples):.2f} max {max(samples):.2f}")
    print(f"straight land |y|<={yc:.3f} mm  length {2*yc:.3f} mm  side tangent at u={RC} y=±{po['xe']:.3f}")
    print(f"chamfer |y|<={yc-0.5:.3f} u=0..{CHAM}  outer h=0..{CHAM}  inner h={WALL-CHAM}..{WALL}")
    print(f"land between chamfers at u=0: {WALL - 2*CHAM:.2f} mm")

    # World-XY before/after of the front 50 mm, using end section swept along true arc
    fig, ax = plt.subplots(figsize=(7.2, 6.2))
    po = prof_params(front["a"], front["alpha"], front["r"], 0.0, front["d"])
    poly = prof_poly(po, 50)
    before, after = [], []
    for s in np.linspace(front["s"], front["s"] + 55, 30):
        k = kick(s)
        p0, rn, t = frame(front["s"], front["k"])
        into = t  # +s
        for y, h in poly[::2]:
            p = world_point(s, y, h, k)
            before.append(p)
            u = np.dot(p - p0, into)
            if outline_u(y, po["xe"]) <= u + 1e-6:
                after.append(p)
    before, after = np.array(before), np.array(after)
    ax.scatter(before[:, 0], before[:, 1], s=4, c="#bbbbbb", label="v2.14 square tip")
    ax.scatter(after[:, 0], after[:, 1], s=5, c="C0", label="v2.15 kept")
    ax.set_aspect("equal")
    ax.grid(alpha=0.3)
    ax.set_xlabel("X (mm)")
    ax.set_ylabel("Y (mm)")
    ax.set_title("Front tip, world plan (XY): grey removed, blue kept")
    ax.legend(fontsize=8)
    fig.tight_layout()
    fig.savefig(os.path.join(OUT, "v2.15_front_plan_before_after.png"), dpi=140)
    plt.close()

    # Rear similarly
    fig, ax = plt.subplots(figsize=(7.2, 6.2))
    po = prof_params(rear["a"], rear["alpha"], rear["r"], 0.0, rear["d"])
    poly = prof_poly(po, 50)
    before, after = [], []
    p0, rn, t = frame(rear["s"], rear["k"])
    into = -t
    for s in np.linspace(rear["s"], rear["s"] - 55, 30):
        for y, h in poly[::2]:
            p = world_point(s, y, h, 0.0)
            before.append(p)
            u = float(np.dot(p - p0, into))
            if outline_u(y, po["xe"]) <= u + 1e-6:
                after.append(p)
    before, after = np.array(before), np.array(after)
    ax.scatter(before[:, 0], before[:, 1], s=4, c="#bbbbbb", label="v2.14 square tail")
    ax.scatter(after[:, 0], after[:, 1], s=5, c="C0", label="v2.15 kept")
    ax.set_aspect("equal")
    ax.grid(alpha=0.3)
    ax.set_xlabel("X (mm)")
    ax.set_ylabel("Y (mm)")
    ax.set_title("Rear end, world plan (XY): grey removed, blue kept")
    ax.legend(fontsize=8)
    fig.tight_layout()
    fig.savefig(os.path.join(OUT, "v2.15_rear_plan_before_after.png"), dpi=140)
    plt.close()

    # Section probe: a station 50 mm in from each end is outside the cutter
    print("s=-115 (50 mm behind the tip zone start) and s=265 distance to ends", -115 - front["s"], rear["s"] - 265)

    # Bed-fit monotonicity is analytic (subset). Estimate the rear-corner point movement anyway.
    # Rear corner point at y=+xe, h=d, s=315 vs the new tangent point u=RC along -t.
    p_old = world_point(rear["s"], po["xe"], rear["d"], 0)
    p_new = p0 + into * RC + np.array([0, po["xe"], 0]) - rear["d"] * rn
    print(f"rear lip corner old {np.round(p_old,1).tolist()}  tangent point {np.round(p_new,1).tolist()}")
    print(f"moved in by {np.linalg.norm(p_new-p_old):.2f} mm (bbox can only shrink)")

    print("OK")

if __name__ == "__main__":
    main()
