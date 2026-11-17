"""Offline check of the v2.19.1 sheet profile: three tangent arcs, one extrude. No Onshape."""
import math

YO = 48.5
WT = 2.5
PROUD = 0.5
YLEG = 72.5
PAD = 1.5
TAN = math.tan(math.radians(19))
D0 = PAD + (97 - 76) / (2 * TAN)
DTOP = D0 - 6.0
DTIP = 135.0
R = 30.0
RB = 40.0
ALPHA = math.radians(15.5)


def rleg(D):
    if D <= 20:
        return 27.5
    if D >= 90:
        return 22.5
    return 27.5 - 5.0 * (D - 20) / 70.0


def arc_points(y, D, h, radius, dh, n):
    """Exact circular arc. dh is the heading change in radians."""
    left_y = -math.cos(h)
    left_d = math.sin(h)
    sgn = 1.0 if dh > 0 else -1.0
    cy = y + sgn * radius * left_y
    cd = D + sgn * radius * left_d
    ang0 = math.atan2(D - cd, y - cy)
    pts = []
    for i in range(1, n + 1):
        a = ang0 + sgn * abs(dh) * i / n
        py = cy + radius * math.cos(a)
        pd = cd + radius * math.sin(a)
        if sgn > 0:
            hh = math.atan2(-math.sin(a), math.cos(a))
        else:
            hh = math.atan2(math.sin(a), -math.cos(a))
        pts.append((py, pd, hh))
    return pts


def outer_path():
    """(y, D) down the leg-facing side, including the 0.05 mm proud embed."""
    pts = [(YO + PROUD, DTOP), (YO + PROUD, D0), (YO, D0)]
    y, D, h = YO, D0, 0.0
    chunks = []
    for radius, dh, n in ((R, ALPHA, 16), (RB, -2 * ALPHA, 24), (R, ALPHA, 16)):
        seg = arc_points(y, D, h, radius, dh, n)
        chunks.append(seg)
        y, D, h = seg[-1]
    for seg in chunks:
        for py, pd, _ in seg:
            pts.append((py, pd))
    pts.append((YO, DTIP))
    return pts, h


def offset_inner(outer):
    inner = []
    for i, (y, D) in enumerate(outer):
        if i == 0 or i == len(outer) - 1:
            inner.append((y - WT, D))
            continue
        vy = outer[i + 1][0] - outer[i - 1][0] if i + 1 < len(outer) else outer[i][0] - outer[i - 1][0]
        vd = outer[i + 1][1] - outer[i - 1][1] if i + 1 < len(outer) else 0.0
        if i == len(outer) - 1:
            vy = outer[i][0] - outer[i - 1][0]
            vd = outer[i][1] - outer[i - 1][1]
        ln = math.hypot(vy, vd) or 1.0
        n1 = (vd / ln, -vy / ln)
        n2 = (-vd / ln, vy / ln)
        nrm = n1 if n1[0] < n2[0] else n2
        inner.append((y + WT * nrm[0], D + WT * nrm[1]))
    return inner


def polygon_area(poly):
    a = 0.0
    for (y1, d1), (y2, d2) in zip(poly, poly[1:]):
        a += y1 * d2 - y2 * d1
    return abs(a) * 0.5


def intersections(poly):
    def cross(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])

    edges = list(zip(poly, poly[1:]))
    n = 0
    for i in range(len(edges)):
        for j in range(i + 2, len(edges)):
            if i == 0 and j == len(edges) - 1:
                continue
            a, b = edges[i]
            c, d = edges[j]
            if cross(a, b, c) * cross(a, b, d) < 0 and cross(c, d, a) * cross(c, d, b) < 0:
                n += 1
                if n <= 4:
                    print("  cross", i, j, a, b, c, d)
    return n


def main():
    outer, hend = outer_path()
    print("heading at end of arcs", round(hend, 6))
    print("arc end", [round(v, 4) for v in outer[-2]], "tip", [round(v, 4) for v in outer[-1]])
    ys = [p[0] for p in outer]
    Ds = [p[1] for p in outer]
    print("belly y", round(min(ys), 4), "at D", round(Ds[ys.index(min(ys))], 4))
    print("collar intrusion mm", round(min(ys) - 45.0, 4))
    print("D monotonic", all(Ds[i + 1] >= Ds[i] - 1e-9 for i in range(len(Ds) - 1)))
    print("D range of bow", round(D0, 3), "to", round(outer[-2][1], 3))

    # tangent check on the arc portion (skip the 0.05 step, which is a real corner the clip removes)
    breaks = []
    for i in range(2, len(outer) - 2):
        v1 = (outer[i][0] - outer[i - 1][0], outer[i][1] - outer[i - 1][1])
        v2 = (outer[i + 1][0] - outer[i][0], outer[i + 1][1] - outer[i][1])
        n1 = math.hypot(*v1)
        n2 = math.hypot(*v2)
        if n1 < 1e-9 or n2 < 1e-9:
            breaks.append(("zero", i))
            continue
        dot = (v1[0] * v2[0] + v1[1] * v2[1]) / (n1 * n2)
        if dot < 0.999:
            breaks.append((i, round(outer[i][1], 3), round(dot, 5)))
    print("tangent breaks on the bow", breaks)

    inner = offset_inner(outer)
    thick = [math.hypot(o[0] - i[0], o[1] - i[1]) for o, i in zip(outer, inner)]
    print("offset distance min/max", round(min(thick), 4), round(max(thick), 4))

    poly = list(outer) + list(reversed(inner))
    clean = [poly[0]]
    for p in poly[1:]:
        if math.hypot(p[0] - clean[-1][0], p[1] - clean[-1][1]) > 1e-5:
            clean.append(p)
    clean.append(clean[0])
    print("area", round(polygon_area(clean), 2), "vertices", len(clean) - 1)
    print("self intersections", intersections(clean))
    zero = sum(1 for a, b in zip(clean, clean[1:]) if math.hypot(a[0] - b[0], a[1] - b[1]) < 1e-4)
    print("zero-length edges", zero)

    # wall overlap of the embed (one side), mm^2
    overlap = 0.0
    step = (D0 - DTOP) / 40
    for i in range(40):
        D = DTOP + step * (i + 0.5)
        y_wall = 38 + max(D - PAD, 0) * TAN
        lo, hi = YO - WT, min(YO + PROUD, y_wall)
        if hi > lo:
            overlap += (hi - lo) * step
    print("embed overlap mm2", round(overlap, 2), "(sheet inside the wall, proud", PROUD, "mm)")

    Rarch = 393.5
    XC = -Rarch * math.sin(45 / Rarch)
    ZB = Rarch * math.cos(45 / Rarch) + PAD

    def tyre(x, y, z):
        rho = math.hypot(x, z)
        return math.hypot(rho - (375 - 34.6), y) - 34.6

    tmin, tat = 99, None
    worst, wat = 99, None
    for (y, D), (iy, iD) in zip(outer, inner):
        if D + 1e-6 < D0:
            continue
        gap = (YLEG - y) - rleg(D)
        if gap < worst:
            worst, wat = gap, (round(D, 2), round(y, 3))
        z = ZB - D
        for x in (XC - 28, XC, XC + 28):
            g = tyre(x, iy, z)
            if g < tmin:
                tmin, tat = g, (round(D, 2), round(iy, 2))
    print("worst leg gap", round(worst, 3), wat)
    print("tyre min", round(tmin, 3), tat)
    print("stanchion", round((YLEG - YO) - 18, 3), "od45", round((YLEG - YO) - 22.5, 3))

    # sketch numbers for FeatureScript: arc centers and a mid point of each arc
    print("--- sketch ---")
    print("C1 y,D", YO - R, D0, "to heading", -ALPHA)
    # centers, for a concentric inner offset
    print("arc1 center", YO - R, round(D0, 6), "ang 0 to", round(math.degrees(ALPHA), 4))
    # reconstruct centers by rerunning with n=1 and the formula used above
    y, D, h = YO, D0, 0.0
    centers = []
    for name, radius, dh in (("a1", R, ALPHA), ("a2", RB, -2 * ALPHA), ("a3", R, ALPHA)):
        left_y = -math.cos(h)
        left_d = math.sin(h)
        sgn = 1.0 if dh > 0 else -1.0
        cy = y + sgn * radius * left_y
        cd = D + sgn * radius * left_d
        centers.append((name, cy, cd, radius, dh))
        seg = arc_points(y, D, h, radius, dh, 2)
        y, D, h = seg[-1]
        print(name, "center", round(cy, 4), round(cd, 4), "end", round(y, 4), round(D, 4))
    # inner radii: arc1 and arc3 centers are inboard, offset toward them (R-WT);
    # arc2 center is outboard, offset away from it (Rb+WT)
    print("inner radii", R - WT, RB + WT, R - WT)
    # inner control points at the same angles
    def on_circle(cy, cd, rad, ang):
        return (cy + rad * math.cos(ang), cd + rad * math.sin(ang))
    a = ALPHA
    c1y, c1d = centers[0][1], centers[0][2]
    c2y, c2d = centers[1][1], centers[1][2]
    c3y, c3d = centers[2][1], centers[2][2]
    ri, rj = R - WT, RB + WT
    samples = {
        "i1s": on_circle(c1y, c1d, ri, 0),
        "i1m": on_circle(c1y, c1d, ri, a / 2),
        "i1e": on_circle(c1y, c1d, ri, a),
        "i2s": on_circle(c2y, c2d, rj, math.atan2(centers[0] and 0, 1)),
    }
    # angles of arc2 from its center
    def ang(cy, cd, py, pd):
        return math.atan2(pd - cd, py - cy)
    p1 = arc_points(YO, D0, 0.0, R, ALPHA, 1)[-1]
    p2 = arc_points(p1[0], p1[1], p1[2], RB, -2 * ALPHA, 1)[-1]
    a2s = ang(c2y, c2d, p1[0], p1[1])
    a2e = ang(c2y, c2d, p2[0], p2[1])
    # mid angle along the short way
    def midang(a0, a1):
        d = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
        return a0 + d / 2
    print("arc2 angles deg", round(math.degrees(a2s), 3), round(math.degrees(a2e), 3))
    i2s = on_circle(c2y, c2d, rj, a2s)
    i2m = on_circle(c2y, c2d, rj, midang(a2s, a2e))
    i2e = on_circle(c2y, c2d, rj, a2e)
    a3s = ang(c3y, c3d, p2[0], p2[1])
    a3e = ang(c3y, c3d, YO, p2[1] and centers[2][2])
    # end of arc3 is (YO, D_end). D_end is centers[2] D because end is directly outboard
    Dend = centers[2][2]
    a3e = ang(c3y, c3d, YO, Dend)
    i3s = on_circle(c3y, c3d, ri, a3s)
    i3m = on_circle(c3y, c3d, ri, midang(a3s, a3e))
    i3e = on_circle(c3y, c3d, ri, a3e)
    print("inner arc1", [round(v, 4) for v in on_circle(c1y, c1d, ri, 0)], [round(v, 4) for v in on_circle(c1y, c1d, ri, a / 2)], [round(v, 4) for v in on_circle(c1y, c1d, ri, a)])
    print("inner arc2", [round(v, 4) for v in i2s], [round(v, 4) for v in i2m], [round(v, 4) for v in i2e])
    print("inner arc3", [round(v, 4) for v in i3s], [round(v, 4) for v in i3m], [round(v, 4) for v in i3e])
    print("joins", "i1e-i2s", round(math.hypot(on_circle(c1y, c1d, ri, a)[0] - i2s[0], on_circle(c1y, c1d, ri, a)[1] - i2s[1]), 5),
          "i2e-i3s", round(math.hypot(i2e[0] - i3s[0], i2e[1] - i3s[1]), 5),
          "i3e y,D", round(i3e[0], 4), round(i3e[1], 4), "expect", YO - WT, round(Dend, 4))


if __name__ == "__main__":
    main()

