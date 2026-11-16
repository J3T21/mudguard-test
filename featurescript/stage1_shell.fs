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
        [-120, 110, 42, 10, 20, w0],
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
        if (s < -15)
            s = -15 + (s + 15) * (fL - 15) / 225;
        else if (s > lo + 15 && row[0] >= 100)
            s = (lo + 15) + (s - 60) * (tL - (lo + 15)) / 105;
        var k = 0;
        if (s < sK)
        {
            const u = sK - s;
            k = u * u * tan(def.kickAngle) / (2 * kickLen);
        }
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

annotation { "Feature Type Name" : "Mudguard V2 Shell Test" }
export const mudguardShellTest = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const def = {
                "ph1" : 395 * millimeter, "padThk" : 1.5 * millimeter, "frontLen" : 240 * millimeter,
                "tailLen" : 165 * millimeter, "wall" : 2.5 * millimeter, "waistWall" : 3.0 * millimeter,
                "ph5" : 60 * millimeter, "legTieOffset" : 45 * millimeter, "kickLen" : 25 * millimeter,
                "kickAngle" : 12 * degree };
        const st = buildStations(def);
        const fout = loftStations(context, id + "out", st, "out", 0);
        const fin = loftStations(context, id + "in", st, "in", 0);
        opBoolean(context, id + "shell", { "targets" : fout, "tools" : fin, "operationType" : BooleanOperationType.SUBTRACTION });
        const parts = qAllModifiableSolidBodies();
        println("nParts=" ~ size(evaluateQuery(context, parts)));
        println("box=" ~ toString(evBox3d(context, { "topology" : parts, "tight" : true })));
        println("vol=" ~ toString(evVolume(context, { "entities" : parts })));
    });
