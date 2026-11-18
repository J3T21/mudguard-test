FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// ---------- helpers (all scalar lengths are plain numbers in mm) ----------
function arcFrame(R is number, s is number, radialOff is number) returns map
{
    const ang = (s / R) * radian;
    const rn = vector(-sin(ang), 0, cos(ang));
    const t = vector(-cos(ang), 0, -sin(ang));
    const origin = rn * (R + radialOff) * millimeter;
    return { "origin" : origin, "rn" : rn, "t" : t, "ang" : ang,
             "plane" : plane(origin, t, vector(0, 1, 0)) };
}

// U profile, opening toward the tyre. Sketch x = lateral (world Y), sketch y = h = depth inward.
// a = top vertex half width, alpha = wall angle from vertical (deg), r = corner radius,
// t = inward offset (wall), hEnd = depth where the profile is closed.
function profSketch(context is Context, sid is Id, pl is Plane, a is number, alpha is number, r is number, t is number, hEnd is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const ca = cos(alpha * degree);
    const sa = sin(alpha * degree);
    const cx = a - r * (1 - sa) / ca;
    const rr = r - t;
    const wtx = cx + rr * ca;
    const wth = r - rr * sa;
    var md = vector(ca, -1 - sa);
    md = md / norm(md);
    const mx = cx + rr * md[0];
    const mh = r + rr * md[1];
    const xe = a + hEnd * sa / ca - t / ca;
    const mm = millimeter;
    skLineSegment(sk, "wl", { "start" : vector(-xe, hEnd) * mm, "end" : vector(-wtx, wth) * mm });
    skArc(sk, "al", { "start" : vector(-wtx, wth) * mm, "mid" : vector(-mx, mh) * mm, "end" : vector(-cx, t) * mm });
    skLineSegment(sk, "top", { "start" : vector(-cx, t) * mm, "end" : vector(cx, t) * mm });
    skArc(sk, "ar", { "start" : vector(cx, t) * mm, "mid" : vector(mx, mh) * mm, "end" : vector(wtx, wth) * mm });
    skLineSegment(sk, "wr", { "start" : vector(wtx, wth) * mm, "end" : vector(xe, hEnd) * mm });
    skLineSegment(sk, "cl", { "start" : vector(xe, hEnd) * mm, "end" : vector(-xe, hEnd) * mm });
    skSolve(sk);
}

// Fox AWL under-brace: circular crown (rise) tangent to R fillets, G1 flare to floorW.
// Sketch x = lateral, h toward tyre. Peak at h = -rise + t (outer t=0). From coupon A4.
function crownRadiusAwl(halfChord is number, rise is number, cornerR is number) returns number
{
    const hx = halfChord - cornerR;
    return (hx * hx + (rise + cornerR) * (rise + cornerR) - cornerR * cornerR) / (2 * rise);
}

function flarePhiAwl(halfS is number, halfF is number, r is number, depth is number) returns number
{
    // Returns φ in radians (plain number).
    const delta = halfF - halfS;
    if (abs(delta) < 1e-12)
        return 0;
    const A = delta + r;
    const B = depth - r;
    const M = sqrt(A * A + B * B);
    return (acos(r / M) - atan2(B, A)) / radian;
}

function arcMidPolar(cx is number, cy is number, r is number, a0rad is number, a1rad is number) returns Vector
{
    var d = a1rad - a0rad;
    while (d > PI)
        d -= 2 * PI;
    while (d < -PI)
        d += 2 * PI;
    const am = (a0rad + 0.5 * d) * radian;
    return vector(cx + r * cos(am), cy + r * sin(am));
}

function profSketchAwl(context is Context, sid is Id, pl is Plane, halfS is number, halfF is number, rise is number, r is number, t is number, hEnd is number)
{
    // Map coupon (x, y_up) -> FS (x, h=-y_up). Fillet centres at h = +r; peak at h = -rise.
    const Rc = crownRadiusAwl(halfS, rise, r);
    const ccy = Rc - rise; // crown centre in h
    const fx = halfS - r;
    const fcy = r; // fillet centre in h
    const phi = flarePhiAwl(halfS, halfF, r, hEnd); // radians, plain number
    const cosph = cos(phi * radian);
    const sinph = sin(phi * radian);
    const C = vector(0, ccy);
    // Inset by wall t: shrink radii; peak moves to -rise+t.
    const rr = r - t;
    const RcI = Rc - t;
    const fxI = fx;
    const fcyI = fcy;
    const lxI = -(fxI + rr * cosph);
    const lhI = fcyI - rr * sinph;
    const rxI = fxI + rr * cosph;
    const rhI = fcyI - rr * sinph;
    const FI = vector(-fxI, fcyI);
    const vI = FI - C;
    const tpI = C + vI * (RcI / norm(vI));
    const tpRI = vector(-tpI[0], tpI[1]);
    const halfFI = halfF - t / max(0.2, cosph);
    const hEndI = hEnd - t;
    const peak = vector(0, -rise + t);
    const aTpL = atan2(tpI[1] - fcyI, tpI[0] + fxI) / radian;
    const aTpR = atan2(tpRI[1] - fcyI, tpRI[0] - fxI) / radian;
    const midL = arcMidPolar(-fxI, fcyI, rr, PI - phi, aTpL);
    const midR = arcMidPolar(fxI, fcyI, rr, aTpR, phi);
    const mm = millimeter;
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    skLineSegment(sk, "wl", { "start" : vector(-halfFI, hEndI) * mm, "end" : vector(lxI, lhI) * mm });
    skArc(sk, "al", { "start" : vector(lxI, lhI) * mm, "mid" : midL * mm, "end" : tpI * mm });
    skArc(sk, "ac", { "start" : tpI * mm, "mid" : peak * mm, "end" : tpRI * mm });
    skArc(sk, "ar", { "start" : tpRI * mm, "mid" : midR * mm, "end" : vector(rxI, rhI) * mm });
    skLineSegment(sk, "wr", { "start" : vector(rxI, rhI) * mm, "end" : vector(halfFI, hEndI) * mm });
    skLineSegment(sk, "cl", { "start" : vector(halfFI, hEndI) * mm, "end" : vector(-halfFI, hEndI) * mm });
    skSolve(sk);
}

function buildStations(def is map) returns array
{
    const R = (def.ph1 - def.padThk) / millimeter;
    const fL = def.frontLen / millimeter;
    const tL = def.tailLen / millimeter;
    const w0 = def.wall / millimeter;
    const ww = def.waistWall / millimeter;
    // v2.6: near-constant width. W = guardWidth everywhere it passes the brace and legs (limited by the 85 mm leg gap
    // minus 2 x 1.5 mm); front blade and tail use frontWidth / tailWidth (default = W, no flare).
    const W = def.guardWidth / millimeter;
    const Wf = def.frontWidth / millimeter;
    const Wt = def.tailWidth / millimeter;
    const tA = tan(19 * degree);
    const dB = (W - def.ph5 / millimeter) / (2 * tA);        // wall depth so the top flat under the brace = PH5
    const WL = def.legZoneWidth / millimeter;                 // v2.8: guard walls out to the leg plane between the legs
    const dL = (WL - def.legZoneTopFlat / millimeter) / (2 * tA); // wider top flat behind the brace, 19 deg walls
    const lo = def.legTieOffset / millimeter;
    // [s, width, depth, alpha, cornerR, wall]
    var raw = [
        [-240, Wf, 28, 13, 12, w0],
        [-227.5, Wf, 30, 13.5, 11.5, w0],
        [-215, Wf, 32, 14, 11, w0],
        [-192.5, Wf, 33, 14.5, 10.5, w0],
        [-170, Wf, 34, 15, 10, w0],
        [-145, Wf, 34, 15, 10, w0],
        [-120, Wf, 34, 15, 10, w0],
        [-95, Wf, 34, 15, 10, w0],
        [-82.5, Wf, 34, 15, 10, w0],
        [-70, Wf, 34, 15, 10, w0],
        [-42.5, (Wf + W) / 2, (34 + dB) / 2, 17, 8, (w0 + ww) / 2],
        [-15, W, dB, 19, 6, ww],
        [0, W, dB, 19, 6, ww],
        [10, W, dB, 19, 6, ww],
        [16, (W + WL) / 2, (dB + dL) / 2, 19, 6, ww],
        [lo - 23, WL, dL, 19, 6, ww],
        [lo - 11, WL, dL, 19, 6, ww],
        [lo, WL, dL, 19, 6, ww],
        [lo + 11, WL, dL, 19, 6, ww],
        [lo + 23, WL, dL, 19, 6, ww],
        [lo + 40, Wt, 31, 19, 6, w0],
        [100, Wt, 28, 19, 6, w0],
        [135, Wt, 25, 19, 6, w0],
        [165, Wt, 22, 19, 6, w0]
    ];
    if (def.sculpted)
    {
        // v2.19.4: Fox AWL under-brace (coupon A4) at s -15..+4 — circular crown rise, R18, G1 flare
        // to floorW. Front/tail stay rounded-box U. Blend via s -35; leg zone from s 16.
        const dC = def.c1Depth / millimeter;
        const rC = def.c1Radius / millimeter;
        const aR = def.roundAlpha / degree;
        const rR = def.roundR / millimeter;
        const awlW = def.awlChord / millimeter;   // shoulder / R18 chord (94)
        raw = [
            [-240, Wf, 28, aR, rR, w0],
            [-227.5, Wf, 30, aR, rR, w0],
            [-215, Wf, 32, aR, rR, w0],
            [-192.5, Wf, 33, aR, rR, w0],
            [-170, Wf, 34, aR, rR, w0],
            [-145, Wf, 34, aR, rR, w0],
            [-120, Wf, 34, aR, rR, w0],
            [-95, Wf, 34, aR, rR, w0],
            [-82.5, Wf, 34, aR, rR, w0],
            [-70, Wf, 34, aR, rR, w0],
            [-55, Wf, 34, aR, rR, w0],
            [-35, 90, 30, 8, 14, (w0 + ww) / 2],
            // AWL brace saddle: width col = chord; floorW/rise carried via station flags below
            [-15, awlW, dC, 0, rC, ww],
            [0, awlW, dC, 0, rC, ww],
            [4, awlW, dC, 0, rC, ww],
            [16, WL, dL, 19, 6, ww],
            [lo - 11, WL, dL, 19, 6, ww],
            [lo, WL, dL, 19, 6, ww],
            [lo + 11, WL, dL, 19, 6, ww],
            [lo + 23, WL, dL, 19, 6, ww],
            [lo + 40, Wt, 31, 19, 6, w0],
            [100, Wt, 28, 19, 6, w0],
            [135, Wt, 25, 19, 6, w0],
            [165, Wt, 22, 19, 6, w0]
        ];
    }
    const awlRiseMm = def.sculpted ? def.awlRise / millimeter : 0;
    const awlFloorMm = def.sculpted ? def.awlFloorW / millimeter : 0;
    const kickLen = def.kickLen / millimeter;
    const sK = -fL + kickLen;
    var out = [];
    for (var row in raw)
    {
        var s = row[0];
        if (s <= -170)
            s = -fL + (s + 240); // tip zone + kicker stay anchored to the tip
        else if (s < -70)
            s = -70 + (s + 70) * (fL - 140) / 100; // 110 mm plateau absorbs the length change
        // flare (-70..-15) stays anchored to the brace
        else if (s > lo + 15 && row[0] >= 100)
            s = (lo + 15) + (s - 60) * (tL - (lo + 15)) / 105;
        var k = 0;
        if (s < sK)
        {
            const u = sK - s;
            k = u * u * tan(def.kickAngle) / (2 * kickLen);
        }
        if (size(out) > 0 && s - out[size(out) - 1].s < 8)
            continue; // drop stations squeezed together by a short front blade
        const a = row[1] / 2 - row[2] * tan(row[3] * degree);
        // AWL crown only under the brace saddle (s -15..+4 in the sculpted table)
        const useAwl = def.sculpted && awlRiseMm > 0 && s >= -15.5 && s <= 4.5;
        out = append(out, {
                    "s" : s, "w" : row[1], "d" : row[2], "alpha" : row[3], "r" : row[4], "wall" : row[5],
                    "a" : a, "k" : k, "R" : R,
                    "rise" : useAwl ? awlRiseMm : 0,
                    "floorW" : useAwl ? awlFloorMm : row[1]
                });
    }
    return out;
}

// ---------- v2.19.3 helpers ----------
function profMode(context is Context, sid is Id, pl is Plane, S is map, mode is string, bossH is number)
{
    if (S.rise > 0)
    {
        const halfS = S.w / 2;
        const halfF = S.floorW / 2;
        if (mode == "out")
            profSketchAwl(context, sid, pl, halfS, halfF, S.rise, S.r, 0, S.d);
        else if (mode == "in")
            profSketchAwl(context, sid, pl, halfS, halfF, S.rise, S.r, S.wall, S.d + 10);
        else
            profSketchAwl(context, sid, pl, halfS, halfF, S.rise, S.r, -bossH, S.d - 0.5);
    }
    else if (mode == "out")
        profSketch(context, sid, pl, S.a, S.alpha, S.r, 0, S.d);
    else if (mode == "in")
        profSketch(context, sid, pl, S.a, S.alpha, S.r, S.wall, S.d + 10);
    else
        profSketch(context, sid, pl, S.a, S.alpha, S.r, -bossH, S.d - 0.5); // v2.19.3.1: boss closes 0.5 above the guard's closing line
}

// Loft through st. endMode "end": the last section leaves normal to its plane; "start": the first one does.
function loftPart(context is Context, id is Id, st is array, mode is string, bossH is number, endMode is string)
{
    var profs = [];
    for (var i = 0; i < size(st); i += 1)
    {
        const S = st[i];
        const fr = arcFrame(S.R, S.s, S.k);
        profMode(context, id + ("p" ~ i), fr.plane, S, mode, bossH);
        profs = append(profs, qSketchRegion(id + ("p" ~ i)));
    }
    const iD = endMode == "end" ? size(st) - 1 : 0;
    const SD = st[iD];
    const frD = arcFrame(SD.R, SD.s, SD.k);
    opLoft(context, id + "loft", { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID,
                "derivativeInfo" : [{ "profileIndex" : iD, "vector" : frD.t, "magnitude" : 1 }] });
    opDeleteBodies(context, id + "delsk", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
}

// v2.19.3: guard solid in three pieces. Front loft up to the first leg-zone station sR0, the leg-zone section
// revolved about the axle from sR0 to sR1 (exact, so the joint band cut and root sit on true surfaces), and the
// tail loft from sR1. Both lofts meet the revolve normal to the shared section plane (G1).
function guardSolid(context is Context, id is Id, st is array, mode is string, bossH is number, sR0 is number, sR1 is number) returns Query
{
    var fr = [];
    var tl = [];
    var leg = undefined;
    for (var S in st)
    {
        if (S.s <= sR0 + 1e-9)
            fr = append(fr, S);
        if (abs(S.s - sR0) < 1e-6)
            leg = S;
        if (S.s > sR1 + 0.5)
            tl = append(tl, S);
    }
    if (leg == undefined)
        throw regenError("v2.19.3: no leg-zone station at s=" ~ sR0);
    var legT = leg;
    legT.s = sR1;
    tl = concatenateArrays([[legT], tl]);
    loftPart(context, id + "f", fr, mode, bossH, "end");
    profMode(context, id + "rsk", arcFrame(leg.R, sR0, leg.k).plane, leg, mode, bossH);
    opRevolve(context, id + "rv", { "entities" : qSketchRegion(id + "rsk"), "axis" : line(vector(0, 0, 0) * millimeter, vector(0, -1, 0)),
                "angleForward" : ((sR1 - sR0) / leg.R) * radian });
    opDeleteBodies(context, id + "rdel", { "entities" : qCreatedBy(id + "rsk", EntityType.BODY) });
    loftPart(context, id + "t", tl, mode, bossH, "start");
    opBoolean(context, id + "u", { "tools" : qUnion([qCreatedBy(id + "f", EntityType.BODY), qCreatedBy(id + "rv", EntityType.BODY), qCreatedBy(id + "t", EntityType.BODY)]),
                "operationType" : BooleanOperationType.UNION });
    const res = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID); // v2.19.3.1: history query, survives later splits
    if (size(evaluateQuery(context, res)) != 1)
        throw regenError("v2.19.3 guardSolid " ~ mode ~ ": " ~ size(evaluateQuery(context, res)) ~ " bodies after union");
    return res;
}

// mid point of the shorter arc from p to q on the circle (c, r); 2D number vectors
function arcMid2(c is Vector, r is number, p is Vector, q is Vector) returns Vector
{
    const m = (p + q) / 2 - c;
    return c + m * (r / norm(m));
}

// closed chain of ["L", p, q] / ["A", p, mid, q] entries (2D numbers, mm); sketch x is multiplied by sx
function chainSketch(context is Context, sid is Id, pl is Plane, chain is array, sx is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    for (var i = 0; i < size(chain); i += 1)
    {
        const e = chain[i];
        const p = vector(sx * e[1][0], e[1][1]) * millimeter;
        if (e[0] == "L")
        {
            skLineSegment(sk, "e" ~ i, { "start" : p, "end" : vector(sx * e[2][0], e[2][1]) * millimeter });
        }
        else
        {
            skArc(sk, "e" ~ i, { "start" : p, "mid" : vector(sx * e[2][0], e[2][1]) * millimeter, "end" : vector(sx * e[3][0], e[3][1]) * millimeter });
        }
    }
    skSolve(sk);
}

// Rear edge of the lower sheet: from p0 heading phi0 (rad from straight down, + toward +x): R rf arc to heading phim,
// straight line, R rf arc back to straight down, ending at (xE, zV). err = 0 when the line heading equals phim.
function flareFit(p0 is Vector, phi0 is number, phim is number, xE is number, zV is number, rf is number) returns map
{
    const s1 = phim > phi0 ? 1 : -1;
    const c1 = vector(p0[0] + s1 * rf * cos(phi0 * radian), p0[1] + s1 * rf * sin(phi0 * radian));
    const q1 = vector(c1[0] - s1 * rf * cos(phim * radian), c1[1] - s1 * rf * sin(phim * radian));
    const s2 = phim < 0 ? 1 : -1;
    const qe = vector(s2 * rf * cos(phim * radian) - s2 * rf, s2 * rf * sin(phim * radian));
    const q2 = vector(xE - qe[0], zV - qe[1]);
    const c2 = vector(q2[0] + s2 * rf * cos(phim * radian), q2[1] + s2 * rf * sin(phim * radian));
    const dx = q2[0] - q1[0];
    const dz = q2[1] - q1[1];
    const pm1 = 0.5 * (phi0 + phim);
    const pm2 = 0.5 * phim;
    return { "err" : atan2(dx, -dz) / radian - phim, "L" : sqrt(dx * dx + dz * dz), "q1" : q1, "q2" : q2,
             "m1" : vector(c1[0] - s1 * rf * cos(pm1 * radian), c1[1] - s1 * rf * sin(pm1 * radian)),
             "m2" : vector(c2[0] - s2 * rf * cos(pm2 * radian), c2[1] - s2 * rf * sin(pm2 * radian)),
             "phim" : phim };
}


function polySketch(context is Context, sid is Id, pl is Plane, pts is array)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const n = size(pts);
    for (var i = 0; i < n; i += 1)
        skLineSegment(sk, "l" ~ i, { "start" : pts[i] * millimeter, "end" : pts[(i + 1) % n] * millimeter });
    skSolve(sk);
}

// stadium (slot) centred at (cx, cy) in sketch coords, long axis along sketch x
function stadiumSketch(context is Context, sid is Id, pl is Plane, cx is number, cy is number, len is number, wid is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const r = wid / 2;
    const hl = len / 2 - r;
    const mm = millimeter;
    skLineSegment(sk, "l1", { "start" : vector(cx - hl, cy - r) * mm, "end" : vector(cx + hl, cy - r) * mm });
    skArc(sk, "a1", { "start" : vector(cx + hl, cy - r) * mm, "mid" : vector(cx + hl + r, cy) * mm, "end" : vector(cx + hl, cy + r) * mm });
    skLineSegment(sk, "l2", { "start" : vector(cx + hl, cy + r) * mm, "end" : vector(cx - hl, cy + r) * mm });
    skArc(sk, "a2", { "start" : vector(cx - hl, cy + r) * mm, "mid" : vector(cx - hl - r, cy) * mm, "end" : vector(cx - hl, cy - r) * mm });
    skSolve(sk);
}

// teardrop hole, centre (cx, cy), radius r, tip direction unit 2D vector dv
function teardropSketch(context is Context, sid is Id, pl is Plane, cx is number, cy is number, r is number, dv is Vector)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const mm = millimeter;
    const c = vector(cx, cy);
    const c45 = cos(45 * degree);
    const s45 = sin(45 * degree);
    const d1 = vector(dv[0] * c45 - dv[1] * s45, dv[0] * s45 + dv[1] * c45);
    const d2 = vector(dv[0] * c45 + dv[1] * s45, -dv[0] * s45 + dv[1] * c45);
    const p1 = c + d1 * r;
    const p2 = c + d2 * r;
    const tip = c + dv * (r * sqrt(2));
    const mid = c - dv * r;
    skArc(sk, "arc", { "start" : p1 * mm, "mid" : mid * mm, "end" : p2 * mm });
    skLineSegment(sk, "l1", { "start" : p2 * mm, "end" : tip * mm });
    skLineSegment(sk, "l2", { "start" : tip * mm, "end" : p1 * mm });
    skSolve(sk);
}

function extrudeSk(context is Context, eid is Id, sid is Id, dir is Vector, depth is number) returns Query
{
    opExtrude(context, eid, { "entities" : qSketchRegion(sid), "direction" : dir,
                "endBound" : BoundingType.BLIND, "endDepth" : depth * millimeter });
    return qCreatedBy(eid, EntityType.BODY);
}

function mkBox(context is Context, bid is Id, c1 is Vector, c2 is Vector) returns Query
{
    const lo = vector(min(c1[0], c2[0]), min(c1[1], c2[1]), min(c1[2], c2[2]));
    const hi = vector(max(c1[0], c2[0]), max(c1[1], c2[1]), max(c1[2], c2[2]));
    fCuboid(context, bid, { "corner1" : lo * millimeter, "corner2" : hi * millimeter });
    return qCreatedBy(bid, EntityType.BODY);
}


function bodyAt(context is Context, p is Vector) returns Query
{
    return qContainsPoint(qAllModifiableSolidBodies(), p);
}

function fmtBox(b is Box3d) returns string
{
    const d = (b.maxCorner - b.minCorner) / millimeter;
    const a = b.minCorner / millimeter;
    const c = b.maxCorner / millimeter;
    return "dims(mm) X=" ~ roundToPrecision(d[0], 1) ~ " Y=" ~ roundToPrecision(d[1], 1) ~ " Z=" ~ roundToPrecision(d[2], 1) ~
        " min=(" ~ roundToPrecision(a[0], 1) ~ "," ~ roundToPrecision(a[1], 1) ~ "," ~ roundToPrecision(a[2], 1) ~ ") max=(" ~
        roundToPrecision(c[0], 1) ~ "," ~ roundToPrecision(c[1], 1) ~ "," ~ roundToPrecision(c[2], 1) ~ ")";
}

// Lay the part on its side (world Y vertical) and rotate in the XZ plane; find the rotation
// minimising the larger in-plane box side.
// Lay the part on its side (world Y vertical) and rotate it in the XZ plane (i.e. on the bed).
// Reports the as-modelled side footprint (ang 0), the minimum-area footprint and the smallest
// square it fits in (for diagonal placement on the 256 x 256 bed).
// Two tyre envelopes (torus about the axle, Y axis): [max-width tyre, actual tyre]; crown radius = tyreRadius.
function makeTyres(context is Context, tid is Id, definition is map) returns array
{
    var out = [];
    var i = 0;
    for (var W in [definition.maxTyreWidth / millimeter, definition.tyreWidth / millimeter])
    {
        const r = W / 2;
        const zc = definition.tyreRadius / millimeter - r;
        const pl = plane(vector(0, 0, 0) * millimeter, vector(1, 0, 0), vector(0, 1, 0));
        const sk = newSketchOnPlane(context, tid + ("sk" ~ i), { "sketchPlane" : pl });
        skCircle(sk, "c", { "center" : vector(0, zc) * millimeter, "radius" : r * millimeter });
        skSolve(sk);
        opRevolve(context, tid + ("rv" ~ i), { "entities" : qSketchRegion(tid + ("sk" ~ i)),
                    "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), "angleForward" : 360 * degree });
        out = append(out, qCreatedBy(tid + ("rv" ~ i), EntityType.BODY));
        i += 1;
    }
    return out;
}

function sideFit(context is Context, q is Query) returns map
{
    var bestSq = { "m" : inf };
    var bestArea = { "a" : inf };
    var aligned = {};
    for (var ang = 0; ang < 180; ang += 3)
    {
        const xa = vector(cos(ang * degree), 0, sin(ang * degree));
        const cs = coordSystem(vector(0, 0, 0) * millimeter, xa, vector(0, 1, 0));
        const b = evBox3d(context, { "topology" : q, "cSys" : cs, "tight" : true });
        const d = (b.maxCorner - b.minCorner) / millimeter;
        const L = roundToPrecision(max(d[0], d[1]), 1);
        const W = roundToPrecision(min(d[0], d[1]), 1);
        const H = roundToPrecision(d[2], 1);
        if (ang == 0)
            aligned = { "L" : L, "W" : W, "H" : H };
        if (max(d[0], d[1]) < bestSq.m)
            bestSq = { "m" : max(d[0], d[1]), "ang" : ang, "L" : L, "W" : W, "H" : H };
        if (d[0] * d[1] < bestArea.a)
            bestArea = { "a" : d[0] * d[1], "ang" : ang, "L" : L, "W" : W, "H" : H };
    }
    return { "asModelled" : aligned,
             "minArea" : { "ang" : bestArea.ang, "L" : bestArea.L, "W" : bestArea.W, "H" : bestArea.H },
             "minSquare" : { "ang" : bestSq.ang, "L" : bestSq.L, "W" : bestSq.W, "H" : bestSq.H } };
}

// Measured lower legs (James, v2.5): inner-face half-gap = legGapUpper/2 at legTaperTop, PH2/2 at legTaperEnd and below,
// linear between and extrapolated above; centre |y| = PH2/2 + PH6/2; axis along Z through (xC, +/-yLeg). grow = radial offset.
function makeLegs(context is Context, lid is Id, definition is map, xC is number, zB is number, grow is number, sides is array) returns array
{
    const mm = millimeter;
    const dT0 = definition.legTaperTop / mm;
    const dT1 = definition.legTaperEnd / mm;
    const yLeg = definition.ph2 / mm / 2 + definition.ph6 / mm / 2;
    const r1 = definition.ph6 / mm / 2 + grow;
    const r0 = yLeg - definition.legGapTop / mm / 2 + grow;       // radius at D = 0 (legGapTop guess)
    const rU = yLeg - definition.legGapUpper / mm / 2 + grow;     // radius at legTaperTop (measured 85 gap @ 40)
    var out = [];
    for (var sg in sides)
    {
        const id2 = lid + ("L" ~ (sg + 1));
        const lpl = plane(vector(xC, sg * yLeg, 0) * mm, vector(1, 0, 0), vector(0, 1, 0));
        const lsk = newSketchOnPlane(context, id2 + "sk", { "sketchPlane" : lpl });
        skPolyline(lsk, "pl", { "points" : [vector(0, zB) * mm, vector(r0, zB) * mm, vector(rU, zB - dT0) * mm, vector(r1, zB - dT1) * mm,
                    vector(r1, zB - 200) * mm, vector(0, zB - 200) * mm, vector(0, zB) * mm] });
        skSolve(lsk);
        opRevolve(context, id2 + "rv", { "entities" : qSketchRegion(id2 + "sk"), "axis" : line(vector(xC, sg * yLeg, 0) * mm, vector(0, 0, 1)), "angleForward" : 360 * degree });
        out = append(out, qCreatedBy(id2 + "rv", EntityType.BODY));
    }
    return out;
}

// v2.15 plan-view end round. intoSign +1 when increasing s goes into the part (front tip);
// -1 at the rear end. Subtracts an R32 corner crescent from `target` on both sides, then a
// 1 mm 45 deg chamfer on the remaining straight end lip (outer and inner top).
// Sketch x = u into the part, sketch y * intoSign = world Y. Extrude is radial (inward).
function v215EndRound(context is Context, id is Id, R is number, stEnd is map, intoSign is number, target is Query)
{
    const mm = millimeter;
    const rc = 32;
    const ySide = stEnd.w / 2 + 0.5; // v2.19.3.1: arc tangent 0.5 outside the side edge and 0.5 beyond the end face (du)
    const du = -0.5;
    const yc = ySide - rc;
    const fr = arcFrame(R, stEnd.s, stEnd.k);
    const into = fr.t * intoSign;
    const rn = fr.rn;
    const Y = vector(0, 1, 0);
    const o = intoSign;
    for (var side in [1, -1])
    {
        const sid = id + ("sk" ~ (side + 1));
        const sk = newSketchOnPlane(context, sid, { "sketchPlane" : plane(fr.origin + rn * 6 * mm, -rn, into) });
        const yOut = o * side;
        skLineSegment(sk, "l1", { "start" : vector(-6 + du, yOut * (ySide + 10)) * mm, "end" : vector(-6 + du, yOut * (yc - 4)) * mm });
        skLineSegment(sk, "l2", { "start" : vector(-6 + du, yOut * (yc - 4)) * mm, "end" : vector(du, yOut * (yc - 4)) * mm });
        skLineSegment(sk, "l3", { "start" : vector(du, yOut * (yc - 4)) * mm, "end" : vector(du, yOut * yc) * mm });
        const midU = rc * (1 - 1 / sqrt(2));
        const midY = yc + rc / sqrt(2);
        skArc(sk, "a", { "start" : vector(du, yOut * yc) * mm, "mid" : vector(midU + du, yOut * midY) * mm, "end" : vector(rc + du, yOut * ySide) * mm });
        skLineSegment(sk, "l4", { "start" : vector(rc + du, yOut * ySide) * mm, "end" : vector(rc + du, yOut * (ySide + 10)) * mm });
        skLineSegment(sk, "l5", { "start" : vector(rc + du, yOut * (ySide + 10)) * mm, "end" : vector(-6 + du, yOut * (ySide + 10)) * mm });
        skSolve(sk);
        opExtrude(context, id + ("x" ~ (side + 1)), { "entities" : qSketchRegion(sid), "direction" : -rn,
                    "endBound" : BoundingType.BLIND, "endDepth" : 50 * mm });
    }
    opBoolean(context, id + "cut", { "targets" : target,
                "tools" : qUnion([qCreatedBy(id + "x0", EntityType.BODY), qCreatedBy(id + "x2", EntityType.BODY)]),
                "operationType" : BooleanOperationType.SUBTRACTION });
    // re-query from a point that stays inside the part (the corner cut is only at the end)
    const sAnch = intoSign > 0 ? -100 : 100;
    const tgt2 = qContainsPoint(qAllModifiableSolidBodies(), arcFrame(R, sAnch, -1.2).origin);

    // straight lip only: |y| <= yc - 0.5, and inside the top flat so the 1 mm chamfer cannot meet the arc
    const ca = cos(stEnd.alpha * degree);
    const sa = sin(stEnd.alpha * degree);
    const cx = stEnd.a - stEnd.r * (1 - sa) / ca;
    const yLim = min(yc - 0.5, cx - 1);
    const wt = stEnd.wall;
    const lipChamfer = false; // v2.16: lip chamfer removed (James, t57u); R32 corners unchanged
    if (lipChamfer && yLim > 2)
    {
        const plC = plane(fr.origin - Y * yLim * mm, -o * Y, into);
        const skO = newSketchOnPlane(context, id + "sko", { "sketchPlane" : plC });
        skLineSegment(skO, "a", { "start" : vector(0, 0) * mm, "end" : vector(1, 0) * mm });
        skLineSegment(skO, "b", { "start" : vector(1, 0) * mm, "end" : vector(0, 1) * mm });
        skLineSegment(skO, "c", { "start" : vector(0, 1) * mm, "end" : vector(0, 0) * mm });
        skSolve(skO);
        opExtrude(context, id + "xo", { "entities" : qSketchRegion(id + "sko"), "direction" : Y,
                    "endBound" : BoundingType.BLIND, "endDepth" : (2 * yLim) * mm });
        const skI = newSketchOnPlane(context, id + "ski", { "sketchPlane" : plC });
        skLineSegment(skI, "a", { "start" : vector(0, wt) * mm, "end" : vector(1, wt) * mm });
        skLineSegment(skI, "b", { "start" : vector(1, wt) * mm, "end" : vector(0, wt - 1) * mm });
        skLineSegment(skI, "c", { "start" : vector(0, wt - 1) * mm, "end" : vector(0, wt) * mm });
        skSolve(skI);
        opExtrude(context, id + "xi", { "entities" : qSketchRegion(id + "ski"), "direction" : Y,
                    "endBound" : BoundingType.BLIND, "endDepth" : (2 * yLim) * mm });
        opBoolean(context, id + "ch", { "targets" : tgt2,
                    "tools" : qUnion([qCreatedBy(id + "xo", EntityType.BODY), qCreatedBy(id + "xi", EntityType.BODY)]),
                    "operationType" : BooleanOperationType.SUBTRACTION });
    }
}

function v215Pt(fr is map, into is Vector, u is number, yw is number, h is number) returns Vector
{
    return fr.origin + into * (u * millimeter) + vector(0, yw, 0) * millimeter - fr.rn * (h * millimeter);
}

// One end: corner point at u=2 must be gone, u=26 kept, arc radius ~32, chamfer hit u~0.7, land u~0.
function v215Probe(context is Context, qE is Query, stE is map, sgn is number, yP is number, hP is number, R is number) returns map
{
    const mm = millimeter;
    const frE = arcFrame(R, stE.s, stE.k);
    const intoE = frE.t * sgn;
    var nGone = 0;
    var nKept = 0;
    if (size(evaluateQuery(context, qContainsPoint(qAllModifiableSolidBodies(), v215Pt(frE, intoE, 2, yP, hP)))) == 0)
        nGone = 1;
    if (size(evaluateQuery(context, qContainsPoint(qAllModifiableSolidBodies(), v215Pt(frE, intoE, 26, yP, hP)))) == 1)
        nKept = 1;
    const hits = evRaycast(context, { "entities" : qE, "ray" : line(v215Pt(frE, intoE, -3, yP, hP), intoE), "closest" : true });
    var uHit = -1;
    if (size(hits) > 0)
        uHit = -3 + hits[0].distance / mm;
    const yC = stE.w / 2 - 32;
    const rad = sqrt((uHit - 32) * (uHit - 32) + (yP - yC) * (yP - yC));
    const hC = evRaycast(context, { "entities" : qE, "ray" : line(v215Pt(frE, intoE, -3, 0, 0.3), intoE), "closest" : true });
    var uC = -1;
    if (size(hC) > 0)
        uC = -3 + hC[0].distance / mm;
    const hL = evRaycast(context, { "entities" : qE, "ray" : line(v215Pt(frE, intoE, -3, 0, 1.2), intoE), "closest" : true });
    var uL = -1;
    if (size(hL) > 0)
        uL = -3 + hL[0].distance / mm;
    const tag = sgn > 0 ? "front" : "rear";
    return { "gone" : nGone, "kept" : nKept,
             "text" : tag ~ " arc u=" ~ roundToPrecision(uHit, 2) ~ " R=" ~ roundToPrecision(rad, 2) ~
                 " chamfer u=" ~ roundToPrecision(uC, 2) ~ " land u=" ~ roundToPrecision(uL, 2) };
}

// Band between two outward (brace-side) offsets of the AWL crown, clipped to |x|<=halfW.
// Sketch: x = lateral, y toward tyre; peak at y = -rise. Extrude along flange tangent afterwards.
function crownBandSketch(context is Context, sid is Id, pl is Plane, halfW is number, ccy is number, Rc is number, d0 is number, d1 is number, rise is number)
{
    const mm = millimeter;
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    if (rise < 1e-6 || Rc > 1e5)
    {
        // Flat-top fallback (legacy): y negative = above outer top
        skLineSegment(sk, "a", { "start" : vector(-halfW, -d1) * mm, "end" : vector(halfW, -d1) * mm });
        skLineSegment(sk, "b", { "start" : vector(halfW, -d1) * mm, "end" : vector(halfW, -d0) * mm });
        skLineSegment(sk, "c", { "start" : vector(halfW, -d0) * mm, "end" : vector(-halfW, -d0) * mm });
        skLineSegment(sk, "d", { "start" : vector(-halfW, -d0) * mm, "end" : vector(-halfW, -d1) * mm });
        skSolve(sk);
        return;
    }
    const R0 = Rc + d0;
    const R1 = Rc + d1;
    // Clamp halfW so arcs stay real
    const hw = min(halfW, R0 * 0.98);
    const y0L = ccy - sqrt(max(1e-6, R0 * R0 - hw * hw));
    const y0R = y0L;
    const y1L = ccy - sqrt(max(1e-6, R1 * R1 - hw * hw));
    const y1R = y1L;
    const mid0 = vector(0, ccy - R0); // peak of inner offset
    const mid1 = vector(0, ccy - R1);
    skArc(sk, "inner", { "start" : vector(-hw, y0L) * mm, "mid" : mid0 * mm, "end" : vector(hw, y0R) * mm });
    skLineSegment(sk, "re", { "start" : vector(hw, y0R) * mm, "end" : vector(hw, y1R) * mm });
    skArc(sk, "outer", { "start" : vector(hw, y1R) * mm, "mid" : mid1 * mm, "end" : vector(-hw, y1L) * mm });
    skLineSegment(sk, "le", { "start" : vector(-hw, y1L) * mm, "end" : vector(-hw, y0L) * mm });
    skSolve(sk);
}

annotation { "Feature Type Name" : "Mudguard V2.19.4 (Fox AWL 36)" }
export const mudguardV2 = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "PH1 axle to brace underside" }
        isLength(definition.ph1, { (millimeter) : [300, 395, 500] } as LengthBoundSpec);
        annotation { "Name" : "PH2 inner leg face-to-face (at/below taper end, measured 100 @ 90 below brace)" }
        isLength(definition.ph2, { (millimeter) : [80, 100, 140] } as LengthBoundSpec);
        annotation { "Name" : "PH3 brace fore-aft depth" }
        isLength(definition.ph3, { (millimeter) : [10, 20, 40] } as LengthBoundSpec);
        annotation { "Name" : "PH4 brace back-face height" }
        isLength(definition.ph4, { (millimeter) : [10, 25, 60] } as LengthBoundSpec);
        annotation { "Name" : "PH5 brace underside flat (top flat width)" }
        isLength(definition.ph5, { (millimeter) : [40, 60, 70] } as LengthBoundSpec);
        annotation { "Name" : "PH6 lower-leg OD at the wing ties (measured 45 @ 90 below brace)" }
        isLength(definition.ph6, { (millimeter) : [30, 45, 60] } as LengthBoundSpec);
        annotation { "Name" : "Inner leg face-to-face at taper top (measured 85 @ 40 below brace)" }
        isLength(definition.legGapUpper, { (millimeter) : [60, 85, 140] } as LengthBoundSpec);
        annotation { "Name" : "Leg taper top, below brace underside" }
        isLength(definition.legTaperTop, { (millimeter) : [0, 40, 100] } as LengthBoundSpec);
        annotation { "Name" : "Leg taper end, below brace underside" }
        isLength(definition.legTaperEnd, { (millimeter) : [20, 90, 150] } as LengthBoundSpec);
        annotation { "Name" : "Inner leg face-to-face at brace underside (D=0, guess: = gap at taper top)" }
        isLength(definition.legGapTop, { (millimeter) : [60, 85, 140] } as LengthBoundSpec);
        annotation { "Name" : "Guard width in the leg zone (walls in the lower-leg plane, seal bulge through windows)" }
        isLength(definition.legZoneWidth, { (millimeter) : [70, 97, 110] } as LengthBoundSpec);
        annotation { "Name" : "Top flat width in the leg zone" }
        isLength(definition.legZoneTopFlat, { (millimeter) : [50, 76, 90] } as LengthBoundSpec);
        annotation { "Name" : "Guard width under the brace" }
        isLength(definition.guardWidth, { (millimeter) : [66, 82, 100] } as LengthBoundSpec);
        annotation { "Name" : "Front blade width" }
        isLength(definition.frontWidth, { (millimeter) : [66, 97, 130] } as LengthBoundSpec);
        annotation { "Name" : "Tail width" }
        isLength(definition.tailWidth, { (millimeter) : [66, 97, 130] } as LengthBoundSpec);
        annotation { "Name" : "PH7 brace tilt (not yet used)" }
        isAngle(definition.ph7, { (degree) : [-30, 0, 30] } as AngleBoundSpec);
        annotation { "Name" : "Tyre radius" }
        isLength(definition.tyreRadius, { (millimeter) : [300, 375, 420] } as LengthBoundSpec);
        annotation { "Name" : "Max tyre width (Fox spec)" }
        isLength(definition.maxTyreWidth, { (millimeter) : [40, 69.2, 90] } as LengthBoundSpec);
        annotation { "Name" : "Actual tyre width (Assegai 2.5)" }
        isLength(definition.tyreWidth, { (millimeter) : [40, 63.5, 90] } as LengthBoundSpec);
        annotation { "Name" : "Tyre clearance" }
        isLength(definition.clearance, { (millimeter) : [5, 16, 40] } as LengthBoundSpec);
        annotation { "Name" : "Brace pad thickness" }
        isLength(definition.padThk, { (millimeter) : [0, 1.5, 5] } as LengthBoundSpec);
        annotation { "Name" : "Front length (arc)" }
        isLength(definition.frontLen, { (millimeter) : [100, 165, 300] } as LengthBoundSpec);
        annotation { "Name" : "Tail length (arc)" }
        isLength(definition.tailLen, { (millimeter) : [100, 315, 420] } as LengthBoundSpec);
        annotation { "Name" : "Wing centre s (leg tie offset behind brace)" }
        isLength(definition.legTieOffset, { (millimeter) : [30, 45, 70] } as LengthBoundSpec);
        annotation { "Name" : "Wall" }
        isLength(definition.wall, { (millimeter) : [1.2, 2.5, 5] } as LengthBoundSpec);
        annotation { "Name" : "Waist / leg-zone wall" }
        isLength(definition.waistWall, { (millimeter) : [1.2, 3.0, 5] } as LengthBoundSpec);
        annotation { "Name" : "Tip kicker length" }
        isLength(definition.kickLen, { (millimeter) : [5, 25, 60] } as LengthBoundSpec);
        annotation { "Name" : "Tip kicker angle" }
        isAngle(definition.kickAngle, { (degree) : [0, 12, 30] } as AngleBoundSpec);
        annotation { "Name" : "Brace tie front row (distance forward of brace centre)" }
        isLength(definition.braceTieFront, { (millimeter) : [8, 12.5, 40] } as LengthBoundSpec);
        annotation { "Name" : "Brace tie rear row (s behind brace centre, part B)" }
        isLength(definition.braceTieRear, { (millimeter) : [8, 12.5, 60] } as LengthBoundSpec);
        annotation { "Name" : "B ear front (stepped joint, s); >= mating plane disables" }
        isLength(definition.earFront, { (millimeter) : [0, 7, 40] } as LengthBoundSpec);
        annotation { "Name" : "Brace tie y (+/-)" }
        isLength(definition.braceTieY, { (millimeter) : [15, 30, 35] } as LengthBoundSpec);
        annotation { "Name" : "Flange width" }
        isLength(definition.flangeWidth, { (millimeter) : [30, 46, 70] } as LengthBoundSpec);
        annotation { "Name" : "Flange height above top" }
        isLength(definition.flangeHeight, { (millimeter) : [8, 14, 25] } as LengthBoundSpec);
        annotation { "Name" : "Flange A thickness" }
        isLength(definition.flangeAThk, { (millimeter) : [5, 8, 15] } as LengthBoundSpec);
        annotation { "Name" : "Flange B thickness" }
        isLength(definition.flangeBThk, { (millimeter) : [2, 4, 10] } as LengthBoundSpec);
        annotation { "Name" : "Bolt spacing" }
        isLength(definition.boltSpacing, { (millimeter) : [8, 15, 25] } as LengthBoundSpec);
        annotation { "Name" : "Bolt hole diameter" }
        isLength(definition.boltDia, { (millimeter) : [3, 3.4, 4] } as LengthBoundSpec);
        annotation { "Name" : "Nyloc slot width (across flats + clearance)" }
        isLength(definition.nutWidth, { (millimeter) : [5.5, 6.15, 7] } as LengthBoundSpec);
        annotation { "Name" : "Nyloc slot thickness" }
        isLength(definition.nutThk, { (millimeter) : [4, 4.2, 5.5] } as LengthBoundSpec);
        annotation { "Name" : "Brace tie slot cut depth below outer top" }
        isLength(definition.tieCutDepth, { (millimeter) : [5, 20, 30] } as LengthBoundSpec);
        annotation { "Name" : "Wing thickness" }
        isLength(definition.wingThk, { (millimeter) : [1.5, 2.5, 5] } as LengthBoundSpec);
        annotation { "Name" : "Wing paddle width" }
        isLength(definition.wingWidth, { (millimeter) : [20, 56, 70] } as LengthBoundSpec);
        annotation { "Name" : "Sheet root front (s rel. leg axis)" }
        isLength(definition.wingRootFront, { (millimeter) : [-40, -28, 0] } as LengthBoundSpec);
        annotation { "Name" : "Sheet root rear (s rel. leg axis)" }
        isLength(definition.wingRootRear, { (millimeter) : [0, 28, 40] } as LengthBoundSpec);
        annotation { "Name" : "Sheet root flare length below the wall edge" }
        isLength(definition.wingFlareDepth, { (millimeter) : [2, 20, 60] } as LengthBoundSpec);
        annotation { "Name" : "Sheet splay outwards from vertical" }
        isAngle(definition.wingSplay, { (degree) : [0, 0, 19] } as AngleBoundSpec);
        annotation { "Name" : "Seal-bulge window if the sheet clashes with the leg", "Default" : true }
        definition.sealWindow is boolean;
        annotation { "Name" : "Seal window width (fore-aft)" }
        isLength(definition.sealWindowWidth, { (millimeter) : [10, 40, 50] } as LengthBoundSpec);
        annotation { "Name" : "Seal window top, below brace underside" }
        isLength(definition.sealWindowTop, { (millimeter) : [0, 12, 80] } as LengthBoundSpec);
        annotation { "Name" : "Seal window bottom, below brace underside (unused in v2.17)" }
        isLength(definition.sealWindowBottom, { (millimeter) : [20, 84, 120] } as LengthBoundSpec);
        annotation { "Name" : "Seal blister over the collar", "Default" : true }
        definition.sealIndent is boolean;
        annotation { "Name" : "Collar OD" }
        isLength(definition.sealCollarOD, { (millimeter) : [40, 55, 80] } as LengthBoundSpec);
        annotation { "Name" : "Radial clearance to the collar" }
        isLength(definition.sealRadialClearance, { (millimeter) : [0.5, 1.5, 4] } as LengthBoundSpec);
        annotation { "Name" : "Collar top, below brace underside" }
        isLength(definition.sealIndentTop, { (millimeter) : [0, 0, 40] } as LengthBoundSpec);
        annotation { "Name" : "Blister flat length along the leg" }
        isLength(definition.sealIndentLength, { (millimeter) : [10, 22, 50] } as LengthBoundSpec);
        annotation { "Name" : "Blister bottom taper length" }
        isLength(definition.sealIndentTaper, { (millimeter) : [10, 64, 100] } as LengthBoundSpec);
        annotation { "Name" : "Junction rib", "Default" : true }
        definition.ribEnable is boolean;
        annotation { "Name" : "Rib proud of the inner face" }
        isLength(definition.ribHeight, { (millimeter) : [1, 2, 4] } as LengthBoundSpec);
        annotation { "Name" : "Rib width down the sheet" }
        isLength(definition.ribWidth, { (millimeter) : [2, 3, 8] } as LengthBoundSpec);
        annotation { "Name" : "Wing tip, below brace underside" }
        isLength(definition.wingTipDepth, { (millimeter) : [60, 135, 180] } as LengthBoundSpec);
        annotation { "Name" : "Wing tip corner radius" }
        isLength(definition.wingCornerR, { (millimeter) : [1, 8, 15] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 1, below brace underside" }
        isLength(definition.wingRow1, { (millimeter) : [20, 90, 170] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 2, below brace underside" }
        isLength(definition.wingRow2, { (millimeter) : [20, 110, 170] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 3, below brace underside" }
        isLength(definition.wingRow3, { (millimeter) : [20, 125, 170] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot pair pitch" }
        isLength(definition.wingSlotPitch, { (millimeter) : [10, 22, 34] } as LengthBoundSpec);
        annotation { "Name" : "Strap channel on tyre side", "Default" : true }
        definition.strapChannel is boolean;
        annotation { "Name" : "Teardrop tip angle (0 = radially out, 90 = +Y side up)" }
        isAngle(definition.teardropAngle, { (degree) : [-180, 90, 180] } as AngleBoundSpec);
        annotation { "Name" : "Spine rib", "Default" : true }
        definition.addRib is boolean;
        annotation { "Name" : "Flange root fillets", "Default" : true }
        definition.addFillets is boolean;
        annotation { "Name" : "v2.12 smooth flush leg-sheet join", "Default" : true }
        definition.smoothJoin is boolean;
        annotation { "Name" : "v2.14 sculpted sections (AWL under the brace, rounded-box U front/tail)" }
        definition.sculpted is boolean;
        annotation { "Name" : "C1 / AWL depth under the brace" }
        isLength(definition.c1Depth, { (millimeter) : [18, 30.49, 34] } as LengthBoundSpec);
        annotation { "Name" : "C1 / AWL corner radius" }
        isLength(definition.c1Radius, { (millimeter) : [6, 18, 24] } as LengthBoundSpec);
        annotation { "Name" : "AWL shoulder chord (R18 ref width)" }
        isLength(definition.awlChord, { (millimeter) : [80, 94, 110] } as LengthBoundSpec);
        annotation { "Name" : "AWL floor width (G1 flare)" }
        isLength(definition.awlFloorW, { (millimeter) : [90, 100, 120] } as LengthBoundSpec);
        annotation { "Name" : "AWL crown rise" }
        isLength(definition.awlRise, { (millimeter) : [0, 7, 12] } as LengthBoundSpec);
        annotation { "Name" : "Front/tail corner radius" }
        isLength(definition.roundR, { (millimeter) : [6, 20, 26] } as LengthBoundSpec);
        annotation { "Name" : "Front/tail wall angle" }
        isAngle(definition.roundAlpha, { (degree) : [0, 5, 19] } as AngleBoundSpec);
        annotation { "Name" : "Join fillet, outside" }
        isLength(definition.joinFilletOut, { (millimeter) : [0.5, 4, 10] } as LengthBoundSpec);
        annotation { "Name" : "Join fillet, inside / sheet ends" }
        isLength(definition.joinFilletIn, { (millimeter) : [0.5, 3, 10] } as LengthBoundSpec);
    }
    {
        const mm = millimeter;
        const R = (definition.ph1 - definition.padThk) / mm;
        const st = buildStations(definition);
        const Y = vector(0, 1, 0);
        const ww = definition.waistWall / mm;

        // v2.19.3: the guard is revolved (exact) over the leg zone s sRev0..sRev1; lofts either side
        const sRev0 = 16;
        const sRev1 = 78;
        // ---------- 1. outer solid ----------
        const fout = guardSolid(context, id + "out", st, "out", 0, sRev0, sRev1);
        var adds = [fout];

        // ---------- 2. spine rib on front blade ----------
        if (definition.addRib)
        {
            const s0 = -definition.frontLen / mm + definition.kickLen / mm + 10;
            const s1 = -22;
            const fr = arcFrame(R, s0, 0);
            polySketch(context, id + "ribSk", fr.plane, [vector(-2, -2), vector(2, -2), vector(2, 1), vector(-2, 1)]);
            opRevolve(context, id + "rib", { "entities" : qSketchRegion(id + "ribSk"),
                        "axis" : line(vector(0, 0, 0) * mm, vector(0, -1, 0)), "angleForward" : ((s1 - s0) / R) * radian });
            adds = append(adds, qCreatedBy(id + "rib", EntityType.BODY));
        }

        // ---------- 3. brace tie bosses (outer +1 mm skin) ----------
        const bossH = 1.0;
        const fboss = guardSolid(context, id + "boss", st, "boss", bossH, sRev0, sRev1);
        const tieSF = definition.braceTieFront / mm;
        const tieSR = definition.braceTieRear / mm;
        const tieY = definition.braceTieY / mm;
        var prisms = [];
        var tieSlots = [];
        var k = 0;
        for (var sv in [-tieSF, tieSR])
        {
            const fr = arcFrame(R, sv, 0);
            const pl = plane(fr.origin + fr.rn * 5 * mm, fr.rn, Y);
            for (var yv in [-tieY, tieY])
            {
                // boss = slot + 4 mm all round; v2.11 rear row (on B's ears beside flange A) 1 mm shorter so it clears the |y| = flange/2 seam
                // v2.14: rear boss kept outboard of the flange seam (|y| = flangeWidth/2 + 0.3); stadium needs length > width
                const bLen = sv > 0 ? min(13, 2 * (tieY - definition.flangeWidth / mm / 2 - 0.3)) : 14;
                stadiumSketch(context, id + ("bp" ~ k), pl, yv, 0, bLen, min(2.4 + 8, bLen - 0.4));
                prisms = append(prisms, extrudeSk(context, id + ("bpx" ~ k), id + ("bp" ~ k), -fr.rn, 18));
                stadiumSketch(context, id + ("ts" ~ k), pl, yv, 0, 6.0, 2.4);
                tieSlots = append(tieSlots, extrudeSk(context, id + ("tsx" ~ k), id + ("ts" ~ k), -fr.rn, 5 + definition.tieCutDepth / mm));
                k += 1;
            }
        }
        const big = mkBox(context, id + "big", vector(-600, -300, -200), vector(600, 300, 700));
        opBoolean(context, id + "mask", { "targets" : big, "tools" : qUnion(prisms), "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "bossCut", { "targets" : fboss, "tools" : big, "operationType" : BooleanOperationType.SUBTRACTION });
        adds = append(adds, fboss);

        // ---------- 4. v2.19.3 leg sheets: dimensions only ----------
        // The joint is built in step 9c, after the hollow: replaced wall band + revolved root + planar lower sheet.
        const wc = definition.legTieOffset / mm;
        const thC = (wc / R) * radian;
        const xC = -R * sin(thC);
        const zTop = R * cos(thC);
        const wT = definition.wingThk / mm;
        const pad = definition.padThk / mm;
        const Wg = definition.legZoneWidth / mm;
        const tA19 = tan(19 * degree);
        const dEdge = (Wg - definition.legZoneTopFlat / mm) / (2 * tA19); // wall depth in the leg zone (= station depth dL)
        const yEdge = Wg / 2;                                         // guard wall bottom edge |y|
        const zEdge = zTop - dEdge;
        const spl = definition.wingSplay;                             // not used in v2.19.3 (0)
        const yo = yEdge + 0.5;                                       // v2.19.3: lower sheet outer plane |y| = 49.0
        const yi = yo - wT;                                           // lower sheet inner plane |y| = 46.5
        const yLeg = definition.ph2 / mm / 2 + definition.ph6 / mm / 2;
        const zB = zTop + pad;
        const hw = definition.wingWidth / mm / 2;
        const dTip = definition.wingTipDepth / mm - pad;              // depth below guard top at the tip
        // v2.17: only the lowest leg-tie pair. Rows are 90 / 110 / 125; the lowest is 125 mm.
        const rowA = definition.wingRow1 / mm - pad;
        const rowB = definition.wingRow2 / mm - pad;
        const rowC = definition.wingRow3 / mm - pad;
        const dBot = max(rowA, max(rowB, rowC));
        const dRows = [dBot];
        const rc = definition.wingCornerR / mm;
        const Dbow0 = pad + dEdge;
        const ySh = function(dv) { return yo; };
        // root / band window (s at R). s 22.5 = B mating plane + 3 (asserted in step 5); rear end 2 mm inside the revolved zone.
        const jS0 = 22.5;
        const jS1 = 76;
        // v2.19.3: guard only (front loft + revolved leg zone + tail loft, bosses, rib).
        // v2.19.3.1: merge every solid present except the tie-slot cutters (guard, all boss lumps, rib),
        // so the boss lumps split off by bossCut are all included whatever their tracking.
        const mergeQ = qSubtraction(qAllModifiableSolidBodies(), qUnion(tieSlots));
        const nMerge = size(evaluateQuery(context, mergeQ));
        if (nMerge < 2)
            throw regenError("v2.19.3.1 merge: only " ~ nMerge ~ " solid bodies (expected guard + boss lumps)");
        opBoolean(context, id + "merge", { "tools" : mergeQ, "operationType" : BooleanOperationType.UNION });
        const nAfter = size(evaluateQuery(context, qSubtraction(qAllModifiableSolidBodies(), qUnion(tieSlots))));
        if (nAfter != 1)
            throw regenError("v2.19.3.1 merge: " ~ nAfter ~ " bodies after union of " ~ nMerge);
        println("v2.19.3.1 merge: " ~ nMerge ~ " bodies -> 1");

        // ---------- 4b. clip the guard to |y| <= legZoneWidth/2 (loft overshoot in the blends) ----------
        if (definition.smoothJoin)
        {
            const v1 = evVolume(context, { "entities" : bodyAt(context, arcFrame(R, 100, -1.2).origin) }) / mm / mm / mm;
            var clips = [];
            for (var sg in [-1, 1])
                clips = append(clips, mkBox(context, id + ("clip" ~ (sg + 1)), vector(-120, sg * (yEdge + 0.5), 200), vector(10, sg * 120, 500))); // v2.19.3.1: 0.5 off the exact guard edge
            opBoolean(context, id + "clipY", { "targets" : bodyAt(context, arcFrame(R, 100, -1.2).origin), "tools" : qUnion(clips), "operationType" : BooleanOperationType.SUBTRACTION });
            const v2 = evVolume(context, { "entities" : bodyAt(context, arcFrame(R, 100, -1.2).origin) }) / mm / mm / mm;
            println("v2.19.3.1 clipY |y|>" ~ (yEdge + 0.5) ~ " removed " ~ roundToPrecision(v1 - v2, 3) ~ " mm3");
        }

        // ---------- 5. split just behind the brace ----------
        const sF = definition.ph3 / mm / 2 + definition.padThk / mm; // A flange front face
        const tA = definition.flangeAThk / mm;
        const tB = definition.flangeBThk / mm;
        const sM = sF + tA; // mating plane
        if (jS0 < sM + 3 - 1e-9)
            throw regenError("v2.19.3: root front s " ~ jS0 ~ " is less than B mating plane + 3 = " ~ (sM + 3));
        const fm = arcFrame(R, sM, 0);
        const pA = arcFrame(R, -100, -1.2).origin;
        const pB = arcFrame(R, 100, -1.2).origin;
        opPlane(context, id + "splitPl", { "plane" : fm.plane, "width" : 600 * mm, "height" : 600 * mm });
        opSplitPart(context, id + "split", { "targets" : bodyAt(context, pA), "tool" : qCreatedBy(id + "splitPl", EntityType.BODY), "keepTools" : false });

        // v2.11 stepped joint: outboard of the flange (|y| > flangeWidth/2) the split steps forward to s = sEar, so B carries
        // two "ears" beside flange A that hold the rear brace-tie slots; the tie loop then clamps A and B together.
        const sEar = definition.earFront / mm;
        if (sEar < sM - 1)
        {
            const yS = definition.flangeWidth / mm / 2;
            const fe = arcFrame(R, sEar, 0);
            var earBoxes = [];
            for (var sg in [-1, 1])
            {
                const eid = id + ("ear" ~ (sg + 1));
                const y0 = sg > 0 ? yS : -170;
                const y1 = sg > 0 ? 170 : -yS;
                polySketch(context, eid + "sk", fe.plane, [vector(y0, -100), vector(y1, -100), vector(y1, 220), vector(y0, 220)]);
                opRevolve(context, eid + "rv", { "entities" : qSketchRegion(eid + "sk"), "axis" : line(vector(0, 0, 0) * mm, vector(0, -1, 0)),
                            "angleForward" : ((sM + 1 - sEar) / R) * radian }); // v2.19.3.1: 1 mm past the split face
                opDeleteBodies(context, eid + "dsk", { "entities" : qCreatedBy(eid + "sk", EntityType.BODY) });
                earBoxes = append(earBoxes, qCreatedBy(eid + "rv", EntityType.BODY));
            }
            var earPieces = [];
            for (var i = 0; i < 2; i += 1)
            {
                opPattern(context, id + ("aCopy" ~ i), { "entities" : bodyAt(context, pA), "transforms" : [identityTransform()], "instanceNames" : ["c" ~ i] });
                opBoolean(context, id + ("earInt" ~ i), { "tools" : qUnion([qCreatedBy(id + ("aCopy" ~ i), EntityType.BODY), earBoxes[i]]),
                            "operationType" : BooleanOperationType.INTERSECTION, "keepTools" : true });
                earPieces = append(earPieces, qCreatedBy(id + ("earInt" ~ i), EntityType.BODY));
                opDeleteBodies(context, id + ("aCopyDel" ~ i), { "entities" : qCreatedBy(id + ("aCopy" ~ i), EntityType.BODY) });
            }
            opBoolean(context, id + "earCutA", { "targets" : bodyAt(context, pA), "tools" : qUnion(earBoxes), "operationType" : BooleanOperationType.SUBTRACTION });
            opBoolean(context, id + "earUnionB", { "tools" : qUnion(concatenateArrays([[bodyAt(context, pB)], earPieces])), "operationType" : BooleanOperationType.UNION });
            println("stepped joint: B ears s " ~ sEar ~ ".." ~ sM ~ " outboard of |y|=" ~ yS ~ "; strip ahead of rear tie slot " ~ roundToPrecision(tieSR - 1.2 - sEar, 2) ~ " mm");
        }

        var qA = bodyAt(context, pA);
        var qB = bodyAt(context, pB);

        // ---------- 6. flanges ----------
        const W = definition.flangeWidth / mm;
        const H = definition.flangeHeight / mm;
        const awlRiseFl = definition.sculpted ? definition.awlRise / mm : 0;
        const awlChordFl = definition.sculpted ? definition.awlChord / mm : definition.guardWidth / mm;
        const embed = 5 + awlRiseFl; // reach past crown sag into the shell
        const flPts = [vector(-W / 2, embed), vector(-W / 2, -H + 1), vector(-W / 2 + 1, -H), vector(W / 2 - 1, -H), vector(W / 2, -H + 1), vector(W / 2, embed)];
        polySketch(context, id + "flASk", plane(fm.origin - fm.t * tA * mm, fm.t, Y), flPts);
        const flA = extrudeSk(context, id + "flA", id + "flASk", fm.t, tA);
        polySketch(context, id + "flBSk", fm.plane, flPts);
        const flB = extrudeSk(context, id + "flB", id + "flBSk", fm.t, tB);
        println("brace ties: front row s=" ~ (-tieSF) ~ ", rear row s=" ~ tieSR ~ "; centre spacing " ~ (tieSF + tieSR) ~ " mm, slot inner-edge gap " ~ roundToPrecision(tieSF + tieSR - 2.4, 2) ~
            " mm (brace " ~ definition.ph3 / mm ~ "); front slot rear edge to brace front face " ~ roundToPrecision(tieSF - 1.2 - definition.ph3 / mm / 2, 2) ~
            " mm, rear slot front edge to brace rear face " ~ roundToPrecision(tieSR - 1.2 - definition.ph3 / mm / 2, 2) ~ " mm; rear slot owner: " ~ ((tieSR + 1.2 < sM && !(tieSR - 1.2 > definition.earFront / mm)) ? "A" : "B") ~
            "; rear slot inner end |y|=" ~ (tieY - 3) ~ " vs flange half-width " ~ W / 2 ~ " -> skin gap " ~ (tieY - 3 - W / 2) ~ " mm");
        println("tie-slot cutter to flange A clearance (mm): " ~ roundToPrecision(evDistance(context, { "side0" : qUnion(tieSlots), "side1" : flA }).distance / mm, 2) ~
            ", to flange B: " ~ roundToPrecision(evDistance(context, { "side0" : qUnion(tieSlots), "side1" : flB }).distance / mm, 2));
        opBoolean(context, id + "uA", { "tools" : qUnion([qA, flA]), "operationType" : BooleanOperationType.UNION });

        // gussets + tongue on B
        var bAdds = [qB, flB];
        const bs = definition.boltSpacing / mm;
        const rearO = fm.origin + fm.t * tB * mm;
        var g = 0;
        for (var gy in [-bs / 2, bs / 2])
        {
            polySketch(context, id + ("gSk" ~ g), plane(rearO + Y * (gy - 1.5) * mm, Y, fm.t), [vector(-0.5, -1), vector(10, -1), vector(-0.5, 10)]);
            bAdds = append(bAdds, extrudeSk(context, id + ("g" ~ g), id + ("gSk" ~ g), Y, 3));
            g += 1;
        }
        const Lt = W / 2 - 2;
        // Crown-following tongue: band between radial offsets d0..d1 of the AWL crown, extruded along s.
        const rFl = definition.c1Radius / mm;
        const RcFl = awlRiseFl > 0 ? crownRadiusAwl(awlChordFl / 2, awlRiseFl, rFl) : 1e6;
        const ccyFl = RcFl - awlRiseFl;
        crownBandSketch(context, id + "tgSk", plane(fm.origin - fm.t * 0.5 * mm, fm.t, Y),
            Lt, ccyFl, RcFl, 0.3, 2.3, awlRiseFl);
        bAdds = append(bAdds, extrudeSk(context, id + "tg", id + "tgSk", fm.t, tA + tB + 1));
        opBoolean(context, id + "uB", { "tools" : qUnion(bAdds), "operationType" : BooleanOperationType.UNION });

        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);

        // ---------- 7. cuts in A: crown-following groove + captive nyloc slots ----------
        var aCuts = [];
        crownBandSketch(context, id + "grSk", plane(fm.origin - fm.t * (tA + 0.5) * mm, fm.t, Y),
            Lt + 0.5, ccyFl, RcFl, 0.05, 2.55, awlRiseFl);
        const grooveQ = extrudeSk(context, id + "gr", id + "grSk", fm.t, tA + 1);
        aCuts = append(aCuts, grooveQ);
        var nutQs = [];
        const boltH = 7;
        const nutW = definition.nutWidth / mm;
        const nutT = definition.nutThk / mm;
        const nutFloor = boltH - 3.2; // above peak: nut centred on bolt axis
        const rb = definition.boltDia / mm / 2;
        var n = 0;
        for (var xb in [-bs, 0, bs])
        {
            polySketch(context, id + ("nSk" ~ n), plane(fm.origin + Y * (xb - nutW / 2) * mm, Y, fm.t),
                [vector(-1.5 - nutT, nutFloor), vector(-1.5, nutFloor), vector(-1.5, H + 1), vector(-1.5 - nutT, H + 1)]);
            const nq = extrudeSk(context, id + ("n" ~ n), id + ("nSk" ~ n), Y, nutW);
            nutQs = append(nutQs, nq);
            aCuts = append(aCuts, nq);
            n += 1;
        }
        const gnDist = evDistance(context, { "side0" : grooveQ, "side1" : qUnion(nutQs) }).distance / mm;
        println("groove-to-nut-slot clearance (mm, 0 = touching/intersecting): " ~ roundToPrecision(gnDist, 3));
        println("rear tie slot cutters to nut slots " ~ roundToPrecision(evDistance(context, { "side0" : qUnion([tieSlots[2], tieSlots[3]]), "side1" : qUnion(nutQs) }).distance / mm, 2) ~
            ", groove " ~ roundToPrecision(evDistance(context, { "side0" : qUnion([tieSlots[2], tieSlots[3]]), "side1" : grooveQ }).distance / mm, 2) ~
            ", part B (incl. gussets/tongue) " ~ roundToPrecision(evDistance(context, { "side0" : qUnion([tieSlots[2], tieSlots[3]]), "side1" : bodyAt(context, pB) }).distance / mm, 2) ~ " mm");
        opBoolean(context, id + "cutA", { "targets" : qA, "tools" : qUnion(aCuts), "operationType" : BooleanOperationType.SUBTRACTION });

        // ---------- 8. bolt holes through both flanges (teardrop) ----------
        const ta = definition.teardropAngle;
        const dv = vector(sin(ta), -cos(ta));
        var holes = [];
        n = 0;
        for (var xb in [-bs, 0, bs])
        {
            teardropSketch(context, id + ("hSk" ~ n), plane(fm.origin - fm.t * (tA + 1) * mm, fm.t, Y), xb, -boltH, rb, dv);
            holes = append(holes, extrudeSk(context, id + ("h" ~ n), id + ("hSk" ~ n), fm.t, tA + tB + 2));
            n += 1;
        }
        println("rear tie slot cutters to bolt holes " ~ roundToPrecision(evDistance(context, { "side0" : qUnion([tieSlots[2], tieSlots[3]]), "side1" : qUnion(holes) }).distance / mm, 2) ~ " mm");
        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);
        opBoolean(context, id + "holes", { "targets" : qUnion([qA, qB]), "tools" : qUnion(holes), "operationType" : BooleanOperationType.SUBTRACTION });

        // ---------- 9. hollow out (inner void) + tie slots ----------
        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);
        const fin = guardSolid(context, id + "in", st, "in", 0, sRev0, sRev1);
        opBoolean(context, id + "hollow", { "targets" : qUnion([qA, qB]), "tools" : qUnion(concatenateArrays([[fin], tieSlots])),
                    "operationType" : BooleanOperationType.SUBTRACTION });

        // ---------- 9c. v2.19.3 leg joint (Split & Join Planner plan, 17 Nov 2026) ----------
        // Cut the lower wall band from B (v 20.5..40, s jS0..jS1), revolve one closed root profile over the same angles
        // (19 deg wall -> R8 -> R53 bow -> R9.31 -> flat |y| 49.0; inner 3.0 -> 2.5 mm), then a planar lower sheet on
        // |y| = 49.0, 2.5 mm thick, that overlaps the root flat by 4 mm. One union. All shared faces are built the same way.
        const jA = Wg / 2 - dEdge * tA19;                 // 38.0: outer wall line u = jA + v tan19 (leg-zone station a)
        const jOffIn = ww / cos(19 * degree);             // inner wall line u = jA + v tan19 - jOffIn
        const jVcut = 20.5;
        const jVbot = 76;
        const jVsh = 72;
        const jS19 = sin(19 * degree);
        const jC19 = cos(19 * degree);
        const jR1 = 8;
        const jRb = 53;
        const jR2i = 5.5;
        const jCb = cos(18 * degree);
        const jSb = sin(18 * degree);
        const jE = vector(yEdge, dEdge);
        const jC2 = vector(yEdge - jR1 * jC19, dEdge + jR1 * jS19);
        const jP2 = vector(jC2[0] + jR1 * jCb, jC2[1] + jR1 * jSb);
        const jC3 = vector(jP2[0] + jRb * jCb, jP2[1] + jRb * jSb);
        const jBelly = vector(jC3[0] - jRb, jC3[1]);
        const jP3 = vector(jC3[0] - jRb * jCb, jC3[1] + jRb * jSb);
        const jR3 = (yo - jP3[0]) / (1 - jCb);
        const jC4 = vector(jP3[0] - jR3 * jCb, jP3[1] + jR3 * jSb);
        const jP4 = vector(yo, jC4[1]);
        const jR4i = jR3 - wT;
        const jR3i = jRb + wT;
        const jQ4 = vector(jC4[0] + jR4i * jCb, jC4[1] - jR4i * jSb);
        const jK0 = jA - jOffIn - jR2i / jC19;
        const jQa = tA19 * tA19 + 1;
        const jQb = 2 * tA19 * (jK0 - jC3[0]) - 2 * jC3[1];
        const jQc = (jK0 - jC3[0]) * (jK0 - jC3[0]) + jC3[1] * jC3[1] - (jR3i + jR2i) * (jR3i + jR2i);
        const jVi2 = (-jQb - sqrt(jQb * jQb - 4 * jQa * jQc)) / (2 * jQa);
        const jCi2 = vector(jK0 + jVi2 * tA19, jVi2);
        const jTw = vector(jCi2[0] + jR2i * jC19, jCi2[1] - jR2i * jS19);
        const jTb = jCi2 + (jC3 - jCi2) * (jR2i / norm(jC3 - jCi2));
        const jTopO = vector(jA + jVcut * tA19, jVcut);
        const jTopI = vector(jA + jVcut * tA19 - jOffIn, jVcut);
        const jUo = function(v) {
                    if (v <= dEdge)
                        return jA + v * tA19;
                    if (v <= jP2[1])
                        return jC2[0] + sqrt(max(0, jR1 * jR1 - (v - jC2[1]) * (v - jC2[1])));
                    if (v <= jP3[1])
                        return jC3[0] - sqrt(max(0, jRb * jRb - (v - jC3[1]) * (v - jC3[1])));
                    if (v <= jP4[1])
                        return jC4[0] + sqrt(max(0, jR3 * jR3 - (v - jC4[1]) * (v - jC4[1])));
                    return yo;
                };
        const jUi = function(v) {
                    if (v <= jTw[1])
                        return jA + v * tA19 - jOffIn;
                    if (v <= jTb[1])
                        return jCi2[0] + sqrt(max(0, jR2i * jR2i - (v - jCi2[1]) * (v - jCi2[1])));
                    if (v <= jQ4[1])
                        return jC3[0] - sqrt(max(0, jR3i * jR3i - (v - jC3[1]) * (v - jC3[1])));
                    if (v <= jC4[1])
                        return jC4[0] + sqrt(max(0, jR4i * jR4i - (v - jC4[1]) * (v - jC4[1])));
                    return yi;
                };
        const jRootChain = [
            ["L", jTopO, jE],
            ["A", jE, arcMid2(jC2, jR1, jE, jP2), jP2],
            ["A", jP2, jBelly, jP3],
            ["A", jP3, arcMid2(jC4, jR3, jP3, jP4), jP4],
            ["L", jP4, vector(yo, jVbot)],
            ["L", vector(yo, jVbot), vector(yi, jVbot)],
            ["L", vector(yi, jVbot), vector(yi, jC4[1])],
            ["A", vector(yi, jC4[1]), arcMid2(jC4, jR4i, vector(yi, jC4[1]), jQ4), jQ4],
            ["A", jQ4, arcMid2(jC3, jR3i, jQ4, jTb), jTb],
            ["A", jTb, arcMid2(jCi2, jR2i, jTb, jTw), jTw],
            ["L", jTw, jTopI],
            ["L", jTopI, jTopO]];
        const jTh0 = jS0 / R;
        const jTh1 = jS1 / R;
        const jSweep = (jTh1 - jTh0) * radian;
        const jFr0 = arcFrame(R, jS0, 0);
        const jAxis = line(vector(0, 0, 0) * mm, vector(0, -1, 0));
        // lower sheet outline in world (x, z)
        const jRtop = R - jVsh;
        const jRbot = R - jVbot;
        const jZv = zB - 107.9;                           // edges vertical from D 107.9 down (rear flare needs ~28 mm of height)
        const jXf = xC + hw;
        const jXr = xC - hw;
        const jZt = zTop - dTip;
        const jFtop = vector(-jRtop * sin(jTh0 * radian), jRtop * cos(jTh0 * radian));
        const jBtop = vector(-jRtop * sin(jTh1 * radian), jRtop * cos(jTh1 * radian));
        const jTmid = vector(-jRtop * sin(0.5 * (jTh0 + jTh1) * radian), jRtop * cos(0.5 * (jTh0 + jTh1) * radian));
        const jF0 = vector(-jRbot * sin(jTh0 * radian), jRbot * cos(jTh0 * radian));
        const jB0 = vector(-jRbot * sin(jTh1 * radian), jRbot * cos(jTh1 * radian));
        // front edge: the radial line carries on, then one tangent arc to the vertical edge x = jXf at z = jZv
        const jSp0 = sin(jTh0 * radian);
        const jCp0 = cos(jTh0 * radian);
        const jFdx = jXf - jF0[0];
        const jFdz = jF0[1] - jZv;
        const jFd = jSp0 * jSp0 - (1 - jCp0) * jCp0;
        const jFl = (jFdx * jSp0 - (1 - jCp0) * jFdz) / jFd;
        const jFr = (jSp0 * jFdz - jCp0 * jFdx) / jFd;
        const jFq = vector(jF0[0] + jFl * jSp0, jF0[1] - jFl * jCp0);
        const jFc = vector(jFq[0] - jFr * jCp0, jFq[1] - jFr * jSp0);
        const jFm = vector(jFc[0] + jFr * cos(0.5 * jTh0 * radian), jFc[1] + jFr * sin(0.5 * jTh0 * radian));
        const jFe = vector(jFc[0] + jFr, jFc[1]);
        if (jFl < 2 || abs(jFe[0] - jXf) > 1e-6 || abs(jFe[1] - jZv) > 1e-6)
            throw regenError("v2.19.3 front edge fit: L " ~ jFl ~ " end " ~ toString(jFe));
        // rear edge: R10 / line / R10 S-curve from the radial line out to x = jXr (bisection; one root in [-1, -0.01])
        var jLo = -1.0;
        var jHi = -0.01;
        for (var it = 0; it < 60; it += 1)
        {
            const jMd = 0.5 * (jLo + jHi);
            if (flareFit(jB0, jTh1, jLo, jXr, jZv, 10).err * flareFit(jB0, jTh1, jMd, jXr, jZv, 10).err <= 0)
                jHi = jMd;
            else
                jLo = jMd;
        }
        const jRf = flareFit(jB0, jTh1, 0.5 * (jLo + jHi), jXr, jZv, 10);
        if (abs(jRf.err) > 1e-6 || jRf.L < 2)
            throw regenError("v2.19.3 rear edge fit: err " ~ jRf.err ~ " L " ~ jRf.L);
        const jC45 = cos(45 * degree);
        const jSheetChain = [
            ["A", jBtop, jTmid, jFtop],
            ["L", jFtop, jFq],
            ["A", jFq, jFm, jFe],
            ["L", jFe, vector(jXf, jZt + rc)],
            ["A", vector(jXf, jZt + rc), vector(jXf - rc + rc * jC45, jZt + rc - rc * jC45), vector(jXf - rc, jZt)],
            ["L", vector(jXf - rc, jZt), vector(jXr + rc, jZt)],
            ["A", vector(jXr + rc, jZt), vector(jXr + rc - rc * jC45, jZt + rc - rc * jC45), vector(jXr, jZt + rc)],
            ["L", vector(jXr, jZt + rc), vector(jXr, jZv)],
            ["A", vector(jXr, jZv), jRf.m2, jRf.q2],
            ["L", jRf.q2, jRf.q1],
            ["A", jRf.q1, jRf.m1, jB0],
            ["L", jB0, jBtop]];
        println("v2.19.3 sheet outline: front line " ~ roundToPrecision(jFl, 2) ~ " + R" ~ roundToPrecision(jFr, 1) ~ " arc; rear flare heading " ~
            roundToPrecision(jRf.phim / PI * 180, 2) ~ " deg, line " ~ roundToPrecision(jRf.L, 2) ~ " mm; root belly |y| " ~ roundToPrecision(jBelly[0], 3) ~
            " at v " ~ roundToPrecision(jBelly[1], 3) ~ ", R3 " ~ roundToPrecision(jR3, 3) ~ ", i2 centre " ~ toString(jCi2));
        // 9c-1: band cut on B
        qB = bodyAt(context, pB);
        var jBands = [];
        for (var sg in [-1, 1])
        {
            const bid = id + ("jBand" ~ (sg + 1));
            polySketch(context, bid + "sk", jFr0.plane, [vector(sg * 40, jVcut), vector(sg * 60, jVcut), vector(sg * 60, 40), vector(sg * 40, 40)]);
            opRevolve(context, bid + "rv", { "entities" : qSketchRegion(bid + "sk"), "axis" : jAxis, "angleForward" : jSweep });
            jBands = append(jBands, qCreatedBy(bid + "rv", EntityType.BODY));
        }
        const jVb0 = evVolume(context, { "entities" : qB }) / mm / mm / mm;
        opBoolean(context, id + "jBandCut", { "targets" : qB, "tools" : qUnion(jBands), "operationType" : BooleanOperationType.SUBTRACTION });
        qB = bodyAt(context, pB);
        const jVb1 = evVolume(context, { "entities" : qB }) / mm / mm / mm;
        println("v2.19.3 band cut removed " ~ roundToPrecision(jVb0 - jVb1, 1) ~ " mm3");
        if (jVb0 - jVb1 < 500)
            throw regenError("v2.19.3 band cut removed only " ~ roundToPrecision(jVb0 - jVb1, 1) ~ " mm3");
        // 9c-2: roots and lower sheets
        var jAdd = [];
        for (var sg in [-1, 1])
        {
            const rid = id + ("jRoot" ~ (sg + 1));
            chainSketch(context, rid + "sk", jFr0.plane, jRootChain, sg);
            opRevolve(context, rid + "rv", { "entities" : qSketchRegion(rid + "sk"), "axis" : jAxis, "angleForward" : jSweep });
            jAdd = append(jAdd, qCreatedBy(rid + "rv", EntityType.BODY));
            const sid = id + ("jSheet" ~ (sg + 1));
            chainSketch(context, sid + "sk", plane(vector(0, sg * yo, 0) * mm, vector(0, -1, 0), vector(1, 0, 0)), jSheetChain, 1);
            const jSh = extrudeSk(context, sid + "ex", sid + "sk", vector(0, -sg, 0), wT);
            var cuts = [];
            var k2 = 0;
            for (var rowD in dRows)
            {
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    cuts = append(cuts, mkBox(context, sid + ("sl" ~ k2), vector(xC - sp - 1.2, sg * (yi - 1), zTop - rowD - 3),
                                vector(xC - sp + 1.2, sg * (yo + 1), zTop - rowD + 3)));
                    k2 += 1;
                }
                if (definition.strapChannel)
                {
                    const hp = definition.wingSlotPitch / mm / 2;
                    cuts = append(cuts, mkBox(context, sid + ("ch" ~ k2), vector(xC - hp, sg * (yi - 1), zTop - rowD - 2.75),
                                vector(xC + hp, sg * (yi + 0.8), zTop - rowD + 2.75)));
                }
            }
            opBoolean(context, sid + "slotCut", { "targets" : jSh, "tools" : qUnion(cuts), "operationType" : BooleanOperationType.SUBTRACTION });
            jAdd = append(jAdd, qCreatedBy(sid + "ex", EntityType.BODY));
        }
        opBoolean(context, id + "jointU", { "tools" : qUnion(concatenateArrays([[bodyAt(context, pB)], jAdd])), "operationType" : BooleanOperationType.UNION });
        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);
        // plan 2.7 (1): small faces in the joint zone straight after the union (before any fillet)
        var jTiny = 0;
        for (var fc in evaluateQuery(context, qOwnedByBody(qB, EntityType.FACE)))
        {
            const ar = evArea(context, { "entities" : fc }) / (mm * mm);
            if (ar >= 1)
                continue;
            const fb = evBox3d(context, { "topology" : fc, "tight" : false });
            const fcc = (fb.minCorner + fb.maxCorner) / 2 / mm;
            const sC = R * atan2(-fcc[0], fcc[2]) / radian;
            if (sC > 20 && sC < 75 && abs(fcc[1]) > 40)
                jTiny += 1;
        }
        // 2.6: R2 on the four short concave corners where the root end faces meet the guard wall's bottom face.
        // Each corner is re-found after the previous fillet (fresh query); a corner that will not fillet stays sharp.
        var jFil = 0;
        var jFilN = 0;
        for (var ci = 0; ci < 4; ci += 1)
        {
            const sEnd = ci < 2 ? jS0 : jS1;
            const ySg = (ci % 2 == 0) ? -1 : 1;
            var fes = [];
            for (var ed in evaluateQuery(context, qOwnedByBody(bodyAt(context, pB), EntityType.EDGE)))
            {
                const p = evEdgeTangentLine(context, { "edge" : ed, "parameter" : 0.5 }).origin / mm;
                const rr = sqrt(p[0] * p[0] + p[2] * p[2]);
                const ss = R * atan2(-p[0], p[2]) / radian;
                if (abs(rr - (R - dEdge)) < 0.3 && abs(ss - sEnd) < 0.3 && p[1] * ySg > 44.5 && p[1] * ySg < 49)
                    fes = append(fes, ed);
            }
            jFilN += size(fes);
            if (size(fes) > 0)
            {
                try silent
                {
                    opFillet(context, id + ("jCornFil" ~ ci), { "entities" : qUnion(fes), "radius" : 2 * mm });
                    jFil += size(fes);
                }
            }
        }
        qB = bodyAt(context, pB);
        println("v2.19.3 joint: B bodies " ~ size(evaluateQuery(context, qB)) ~ ", small faces " ~ jTiny ~ ", corner fillets " ~ jFil ~ "/" ~ jFilN);

        // ---------- 10. identify parts, fillets, names ----------
        const pA2 = arcFrame(R, -100, -1.2).origin;
        qA = bodyAt(context, pA2);
        qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);

        if (definition.addFillets)
        {
            const frontO = fm.origin - fm.t * tA * mm;
            const rearO2 = fm.origin + fm.t * tB * mm;
            var fe = [];
            for (var pq in [[qA, frontO], [qB, rearO2]])
            {
                for (var ed in evaluateQuery(context, qOwnedByBody(pq[0], EntityType.EDGE)))
                {
                    const p = evEdgeTangentLine(context, { "edge" : ed, "parameter" : 0.5 }).origin;
                    const dPl = abs(dot(p - pq[1], fm.t)) / mm;
                    const rad = norm(p) / mm;
                    if (dPl < 0.05 && abs(rad - R) < 0.5 && abs(p[1] / mm) < W / 2 - 0.5)
                        fe = append(fe, ed);
                }
            }
            println("root fillet edges: " ~ size(fe));
            if (size(fe) > 0)
            {
                var filOk = false;
                try silent
                {
                    opFillet(context, id + "rootFil", { "entities" : qUnion(fe), "radius" : 2 * mm });
                    filOk = true;
                }
                println("root fillet ok=" ~ filOk);
            }
            qA = bodyAt(context, pA2);
            qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);
        }

        // ---------- v2.15: round the front tip and the rear end in plan ----------
        // R32 corners tangent to the end and to y = +/- width/2 (33 mm straight land), then a 1 mm
        // 45 deg chamfer on the straight lip. Only these two ends; the cutters stop ~32 mm in.
        qA = bodyAt(context, pA2);
        qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);
        const mm3 = millimeter * millimeter * millimeter;
        const vA0 = evVolume(context, { "entities" : qA }) / mm3;
        const vB0 = evVolume(context, { "entities" : qB }) / mm3;
        v215EndRound(context, id + "eF", R, st[0], 1, qA);
        v215EndRound(context, id + "eR", R, st[size(st) - 1], -1, qB);
        qA = bodyAt(context, pA2);
        qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);
        println("v2.15 end cut removed A " ~ roundToPrecision(vA0 - evVolume(context, { "entities" : qA }) / mm3, 0) ~
            " mm3, B " ~ roundToPrecision(vB0 - evVolume(context, { "entities" : qB }) / mm3, 0) ~ " mm3");
        const prF = v215Probe(context, qA, st[0], 1, 47, 22, R);
        const prR = v215Probe(context, qB, st[size(st) - 1], -1, 45, 16, R);
        println("v2.15 end outline R32: corners removed " ~ (prF.gone + prR.gone) ~ "/2, inside-arc kept " ~ (prF.kept + prR.kept) ~
            "/2, solids A+B " ~ (size(evaluateQuery(context, qA)) + size(evaluateQuery(context, qB))) ~ " | " ~ prF.text ~ "; " ~ prR.text);

        println("v2.19.3 root s " ~ jS0 ~ ".." ~ jS1 ~ ": belly |y| " ~ roundToPrecision(jBelly[0], 3) ~ " at v " ~ roundToPrecision(jBelly[1], 2) ~
            ", flat |y| " ~ yo ~ ", collar intrusion " ~ roundToPrecision(jBelly[0] - (yLeg - definition.sealCollarOD / mm / 2), 3) ~ " mm");
        qA = bodyAt(context, pA2);
        qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);

        setProperty(context, { "entities" : qA, "propertyType" : PropertyType.NAME, "value" : "Guard Front A" });
        setProperty(context, { "entities" : qB, "propertyType" : PropertyType.NAME, "value" : "Guard Rear B" });

        // cleanup sketches / construction bodies
        opDeleteBodies(context, id + "clean", { "entities" : qSubtraction(qCreatedBy(id, EntityType.BODY), qAllModifiableSolidBodies()) });

        // ---------- 11. verification report ----------
        const solids = qAllModifiableSolidBodies();
        for (var bq in evaluateQuery(context, solids))
            println("  solid " ~ fmtBox(evBox3d(context, { "topology" : bq, "tight" : true })));
        println("nSolids=" ~ size(evaluateQuery(context, solids)) ~ " nA=" ~ size(evaluateQuery(context, qA)) ~ " nB=" ~ size(evaluateQuery(context, qB)));
        println("A " ~ fmtBox(evBox3d(context, { "topology" : qA, "tight" : true })));
        println("B " ~ fmtBox(evBox3d(context, { "topology" : qB, "tight" : true })));
        const cm3 = centimeter * centimeter * centimeter;
        println("volA(cm3)=" ~ roundToPrecision(evVolume(context, { "entities" : qA }) / cm3, 1) ~
            " volB(cm3)=" ~ roundToPrecision(evVolume(context, { "entities" : qB }) / cm3, 1));
        const clashes = evCollision(context, { "tools" : qA, "targets" : qB });
        var nInterfere = 0;
        var nAbut = 0;
        for (var cl in clashes)
        {
            if (cl["type"] == ClashType.INTERFERE)
                nInterfere += 1;
            else
                nAbut += 1;
        }
        println("A/B collision check: interfere=" ~ nInterfere ~ " abut(face contact)=" ~ nAbut);
        println("A on side: " ~ toString(sideFit(context, qA)));
        println("B on side: " ~ toString(sideFit(context, qB)));
        for (var xb in [-bs, 0, bs])
        {
            const org = fm.origin - fm.t * (tA + 3) * mm + Y * xb * mm - fm.rn * boltH * mm;
            const hits = evRaycast(context, { "entities" : qUnion([qA, qB]), "ray" : line(org, fm.t), "closest" : false });
            var nHit = 0;
            for (var hh in hits)
            {
                if (hh.distance < (tA + tB + 6) * mm)
                {
                    nHit += 1;
                }
            }
            println("bolt y=" ~ xb ~ " axis hits within flange stack: " ~ nHit);
        }
        // tie slot vs R6 corner: radial rays through slot centre-line points; a hit means skin left in the slot path
        for (var sv in [-tieSF, tieSR])
        {
            const fr = arcFrame(R, sv, 0);
            var line_ = "tie slot s=" ~ sv ~ ":";
            for (var dy in [-2.9, -1.5, 0, 1.5, 2.9])
            {
                const yy = tieY + dy;
                const org = fr.origin + fr.rn * 6 * mm + Y * yy * mm;
                const hits = evRaycast(context, { "entities" : qUnion([qA, qB]), "ray" : line(org, -fr.rn), "closest" : true });
                var txt = "clear";
                if (size(hits) > 0)
                    txt = "hit@h=" ~ roundToPrecision(hits[0].distance / mm - 6, 2);
                line_ = line_ ~ " y=" ~ roundToPrecision(yy, 1) ~ "->" ~ txt;
            }
            println(line_);
        }
        {
            // tie path: exit ray outward from each slot (nothing of A/B in the way for 30 mm) + inner run under the skin between rows
            for (var sv in [-tieSF, tieSR])
            {
                const fr = arcFrame(R, sv, 0);
                for (var sy in [-1, 1])
                {
                    const hits = evRaycast(context, { "entities" : qUnion([qA, qB]), "ray" : line(fr.origin + fr.rn * 0.3 * mm + Y * sy * tieY * mm, fr.rn), "closest" : true });
                    var hd = "clear 30";
                    if (size(hits) > 0 && hits[0].distance < 30 * mm)
                        hd = "HIT@" ~ roundToPrecision(hits[0].distance / mm, 2);
                    println("tie exit s=" ~ sv ~ " y=" ~ sy * tieY ~ ": " ~ hd);
                }
            }
            // tie inner run follows the inside of the shell; check A and B inner/outer surfaces line up across the step (s = earFront)
            const sE2 = definition.earFront / mm;
            var mis = 0;
            for (var yy in [24, 27, 30, 33, 36, 40])
            {
                var d = [];
                for (var sv in [sE2 - 0.15, sE2 + 0.15])
                {
                    const fr = arcFrame(R, sv, 0);
                    const hits = evRaycast(context, { "entities" : qUnion([qA, qB]), "ray" : line(fr.origin + fr.rn * 6 * mm + Y * yy * mm, -fr.rn), "closest" : false });
                    d = append(d, size(hits) >= 2 ? [hits[0].distance / mm, hits[1].distance / mm] : [0, 0]);
                }
                mis = max([mis, abs(d[0][0] - d[1][0]), abs(d[0][1] - d[1][1])]);
            }
            println("step joint surface mismatch A vs B at s=" ~ sE2 ~ " (y 24..40, outer+inner): max " ~ roundToPrecision(mis, 3) ~ " mm -> tie inner run continuous");
        }
        const tyre2 = makeTyres(context, id + "tyres2", definition);
        println("min clearance to tyre [maxTyre, Assegai]: A=" ~ roundToPrecision(evDistance(context, { "side0" : qA, "side1" : tyre2[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qA, "side1" : tyre2[1] }).distance / mm, 2) ~ " B=" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : tyre2[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : tyre2[1] }).distance / mm, 2));
        for (var qq in [qA, qB])
        {
            const dd2 = evDistance(context, { "side0" : qq, "side1" : tyre2[0] });
            println("  closest point to max tyre: " ~ toString(roundToPrecision(dd2.sides[0].point[0] / mm, 1)) ~ "," ~ toString(roundToPrecision(dd2.sides[0].point[1] / mm, 1)) ~ "," ~ toString(roundToPrecision(dd2.sides[0].point[2] / mm, 1)));
        }
        const legDbg = makeLegs(context, id + "legdbg", definition, xC, zB, 0, [-1, 1]);
        const dl2 = evDistance(context, { "side0" : qB, "side1" : qUnion(legDbg) });
        println("  B closest point to legs: " ~ roundToPrecision(dl2.sides[0].point[0] / mm, 1) ~ "," ~ roundToPrecision(dl2.sides[0].point[1] / mm, 1) ~ "," ~ roundToPrecision(dl2.sides[0].point[2] / mm, 1));
        opDeleteBodies(context, id + "legDbgDel", { "entities" : qUnion(legDbg) });
        {
            // v2.12 join check: lateral rays onto each side of B over the sheet span, from the tip up to 16 mm above the wall edge
            var maxOut = 0;
            var sheetDev = 0;
            var maxJump = 0;
            var jumpAt = "";
            var nR = 0;
            for (var sg in [-1, 1])
            {
                for (var xo = -hw + 1; xo <= hw - 1; xo += 3)
                {
                    var prev = undefined;
                    for (var zz = zEdge - 40; zz <= zEdge + 16; zz += 0.5)
                    {
                        const hits = evRaycast(context, { "entities" : qB, "ray" : line(vector(xC + xo, sg * 80, zz) * mm, vector(0, -sg, 0)), "closest" : true });
                        if (size(hits) == 0)
                        {
                            prev = undefined;
                            continue;
                        }
                        const yy = abs(hits[0].intersection[1] / mm);
                        nR += 1;
                        maxOut = max(maxOut, yy);
                        if (zz < zEdge - 3 && yy > yo - 1)
                            sheetDev = max(sheetDev, abs(yy - yo));
                        if (prev != undefined && abs(yy - prev) < 4 && abs(xo) > definition.sealWindowWidth / mm / 2 + 0.5 && abs(yy - prev) > maxJump)
                        {
                            maxJump = abs(yy - prev);
                            jumpAt = "x" ~ roundToPrecision(xC + xo, 1) ~ " z" ~ roundToPrecision(zz, 1) ~ " y" ~ roundToPrecision(sg * yy, 2) ~ " prev " ~ roundToPrecision(sg * prev, 2);
                        }
                        prev = (abs(yy - (prev == undefined ? yy : prev)) < 4) ? yy : undefined;
                    }
                }
            }
            var plA = 0;
            var nPlF = 0;
            for (var fc in evaluateQuery(context, qOwnedByBody(qB, EntityType.FACE)))
            {
                const pl = try silent(evPlane(context, { "face" : fc }));
                if (pl != undefined && abs(pl.normal[1]) > 0.99999 && abs(abs(pl.origin[1] / mm) - yo) < 0.01)
                {
                    plA += evArea(context, { "entities" : fc }) / mm / mm;
                    nPlF += 1;
                }
            }
            println("sheet outer planes at |y|=" ~ yo ~ ": " ~ nPlF ~ " faces, " ~ roundToPrecision(plA, 0) ~ " mm2; any B face beyond the plane: " ~ (evBox3d(context, { "topology" : qB, "tight" : true }).maxCorner[1] / mm - yo > 0.01 || -evBox3d(context, { "topology" : qB, "tight" : true }).minCorner[1] / mm - yo > 0.01));
            println("join check (" ~ nR ~ " lateral rays, sheet span, tip..edge+16): max |y| " ~ roundToPrecision(maxOut, 3) ~ " (plane " ~ yEdge ~ "), sheet plane deviation " ~ roundToPrecision(sheetDev, 3) ~
                " mm, max surface jump between 0.5 mm rows (strips) " ~ roundToPrecision(maxJump, 3) ~ " at " ~ jumpAt ~ " mm (19 deg wall alone = " ~ roundToPrecision(0.5 * tan(19 * degree), 3) ~ ")");
        }
        {
            // v2.13 inside join check: rays from the tyre side outward onto the strips (front and rear of the seal window)
            var maxJ = 0;
            var jAt = "";
            var nI = 0;
            var nStep = 0;
            var jList = [];
            for (var sg in [-1, 1])
            {
                for (var xo = -hw + 0.5; xo <= hw - 0.5; xo += 1)
                {
                    if (abs(xo) < definition.sealWindowWidth / mm / 2 + 0.5)
                        continue;
                    var prev = undefined;
                    for (var zz = zEdge - 16; zz <= zEdge + 12; zz += 0.25)
                    {
                        const hits = evRaycast(context, { "entities" : qB, "ray" : line(vector(xC + xo, sg * 30, zz) * mm, vector(0, sg, 0)), "closest" : true });
                        if (size(hits) == 0)
                        {
                            prev = undefined;
                            continue;
                        }
                        const yy = abs(hits[0].intersection[1] / mm);
                        nI += 1;
                        if (prev != undefined && abs(yy - prev) < 5)
                        {
                            const j = abs(yy - prev);
                            if (j > 0.15)
                            {
                                nStep += 1;
                                if (nStep <= 14)
                                    jList = append(jList, [roundToPrecision(sg * (xC + xo), 1), roundToPrecision(zz - zEdge, 2), roundToPrecision(prev, 2), roundToPrecision(yy, 2)]);
                            }
                            if (j > maxJ)
                            {
                                maxJ = j;
                                jAt = "x" ~ roundToPrecision(xC + xo, 1) ~ " z" ~ roundToPrecision(zz, 2) ~ " (zEdge" ~ (zz - zEdge >= 0 ? "+" : "") ~ roundToPrecision(zz - zEdge, 2) ~ ") |y| " ~ roundToPrecision(yy, 3) ~ " prev " ~ roundToPrecision(prev, 3);
                            }
                        }
                        prev = yy;
                    }
                }
            }
            println("INSIDE join check (" ~ nI ~ " rays on the strips, zEdge-16..+12 at 0.25): max jump between rows " ~ roundToPrecision(maxJ, 3) ~ " at " ~ jAt ~
                " ; row steps > 0.15 mm: " ~ nStep ~ " " ~ toString(jList) ~ " (19 deg wall alone = " ~ roundToPrecision(0.25 * tan(19 * degree), 3) ~ ")");
        }
        {
            // v2.14 section probe at s = 0 (under the brace centre): outer/inner surface points (y, d below R_top), part A
            const f0 = arcFrame(R, 0, 0);
            var ot = []; var it = []; var ol = []; var il = [];
            for (var yy = -50; yy <= 50; yy += 1)
            {
                const h = evRaycast(context, { "entities" : qA, "ray" : line(f0.origin + f0.rn * 20 * mm + Y * yy * mm, -f0.rn), "closest" : false });
                if (size(h) > 0)
                    ot = append(ot, [yy, roundToPrecision(h[0].distance / mm - 20, 2)]);
                if (size(h) > 1)
                    it = append(it, [yy, roundToPrecision(h[1].distance / mm - 20, 2)]);
            }
            for (var dd = 0; dd <= 40; dd += 1)
            {
                for (var sg in [-1, 1])
                {
                    const h = evRaycast(context, { "entities" : qA, "ray" : line(f0.origin - f0.rn * dd * mm + Y * sg * 80 * mm, Y * -sg), "closest" : true });
                    if (size(h) > 0)
                        ol = append(ol, [roundToPrecision(sg * (80 - h[0].distance / mm), 2), dd]);
                    const h2 = evRaycast(context, { "entities" : qA, "ray" : line(f0.origin - f0.rn * dd * mm, Y * sg), "closest" : true });
                    if (size(h2) > 0)
                        il = append(il, [roundToPrecision(sg * h2[0].distance / mm, 2), dd]);
                }
            }
            println("SECTION0 " ~ toString({ "outerTop" : ot, "innerTop" : it, "outerLat" : ol, "innerLat" : il }));
        }
        opDeleteBodies(context, id + "tyres2Del", { "entities" : qUnion(tyre2) });
        // wing slots: lateral rays through each slot centre
        var openSl = 0;
        var totSl = 0;
        for (var sg in [-1, 1])
        {
            for (var rowD in dRows)
            {
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    const org = vector(xC - sp, sg * (ySh(rowD) - wT - 4), zTop - rowD) * mm;
                    const hits = evRaycast(context, { "entities" : qB, "ray" : line(org, vector(0, sg, 0)), "closest" : true });
                    var blocked = false;
                    if (size(hits) > 0)
                        blocked = hits[0].distance < (wT / cos(spl) + 5) * mm;
                    if (!blocked)
                        openSl += 1;
                    totSl += 1;
                }
            }
        }
        println("wing slots open: " ~ openSl ~ "/" ~ totSl);
        var blkUp = 0;
        var totUp = 0;
        for (var sg in [-1, 1])
        {
            for (var rowD in [rowA, rowB, rowC])
            {
                if (abs(rowD - dBot) < 0.05)
                    continue;
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    const org = vector(xC - sp, sg * (ySh(rowD) - wT - 4), zTop - rowD) * mm;
                    const hits = evRaycast(context, { "entities" : qB, "ray" : line(org, vector(0, sg, 0)), "closest" : true });
                    var blocked = false;
                    if (size(hits) > 0)
                        blocked = hits[0].distance < (wT / cos(spl) + 5) * mm;
                    if (blocked)
                        blkUp += 1;
                    totUp += 1;
                }
            }
        }
        println("wing upper rows solid: " ~ blkUp ~ "/" ~ totUp);
        println("sheet outer face |y| at pivot " ~ yo ~ "; tip z=" ~ roundToPrecision(zTop - dTip, 1) ~ " (D=" ~ (dTip + pad) ~ " below brace)");
        const legQs = makeLegs(context, id + "legchk", definition, xC, zB, 0, [-1, 1]);
        const legTy = makeTyres(context, id + "tyres3", definition);
        println("leg model (measured taper): centre |y|=" ~ yLeg ~ ", min dist guard A/B to legs: " ~ roundToPrecision(evDistance(context, { "side0" : qA, "side1" : qUnion(legQs) }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : qUnion(legQs) }).distance / mm, 2) ~
            " ; legs to tyre [max, Assegai]: " ~ roundToPrecision(evDistance(context, { "side0" : qUnion(legQs), "side1" : legTy[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qUnion(legQs), "side1" : legTy[1] }).distance / mm, 2) ~
            " ; A/legs, B/legs clashes: " ~ size(evCollision(context, { "tools" : qA, "targets" : qUnion(legQs) })) ~ "/" ~ size(evCollision(context, { "tools" : qB, "targets" : qUnion(legQs) })));
        opDeleteBodies(context, id + "legsDel", { "entities" : qUnion(concatenateArrays([legQs, legTy])) });
        // v2.19.3: one string, also the throw message. Collar and OD 45 are reported and do not fail the build.
        var fail = "";
        const vA = evVolume(context, { "entities" : qA }) / cm3;
        const vB = evVolume(context, { "entities" : qB }) / cm3;
        // ±8% around offline A 62.5 / B 125.8 (v2.19.2 measured 62.48 / 135.61; joint voxel model -10.16, C1/skin +0.33)
        if (vA < 57.5 || vA > 67.5)
            fail = fail ~ " volA=" ~ roundToPrecision(vA, 2);
        if (vB < 115.7 || vB > 135.8)
            fail = fail ~ " volB=" ~ roundToPrecision(vB, 2);
        if (size(evaluateQuery(context, qA)) != 1 || size(evaluateQuery(context, qB)) != 1)
            fail = fail ~ " parts";
        if (openSl != 4 || totSl != 4)
            fail = fail ~ " holes " ~ openSl ~ "/" ~ totSl;
        if (blkUp != 8 || totUp != 8)
            fail = fail ~ " upper " ~ blkUp ~ "/" ~ totUp;
        const faceAt = function(DD) {
                    const hits = evRaycast(context, { "entities" : qB, "ray" : line(vector(xC, 70, zB - DD) * mm, vector(0, -1, 0)), "closest" : true });
                    return size(hits) == 0 ? 999 : abs(hits[0].intersection[1] / mm);
                };
        const yBow = faceAt(pad + jBelly[1]);
        const yFlat = faceAt(110);
        println("v2.19.3 face |y| at xC: bow=" ~ roundToPrecision(yBow, 3) ~ " D110=" ~ roundToPrecision(yFlat, 3));
        if (yBow > jBelly[0] + 0.6 || yBow < jBelly[0] - 0.6)
            fail = fail ~ " bow";
        if (abs(yFlat - yo) > 0.05)
            fail = fail ~ " flat";
        const bbB = evBox3d(context, { "topology" : qB, "tight" : true });
        if (bbB.maxCorner[1] / mm > yo + 0.4 || -bbB.minCorner[1] / mm > yo + 0.4)
            fail = fail ~ " bump";
        {
            const hits = evRaycast(context, { "entities" : qB, "ray" : line(vector(xC, 70, zB - 50) * mm, vector(0, -1, 0)), "closest" : true });
            var solidW = false;
            if (size(hits) > 0)
                solidW = hits[0].distance / mm < 40;
            if (!solidW)
                fail = fail ~ " window";
        }
        // positive collar = mm the bowed face sits inside the OD 55 radius. Not a failure.
        const collarIntr = yBow - (yLeg - definition.sealCollarOD / mm / 2);
        const od45 = (yLeg - yFlat) - definition.ph6 / mm / 2;
        const rC = definition.sealCollarOD / mm / 2;
        const rL = definition.ph6 / mm / 2;
        var legMin = 999;
        for (var kD = 0; kD <= 16; kD += 1)
        {
            const DD = Dbow0 + kD * 5;
            const yy = faceAt(DD);
            const rr = (DD <= 20) ? rC : ((DD >= 90) ? rL : (rC + (rL - rC) * (DD - 20) / 70));
            const gap = (yLeg - yy) - rr;
            if (gap < legMin)
                legMin = gap;
        }
        println("v2.19.3 collarIntr " ~ roundToPrecision(collarIntr, 2) ~ " od45 " ~ roundToPrecision(od45, 2) ~ " legMin " ~ roundToPrecision(legMin, 2));
        // ---------- plan 2.7: joint ray checks ----------
        const jRay = function(sv, vv, sg, fromOut) {
                    const th = (sv / R) * radian;
                    const p0 = vector(-sin(th), 0, cos(th)) * (R - vv) * mm;
                    const org = p0 + vector(0, sg * (fromOut ? 80 : 30), 0) * mm;
                    const hits = evRaycast(context, { "entities" : qB, "ray" : line(org, vector(0, fromOut ? -sg : sg, 0)), "closest" : true });
                    return size(hits) == 0 ? -1 : abs(hits[0].intersection[1] / mm);
                };
        // (2/3a) v = 25 across the band ends: guard and root walls on one line, outside and inside
        var jV25 = 0;
        var jV25miss = 0;
        for (var sv in [20, jS0, jS0 + 1, 0.5 * (jS0 + jS1), jS1 - 1, jS1, sRev1])
        {
            for (var sg in [-1, 1])
            {
                const uO = jRay(sv, 25, sg, true);
                const uI = jRay(sv, 25, sg, false);
                if (uO < 0 || uI < 0)
                {
                    jV25miss += 1;
                }
                else
                {
                    jV25 = max([jV25, abs(uO - jUo(25)), abs(uI - jUi(25))]);
                }
            }
        }
        // (2/3b) profile scan v 21..80 inside the window, outside and inside, against the analytic chains
        var jStepO = 0;
        var jStepI = 0;
        var jStepAt = "";
        var jNscan = 0;
        for (var sv in [jS0 + 1, 0.5 * (jS0 + jS1), jS1 - 1])
        {
            for (var sg in [-1, 1])
            {
                for (var vv = 21; vv <= 80; vv += 0.5)
                {
                    const uO = jRay(sv, vv, sg, true);
                    const uI = jRay(sv, vv, sg, false);
                    jNscan += 1;
                    const eO = uO < 0 ? 99 : abs(uO - jUo(vv));
                    const eI = uI < 0 ? 99 : abs(uI - jUi(vv));
                    if (eO > jStepO)
                    {
                        jStepO = eO;
                        jStepAt = jStepAt ~ " out(s" ~ roundToPrecision(sv, 1) ~ ",v" ~ vv ~ ")=" ~ roundToPrecision(eO, 3);
                    }
                    if (eI > jStepI)
                    {
                        jStepI = eI;
                        jStepAt = jStepAt ~ " in(s" ~ roundToPrecision(sv, 1) ~ ",v" ~ vv ~ ")=" ~ roundToPrecision(eI, 3);
                    }
                }
            }
        }
        println("v2.19.3 joint rays: v25 dev " ~ roundToPrecision(jV25, 4) ~ " miss " ~ jV25miss ~ "; scan " ~ jNscan ~ " rows, max out " ~
            roundToPrecision(jStepO, 4) ~ " in " ~ roundToPrecision(jStepI, 4) ~ " ;" ~ jStepAt);
        if (jV25 > 0.02 || jV25miss > 0)
            fail = fail ~ " v25";
        if (jStepO > 0.05)
            fail = fail ~ " stepOut";
        if (jStepI > 0.05)
            fail = fail ~ " stepIn";
        if (jTiny > 0)
            fail = fail ~ " tiny";
        var dSt = 0;
        var nSt = 0;
        {
            var st = [];
            for (var sg in [-1, 1])
            {
                const sid2 = id + ("stn" ~ (sg + 1));
                const sk2 = newSketchOnPlane(context, sid2 + "sk", { "sketchPlane" : plane(vector(xC, sg * yLeg, 0) * mm, vector(1, 0, 0), vector(0, 1, 0)) });
                skPolyline(sk2, "pl", { "points" : [vector(0, zB - 40) * mm, vector(18, zB - 40) * mm, vector(18, zB + 30) * mm, vector(0, zB + 30) * mm, vector(0, zB - 40) * mm] });
                skSolve(sk2);
                opRevolve(context, sid2 + "rv", { "entities" : qSketchRegion(sid2 + "sk"), "axis" : line(vector(xC, sg * yLeg, 0) * mm, vector(0, 0, 1)), "angleForward" : 360 * degree });
                st = append(st, qCreatedBy(sid2 + "rv", EntityType.BODY));
            }
            dSt = evDistance(context, { "side0" : qB, "side1" : qUnion(st) }).distance / mm;
            nSt = size(evCollision(context, { "tools" : qB, "targets" : qUnion(st) }));
            println("v2.19.1 stanchion " ~ roundToPrecision(dSt, 2) ~ " clashes " ~ nSt);
            if (dSt < 0.2 || nSt != 0)
                fail = fail ~ " stanchion";
            opDeleteBodies(context, id + "v219tmp", { "entities" : qUnion(st) });
        }
        const fitA = sideFit(context, qA);
        const fitB = sideFit(context, qB);
        if (fitA.minSquare.L > 250 || fitA.minSquare.W > 250 || fitB.minSquare.L > 250 || fitB.minSquare.W > 250)
            fail = fail ~ " bed";
        if (size(evaluateQuery(context, qAllModifiableSolidBodies())) != 2)
            fail = fail ~ " nSolids=" ~ size(evaluateQuery(context, qAllModifiableSolidBodies()));
        const tyreR = makeTyres(context, id + "tyreR", definition);
        const tyreGap = evDistance(context, { "side0" : qB, "side1" : tyreR[0] }).distance / mm;
        opDeleteBodies(context, id + "tyreRdel", { "entities" : qUnion(tyreR) });
        if (tyreGap < 8)
            fail = fail ~ " tyre";
        const collarTxt = collarIntr >= 0 ? (roundToPrecision(collarIntr, 2) ~ "in") : (roundToPrecision(-collarIntr, 2) ~ "clr");
        const odTxt = od45 >= 0 ? (roundToPrecision(od45, 2) ~ "clr") : (roundToPrecision(-od45, 2) ~ "in");
        const report = "v2.19.3.1 volA=" ~ roundToPrecision(vA, 2) ~ " volB=" ~ roundToPrecision(vB, 2) ~
            " tyreGap=" ~ roundToPrecision(tyreGap, 2) ~ " stanchion=" ~ roundToPrecision(dSt, 2) ~
            " collar=" ~ collarTxt ~ " od45=" ~ odTxt ~
            " legMin=" ~ roundToPrecision(legMin, 2) ~
            " holes=" ~ openSl ~ "/" ~ totSl ~ " bedB=" ~ fitB.minSquare.L ~ "x" ~ fitB.minSquare.W ~
            " bowY=" ~ roundToPrecision(yBow, 2) ~ " bowV=" ~ roundToPrecision(jBelly[1], 2) ~ " flatY=" ~ roundToPrecision(yFlat, 2) ~
            " root=s" ~ jS0 ~ "-" ~ jS1 ~ " v25=" ~ roundToPrecision(jV25, 3) ~ " stepOut=" ~ roundToPrecision(jStepO, 3) ~
            " stepIn=" ~ roundToPrecision(jStepI, 3) ~ " tiny=" ~ jTiny ~ " fil=" ~ jFil ~ "/" ~ jFilN;
        if (fail != "")
            throw regenError(report ~ " FAIL:" ~ fail);
        println(report ~ " PASS");
        println("R_top=" ~ R ~ " inner top (waist)=" ~ (R - ww) ~ " tyre+clr=" ~ ((definition.tyreRadius + definition.clearance) / mm));
        println("sM(mating s)=" ~ sM ~ " sFront=" ~ sF);
    }, {
            "ph1" : 395 * millimeter, "ph2" : 100 * millimeter, "ph3" : 20 * millimeter, "ph4" : 25 * millimeter,
            "ph5" : 60 * millimeter, "ph6" : 45 * millimeter, "legGapUpper" : 85 * millimeter, "legTaperTop" : 40 * millimeter, "legTaperEnd" : 90 * millimeter, "legGapTop" : 85 * millimeter, "guardWidth" : 82 * millimeter, "legZoneWidth" : 97 * millimeter, "legZoneTopFlat" : 76 * millimeter, "frontWidth" : 97 * millimeter, "tailWidth" : 97 * millimeter, "ph7" : 0 * degree,
            "tyreRadius" : 375 * millimeter, "clearance" : 16 * millimeter, "maxTyreWidth" : 69.2 * millimeter, "tyreWidth" : 63.5 * millimeter, "padThk" : 1.5 * millimeter,
            "frontLen" : 165 * millimeter, "tailLen" : 315 * millimeter, "legTieOffset" : 45 * millimeter,
            "wall" : 2.5 * millimeter, "waistWall" : 3.0 * millimeter, "kickLen" : 25 * millimeter, "kickAngle" : 12 * degree,
            "braceTieFront" : 12.5 * millimeter, "braceTieRear" : 12.5 * millimeter, "earFront" : 7 * millimeter, "braceTieY" : 28 * millimeter, "flangeWidth" : 46 * millimeter,
            "flangeHeight" : 14 * millimeter, "flangeAThk" : 8 * millimeter, "flangeBThk" : 4 * millimeter,
            "boltSpacing" : 15 * millimeter, "boltDia" : 3.4 * millimeter, "nutWidth" : 6.15 * millimeter, "nutThk" : 4.2 * millimeter, "wingThk" : 2.5 * millimeter, "wingWidth" : 56 * millimeter, "wingRootFront" : -28 * millimeter, "wingRootRear" : 28 * millimeter, "wingSplay" : 0 * degree, "sealWindow" : false, "sealWindowWidth" : 40 * millimeter, "sealWindowTop" : 12 * millimeter, "sealWindowBottom" : 84 * millimeter, "sealIndent" : true, "sealCollarOD" : 55 * millimeter, "sealRadialClearance" : 1.5 * millimeter, "sealIndentTop" : 0 * millimeter, "sealIndentLength" : 22 * millimeter, "sealIndentTaper" : 64 * millimeter, "ribEnable" : true, "ribHeight" : 2 * millimeter, "ribWidth" : 3 * millimeter,
            "wingFlareDepth" : 20 * millimeter, "wingTipDepth" : 135 * millimeter, "wingCornerR" : 8 * millimeter,
            "wingRow1" : 90 * millimeter, "wingRow2" : 110 * millimeter, "wingRow3" : 125 * millimeter,
            "wingSlotPitch" : 22 * millimeter, "strapChannel" : true, "tieCutDepth" : 20 * millimeter, "teardropAngle" : 90 * degree,
            "addRib" : false, "addFillets" : true, "smoothJoin" : true, "sculpted" : true, "c1Depth" : 30.49 * millimeter, "c1Radius" : 18 * millimeter, "awlChord" : 94 * millimeter, "awlFloorW" : 100 * millimeter, "awlRise" : 7 * millimeter, "roundR" : 20 * millimeter, "roundAlpha" : 5 * degree, "joinFilletOut" : 4 * millimeter, "joinFilletIn" : 4 * millimeter
        });
