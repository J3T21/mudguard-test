"""Offline renders for mudguard v2.19.3: replaced wall band + revolved root + planar lower sheet."""
import os, sys, math
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import v2_19_3_geom as g
import v2_19_3_volume as vm

R = g.R
OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "out"))


def rn(s):
    a = s / R
    return np.array([-math.sin(a), 0.0, math.cos(a)])


def section(s, inner=False):
    """half section (u, v) list from the centreline over the top, down the wall to the bottom edge."""
    # blend C1 (82 wide, vertical walls, R18, flat +-23) -> leg zone (97, 19 deg, R6) over s 4..16
    t = min(1.0, max(0.0, (s - 4.0) / 12.0)) if s < 16 else 1.0
    t = t * t * (3 - 2 * t)
    def prof(a, alpha, r, d, wall):
        ca, sa = math.cos(math.radians(alpha)), math.sin(math.radians(alpha))
        cx = a - r * (1 - sa) / ca
        off = wall if inner else 0.0
        rr = r - off
        pts = [(0.0, off)] + [(cx + rr * math.sin(th), r - rr * math.cos(th)) for th in np.linspace(0, math.radians(90 - alpha), 8)]
        xe = a + d * sa / ca - off / ca
        pts.append((xe, d))
        return pts
    legp = prof(38.0, 19, 6, g.dL, 3.0)
    c1p = prof(41.0, 0, 18, 30.49, 3.0)
    n = max(len(legp), len(c1p))
    out = []
    for i in range(n):
        a = c1p[min(i, len(c1p) - 1)]; b = legp[min(i, len(legp) - 1)]
        out.append((a[0] * (1 - t) + b[0] * t, a[1] * (1 - t) + b[1] * t))
    return out


def guard_tris(s0, s1, inner=False, cut=True):
    ss = np.linspace(s0, s1, int((s1 - s0) / 2) + 2)
    tris = []
    for sg in (-1, 1):
        rows = []
        for s in ss:
            sec = section(s, inner)
            if cut and g.S0 <= s <= g.S1:
                # band removed below v 20.5
                kept = [p for p in sec if p[1] <= g.VCUT]
                last = sec[[i for i, p in enumerate(sec) if p[1] > g.VCUT][0]]
                prev = kept[-1]
                f = (g.VCUT - prev[1]) / (last[1] - prev[1])
                kept.append((prev[0] + f * (last[0] - prev[0]), g.VCUT))
                while len(kept) < len(sec):
                    kept.append(kept[-1])
                sec = kept
            rows.append([rn(s) * (R - v) + np.array([0, sg * u, 0]) for u, v in sec])
        for a, b in zip(rows, rows[1:]):
            for i in range(len(a) - 1):
                tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
    return tris


def chain_pts(ch, n=10):
    pts = []
    for seg in ch:
        if seg[0] == 'L':
            pts += [seg[1], seg[2]]
        else:
            pts += vm.arcpts(seg[1], seg[2], seg[3], n)
    return pts


def root_tris():
    outer = chain_pts(g.root_profile()[:5])
    ch = g.root_profile()
    inner = chain_pts(ch[6:11])
    tris = []
    ss = np.linspace(g.S0, g.S1, 30)
    for sg in (-1, 1):
        for path in (outer, inner):
            rows = [[rn(s) * (R - v) + np.array([0, sg * u, 0]) for u, v in path] for s in ss]
            for a, b in zip(rows, rows[1:]):
                for i in range(len(a) - 1):
                    tris += [[a[i], b[i], b[i + 1]], [a[i], b[i + 1], a[i + 1]]]
        # end faces: fan between outer and reversed inner
        allp = chain_pts(ch)
        for s in (g.S0, g.S1):
            P = [rn(s) * (R - v) + np.array([0, sg * u, 0]) for u, v in allp]
            c = np.mean(P, axis=0)
            for i in range(len(P) - 1):
                tris.append([c, P[i], P[i + 1]])
    return tris


def sheet_tris():
    pts = vm.SHEETPTS
    P = np.array(pts)
    tris = []
    from matplotlib.tri import Triangulation
    for sg in (-1, 1):
        for y in (49.0, 46.5):
            # ear-clipping via fan from a point inside works poorly for concave; use constrained grid fill instead
            xs = np.linspace(P[:, 0].min(), P[:, 0].max(), 60)
            zs = np.linspace(P[:, 1].min(), P[:, 1].max(), 90)
            X, Z = np.meshgrid(xs, zs)
            ins = vm.SHEETP.contains_points(np.c_[X.ravel(), Z.ravel()]).reshape(X.shape)
            for i in range(len(zs) - 1):
                for j in range(len(xs) - 1):
                    if ins[i, j] and ins[i + 1, j] and ins[i, j + 1] and ins[i + 1, j + 1]:
                        a = np.array([X[i, j], sg * y, Z[i, j]]); b = np.array([X[i, j + 1], sg * y, Z[i, j + 1]])
                        c = np.array([X[i + 1, j + 1], sg * y, Z[i + 1, j + 1]]); d = np.array([X[i + 1, j], sg * y, Z[i + 1, j]])
                        tris += [[a, b, c], [a, c, d]]
        # boundary band
        for i in range(len(P) - 1):
            a = np.array([P[i, 0], sg * 49.0, P[i, 1]]); b = np.array([P[i + 1, 0], sg * 49.0, P[i + 1, 1]])
            c = np.array([P[i + 1, 0], sg * 46.5, P[i + 1, 1]]); d = np.array([P[i, 0], sg * 46.5, P[i, 1]])
            tris += [[a, b, c], [a, c, d]]
    return tris


def add(ax, tris, color, alpha=1.0):
    v = np.asarray(tris, dtype=float)
    poly = Poly3DCollection(v, linewidths=0, alpha=alpha)
    n = np.cross(v[:, 1] - v[:, 0], v[:, 2] - v[:, 0])
    nn = np.linalg.norm(n, axis=1); nn[nn == 0] = 1
    light = np.array([0.3, 0.5, 0.8]); light /= np.linalg.norm(light)
    shade = np.clip(np.abs((n / nn[:, None]) @ light), 0.35, 1)
    base = np.array(matplotlib.colors.to_rgb(color))
    poly.set_facecolor(base * shade[:, None])
    ax.add_collection3d(poly)


def scene(ax, s0=-40, s1=150):
    add(ax, guard_tris(s0, s1, False), "#5c6b62")
    add(ax, guard_tris(s0, s1, True), "#4d5a52")
    add(ax, root_tris(), "#b79f7a")
    add(ax, sheet_tris(), "#c4b49a")


def save(path, elev, azim, title, lim, s0=-40, s1=150):
    fig = plt.figure(figsize=(11, 8), dpi=110)
    ax = fig.add_subplot(111, projection="3d")
    scene(ax, s0, s1)
    ax.view_init(elev=elev, azim=azim)
    ax.set_xlim(*lim[0]); ax.set_ylim(*lim[1]); ax.set_zlim(*lim[2])
    ax.set_box_aspect((lim[0][1] - lim[0][0], lim[1][1] - lim[1][0], lim[2][1] - lim[2][0]))
    ax.set_title(title)
    fig.tight_layout(); fig.savefig(path); plt.close(fig); print(path)


def section_plot(path):
    fig, axs = plt.subplots(1, 2, figsize=(12, 8), dpi=110)
    ax = axs[0]
    old, _ = vm.old_profile()
    ov = old.vertices
    ax.fill(ov[:, 0], ov[:, 1] - g.PAD, color="#cccccc", label="v2.19.2 sheet (before shave)")
    rp = np.array(chain_pts(g.root_profile(), 20))
    ax.fill(rp[:, 0], rp[:, 1], color="#b79f7a", label="v2.19.3 root (revolved)")
    vv = np.linspace(0, g.dL, 30)
    ax.fill(np.r_[[g.uo(v) for v in vv], [g.ui(v) for v in vv[::-1]]], np.r_[vv, vv[::-1]], color="#5c6b62", alpha=0.6, label="guard wall")
    ax.fill([46.5, 49, 49, 46.5], [72, 72, 110, 110], color="#c4b49a", alpha=0.8, label="v2.19.3 lower sheet")
    ax.axhline(g.VCUT, color="k", lw=0.6, ls=":"); ax.text(30.2, g.VCUT - 0.6, "band cut v 20.5", fontsize=8)
    ax.axvline(48.5, color="r", lw=0.6, ls="--"); ax.text(48.6, 12, "clipY 48.5", color="r", fontsize=8, rotation=90)
    ax.axvline(50.0, color="b", lw=0.6, ls="--"); ax.text(50.1, 100, "OD45 leg face 50.0", color="b", fontsize=8, rotation=90)
    th = np.linspace(-math.pi / 2, math.pi / 2, 50)
    ax.plot(72.5 - 27.5 * np.cos(th[th > -9]) * 0 + 72.5 - 27.5, np.linspace(0, 18.5, 50), color="#8aa0b4", lw=2, label="seal collar OD55 (|y| 45.0)")
    ax.set_xlim(30, 56); ax.set_ylim(112, -2); ax.set_aspect("equal")
    ax.set_xlabel("|y| (mm)"); ax.set_ylabel("v = depth below guard top R (mm)")
    ax.set_title("joint section at the leg (s 45)"); ax.legend(fontsize=7, loc="lower left")
    ax = axs[1]
    P = np.array(vm.SHEETPTS)
    ax.fill(P[:, 0], P[:, 1], color="#c4b49a", label="lower sheet outline (|y| 49)")
    for th_ in (g.TH0, g.TH1):
        ax.plot([-(R - 20.5) * math.sin(th_), -(R - 76) * math.sin(th_)], [(R - 20.5) * math.cos(th_), (R - 76) * math.cos(th_)], color="#b79f7a", lw=3)
    t = np.linspace(g.TH0, g.TH1, 40)
    for rr, lab in ((R - 20.5, "root top r=R-20.5"), (R - 76, "root bottom r=R-76"), (R - g.dL, "guard edge r=R-30.49")):
        ax.plot(-rr * np.sin(t), rr * np.cos(t), lw=1, label=lab)
    ax.axvline(g.XC + 28, color="grey", lw=0.5, ls=":"); ax.axvline(g.XC - 28, color="grey", lw=0.5, ls=":")
    ax.set_aspect("equal"); ax.invert_xaxis()
    ax.set_xlabel("x (mm, + forward)"); ax.set_ylabel("z (mm)")
    ax.set_title("side view of the joint (root s %.1f..%.0f)" % (g.S0, g.S1)); ax.legend(fontsize=7)
    fig.tight_layout(); fig.savefig(path); plt.close(fig); print(path)


def main():
    os.makedirs(OUT, exist_ok=True)
    whole = ((-180, 40), (-60, 80), (200, 420))
    xc, zb = g.XC, g.ZB
    root = ((xc - 40, xc + 40), (25, 60), (zb - 120, zb + 5))
    save(os.path.join(OUT, "v2.19.3_top.png"), 88, -90, "v2.19.3 top", whole, -165, 200)
    save(os.path.join(OUT, "v2.19.3_front.png"), 8, 180, "v2.19.3 front", whole, -165, 200)
    save(os.path.join(OUT, "v2.19.3_left.png"), 6, 90, "v2.19.3 left", whole, -165, 200)
    save(os.path.join(OUT, "v2.19.3_joint_outside.png"), 10, 75, "v2.19.3 leg joint close-up, leg side (+Y)", root, -10, 110)
    save(os.path.join(OUT, "v2.19.3_joint_inside.png"), 12, -115, "v2.19.3 leg joint close-up, tyre side", ((xc - 40, xc + 40), (20, 60), (zb - 120, zb + 5)), -10, 110)
    section_plot(os.path.join(OUT, "v2.19.3_joint_section.png"))


if __name__ == "__main__":
    main()
