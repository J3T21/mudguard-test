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
    const dB = 26;
    const wB = def.ph5 / millimeter + 2 * dB * tan(19 * degree);
    const lo = def.legTieOffset / millimeter;
    // [s, width, depth, alpha, cornerR, wall]
    const raw = [
        [-240, 100, 30, 8, 22, w0],
        [-227.5, 102, 32.5, 8.5, 21.5, w0],
        [-215, 104, 35, 9, 21, w0],
        [-192.5, 108, 39.5, 9.5, 20.5, w0],
        [-170, 110, 42, 10, 20, w0],
        [-145, 110, 42, 10, 20, w0],
        [-120, 110, 42, 10, 20, w0],
        [-95, 110, 42, 10, 20, w0],
        [-82.5, 110, 42, 10, 20, w0],
        [-70, 110, 42, 10, 20, w0],
        [-42.5, (110 + wB) / 2, 34, 14.5, 13, (w0 + ww) / 2],
        [-15, wB, dB, 19, 6, ww],
        [0, wB, dB, 19, 6, ww],
        [15, wB, dB, 19, 6, ww],
        [lo - 15, 82, 26, 19, 6, ww],
        [lo, 84, 26, 19, 6, ww],
        [lo + 15, 84, 26, 19, 6, ww],
        [100, 83, 25, 19, 6, w0],
        [135, 82, 24, 19, 6, w0],
        [165, 80, 22, 19, 6, w0]
    ];
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

annotation { "Feature Type Name" : "Mudguard V2 render view" }
export const mudguardRenderView = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        mudguardV2(context, id + "mg", {});
        opTransform(context, id + "rot", { "bodies" : qAllModifiableSolidBodies(),
                    "transform" : rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 135 * degree) });
    }, {});


annotation { "Feature Type Name" : "Mudguard V2 (Fox AWL 36)" }
export const mudguardV2 = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "PH1 axle to brace underside" }
        isLength(definition.ph1, { (millimeter) : [300, 395, 500] } as LengthBoundSpec);
        annotation { "Name" : "PH2 inner leg face-to-face" }
        isLength(definition.ph2, { (millimeter) : [80, 100, 140] } as LengthBoundSpec);
        annotation { "Name" : "PH3 brace fore-aft depth" }
        isLength(definition.ph3, { (millimeter) : [10, 22, 40] } as LengthBoundSpec);
        annotation { "Name" : "PH4 brace back-face height" }
        isLength(definition.ph4, { (millimeter) : [10, 25, 60] } as LengthBoundSpec);
        annotation { "Name" : "PH5 brace underside flat (top flat width)" }
        isLength(definition.ph5, { (millimeter) : [40, 60, 70] } as LengthBoundSpec);
        annotation { "Name" : "PH6 lower-leg OD" }
        isLength(definition.ph6, { (millimeter) : [30, 45, 60] } as LengthBoundSpec);
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
        isLength(definition.tailLen, { (millimeter) : [100, 240, 300] } as LengthBoundSpec);
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
        isLength(definition.braceTieFront, { (millimeter) : [8, 14, 40] } as LengthBoundSpec);
        annotation { "Name" : "Brace tie rear row (s behind brace centre, part B)" }
        isLength(definition.braceTieRear, { (millimeter) : [8, 29, 60] } as LengthBoundSpec);
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
        isLength(definition.wingWidth, { (millimeter) : [20, 40, 70] } as LengthBoundSpec);
        annotation { "Name" : "Wing root front (s rel. centre)" }
        isLength(definition.wingRootFront, { (millimeter) : [-40, -24, -10] } as LengthBoundSpec);
        annotation { "Name" : "Wing root rear (s rel. centre)" }
        isLength(definition.wingRootRear, { (millimeter) : [10, 32, 40] } as LengthBoundSpec);
        annotation { "Name" : "Wing top edge depth below guard top" }
        isLength(definition.wingRootDepth, { (millimeter) : [5, 12, 30] } as LengthBoundSpec);
        annotation { "Name" : "Wing flare end depth" }
        isLength(definition.wingFlareDepth, { (millimeter) : [15, 37, 60] } as LengthBoundSpec);
        annotation { "Name" : "Wing tip depth below guard top" }
        isLength(definition.wingTipDepth, { (millimeter) : [60, 112, 160] } as LengthBoundSpec);
        annotation { "Name" : "Wing tip corner radius" }
        isLength(definition.wingCornerR, { (millimeter) : [1, 8, 15] } as LengthBoundSpec);
        annotation { "Name" : "Wing joggle bottom depth" }
        isLength(definition.wingJoggleDepth, { (millimeter) : [15, 28, 40] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 1 depth" }
        isLength(definition.wingRow1, { (millimeter) : [20, 48, 150] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 2 depth" }
        isLength(definition.wingRow2, { (millimeter) : [20, 74, 150] } as LengthBoundSpec);
        annotation { "Name" : "Wing slot row 3 depth" }
        isLength(definition.wingRow3, { (millimeter) : [20, 100, 150] } as LengthBoundSpec);
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
                stadiumSketch(context, id + ("bp" ~ k), pl, yv, 0, 6 + 8, 2.4 + 8); // boss = slot + 4 mm all round
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

        // ---------- 4. integral flat leg wings (EVO style) ----------
        // One flat plate per side, parallel to the wheel plane (world XZ), outer (leg-side) face at
        // |y| = PH2/2 - padThk, thickness wingThk; joined to the side wall by a wedge joggle (19 deg wall -> vertical wing).
        const wc = definition.legTieOffset / mm;                  // wing centre s
        const thC = (wc / R) * radian;
        const xC = -R * sin(thC);
        const zTop = R * cos(thC);
        const wT = definition.wingThk / mm;
        const yo = definition.ph2 / mm / 2 - definition.padThk / mm; // leg-side face
        const yi = yo - wT;                                           // tyre-side face
        const hw = definition.wingWidth / mm / 2;
        const rootF = definition.wingRootFront / mm;                  // s of root front (rel. to centre, negative)
        const rootR = definition.wingRootRear / mm;
        const dRoot = definition.wingRootDepth / mm;                  // top edge depth
        const dFlare = definition.wingFlareDepth / mm;                // where the flare meets the straight edge
        const dTip = definition.wingTipDepth / mm;
        const rc = definition.wingCornerR / mm;
        var wingBodies = [];
        var wingCuts = [];
        var wi = 0;
        for (var sg in [-1, 1])
        {
            const wid = id + ("wing" ~ wi);
            // sketch plane: normal +Y, sketch u = s - wc (rearward = -X), v = world Z
            const yPl = (sg > 0) ? yi : -yo;
            const pl = plane(vector(xC, yPl, 0) * mm, vector(0, 1, 0), vector(-1, 0, 0));
            const sk = newSketchOnPlane(context, wid + "sk", { "sketchPlane" : pl });
            const P = function(su, dv) { return vector(su, zTop - dv) * mm; };
            // flare arcs: tangent to the straight edge at (+-hw, dFlare), through the root corner
            const aF = abs(-rootF - hw);       // lateral flare at front
            const aR = abs(rootR - hw);
            const dd = dFlare - dRoot;
            const rF = (aF * aF + dd * dd) / (2 * aF);
            const rR = (aR * aR + dd * dd) / (2 * aR);
            const phF = asin(dd / rF) / 2;
            const phR = asin(dd / rR) / 2;
            const midF = P(-hw - rF * (1 - cos(phF)), dFlare - rF * sin(phF));
            const midR = P(hw + rR * (1 - cos(phR)), dFlare - rR * sin(phR));
            const c45 = cos(45 * degree);
            skLineSegment(sk, "top", { "start" : P(rootF, dRoot), "end" : P(rootR, dRoot) });
            skArc(sk, "flR", { "start" : P(rootR, dRoot), "mid" : midR, "end" : P(hw, dFlare) });
            skLineSegment(sk, "rear", { "start" : P(hw, dFlare), "end" : P(hw, dTip - rc) });
            skArc(sk, "cR", { "start" : P(hw, dTip - rc), "mid" : P(hw - rc + rc * c45, dTip - rc + rc * c45), "end" : P(hw - rc, dTip) });
            skLineSegment(sk, "tip", { "start" : P(hw - rc, dTip), "end" : P(-hw + rc, dTip) });
            skArc(sk, "cF", { "start" : P(-hw + rc, dTip), "mid" : P(-hw + rc - rc * c45, dTip - rc + rc * c45), "end" : P(-hw, dTip - rc) });
            skLineSegment(sk, "front", { "start" : P(-hw, dTip - rc), "end" : P(-hw, dFlare) });
            skArc(sk, "flF", { "start" : P(-hw, dFlare), "mid" : midF, "end" : P(rootF, dRoot) });
            skSolve(sk);
            const wq = extrudeSk(context, wid + "ex", wid + "sk", vector(0, 1, 0), wT);
            // joggle: wedge between the side wall and the wing's tyre-side face (void trims the inside later)
            const jq = mkBox(context, wid + "jog", vector(xC - rootR, sg * 30, zTop - (definition.wingJoggleDepth / mm)),
                vector(xC - rootF, sg * (yi + 0.01), zTop - dRoot));
            wingBodies = append(wingBodies, wq);
            wingBodies = append(wingBodies, jq);
            // tie slots (long side along the leg) and strap channels on the tyre side
            var k2 = 0;
            for (var rowD in [definition.wingRow1 / mm, definition.wingRow2 / mm, definition.wingRow3 / mm])
            {
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    wingCuts = append(wingCuts, mkBox(context, wid + ("sl" ~ k2), vector(xC - sp - 1.2, sg * (yi - 1), zTop - rowD - 3),
                                vector(xC - sp + 1.2, sg * (yo + 1), zTop - rowD + 3)));
                    k2 += 1;
                }
                if (definition.strapChannel)
                {
                    const hp = definition.wingSlotPitch / mm / 2;
                    wingCuts = append(wingCuts, mkBox(context, wid + ("ch" ~ k2), vector(xC - hp, sg * (yi - 1), zTop - rowD - 2.75),
                                vector(xC + hp, sg * (yi + 0.8), zTop - rowD + 2.75)));
                }
            }
            wi += 1;
        }
        // per-side clearance of the bare wing plates to the tyre (torus) - measured before merging
        const tyreQs = makeTyres(context, id + "tyres", definition);
        var wingClr = [];
        for (var q in [wingBodies[0], wingBodies[2]])
        {
            wingClr = append(wingClr, [roundToPrecision(evDistance(context, { "side0" : q, "side1" : tyreQs[0] }).distance / mm, 2),
                        roundToPrecision(evDistance(context, { "side0" : q, "side1" : tyreQs[1] }).distance / mm, 2)]);
        }
        println("wing plate to tyre clearance per side [maxTyre69.2, Assegai63.5] (-Y, +Y): " ~ toString(wingClr));
        opDeleteBodies(context, id + "tyresDel", { "entities" : qUnion(tyreQs) });
        adds = concatenateArrays([adds, wingBodies]);
        opBoolean(context, id + "merge", { "tools" : qUnion(adds), "operationType" : BooleanOperationType.UNION });

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
        println("rear tie row s=" ~ tieSR ~ ": slot front edge to flange B rear face (along s) = " ~ roundToPrecision(tieSR - 1.2 - (sM + tB), 2) ~ " mm; front row s=" ~ (-tieSF) ~
            ", slot rear edge to brace front face = " ~ roundToPrecision(-(definition.ph3 / mm / 2) - (-tieSF + 1.2), 2) ~ " mm");
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
        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);
        opBoolean(context, id + "holes", { "targets" : qUnion([qA, qB]), "tools" : qUnion(holes), "operationType" : BooleanOperationType.SUBTRACTION });

        // ---------- 9. hollow out (inner void) + tie slots ----------
        qA = bodyAt(context, pA);
        qB = bodyAt(context, pB);
        const fin = loftStations(context, id + "in", st, "in", 0);
        opBoolean(context, id + "hollow", { "targets" : qUnion([qA, qB]), "tools" : qUnion(concatenateArrays([[fin], tieSlots, wingCuts])),
                    "operationType" : BooleanOperationType.SUBTRACTION });

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

        setProperty(context, { "entities" : qA, "propertyType" : PropertyType.NAME, "value" : "Guard Front A" });
        setProperty(context, { "entities" : qB, "propertyType" : PropertyType.NAME, "value" : "Guard Rear B" });

        // cleanup sketches / construction bodies
        opDeleteBodies(context, id + "clean", { "entities" : qSubtraction(qCreatedBy(id, EntityType.BODY), qAllModifiableSolidBodies()) });

        // ---------- 11. verification report ----------
        const solids = qAllModifiableSolidBodies();
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
        const tyre2 = makeTyres(context, id + "tyres2", definition);
        println("min clearance to tyre [maxTyre, Assegai]: A=" ~ roundToPrecision(evDistance(context, { "side0" : qA, "side1" : tyre2[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qA, "side1" : tyre2[1] }).distance / mm, 2) ~ " B=" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : tyre2[0] }).distance / mm, 2) ~ "/" ~
            roundToPrecision(evDistance(context, { "side0" : qB, "side1" : tyre2[1] }).distance / mm, 2));
        opDeleteBodies(context, id + "tyres2Del", { "entities" : qUnion(tyre2) });
        // wing slots: lateral rays through each slot centre
        var openSl = 0;
        var totSl = 0;
        for (var sg in [-1, 1])
        {
            for (var rowD in [definition.wingRow1 / mm, definition.wingRow2 / mm, definition.wingRow3 / mm])
            {
                for (var sp in [-definition.wingSlotPitch / mm / 2, definition.wingSlotPitch / mm / 2])
                {
                    const org = vector(xC - sp, sg * (yi - 3), zTop - rowD) * mm;
                    const hits = evRaycast(context, { "entities" : qB, "ray" : line(org, vector(0, sg, 0)), "closest" : true });
                    var blocked = false;
                    if (size(hits) > 0)
                        blocked = hits[0].distance < (wT + 4) * mm;
                    if (!blocked)
                        openSl += 1;
                    totSl += 1;
                }
            }
        }
        println("wing slots open: " ~ openSl ~ "/" ~ totSl);
        println("wing faces |y|: tyre side " ~ yi ~ ", leg side " ~ yo ~ "; tip z=" ~ roundToPrecision(zTop - dTip, 1) ~ " (" ~ (dTip - dRoot) ~ " mm wing below its root)");
        println("R_top=" ~ R ~ " inner top (waist)=" ~ (R - ww) ~ " tyre+clr=" ~ ((definition.tyreRadius + definition.clearance) / mm));
        println("sM(mating s)=" ~ sM ~ " sFront=" ~ sF);
    }, {
            "ph1" : 395 * millimeter, "ph2" : 100 * millimeter, "ph3" : 22 * millimeter, "ph4" : 25 * millimeter,
            "ph5" : 60 * millimeter, "ph6" : 45 * millimeter, "ph7" : 0 * degree,
            "tyreRadius" : 375 * millimeter, "clearance" : 16 * millimeter, "maxTyreWidth" : 69.2 * millimeter, "tyreWidth" : 63.5 * millimeter, "padThk" : 1.5 * millimeter,
            "frontLen" : 165 * millimeter, "tailLen" : 240 * millimeter, "legTieOffset" : 45 * millimeter,
            "wall" : 2.5 * millimeter, "waistWall" : 3.0 * millimeter, "kickLen" : 25 * millimeter, "kickAngle" : 12 * degree,
            "braceTieFront" : 14 * millimeter, "braceTieRear" : 29 * millimeter, "braceTieY" : 30 * millimeter, "flangeWidth" : 46 * millimeter,
            "flangeHeight" : 14 * millimeter, "flangeAThk" : 8 * millimeter, "flangeBThk" : 4 * millimeter,
            "boltSpacing" : 15 * millimeter, "boltDia" : 3.4 * millimeter, "nutWidth" : 5.95 * millimeter, "nutThk" : 4.5 * millimeter, "wingThk" : 2.5 * millimeter, "wingWidth" : 40 * millimeter, "wingRootFront" : -24 * millimeter, "wingRootRear" : 32 * millimeter,
            "wingRootDepth" : 12 * millimeter, "wingFlareDepth" : 37 * millimeter, "wingTipDepth" : 112 * millimeter, "wingCornerR" : 8 * millimeter,
            "wingJoggleDepth" : 28 * millimeter, "wingRow1" : 48 * millimeter, "wingRow2" : 74 * millimeter, "wingRow3" : 100 * millimeter,
            "wingSlotPitch" : 22 * millimeter, "strapChannel" : true, "tieCutDepth" : 20 * millimeter, "teardropAngle" : 90 * degree,
            "addRib" : true, "addFillets" : true
        });
