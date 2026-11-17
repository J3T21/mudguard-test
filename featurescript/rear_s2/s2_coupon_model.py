"""Offline voxel model of the S2 saddle coupons exactly as built by rear_s2_coupons.fs (simplified A geometry).
Frame: axle at origin, Y lateral, s = 0 at +Z (brace), +s toward +X. RO = 369.5."""
import math, numpy as np, json, sys
RO, RI = 369.5, 367.0
NECK_A, NECK_D = 28.0, 11.47
BR_LO, HB = 380.0, 20.0
VARS = {"S2-A": (0.5, 1.2, 0.4), "S2-B": (1.0, 1.2, 0.4), "S2-C": (1.5, 1.6, 0.4), "S2-D": (1.0, 0.8, 1.2)}
YS = [10, 13, 15.5, 18, 20.5, 23, 25.5, 28]
def hb(y):
    y = abs(y)
    if y <= 10: return 10.0
    u = min((y - 10) / 20.0, 1.0); return 17.5 - 7.5 * math.sqrt(max(0, 1 - u * u))
def poly_x(y, ys):  # piecewise-linear through sampled hb, as in the FS polygon
    y = abs(y); pts = [(v, hb(v)) for v in ys]
    if y <= pts[0][0]: return pts[0][1]
    for (y0, x0), (y1, x1) in zip(pts, pts[1:]):
        if y <= y1: return x0 + (x1 - x0) * (y - y0) / (y1 - y0)
    return pts[-1][1]
def po(z, c): return 30 + 0.3 * (380 - z) - c
def model(c, lip, flt, h=0.25):
    lf = BR_LO + HB + flt; top = lf + 3.4
    X = np.arange(-41, 41, h) + h / 2; Y = np.arange(-39, 39, h) + h / 2; Z = np.arange(352, 406, h) + h / 2
    XX, YY, ZZ = np.meshgrid(X, Y, Z, indexing="ij"); AY = np.abs(YY)
    r = np.hypot(XX, ZZ); s = np.arctan2(XX, ZZ) * RO
    yc = po(RO, c) - 0.5
    crown = (r >= RI) & (r <= RO) & (np.abs(s) <= 40) & (AY <= np.where(np.abs(s) <= 27, yc, 27.5))
    neck = (np.abs(s) >= 27.5) & (np.abs(s) <= 40) & (AY >= 25.5) & (AY <= 28) & (r >= RO - NECK_D) & (r <= 368.3)
    rib = (np.abs(s) >= 21.7) & (np.abs(s) <= 40) & (r >= 368.9) & (r <= 372) & (np.abs(AY - 22) <= 3.6 - (r - 368.9))
    pz = po(ZZ, c)
    panel = (np.abs(XX) <= 26) & (ZZ >= 354) & (ZZ <= 367.5) & (AY <= pz) & (AY >= pz - 4)
    pxv = np.vectorize(lambda v: poly_x(v, YS))(Y)[None, :, None]
    sad = (ZZ >= 368.6) & (ZZ <= 380) & (AY <= 27.5) & (np.abs(XX) <= pxv + c + 1)
    wall = (ZZ >= 368.0) & (ZZ <= top) & (AY <= 28) & (np.abs(XX) >= pxv + c) & (np.abs(XX) <= pxv + c + 3)
    xi = 10 + c; q = xi - np.abs(XX)  # >0 inside the recess
    lipz = (AY <= 6.5) & (ZZ >= lf) & (np.abs(XX) <= xi + 1) & (q <= lip) & (
        ZZ <= np.where(q > 0, lf + 1.2 + 1.2 * (lip - q) / lip, lf + 2.7))
    solid = crown | neck | rib | panel | sad | wall | lipz
    tie = (np.abs(AY - 10) <= 2.75) & (ZZ >= 378.2) & (ZZ <= 380.5)
    xo = pxv + c + 3
    groove = (np.abs(AY - 10) <= 2.75) & (ZZ >= 379.0) & (np.abs(XX) >= 12 + c) & (np.abs(XX) <= 16 + c)
    notch = (np.abs(AY - 10) <= 2.75) & (ZZ >= top - 1.3) & (np.abs(XX) >= 9 + c) & (np.abs(XX) <= 14 + c)
    slit = (np.abs(AY - 13.75) <= 0.5) & (ZZ >= 384) & (np.abs(XX) >= 5) & (np.abs(XX) <= 25)
    pin = pz - 4
    pslot = (np.abs(np.abs(XX) - 21) <= 1) & (ZZ >= 359.5) & (ZZ <= 365) & (AY >= pin - 1) & (AY <= pz + 1)
    prec = (np.abs(XX) <= 21.5) & (ZZ >= 359.0) & (ZZ <= 365.5) & (AY >= pin - 1) & (AY <= pin + 1.5)
    solid &= ~(tie | groove | notch | slit | pslot | prec)
    v = solid.sum() * h ** 3 / 1000
    xs, ys_, zs = XX[solid], YY[solid], ZZ[solid]
    return dict(vol=round(float(v), 2), x=(round(float(xs.min()), 1), round(float(xs.max()), 1)),
                y=(round(float(ys_.min()), 1), round(float(ys_.max()), 1)), z=(round(float(zs.min()), 1), round(float(zs.max()), 1)), top=top)
if __name__ == "__main__":
    out = {}
    for k, (c, lip, flt) in VARS.items():
        out[k] = model(c, lip, flt); print(k, c, lip, flt, out[k]); sys.stdout.flush()
    json.dump(out, open("/tmp/mgt/featurescript/rear_s2/s2_coupon_model.json", "w"), indent=1)
