"""v2.19.3 leg-joint geometry (offline). Root profile (u=|y|, v=depth below R), lower-sheet outline (world x,z)."""
import math
R = 393.5; PAD = 1.5; LO = 45.0
T19 = math.tan(math.radians(19)); C19 = math.cos(math.radians(19)); S19 = math.sin(math.radians(19))
WL, TF = 97.0, 76.0
dL = (WL - TF) / (2 * T19)
A = WL / 2 - dL * T19            # 38.0
WW = 3.0; WT = 2.5
OFF_IN = WW / C19                # 3.17289
R1, BETA, RB, YF = 8.0, math.radians(18), 53.0, 49.0
RI2 = 5.5
VCUT, VBOT, VSHEET = 20.5, 76.0, 72.0
S0, S1 = 22.5, 76.0             # root / band-cut window (s at R); constant revolved leg zone is REV0..REV1
REV0, REV1 = 16.0, 78.0
TH0, TH1 = S0 / R, S1 / R
thC = LO / R
XC = -R * math.sin(thC); ZTOP = R * math.cos(thC); ZB = ZTOP + PAD
HW = 28.0; ZV_D = 107.9; RFL = 10.0; RC = 8.0   # flare ends (vertical) at D = ZV_D below the brace underside
DTIP = 135.0 - PAD
ZT = ZTOP - DTIP

def uo(v): return A + v * T19
def ui(v): return A + v * T19 - OFF_IN

E_ = (WL / 2, dL)
C2 = (E_[0] - R1 * C19, E_[1] + R1 * S19)
P2 = (C2[0] + R1 * math.cos(BETA), C2[1] + R1 * math.sin(BETA))
C3 = (P2[0] + RB * math.cos(BETA), P2[1] + RB * math.sin(BETA))
BELLY = (C3[0] - RB, C3[1])
P3 = (C3[0] - RB * math.cos(BETA), C3[1] + RB * math.sin(BETA))
R3 = (YF - P3[0]) / (1 - math.cos(BETA))
C4 = (P3[0] - R3 * math.cos(BETA), P3[1] + R3 * math.sin(BETA))
P4 = (C4[0] + R3, C4[1])
# inner chain
R4i = R3 - WT; R3i = RB + WT
Q4 = (C4[0] + R4i * math.cos(BETA), C4[1] - R4i * math.sin(BETA))
Q3 = (C3[0] - R3i * math.cos(BETA), C3[1] + R3i * math.sin(BETA))
# i2 centre: on line u = A - OFF_IN - RI2/C19 + v*T19, at distance R3i+RI2 from C3, upper root
k0 = A - OFF_IN - RI2 / C19
# (k0 + v T - C3u)^2 + (v - C3v)^2 = (R3i+RI2)^2
aa = T19 ** 2 + 1; bb = 2 * T19 * (k0 - C3[0]) - 2 * C3[1]; cc = (k0 - C3[0]) ** 2 + C3[1] ** 2 - (R3i + RI2) ** 2
v2 = (-bb - math.sqrt(bb * bb - 4 * aa * cc)) / (2 * aa)
CI2 = (k0 + v2 * T19, v2)
TW = (CI2[0] + RI2 * C19, CI2[1] - RI2 * S19)                  # tangency with inner wall line
dd = math.hypot(C3[0] - CI2[0], C3[1] - CI2[1])
TB = (CI2[0] + RI2 * (C3[0] - CI2[0]) / dd, CI2[1] + RI2 * (C3[1] - CI2[1]) / dd)  # tangency with i3

def arcpt(c, r, ang_deg_from_plus_u):
    a = math.radians(ang_deg_from_plus_u); return (c[0] + r * math.cos(a), c[1] + r * math.sin(a))

def ang(c, p): return math.degrees(math.atan2(p[1] - c[1], p[0] - c[0]))

def midarc(c, r, p, q):
    a1, a2 = ang(c, p), ang(c, q)
    d = (a2 - a1 + 180) % 360 - 180
    return arcpt(c, r, a1 + d / 2)

def u_out(v):
    if v <= dL: return uo(v)
    if v <= P2[1]: return C2[0] + math.sqrt(R1 ** 2 - (v - C2[1]) ** 2)
    if v <= P3[1]: return C3[0] - math.sqrt(RB ** 2 - (v - C3[1]) ** 2)
    if v <= P4[1]: return C4[0] + math.sqrt(R3 ** 2 - (v - C4[1]) ** 2)
    return YF

def u_in(v):
    if v <= TW[1]: return ui(v)
    if v <= TB[1]: return CI2[0] + math.sqrt(RI2 ** 2 - (v - CI2[1]) ** 2)
    if v <= Q4[1]: return C3[0] - math.sqrt(R3i ** 2 - (v - C3[1]) ** 2)
    if v <= C4[1]: return C4[0] + math.sqrt(R4i ** 2 - (v - C4[1]) ** 2)
    return YF - WT

def root_profile():
    """closed chain: list of ('L', p, q) / ('A', p, mid, q)"""
    top_o = (uo(VCUT), VCUT); top_i = (ui(VCUT), VCUT)
    ch = [('L', top_o, E_), ('A', E_, midarc(C2, R1, E_, P2), P2), ('A', P2, BELLY, P3),
          ('A', P3, midarc(C4, R3, P3, P4), P4), ('L', P4, (YF, VBOT)), ('L', (YF, VBOT), (YF - WT, VBOT)),
          ('L', (YF - WT, VBOT), (YF - WT, C4[1])), ('A', (YF - WT, C4[1]), midarc(C4, R4i, (YF - WT, C4[1]), Q4), Q4),
          ('A', Q4, midarc(C3, R3i, Q4, TB), TB), ('A', TB, midarc(CI2, RI2, TB, TW), TW), ('L', TW, top_i), ('L', top_i, top_o)]
    return ch

# ---------- lower sheet outline (world x, z), plane |y| = 49 ----------
def _arc(P, pa, pb, r):
    s = 1 if pb > pa else -1
    c = (P[0] + s * r * math.cos(pa), P[1] + s * r * math.sin(pa))
    Q = (c[0] - s * r * math.cos(pb), c[1] - s * r * math.sin(pb))
    return c, Q, s


def flare(P0, phi0, xE, zV):
    """P0 heading phi0 (rad from straight down, + toward +x) -> arc RFL -> line -> arc RFL -> vertical at (xE, zV)."""
    def ep(phim):
        c1, Q1, s1 = _arc(P0, phi0, phim, RFL)
        _, QE, _ = _arc((0.0, 0.0), phim, 0.0, RFL)
        Q2 = (xE - QE[0], zV - QE[1])
        dx, dz = Q2[0] - Q1[0], Q2[1] - Q1[1]
        return math.atan2(dx, -dz) - phim, math.hypot(dx, dz), c1, Q1, Q2, s1
    N = 4000; prev = None; root = None
    for i in range(N + 1):
        p = -1.4 + 2.8 * i / N
        val, L = ep(p)[:2]
        ok = L > 0.5 and abs(val) < 1.0
        if prev is not None and ok and prev[2] and prev[1] * val <= 0:
            a, b = prev[0], p
            for _ in range(100):
                m = 0.5 * (a + b)
                if ep(a)[0] * ep(m)[0] <= 0: b = m
                else: a = m
            root = 0.5 * (a + b); break
        prev = (p, val, ok)
    if root is None:
        raise RuntimeError('flare infeasible')
    val, L, c1, Q1, Q2, s1 = ep(root)
    s2 = 1 if 0.0 > root else -1
    c2 = (Q2[0] + s2 * RFL * math.cos(root), Q2[1] + s2 * RFL * math.sin(root))
    m1 = (c1[0] - s1 * RFL * math.cos((phi0 + root) / 2), c1[1] - s1 * RFL * math.sin((phi0 + root) / 2))
    m2 = (c2[0] - s2 * RFL * math.cos(root / 2), c2[1] - s2 * RFL * math.sin(root / 2))
    return dict(phim=root, L=L, P0=P0, Q1=Q1, M1=m1, Q2=Q2, M2=m2, PE=(xE, zV), c1=c1, c2=c2, s1=s1, s2=s2, err=val)


def front_edge(P0, phi0, xE, zV):
    """front side: continue the radial line (heading phi0) for L, then one tangent arc (radius r) to vertical at (xE, zV)."""
    dx, dz = xE - P0[0], P0[1] - zV
    sp, cp = math.sin(phi0), math.cos(phi0)
    # L sp + r (1 - cp) = dx ; L cp + r sp = dz
    det = sp * sp - (1 - cp) * cp
    r = (sp * dz - cp * dx) / (sp * sp - cp * (1 - cp)) if False else None
    # solve linear system [[sp, 1-cp],[cp, sp]] [L, r] = [dx, dz]
    a11, a12, a21, a22 = sp, 1 - cp, cp, sp
    D = a11 * a22 - a12 * a21
    L = (dx * a22 - a12 * dz) / D
    r = (a11 * dz - a21 * dx) / D
    Q = (P0[0] + L * sp, P0[1] - L * cp)
    c = (Q[0] - r * cp, Q[1] - r * sp)        # turning toward -phi (s=-1): centre = Q - r(cos, sin)
    M = (c[0] + r * math.cos(phi0 / 2), c[1] + r * math.sin(phi0 / 2))
    E2 = (c[0] + r, c[1])
    return dict(L=L, r=r, Q=Q, M=M, PE=E2, c=c)


def radial_pt(th, r): return (-r * math.sin(th), r * math.cos(th))

def sheet_outline():
    """returns segments in (x,z) for the lower sheet, front side and rear side flares."""
    r_top, r_bot = R - VSHEET, R - VBOT
    F0 = radial_pt(TH0, r_bot); B0 = radial_pt(TH1, r_bot)
    zV = ZB - ZV_D
    fr = front_edge(F0, TH0, XC + HW, zV)
    rr = flare(B0, TH1, XC - HW, zV)
    return dict(Ftop=radial_pt(TH0, r_top), Btop=radial_pt(TH1, r_top), Tmid=radial_pt((TH0 + TH1) / 2, r_top),
                F0=F0, B0=B0, fr=fr, rr=rr)

if __name__ == '__main__':
    print('dL', dL, 'A', A)
    for n, p in [('E', E_), ('C2', C2), ('P2', P2), ('C3', C3), ('BELLY', BELLY), ('P3', P3), ('R3', (R3, 0)), ('C4', C4), ('P4', P4),
                 ('Q4', Q4), ('Q3', Q3), ('CI2', CI2), ('TW', TW), ('TB', TB), ('top_o', (uo(VCUT), VCUT)), ('top_i', (ui(VCUT), VCUT))]:
        print(f'{n:6s} {p[0]:.4f} {p[1]:.4f}')
    for v in (25, 30.5, 33, 35.6, 40, 51.945, 60, 70, 74):
        print(v, round(u_out(v), 3), round(u_in(v), 3), 'thk(u)', round(u_out(v) - u_in(v), 3))
    so = sheet_outline()
    for k, val in so.items():
        print(k, val)
