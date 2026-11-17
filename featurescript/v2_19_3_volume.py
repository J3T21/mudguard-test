"""Voxel estimate of the B volume change v2.19.2 -> v2.19.3 in the leg-joint box (both sides)."""
import math, numpy as np
from matplotlib.path import Path
import v2_19_3_geom as g

R, dL, T19, C19 = g.R, g.dL, g.T19, g.C19
XC, ZTOP, ZB = g.XC, g.ZTOP, g.ZB
HW = 28.0; WT = 2.5; YO = 48.5; YI = YO - WT; PAD = 1.5
ZEDGE = ZTOP - dL
DTIP = 135 - PAD
ROWD = 125 - PAD; PITCH = 22.0

def arcpts(p, m, q, n=24):
    # circle through p, m, q
    ax, ay = p; bx, by = m; cx, cy = q
    d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by))
    ux = ((ax*ax+ay*ay)*(by-cy)+(bx*bx+by*by)*(cy-ay)+(cx*cx+cy*cy)*(ay-by))/d
    uy = ((ax*ax+ay*ay)*(cx-bx)+(bx*bx+by*by)*(ax-cx)+(cx*cx+cy*cy)*(bx-ax))/d
    r = math.hypot(ax-ux, ay-uy)
    a0 = math.atan2(ay-uy, ax-ux); a1 = math.atan2(by-uy, bx-ux); a2 = math.atan2(cy-uy, cx-ux)
    def norm(a): return (a + 2*math.pi) % (2*math.pi)
    # choose direction that passes through m
    d1 = norm(a1 - a0); d2 = norm(a2 - a0)
    sweep = d2 if d1 < d2 else d2 - 2*math.pi
    return [(ux + r*math.cos(a0 + sweep*t/n), uy + r*math.sin(a0 + sweep*t/n)) for t in range(n+1)]

def chain_to_poly(ch):
    pts = []
    for seg in ch:
        if seg[0] == 'L': pts += [seg[1], seg[2]]
        else: pts += arcpts(seg[1], seg[2], seg[3])
    return Path(np.array(pts))

# ---- old (v2.19.2) sheet profile (u, D) ----
def old_profile():
    bowR, bowRb, ang = 30.0, 40.0, math.radians(15.5); proud = 0.5; ov = 6
    Dbow0 = PAD + dL; Dtip = DTIP + PAD
    ca, sa, cha, sha = math.cos(ang), math.sin(ang), math.cos(ang/2), math.sin(ang/2)
    yo = YO; c1y = yo - bowR
    a1ey = c1y + bowR*ca; a1ed = Dbow0 + bowR*sa; a1my = c1y + bowR*cha; a1md = Dbow0 + bowR*sha
    c2y = a1ey + bowRb*ca; c2d = a1ed + bowRb*sa; a2my = c2y - bowRb; a2md = c2d
    a2ey = a1ey; a2ed = c2d + (c2d - a1ed); dend = a2ed + bowR*sa
    a3my = c1y + bowR*cha; a3md = dend - bowR*sha
    ri = bowR - WT; rj = bowRb + WT; scj = rj/bowRb
    i1my = c1y + ri*cha; i1md = Dbow0 + ri*sha
    j12y = c2y + (a1ey-c2y)*scj; j12d = c2d + (a1ed-c2d)*scj
    j23y = c2y + (a2ey-c2y)*scj; j23d = c2d + (a2ed-c2d)*scj
    i2my = c2y - rj; i3my = c1y + ri*cha; i3md = dend - ri*sha
    Dtop = Dbow0 - ov
    P = lambda y, d: (y, d)
    ch = [('L', P(yo+proud, Dtop), P(yo+proud, Dbow0)), ('L', P(yo+proud, Dbow0), P(yo, Dbow0)),
          ('A', P(yo, Dbow0), P(a1my, a1md), P(a1ey, a1ed)), ('A', P(a1ey, a1ed), P(a2my, a2md), P(a2ey, a2ed)),
          ('A', P(a2ey, a2ed), P(a3my, a3md), P(yo, dend)), ('L', P(yo, dend), P(yo+proud, dend)),
          ('L', P(yo+proud, dend), P(yo+proud, Dtip)), ('L', P(yo+proud, Dtip), P(yo-WT, Dtip)),
          ('L', P(yo-WT, Dtip), P(yo-WT, dend)), ('A', P(yo-WT, dend), P(i3my, i3md), P(j23y, j23d)),
          ('A', P(j23y, j23d), P(i2my, a2md), P(j12y, j12d)), ('A', P(j12y, j12d), P(i1my, i1md), P(yo-WT, Dbow0)),
          ('L', P(yo-WT, Dbow0), P(yo-WT, Dtop)), ('L', P(yo-WT, Dtop), P(yo+proud, Dtop))]
    bowA = yo - a2my
    return chain_to_poly(ch), bowA

OLDP, BOWA = old_profile()
ROOTP = chain_to_poly(g.root_profile())
IFILL = Path(np.array([[41.5, dL-3], [YO-3*T19-0.1, dL-3], [YO-0.5, dL-0.3], [YO-0.5, dL+0.5], [41.5, dL+0.5]]))

def sheet_outline_path():
    so = g.sheet_outline(); fr, rr = so['fr'], so['rr']
    zT = ZTOP - DTIP; rc = 8.0
    xF, xB = XC + HW, XC - HW
    c45 = math.cos(math.pi/4)
    top = arcpts(so['Btop'], so['Tmid'], so['Ftop'])          # rear -> front along r = R-72
    pts = top + [fr['Q']] + arcpts(fr['Q'], fr['M'], fr['PE'])[1:]
    pts += [(xF, zT + rc)] + arcpts((xF, zT+rc), (xF - rc + rc*c45, zT + rc - rc*c45), (xF - rc, zT))[1:]
    pts += [(xB + rc, zT)] + arcpts((xB + rc, zT), (xB + rc - rc*c45, zT + rc - rc*c45), (xB, zT + rc))[1:]
    pts += [rr['PE']] + arcpts(rr['PE'], rr['M2'], rr['Q2'])[1:] + [rr['Q1']] + arcpts(rr['Q1'], rr['M1'], rr['P0'])[1:] + [so['Btop']]
    return Path(np.array(pts)), pts

SHEETP, SHEETPTS = sheet_outline_path()

def uo(v): return g.A + v*T19
def ui(v): return g.A + v*T19 - g.OFF_IN

def slots(x, u, z, yi, yo):
    cut = np.zeros_like(x, dtype=bool)
    for sp in (-PITCH/2, PITCH/2):
        cut |= (np.abs(x - (XC - sp)) <= 1.2) & (u >= yi - 1) & (u <= yo + 1) & (np.abs(z - (ZTOP - ROWD)) <= 3)
    cut |= (np.abs(x - XC) <= PITCH/2) & (u >= yi - 1) & (u <= yi + 0.8) & (np.abs(z - (ZTOP - ROWD)) <= 2.75)
    return cut

def materials(x, u, z):
    r = np.hypot(x, z); v = R - r; th = np.arctan2(-x, z)
    Gout = (v >= 0) & (v <= dL) & (u <= uo(v))
    V = (v >= 3) & (v <= dL + 10) & (u <= ui(v))
    inX = (x >= XC - HW) & (x <= XC + HW)
    uv = np.stack([u.ravel(), v.ravel()], 1)
    # ---- old ----
    ifill = inX & IFILL.contains_points(uv).reshape(u.shape)
    uD = uo(v) - 0.05 / C19
    deep = inX & (u > 40) & (v >= 0.05) & (v <= dL + 12) & (u <= uD)
    pLow = (x >= XC-HW-1) & (x <= XC+HW+1) & (u < YI) & (z < ZEDGE - 4.8)
    deep &= ~pLow
    trim = (u > 40) & (v <= dL + 11.5) & ((v < 0.05) | (u > uD))
    m = (Gout | ifill | deep) & ~trim & (u <= YO)
    D = ZB - z
    sh = inX & OLDP.contains_points(np.stack([u.ravel(), D.ravel()], 1)).reshape(u.shape)
    # corner bites R8 at the tip corners
    zT = ZTOP - DTIP; rc = 8.0
    for xc0, sgn in ((XC + HW - rc, 1), (XC - HW + rc, -1)):
        corner = (sgn*(x - xc0) > 0) & (z < zT + rc) & (np.hypot(x - xc0, z - (zT + rc)) > rc)
        sh &= ~corner
    band = inX & (u >= YO - 7) & (u <= YO - 0.6) & (z >= ZEDGE + 0.4) & (z <= ZEDGE + 13)
    sh = (sh | band) & ~slots(x, u, z, YI, YO)
    m = (m | sh) & (u <= YO)
    K = inX & (u >= YI - BOWA - 1.5) & (z >= zT - 5) & (z <= ZEDGE + 3)
    old = m & ~(V & ~K)
    # ---- new ----
    win = (th >= g.TH0) & (th <= g.TH1)
    cut = win & (v >= 20.5) & (v <= 40) & (u >= 40)
    gn = Gout & (u <= YO) & ~V & ~cut
    root = win & ROOTP.contains_points(uv).reshape(u.shape)
    sheet = (u >= 46.5) & (u <= 49.0) & SHEETP.contains_points(np.stack([x.ravel(), z.ravel()], 1)).reshape(u.shape)
    sheet &= ~slots(x, u, z, 46.5, 49.0)
    new = gn | root | sheet
    return old, new

def run(dx=0.25, du=0.05, dz=0.25):
    xs = np.arange(-80, -10, dx) + dx/2
    us = np.arange(38, 56, du) + du/2
    zs = np.arange(250, 386, dz) + dz/2
    U, Z = np.meshgrid(us, zs, indexing='ij')
    vo = vn = 0
    for xv in xs:
        X = np.full_like(U, xv)
        o, n = materials(X, U, Z)
        vo += o.sum(); vn += n.sum()
    cell = dx*du*dz
    return vo*cell*2/1000, vn*cell*2/1000   # cm3, both sides

if __name__ == '__main__':
    import sys
    vo, vn = run(*( [float(a) for a in sys.argv[1:4]] if len(sys.argv) > 3 else []))
    print(f'box volume both sides: old {vo:.3f} cm3, new {vn:.3f} cm3, delta {vn-vo:+.3f} cm3')
