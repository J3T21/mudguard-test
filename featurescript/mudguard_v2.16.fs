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
        // v2.14 sculpted sections (Reference Analyst brace_arch.md): rounded-box U everywhere except the leg zone.
        // Under the brace "C1": flat top +-23, R18 corners, vertical walls at +-41 (82 wide), c1Depth deep (v2.14: 30.49 = leg-zone depth, no wall dip at the 10 -> 22 blend), s -15..+10.
        // Front: 97 wide, 5 deg walls, R20 corners. Front blend via s -35 (w90 d30 a8 r14); rear blend 10 -> 22. v2.14a: tail kept at v2.13 19 deg / r6.
        const dC = def.c1Depth / millimeter;
        const rC = def.c1Radius / millimeter;
        const aR = def.roundAlpha / degree;
        const rR = def.roundR / millimeter;
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
            [-15, W, dC, 0, rC, ww],
            [0, W, dC, 0, rC, ww],
            [10, W, dC, 0, rC, ww],   // v2.14a: C1 ends at s 10 (as v2.13), 10 -> 22 blend
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
    }
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
        out = append(out, { "s" : s, "w" : row[1], "d" : row[2], "alpha" : row[3], "r" : row[4], "wall" : row[5], "a" : a, "k" : k, "R" : R });
    }
    return out;
}

function loftStations(context is Context, id is Id, st is array, mode is string, bossH is number) returns Query
{
    var profs = [];
    for (var i = 0; i < size(st); i += 1)
    {
        const S = st[i];
        const fr = arcFrame(S.R, S.s, S.k);
        const sid = id + ("p" ~ i);
        if (mode == "out")
            profSketch(context, sid, fr.plane, S.a, S.alpha, S.r, 0, S.d);
        else if (mode == "deep")
            profSketch(context, sid, fr.plane, S.a, S.alpha, S.r, 0.05, S.d + 12); // v2.14: 12 mm (was 6) so the trim reaches the sheet plane at the sheet front corner
        else if (mode == "in")
            profSketch(context, sid, fr.plane, S.a, S.alpha, S.r, S.wall, S.d + 10);
        else
            profSketch(context, sid, fr.plane, S.a, S.alpha, S.r, -bossH, S.d);
        profs = append(profs, qSketchRegion(sid));
    }
    opLoft(context, id + "loft", { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    opDeleteBodies(context, id + "delsk", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qCreatedBy(id + "loft", EntityType.BODY);
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


// v2.13: revolve a closed (u = |y|, v = depth below radius R) section about the axle (world Y) over +-6 deg around s = sSk,
// then clip to the sheet's x range. sg = side sign. arc = [] or [startIdx, mid(u,v)] (arc from pts[startIdx] to pts[startIdx+1]).
function revolveSection(context is Context, rid is Id, R is number, sSk is number, sg is number, pts is array, arc is array, x0 is number, x1 is number) returns Query
{
    const mm = millimeter;
    const fr = arcFrame(R, sSk, 0);
    const sk = newSketchOnPlane(context, rid + "sk", { "sketchPlane" : fr.plane });
    const n = size(pts);
    for (var i = 0; i < n; i += 1)
    {
        const a = vector(sg * pts[i][0], pts[i][1]) * mm;
        const b = vector(sg * pts[(i + 1) % n][0], pts[(i + 1) % n][1]) * mm;
        if (size(arc) > 0 && i == arc[0])
            skArc(sk, "a" ~ i, { "start" : a, "mid" : vector(sg * arc[1][0], arc[1][1]) * mm, "end" : b });
        else
            skLineSegment(sk, "l" ~ i, { "start" : a, "end" : b });
    }
    skSolve(sk);
    opRevolve(context, rid + "rv", { "entities" : qSketchRegion(rid + "sk"), "axis" : line(vector(0, 0, 0) * mm, vector(0, 1, 0)),
                "angleForward" : 6 * degree, "angleBack" : 6 * degree });
    opDeleteBodies(context, rid + "delSk", { "entities" : qCreatedBy(rid + "sk", EntityType.BODY) });
    const rv = qCreatedBy(rid + "rv", EntityType.BODY);
    const bx = mkBox(context, rid + "bx", vector(x0, sg * 20, 250), vector(x1, sg * 70, 450));
    opBoolean(context, rid + "clip", { "tools" : qUnion([rv, bx]), "operationType" : BooleanOperationType.INTERSECTION });
    const res = qUnion([qCreatedBy(rid + "rv", EntityType.BODY), qCreatedBy(rid + "bx", EntityType.BODY), qCreatedBy(rid + "clip", EntityType.BODY)]);
    if (size(evaluateQuery(context, res)) == 0)
        throw regenError("revolveSection " ~ toString(rid) ~ ": empty after clip");
    return res;
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
    const ySide = stEnd.w / 2;
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
        skLineSegment(sk, "l1", { "start" : vector(-6, yOut * (ySide + 10)) * mm, "end" : vector(-6, yOut * (yc - 4)) * mm });
        skLineSegment(sk, "l2", { "start" : vector(-6, yOut * (yc - 4)) * mm, "end" : vector(0, yOut * (yc - 4)) * mm });
        skLineSegment(sk, "l3", { "start" : vector(0, yOut * (yc - 4)) * mm, "end" : vector(0, yOut * yc) * mm });
        const midU = rc * (1 - 1 / sqrt(2));
        const midY = yc + rc / sqrt(2);
        skArc(sk, "a", { "start" : vector(0, yOut * yc) * mm, "mid" : vector(midU, yOut * midY) * mm, "end" : vector(rc, yOut * ySide) * mm });
        skLineSegment(sk, "l4", { "start" : vector(rc, yOut * ySide) * mm, "end" : vector(rc, yOut * (ySide + 10)) * mm });
        skLineSegment(sk, "l5", { "start" : vector(rc, yOut * (ySide + 10)) * mm, "end" : vector(-6, yOut * (ySide + 10)) * mm });
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

annotation { "Feature Type Name" : "Mudguard V2.16 (Fox AWL 36)" }
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
        isLength(definition.nutWidth, { (millimeter) : [5.5, 5.95, 7] } as LengthBoundSpec);
        annotation { "Name" : "Nyloc slot thickness" }
        isLength(definition.nutThk, { (millimeter) : [4, 4.5, 5.5] } as LengthBoundSpec);
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
        annotation { "Name" : "Seal window bottom, below brace underside" }
        isLength(definition.sealWindowBottom, { (millimeter) : [20, 84, 120] } as LengthBoundSpec);
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
        annotation { "Name" : "v2.14 sculpted sections (C1 under the brace, rounded-box U front/tail)" }
        definition.sculpted is boolean;
        annotation { "Name" : "C1 depth under the brace" }
        isLength(definition.c1Depth, { (millimeter) : [18, 30.49, 34] } as LengthBoundSpec);
        annotation { "Name" : "C1 corner radius" }
        isLength(definition.c1Radius, { (millimeter) : [6, 18, 24] } as LengthBoundSpec);
        annotation { "Name" : "Front/tail corner radius" }
        isLength(definition.roundR, { (millimeter) : [6, 20, 26] } as LengthBoundSpec);
        annotation { "Name" : "Front/tail wall angle" }
        isAngle(definition.roundAlpha, { (degree) : [0, 5, 19] } as AngleBoundSpec);
        annotation { "Name" : "v2.13 inside blend (sheet inner face to wall)" }
        definition.innerBlend is boolean;
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

        // ---------- 1. outer solid ----------
        const fout = loftStations(context, id + "out", st, "out", 0);
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
        const fboss = loftStations(context, id + "boss", st, "boss", bossH);
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

        // ---------- 4. v2.8 Mudhugger-style leg sheets ----------
        // A flat 2.5 mm sheet per side that continues straight on from the bottom edge of the guard side wall (outer faces
        // flush), splayed out from vertical by wingSplay, centred on the leg (s = legTieOffset). Relaxed (as-printed) shape;
        // the ties flex it onto the lower leg. Optional rectangular window around the seal bulge if the plane clashes.
        const wc = definition.legTieOffset / mm;
        const thC = (wc / R) * radian;
        const xC = -R * sin(thC);
        const zTop = R * cos(thC);
        const wT = definition.wingThk / mm;
        const pad = definition.padThk / mm;
        const Wg = definition.legZoneWidth / mm;
        const tA19 = tan(19 * degree);
        const dEdge = (Wg - definition.legZoneTopFlat / mm) / (2 * tA19); // wall depth in the leg zone (= station depth dL)
        const yEdge = Wg / 2;                                         // outer wall bottom edge |y|
        const zEdge = zTop - dEdge;
        const spl = definition.wingSplay;
        const yo = yEdge;                                             // outer face at the pivot (sheet is flush with the wall)
        const yi = yo - wT;
        const dT0 = definition.legTaperTop / mm;
        const dT1 = definition.legTaperEnd / mm;
        const yLeg = definition.ph2 / mm / 2 + definition.ph6 / mm / 2;
        const zB = zTop + pad;
        const hw = definition.wingWidth / mm / 2;
        const rootF = definition.wingRootFront / mm;                  // su of root front edge (rel. leg axis, negative = forward)
        const rootR = definition.wingRootRear / mm;
        const dFl = definition.wingFlareDepth / mm;                   // flare from root width to paddle width over this length
        const dTip = definition.wingTipDepth / mm - pad;              // depth below guard top at the tip (vertical, before splay)
        const dRows = [definition.wingRow1 / mm - pad, definition.wingRow2 / mm - pad, definition.wingRow3 / mm - pad];
        const rc = definition.wingCornerR / mm;
        const ov = 6;                                                 // overlap up into the wall for a solid union
        // outer-face |y| of the splayed sheet at guard depth dv
        const ySh = function(dv) { return yEdge + max(0, dv - dEdge) * tan(spl); };
        var wingBodies = [];
        var wingCuts = [];
        var windowUsed = [];
        var wi = 0;
        for (var sg in [-1, 1])
        {
            const wid = id + ("wing" ~ wi);
            // build vertical (in the plane y = sg*yo outer face, extruded inwards), then rotate about the wall edge line
            const yPl = (sg > 0) ? yi : -yo;
            const pl = plane(vector(xC, yPl, 0) * mm, vector(0, 1, 0), vector(-1, 0, 0));
            const sk = newSketchOnPlane(context, wid + "sk", { "sketchPlane" : pl });
            const P = function(su, dv) { return vector(su, zTop - dv) * mm; };
            const c45 = cos(45 * degree);
            const d0 = dEdge - ov;
            skLineSegment(sk, "top", { "start" : P(rootF, d0), "end" : P(rootR, d0) });
            skLineSegment(sk, "r0", { "start" : P(rootR, d0), "end" : P(rootR, dEdge) });
            skLineSegment(sk, "rf", { "start" : P(rootR, dEdge), "end" : P(hw, dEdge + dFl) });
            skLineSegment(sk, "rear", { "start" : P(hw, dEdge + dFl), "end" : P(hw, dTip - rc) });
            skArc(sk, "cR", { "start" : P(hw, dTip - rc), "mid" : P(hw - rc + rc * c45, dTip - rc + rc * c45), "end" : P(hw - rc, dTip) });
            skLineSegment(sk, "tip", { "start" : P(hw - rc, dTip), "end" : P(-hw + rc, dTip) });
            skArc(sk, "cF", { "start" : P(-hw + rc, dTip), "mid" : P(-hw + rc - rc * c45, dTip - rc + rc * c45), "end" : P(-hw, dTip - rc) });
            skLineSegment(sk, "front", { "start" : P(-hw, dTip - rc), "end" : P(-hw, dEdge + dFl) });
            skLineSegment(sk, "ff", { "start" : P(-hw, dEdge + dFl), "end" : P(rootF, dEdge) });
            skLineSegment(sk, "f0", { "start" : P(rootF, dEdge), "end" : P(rootF, d0) });
            skSolve(sk);
            const wq0 = extrudeSk(context, wid + "ex", wid + "sk", vector(0, 1, 0), wT);
            // root band: fills the wedge between the 19 deg wall and the sheet plane so the outer face is flush and the union is solid
            const band = mkBox(context, wid + "band", vector(xC + hw, sg * (yo - 7), zTop - dEdge + 13), vector(xC - hw, sg * yo, zTop - dEdge - 1));
            opBoolean(context, wid + "bandU", { "tools" : qUnion([wq0, band]), "operationType" : BooleanOperationType.UNION });
            const wq = qUnion(evaluateQuery(context, qContainsPoint(qAllModifiableSolidBodies(), vector(xC + hw - 3, sg * (yo - wT / 2), zTop - dTip + 3) * mm)));
            // tie slots (long side along the leg) and tyre-side strap channels, cut in the vertical frame
            var cuts = [];
            var k2 = 0;
            for (var rowD in dRows)
            {
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    cuts = append(cuts, mkBox(context, wid + ("sl" ~ k2), vector(xC - sp - 1.2, sg * (yi - 1), zTop - rowD - 3),
                                vector(xC - sp + 1.2, sg * (yo + 1), zTop - rowD + 3)));
                    k2 += 1;
                }
                if (definition.strapChannel)
                {
                    const hp = definition.wingSlotPitch / mm / 2;
                    cuts = append(cuts, mkBox(context, wid + ("ch" ~ k2), vector(xC - hp, sg * (yi - 1), zTop - rowD - 2.75),
                                vector(xC + hp, sg * (yi + 0.8), zTop - rowD + 2.75)));
                }
            }
            opBoolean(context, wid + "slotCut", { "targets" : wq, "tools" : qUnion(cuts), "operationType" : BooleanOperationType.SUBTRACTION });
            // splay: rotate about the outer wall-edge line so the sheet's lower part moves outwards
            opTransform(context, wid + "splay", { "bodies" : wq,
                        "transform" : rotationAround(line(vector(xC, sg * yEdge, zEdge) * mm, vector(1, 0, 0)), sg * spl) });
            // seal-bulge window (rectangular, through the wall and the sheet; material left in front and behind)
            const legT = makeLegs(context, wid + "legT", definition, xC, zB, 0.5, [sg]);
            const nClash = size(evCollision(context, { "tools" : wq, "targets" : qUnion(legT) }));
            opDeleteBodies(context, wid + "legTDel", { "entities" : qUnion(legT) });
            if (definition.sealWindow)
            {
                const hwW = definition.sealWindowWidth / mm / 2;
                wingCuts = append(wingCuts, mkBox(context, wid + "win", vector(xC - hwW, sg * 36, zB - definition.sealWindowBottom / mm),
                            vector(xC + hwW, sg * 80, zB - definition.sealWindowTop / mm)));
            }
            windowUsed = append(windowUsed, [nClash, definition.sealWindow]);
            wingBodies = append(wingBodies, wq);
            wi += 1;
        }
        println("leg sheets: wall edge |y|=" ~ yEdge ~ " at d=" ~ roundToPrecision(dEdge, 2) ~ ", splay " ~ roundToPrecision(spl / degree, 1) ~
            " deg; outer |y| at D40/50/85/110/125/135: " ~ roundToPrecision(ySh(40 - pad), 1) ~ "/" ~ roundToPrecision(ySh(50 - pad), 1) ~ "/" ~
            roundToPrecision(ySh(85 - pad), 1) ~ "/" ~ roundToPrecision(ySh(110 - pad), 1) ~ "/" ~ roundToPrecision(ySh(125 - pad), 1) ~ "/" ~
            roundToPrecision(ySh(135 - pad), 1) ~ " ; sheet clash with leg(+0.5) before window / window on, per side: " ~ toString(windowUsed) ~
            " ; sheet s " ~ (wc - hw) ~ ".." ~ (wc + hw) ~ ", window s " ~ (wc - definition.sealWindowWidth / mm / 2) ~ ".." ~ (wc + definition.sealWindowWidth / mm / 2) ~
            " (strips " ~ (hw - definition.sealWindowWidth / mm / 2) ~ " mm), D " ~ (definition.sealWindowTop / mm) ~ ".." ~ (definition.sealWindowBottom / mm));
        // per-side clearance of the bare wing plates to the tyre (torus) - measured before merging
        const tyreQs = makeTyres(context, id + "tyres", definition);
        var wingClr = [];
        for (var q in [wingBodies[0], wingBodies[1]])
        {
            wingClr = append(wingClr, [roundToPrecision(evDistance(context, { "side0" : q, "side1" : tyreQs[0] }).distance / mm, 2),
                        roundToPrecision(evDistance(context, { "side0" : q, "side1" : tyreQs[1] }).distance / mm, 2)]);
        }
        println("wing plate to tyre clearance per side [maxTyre69.2, Assegai63.5] (-Y, +Y): " ~ toString(wingClr));
        opDeleteBodies(context, id + "tyresDel", { "entities" : qUnion(tyreQs) });
        adds = concatenateArrays([adds, wingBodies]);
        opBoolean(context, id + "merge", { "tools" : qUnion(adds), "operationType" : BooleanOperationType.UNION });

        // ---------- 4a. v2.13 inside fill ----------
        // the wall loft stops at its edge depth and the root band at zEdge-1, which left a downward ledge/notch on the inside
        // between the wall's inner face and the sheet's inner plane. Fill the root zone (revolved about the axle, so it follows the
        // wall edge arc) from the wall line down to dEdge+9; the hollow (step 9) then carves the inside back to the 19 deg wall
        // line, which meets the sheet inner plane cleanly; step 9b rounds that crease.
        const sMid = R * asin(-xC / (R - dEdge)) / radian;
        if (definition.innerBlend)
        {
            for (var sg in [-1, 1])
            {
                const fq = revolveSection(context, id + ("ifill" ~ (sg + 1)), R, sMid, sg,
                    [[41.5, dEdge - 3], [yEdge - 3 * tA19 - 0.1, dEdge - 3], [yEdge - 0.1, dEdge - 0.3], [yEdge - 0.1, dEdge + 9], [41.5, dEdge + 9]], [],
                    xC - hw, xC + hw);
                opBoolean(context, id + ("ifillU" ~ (sg + 1)), { "tools" : qUnion([bodyAt(context, arcFrame(R, 100, -1.2).origin), fq]), "operationType" : BooleanOperationType.UNION });
            }
        }

        // ---------- 4b. v2.12 flush leg-sheet join ----------
        var joinOutOk = "n/a";
        // (1) trim everything of the sheet/root band that stands proud of the guard's outer surface above the wall bottom edge,
        // (2) clip the leg zone to |y| <= legZoneWidth/2 (removes the 0.77 mm loft overshoot), so the sheet's outer face is one plane
        // that the wall runs into; the crease and the inside/ends get fillets in step 10.
        if (definition.smoothJoin)
        {
            const fout2 = loftStations(context, id + "out2", st, "deep", 0); // outer surface with the 19 deg wall extended 6 mm past its edge
            var slabSt = [];
            for (var S in st)
            {
                if (S.s > 5 && S.s < 110)
                    slabSt = append(slabSt, S);
            }
            var sProfs = [];
            for (var i = 0; i < size(slabSt); i += 1)
            {
                const S = slabSt[i];
                const fr = arcFrame(S.R, S.s, S.k);
                polySketch(context, id + ("slabSk" ~ i), fr.plane, [vector(-80, -40), vector(80, -40), vector(80, S.d + 11.5), vector(-80, S.d + 11.5)]); // v2.14: floor d+11.5 (was 5.5); only the sheets have material below the wall edge
                sProfs = append(sProfs, qSketchRegion(id + ("slabSk" ~ i)));
            }
            opLoft(context, id + "slab", { "profileSubqueries" : sProfs, "bodyType" : ToolBodyType.SOLID });
            opDeleteBodies(context, id + "slabSkDel", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
            const slab = qCreatedBy(id + "slab", EntityType.BODY);
            const mid = mkBox(context, id + "slabMid", vector(-200, -40, 200), vector(200, 40, 500));
            opBoolean(context, id + "slabCut", { "targets" : slab, "tools" : qUnion([fout2, mid]), "operationType" : BooleanOperationType.SUBTRACTION });
            // v2.14 fix: in the C1 -> leg-zone blend the outer loft runs outside the deep loft (s ~20..31), so after the trim the
            // wall there was a mix of out-loft and deep-loft faces crossing each other near the plane (slivers, failed clip and
            // crease fillet). Over the sheet's x range only, add the deep-loft solid (|y| > 40) so the trimmed outer surface there is
            // the single deep-loft surface down to the sheet plane; clipY then gives one clean crease. No section changes.
            {
                const fdeepP = loftStations(context, id + "deepP", st, "deep", 0);
                const pMid = mkBox(context, id + "patchMid", vector(-400, -40, 100), vector(200, 40, 520));
                const pFr = mkBox(context, id + "patchFr", vector(xC + hw, -150, 100), vector(200, 150, 520));
                const pRr = mkBox(context, id + "patchRr", vector(-400, -150, 100), vector(xC - hw, 150, 520));
                // the deep loft is filled down to d+12: drop its interior below the wall edge (|y| < sheet inner face) so nothing is left under the void floor
                const pLow = mkBox(context, id + "patchLow", vector(xC - hw - 1, -yi, 100), vector(xC + hw + 1, yi, zEdge - 4.8));
                opBoolean(context, id + "patchCut", { "targets" : fdeepP, "tools" : qUnion([pMid, pFr, pRr, pLow]), "operationType" : BooleanOperationType.SUBTRACTION });
                const pBodies = qCreatedBy(id + "deepP", EntityType.BODY);
                println("v2.14 deep patch over sheet x " ~ roundToPrecision(xC - hw, 1) ~ ".." ~ roundToPrecision(xC + hw, 1) ~ ": bodies " ~ size(evaluateQuery(context, pBodies)) ~ ", vol " ~ roundToPrecision(evVolume(context, { "entities" : pBodies }) / mm / mm / mm, 0) ~ " mm3");
                const gq0 = bodyAt(context, arcFrame(R, 100, -1.2).origin);
                const vp0 = evVolume(context, { "entities" : gq0 }) / mm / mm / mm;
                opBoolean(context, id + "patchU", { "tools" : qUnion([gq0, pBodies]), "operationType" : BooleanOperationType.UNION });
                const gq1 = bodyAt(context, arcFrame(R, 100, -1.2).origin);
                println("v2.14 deep patch union: guard +" ~ roundToPrecision(evVolume(context, { "entities" : gq1 }) / mm / mm / mm - vp0, 2) ~ " mm3, bodies " ~ size(evaluateQuery(context, gq1)) ~ ", all solids " ~ size(evaluateQuery(context, qAllModifiableSolidBodies())));
            }
            const gq = bodyAt(context, arcFrame(R, 100, -1.2).origin);
            const v0 = evVolume(context, { "entities" : gq }) / mm / mm / mm;
            opBoolean(context, id + "trimProud", { "targets" : gq, "tools" : slab, "operationType" : BooleanOperationType.SUBTRACTION });
            const v1 = evVolume(context, { "entities" : bodyAt(context, arcFrame(R, 100, -1.2).origin) }) / mm / mm / mm;
            var clips = [];
            for (var sg in [-1, 1])
                clips = append(clips, mkBox(context, id + ("clip" ~ (sg + 1)), vector(-120, sg * yEdge, 200), vector(10, sg * 120, 500)));
            opBoolean(context, id + "clipY", { "targets" : bodyAt(context, arcFrame(R, 100, -1.2).origin), "tools" : qUnion(clips), "operationType" : BooleanOperationType.SUBTRACTION });
            const v2 = evVolume(context, { "entities" : bodyAt(context, arcFrame(R, 100, -1.2).origin) }) / mm / mm / mm;
            {
                const gq2 = bodyAt(context, arcFrame(R, 100, -1.2).origin);
                var es = [];
                for (var ed in evaluateQuery(context, qOwnedByBody(gq2, EntityType.EDGE)))
                {
                    const p = evEdgeTangentLine(context, { "edge" : ed, "parameter" : 0.5 }).origin / mm;
                    if (p[0] > xC + hw + 3 || p[0] < xC - hw - 3 || p[2] < zEdge - 8 || p[2] > zEdge + 16 || abs(p[1]) < 38)
                        continue;
                    if (evLength(context, { "entities" : ed }) < 1.0 * mm) // v2.14: skip sub-1 mm fragments
                        continue;
                    const fcs = evaluateQuery(context, qAdjacent(ed, AdjacencyType.EDGE, EntityType.FACE));
                    if (size(fcs) != 2)
                        continue;
                    var nPl = 0;
                    var hit = false;
                    for (var fc in fcs)
                    {
                        const pl = try silent(evPlane(context, { "face" : fc }));
                        if (pl == undefined)
                            continue;
                        nPl += 1;
                        if (abs(pl.normal[1]) > 0.999 && abs(abs(pl.origin[1] / mm) - yEdge) < 0.06)
                            hit = true;
                    }
                    if (hit && nPl == 1)
                        es = append(es, ed);
                }
                var lens = "";
                for (var ed in es)
                    lens = lens ~ " " ~ roundToPrecision(evLength(context, { "entities" : ed }) / mm, 1);
                var ok = "none";
                for (var rr in [definition.joinFilletOut, definition.joinFilletOut * 0.6, definition.joinFilletOut * 0.35])
                {
                    if (size(es) == 0)
                        break;
                    try silent
                    {
                        opFillet(context, id + "joinFilOut", { "entities" : qUnion(es), "radius" : rr });
                        ok = "R" ~ roundToPrecision(rr / mm, 2);
                    }
                    if (ok != "none")
                        break;
                }
                println("join fillet out (crease sheet|wall, pre-hollow): edges " ~ size(es) ~ " [" ~ lens ~ "] -> " ~ ok);
                joinOutOk = ok;
            }
            println("v2.12 join: proud material trimmed " ~ roundToPrecision(v0 - v1, 1) ~ " mm3, |y|>" ~ yEdge ~ " clip " ~ roundToPrecision(v1 - v2, 2) ~ " mm3");
        }

        // ---------- 5. split just behind the brace ----------
        const sF = definition.ph3 / mm / 2 + definition.padThk / mm; // A flange front face
        const tA = definition.flangeAThk / mm;
        const tB = definition.flangeBThk / mm;
        const sM = sF + tA; // mating plane
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
                            "angleForward" : ((sM - sEar) / R) * radian });
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
        const flPts = [vector(-W / 2, 5), vector(-W / 2, -H + 1), vector(-W / 2 + 1, -H), vector(W / 2 - 1, -H), vector(W / 2, -H + 1), vector(W / 2, 5)];
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
        polySketch(context, id + "tgSk", plane(fm.origin - Y * Lt * mm, Y, fm.t),
            [vector(0.5, 0.3), vector(-1.1, 0.3), vector(-1.5, 0.7), vector(-1.5, 1.9), vector(-1.1, 2.3), vector(0.5, 2.3)]);
        bAdds = append(bAdds, extrudeSk(context, id + "tg", id + "tgSk", Y, 2 * Lt));
        opBoolean(context, id + "uB", { "tools" : qUnion(bAdds), "operationType" : BooleanOperationType.UNION });

        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);

        // ---------- 7. cuts in A: groove + captive nyloc slots ----------
        var aCuts = [];
        polySketch(context, id + "grSk", plane(fm.origin - Y * (Lt + 0.5) * mm, Y, fm.t),
            [vector(-1.8, 0.05), vector(0.5, 0.05), vector(0.5, 2.55), vector(-1.8, 2.55)]);
        const grooveQ = extrudeSk(context, id + "gr", id + "grSk", Y, 2 * (Lt + 0.5));
        aCuts = append(aCuts, grooveQ);
        var nutQs = [];
        const boltH = 7;
        const nutW = definition.nutWidth / mm;
        const nutT = definition.nutThk / mm;
        const nutFloor = boltH - 3.2; // 3.8 above outer top: nut centred on bolt axis
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
        const fin = loftStations(context, id + "in", st, "in", 0);
        // keep the void from eating the leg sheets below the wall bottom edge
        var keep = [];
        for (var sg in [-1, 1])
            keep = append(keep, definition.innerBlend ?
                mkBox(context, id + ("keep" ~ (sg + 1)), vector(xC + hw, sg * yi, zEdge + 3), vector(xC - hw, sg * 90, zTop - dTip - 5)) :
                mkBox(context, id + ("keep" ~ (sg + 1)), vector(xC + hw, sg * (yo - wT - 0.05), zEdge + 0.5), vector(xC - hw, sg * 90, zTop - dTip - 5)));
        opBoolean(context, id + "finKeep", { "targets" : fin, "tools" : qUnion(keep), "operationType" : BooleanOperationType.SUBTRACTION });
        opBoolean(context, id + "hollow", { "targets" : qUnion([qA, qB]), "tools" : qUnion(concatenateArrays([[fin], tieSlots, wingCuts])),
                    "operationType" : BooleanOperationType.SUBTRACTION });

        var innerRevolveFallback = false;
        // ---------- 9b. v2.13 inside crease blend (fallback, revolved; only used if set) ----------
        // measure the wall's inner line on the rear strip (rays from the tyre side), then add a revolved concave round where it
        // meets the sheet inner plane |y| = yi, and re-cut the seal windows through it.
        if (definition.innerBlend && innerRevolveFallback)
        {
            const Rf = definition.joinFilletIn / mm;
            const qBb = bodyAt(context, arcFrame(R, 100, -1.2).origin);
            const frS = arcFrame(R, sMid, 0);
            const thR = asin((-(xC - hw + 4)) / (R - dEdge)) / radian;
            const frR = arcFrame(R, R * thR, 0);
            var lineFit = [];
            for (var sg in [-1, 1])
            {
                var uu = [];
                for (var vv in [dEdge - 5, dEdge - 1.5])
                {
                    const o = frR.origin - frR.rn * vv * mm + vector(0, sg * 38, 0) * mm;
                    const h = evRaycast(context, { "entities" : qBb, "ray" : line(o, vector(0, sg, 0)), "closest" : true });
                    uu = append(uu, size(h) > 0 ? abs(h[0].intersection[1] / mm) : -1);
                }
                const slope = (uu[1] - uu[0]) / 3.5;
                const vC = dEdge - 1.5 + (yi - uu[1]) / slope;
                lineFit = append(lineFit, [roundToPrecision(uu[0], 3), roundToPrecision(uu[1], 3), roundToPrecision(atan(slope) / degree, 2), roundToPrecision(vC - dEdge, 3)]);
                const ph = atan(slope);
                const tl = Rf * tan(ph / 2);
                const dL = vector(sin(ph), cos(ph));
                const C = vector(yi, vC);
                const T1 = C - dL * tl;
                const T2 = C + vector(0, 1) * tl;
                const cen = T2 + vector(-1, 0) * Rf;
                const md = cen + normalize(C - cen) * Rf;
                const fb = revolveSection(context, id + ("iblend" ~ (sg + 1)), R, sMid, sg,
                    [[T1[0], T1[1]], [T2[0], T2[1]], [yi + 1, T2[1]], [yi + 1, T1[1]]], [0, [md[0], md[1]]], xC - hw, xC + hw);
                opBoolean(context, id + ("iblendU" ~ (sg + 1)), { "tools" : qUnion([bodyAt(context, arcFrame(R, 100, -1.2).origin), fb]), "operationType" : BooleanOperationType.UNION });
            }
            if (definition.sealWindow)
            {
                var wc2 = [];
                for (var sg in [-1, 1])
                {
                    const hwW = definition.sealWindowWidth / mm / 2;
                    wc2 = append(wc2, mkBox(context, id + ("win2" ~ (sg + 1)), vector(xC - hwW, sg * 36, zB - definition.sealWindowBottom / mm),
                                vector(xC + hwW, sg * 80, zB - definition.sealWindowTop / mm)));
                }
                opBoolean(context, id + "win2Cut", { "targets" : bodyAt(context, arcFrame(R, 100, -1.2).origin), "tools" : qUnion(wc2), "operationType" : BooleanOperationType.SUBTRACTION });
            }
            println("v2.13 inside blend: inner wall |y| at dEdge-5/-1.5, angle, crease depth below edge, per side: " ~ toString(lineFit) ~ " ; R " ~ Rf);
        }

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
        if (definition.smoothJoin)
        {
            // v2.14 fix (ii): last body step before the join fillets - trim anything on B past the leg plate |y| = yEdge (full B length)
            const qBc0 = bodyAt(context, arcFrame(R, 100, -1.2).origin);
            const vc0 = evVolume(context, { "entities" : qBc0 }) / mm / mm / mm;
            var fClips = [];
            for (var sg in [-1, 1])
                fClips = append(fClips, mkBox(context, id + ("finClip" ~ (sg + 1)), vector(-450, sg * yEdge, 100), vector(30, sg * 150, 520)));
            opBoolean(context, id + "finClipY", { "targets" : qBc0, "tools" : qUnion(fClips), "operationType" : BooleanOperationType.SUBTRACTION });
            const qBc1 = bodyAt(context, arcFrame(R, 100, -1.2).origin);
            println("v2.14 final B clip |y|>" ~ yEdge ~ ": removed " ~ roundToPrecision(vc0 - evVolume(context, { "entities" : qBc1 }) / mm / mm / mm, 2) ~ " mm3, nB " ~ size(evaluateQuery(context, qBc1)));
        }
        if (definition.smoothJoin)
        {
            // sheet-to-guard fillets: outside crease (sheet outer plane | wall), inside (sheet inner plane | wall/void),
            // sheet front/rear ends (end planes | wall rim), all near the wall bottom edge
            var groups = definition.innerBlend ? [["in", definition.joinFilletIn, yi, 1], ["endF", 1 * mm, 0, 0, xC + hw, 0], ["endF-", 1 * mm, 0, 0, xC + hw, -1], ["endF+", 1 * mm, 0, 0, xC + hw, 1], ["endFo", 1 * mm, 0, 0, xC + hw, 0],
                                                    ["endR-", 1 * mm, 0, 0, xC - hw, -1], ["endR+", 1 * mm, 0, 0, xC - hw, 1]] : [["in", definition.joinFilletIn, yi, 1], ["ends", definition.joinFilletIn, 0, 0]];
            if (joinOutOk == "none")
                groups = concatenateArrays([[["out", definition.joinFilletOut, yEdge, 1]], groups]); // v2.14: retry the outside crease after the final clip
            var fBoth = false;
            var fSide = { "m" : false, "p" : false };
            var endFres = "sharp";
            for (var gi = 0; gi < size(groups); gi += 1)
            {
                const g = groups[gi];
                const isF = (g[0] == "endF" || g[0] == "endF-" || g[0] == "endF+" || g[0] == "endFo");
                // v2.14 front ends: both sides as one set (R1 -> R0.5), else per side, else outer edges only on the sides still sharp
                if ((g[0] == "endF-" || g[0] == "endF+") && fBoth)
                    continue;
                if (g[0] == "endFo" && (fBoth || (fSide["m"] && fSide["p"])))
                    continue;
                var es = [];
                qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);
                for (var ed in evaluateQuery(context, qOwnedByBody(qB, EntityType.EDGE)))
                {
                    const p = evEdgeTangentLine(context, { "edge" : ed, "parameter" : 0.5 }).origin / mm;
                    if (p[0] > xC + hw + 3 || p[0] < xC - hw - 3 || p[2] < zEdge - 8 || p[2] > zEdge + 16 || abs(p[1]) < 38)
                        continue;
                    const fcs = evaluateQuery(context, qAdjacent(ed, AdjacencyType.EDGE, EntityType.FACE));
                    if (size(fcs) != 2)
                        continue;
                    var nPl = 0;
                    var hit = false;
                    for (var fc in fcs)
                    {
                        const pl = try silent(evPlane(context, { "face" : fc }));
                        if (pl == undefined)
                            continue;
                        nPl += 1;
                        if (g[3] == 1 && abs(pl.normal[1]) > 0.999 && abs(abs(pl.origin[1] / mm) - g[2]) < 0.06)
                            hit = true;
                        if (g[3] == 0 && size(g) == 4 && abs(pl.normal[0]) > 0.999 && (abs(pl.origin[0] / mm - (xC + hw)) < 0.06 || abs(pl.origin[0] / mm - (xC - hw)) < 0.06))
                            hit = true;
                        if (g[3] == 0 && size(g) == 6 && (g[5] == 0 ? (g[0] != "endFo" || (abs(p[1]) > 47 && !fSide[p[1] > 0 ? "p" : "m"])) : p[1] * g[5] > 0) && abs(pl.normal[0]) > 0.999 && abs(pl.origin[0] / mm - g[4]) < 0.06)
                            hit = true;
                    }
                    if (hit && nPl == 1 && evLength(context, { "entities" : ed }) > ((g[0] == "out" || isF) ? 1.0 : 0.3) * mm) // v2.14: skip sub-1 mm fragments on crease/front ends
                        es = append(es, ed);
                }
                var lens0 = [];
                for (var ed in es)
                    lens0 = append(lens0, roundToPrecision(evLength(context, { "entities" : ed }) / mm, 1));
                println("join fillet " ~ g[0] ~ " candidate edge lengths " ~ toString(lens0));
                var ok = "none";
                if (size(es) > 0)
                {
                    for (var rr in (isF ? [1 * mm, 0.5 * mm] : [g[1], g[1] * 0.6, g[1] * 0.35]))
                    {
                        try silent
                        {
                            opFillet(context, id + ("joinFil" ~ gi), { "entities" : qUnion(es), "radius" : rr });
                            ok = "all R" ~ roundToPrecision(rr / mm, 2);
                        }
                        if (ok != "none")
                            break;
                    }
                    if (ok == "none")
                        ok = "FAILED (left sharp)";
                }
                if (isF && ok != "none" && ok != "FAILED (left sharp)")
                {
                    if (g[0] == "endF")
                        fBoth = true;
                    if (g[0] == "endF-")
                        fSide["m"] = true;
                    if (g[0] == "endF+")
                        fSide["p"] = true;
                    endFres = (endFres == "sharp" ? "" : endFres ~ "; ") ~ g[0] ~ " " ~ ok;
                }
                var einfo = [];
                for (var ed in es)
                {
                    const ip = try silent(evEdgeTangentLine(context, { "edge" : ed, "parameter" : 0.5 }).origin / mm);
                    if (ip != undefined)
                        einfo = append(einfo, [roundToPrecision(ip[0], 1), roundToPrecision(ip[1], 1), roundToPrecision(ip[2] - zEdge, 1)]);
                }
                const qBc = bodyAt(context, arcFrame(R, 100, -1.2).origin);
                println("  after " ~ g[0] ~ ": nB " ~ size(evaluateQuery(context, qBc)) ~ " vol " ~ roundToPrecision(evVolume(context, { "entities" : qBc }) / mm / mm / mm, 1) ~ " " ~ fmtBox(evBox3d(context, { "topology" : qBc, "tight" : true })));
                println("join fillet " ~ g[0] ~ ": edges " ~ size(es) ~ " -> " ~ ok ~ (ok == "FAILED (left sharp)" ? " at " ~ toString(einfo) : ""));
            }
            qA = bodyAt(context, pA2);
            qB = bodyAt(context, arcFrame(R, 100, -1.2).origin);
            println("after join fillets: B " ~ fmtBox(evBox3d(context, { "topology" : qB, "tight" : true })));
            println("FRONT END ROUNDS APPLIED: " ~ endFres);
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
                        if (zz < zEdge - 3 && yy > yEdge - 1)
                            sheetDev = max(sheetDev, abs(yy - yEdge));
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
                if (pl != undefined && abs(pl.normal[1]) > 0.99999 && abs(abs(pl.origin[1] / mm) - yEdge) < 0.01)
                {
                    plA += evArea(context, { "entities" : fc }) / mm / mm;
                    nPlF += 1;
                }
            }
            println("sheet outer planes at |y|=" ~ yEdge ~ ": " ~ nPlF ~ " faces, " ~ roundToPrecision(plA, 0) ~ " mm2; any B face beyond the plane: " ~ (evBox3d(context, { "topology" : qB, "tight" : true }).maxCorner[1] / mm - yEdge > 0.01 || -evBox3d(context, { "topology" : qB, "tight" : true }).minCorner[1] / mm - yEdge > 0.01));
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
        println("sheet outer face |y| at pivot " ~ yo ~ "; tip z=" ~ roundToPrecision(zTop - dTip, 1) ~ " (D=" ~ (dTip + pad) ~ " below brace)");
        const legQs = makeLegs(context, id + "legchk", definition, xC, zB, 0, [-1, 1]);
        const legTy = makeTyres(context, id + "tyres3", definition);
        println("leg model (measured taper): centre |y|=" ~ yLeg ~ ", min dist guard A/B to legs: " ~ roundToPrecision(evDistance(context, { "side0" : qA, "side1" : qUnion(legQs) }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : qUnion(legQs) }).distance / mm, 2) ~
            " ; legs to tyre [max, Assegai]: " ~ roundToPrecision(evDistance(context, { "side0" : qUnion(legQs), "side1" : legTy[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qUnion(legQs), "side1" : legTy[1] }).distance / mm, 2) ~
            " ; A/legs, B/legs clashes: " ~ size(evCollision(context, { "tools" : qA, "targets" : qUnion(legQs) })) ~ "/" ~ size(evCollision(context, { "tools" : qB, "targets" : qUnion(legQs) })));
        opDeleteBodies(context, id + "legsDel", { "entities" : qUnion(concatenateArrays([legQs, legTy])) });
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
            "boltSpacing" : 15 * millimeter, "boltDia" : 3.4 * millimeter, "nutWidth" : 5.95 * millimeter, "nutThk" : 4.5 * millimeter, "wingThk" : 2.5 * millimeter, "wingWidth" : 56 * millimeter, "wingRootFront" : -28 * millimeter, "wingRootRear" : 28 * millimeter, "wingSplay" : 0 * degree, "sealWindow" : true, "sealWindowWidth" : 40 * millimeter, "sealWindowTop" : 12 * millimeter, "sealWindowBottom" : 84 * millimeter,
            "wingFlareDepth" : 20 * millimeter, "wingTipDepth" : 135 * millimeter, "wingCornerR" : 8 * millimeter,
            "wingRow1" : 90 * millimeter, "wingRow2" : 110 * millimeter, "wingRow3" : 125 * millimeter,
            "wingSlotPitch" : 22 * millimeter, "strapChannel" : true, "tieCutDepth" : 20 * millimeter, "teardropAngle" : 90 * degree,
            "addRib" : false, "addFillets" : true, "smoothJoin" : true, "innerBlend" : true, "sculpted" : true, "c1Depth" : 30.49 * millimeter, "c1Radius" : 18 * millimeter, "roundR" : 20 * millimeter, "roundAlpha" : 5 * degree, "joinFilletOut" : 4 * millimeter, "joinFilletIn" : 4 * millimeter
        });
