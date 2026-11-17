FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Rear S2 (cradle under the brace) saddle test coupons S2-A..S2-D, from Split & Join Planner's sicomte_S2_spec.md section 6.
// Frame: rear axle at the origin, Y lateral, s = 0 (brace centreline) on +Z, +s toward +X; s is arc length at R 369.5.
// All lengths are plain numbers in mm. Coupons are built at the origin one at a time, then moved to their plate slot
// (plate = world XZ, print direction = world Y).


function s2Hb(y is number) returns number
{
    const ya = abs(y);
    if (ya <= 10)
        return 10.0;
    const u = min((ya - 10) / 20.0, 1.0);
    return 17.5 - 7.5 * sqrt(max(0, 1 - u * u));
}

function s2Pz(z is number, c is number) returns number
{
    return 30 + 0.3 * (380 - z) - c; // panel contact (outer) face |y| at height z
}

function s2Rn(s is number) returns Vector
{
    const a = (s / 369.5) * radian;
    return vector(sin(a), 0, cos(a));
}

function s2T(s is number) returns Vector
{
    const a = (s / 369.5) * radian;
    return vector(cos(a), 0, -sin(a));
}

function s2Poly(context is Context, sid is Id, pl is Plane, pts is array)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    for (var i = 0; i < size(pts); i += 1)
    {
        const j = (i + 1) % size(pts);
        skLineSegment(sk, "l" ~ i, { "start" : pts[i] * millimeter, "end" : pts[j] * millimeter });
    }
    skSolve(sk);
}

function s2Extr(context is Context, eid is Id, sid is Id, dir is Vector, depth is number) returns Query
{
    opExtrude(context, eid, { "entities" : qSketchRegion(sid), "direction" : dir, "endBound" : BoundingType.BLIND, "endDepth" : depth * millimeter });
    return qCreatedBy(eid, EntityType.BODY);
}

// rectangle (y0..y1 lateral, r0..r1 radial) revolved about the axle from s0 to s1
function s2RevRect(context is Context, rid is Id, y0 is number, y1 is number, r0 is number, r1 is number, s0 is number, s1 is number) returns Query
{
    s2Poly(context, rid + "sk", plane(vector(0, 0, 0) * millimeter, s2T(s0), vector(0, 1, 0)),
        [vector(y0, r0), vector(y1, r0), vector(y1, r1), vector(y0, r1)]);
    opRevolve(context, rid + "rv", { "entities" : qSketchRegion(rid + "sk"), "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)),
                "angleForward" : ((s1 - s0) / 369.5) * radian });
    return qCreatedBy(rid + "rv", EntityType.BODY);
}

function s2RevPoly(context is Context, rid is Id, pts is array, s0 is number, s1 is number) returns Query
{
    s2Poly(context, rid + "sk", plane(vector(0, 0, 0) * millimeter, s2T(s0), vector(0, 1, 0)), pts);
    opRevolve(context, rid + "rv", { "entities" : qSketchRegion(rid + "sk"), "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)),
                "angleForward" : ((s1 - s0) / 369.5) * radian });
    return qCreatedBy(rid + "rv", EntityType.BODY);
}

function s2Box(context is Context, bid is Id, c1 is Vector, c2 is Vector) returns Query
{
    const lo = vector(min(c1[0], c2[0]), min(c1[1], c2[1]), min(c1[2], c2[2]));
    const hi = vector(max(c1[0], c2[0]), max(c1[1], c2[1]), max(c1[2], c2[2]));
    fCuboid(context, bid, { "corner1" : lo * millimeter, "corner2" : hi * millimeter });
    return qCreatedBy(bid, EntityType.BODY);
}

// half-space beyond the radial plane at s (keepBelow = true removes s' > s, false removes s' < s)
function s2Half(context is Context, hid is Id, s is number, removeAbove is boolean) returns Query
{
    const cs = coordSystem(vector(0, 0, 0) * millimeter, s2Rn(s), s2T(s));
    fCuboid(context, hid, { "corner1" : vector(-1000, -1000, removeAbove ? 0 : -1000) * millimeter, "corner2" : vector(1000, 1000, removeAbove ? 1000 : 0) * millimeter });
    opTransform(context, hid + "tx", { "bodies" : qCreatedBy(hid, EntityType.BODY), "transform" : toWorld(cs) });
    return qCreatedBy(hid, EntityType.BODY);
}

// top-view outline points (x, y) of a curve x = sx * (hb(y) + off) for y from -yE to yE
function s2Outline(off is number, yE is number, sx is number) returns array
{
    const ysmp = [10, 13, 15.5, 18, 20.5, 23, 25.5, 28];
    var ys = [];
    for (var i = size(ysmp) - 1; i >= 0; i -= 1)
        ys = append(ys, -min(ysmp[i], yE));
    for (var v in ysmp)
        ys = append(ys, min(v, yE));
    var pts = [];
    for (var y in ys)
        pts = append(pts, vector(sx * (s2Hb(y) + off), y));
    return pts;
}

function s2Rev(a is array) returns array
{
    var o = [];
    for (var i = size(a) - 1; i >= 0; i -= 1)
        o = append(o, a[i]);
    return o;
}

function s2BuildCoupon(context is Context, id is Id, name is string, c is number, lip is number, flt is number) returns map
{
    const X = vector(1, 0, 0);
    const Y = vector(0, 1, 0);
    const Z = vector(0, 0, 1);
    const lf = 380.0 + 20.0 + flt;      // lip retaining face
    const top = lf + 3.4;                  // wall top (lead-in ends at lf + 2.4; 1.0 of plain wall above)
    const yc = s2Pz(369.5, c) - 0.5;            // widened crown half-width (0.5 inside the panel outer face line)
    // 1. crown: one revolve, |y| <= yc, trimmed to |y| <= 27.5 beyond |s| 27
    var crown = s2RevRect(context, id + "cr", -yc, yc, 367.0, 369.5, -41, 41);
    var strips = [];
    var k = 0;
    for (var sg in [-1, 1])
    {
        strips = append(strips, s2RevRect(context, id + ("st" ~ k), sg * 27.5, sg * 40, 360, 375, 27, 42));
        strips = append(strips, s2RevRect(context, id + ("st" ~ (k + 1)), sg * 27.5, sg * 40, 360, 375, -42, -27));
        k += 2;
    }
    opBoolean(context, id + "crTrim", { "targets" : crown, "tools" : qUnion(strips), "operationType" : BooleanOperationType.SUBTRACTION });
    var adds = [];
    // 2. neck walls |y| 25.5..28, r 369.5-11.47..368.3, |s| 27.5..41.5 (caps 0.5 past the crown cap at 41)
    k = 0;
    for (var sg in [-1, 1])
    {
        adds = append(adds, s2RevRect(context, id + ("nk" ~ k), sg * 25.5, sg * 28, 369.5 - 11.47, 368.3, 27.5, 41.5));
        adds = append(adds, s2RevRect(context, id + ("nk" ~ (k + 1)), sg * 25.5, sg * 28, 369.5 - 11.47, 368.3, -41.5, -27.5));
        k += 2;
    }
    // 3. ribs y +-22, 2.5 proud, 45 deg flanks, top 1.0, embedded 0.6 (base r 368.9, 0.6 above the neck wall top 368.3), |s| 21.7..41.5
    k = 0;
    for (var sg in [-1, 1])
    {
        const rp = [vector(sg * 22 - 3.6, 368.9), vector(sg * 22 + 3.6, 368.9), vector(sg * 22 + 0.5, 372.0), vector(sg * 22 - 0.5, 372.0)];
        adds = append(adds, s2RevPoly(context, id + ("rb" ~ k), rp, 21.7, 41.5));
        adds = append(adds, s2RevPoly(context, id + ("rb" ~ (k + 1)), rp, -41.5, -21.7));
        k += 2;
    }
    // 4. side panels: planar 4.0 plates parallel to the stay inner faces, contact face c off the stay, z 354..367.5, x -26..26
    k = 0;
    for (var sg in [-1, 1])
    {
        s2Poly(context, id + ("pnS" ~ k), plane(vector(-26, 0, 0) * millimeter, X, Y),
            [vector(sg * s2Pz(354, c), 354), vector(sg * s2Pz(367.5, c), 367.5), vector(sg * (s2Pz(367.5, c) - 4), 367.5), vector(sg * (s2Pz(354, c) - 4), 354)]);
        adds = append(adds, s2Extr(context, id + ("pn" ~ k), id + ("pnS" ~ k), X, 52));
        k += 1;
    }
    // 5. saddle block: brace outline + c + 1 (1.0 into the walls), |y| <= 27.5, z 368.6..380 (flat brace underside)
    const sadPts = concatenateArrays([s2Outline(c + 1, 27.5, 1), s2Rev(s2Outline(c + 1, 27.5, -1))]);
    s2Poly(context, id + "sdS", plane(vector(0, 0, 368.6) * millimeter, Z, X), sadPts);
    adds = append(adds, s2Extr(context, id + "sd", id + "sdS", Z, 380 - 368.6));
    // 6. snap walls: 3.0 thick from the brace outline + c, |y| <= 28, z 368.0..top
    k = 0;
    for (var sx in [-1, 1])
    {
        const wp = concatenateArrays([s2Outline(c, 28, sx), s2Rev(s2Outline(c + 3, 28, sx))]);
        s2Poly(context, id + ("wlS" ~ k), plane(vector(0, 0, 368.0) * millimeter, Z, X), wp);
        adds = append(adds, s2Extr(context, id + ("wl" ~ k), id + ("wlS" ~ k), Z, top - 368.0));
        k += 1;
    }
    // 7. lips |y| <= 6.5: retaining face lf, flat 1.2, lead-in to the wall face at lf + 2.4 (continued 0.25*lip into the wall)
    const xi = 10 + c;
    k = 0;
    for (var sx in [-1, 1])
    {
        const lp = [vector(sx * (xi + 1), lf), vector(sx * (xi - lip), lf), vector(sx * (xi - lip), lf + 1.2),
                    vector(sx * (xi + 0.25 * lip), lf + 2.7), vector(sx * (xi + 1), lf + 2.7)];
        s2Poly(context, id + ("lpS" ~ k), plane(vector(0, 6.5, 0) * millimeter, -Y, X), lp);
        adds = append(adds, s2Extr(context, id + ("lp" ~ k), id + ("lpS" ~ k), -Y, 13));
        k += 1;
    }
    // 8. embossed ID on the crown at s -30: 6 mm text, 0.6 proud (sketch 0.5 inside the crown, extruded 1.1)
    const tPl = plane(s2Rn(-30) * (369.5 - 0.5) * millimeter, s2Rn(-30), Y);
    const tsk = newSketchOnPlane(context, id + "idS", { "sketchPlane" : tPl });
    skText(tsk, "id", { "text" : name, "fontName" : "OpenSans-Bold.ttf", "firstCorner" : vector(-8, -3) * millimeter, "secondCorner" : vector(8, 3) * millimeter });
    skSolve(tsk);
    opExtrude(context, id + "idX", { "entities" : qSketchRegion(id + "idS", true), "direction" : s2Rn(-30), "endBound" : BoundingType.BLIND, "endDepth" : 1.1 * millimeter });
    const nTxt = size(evaluateQuery(context, qCreatedBy(id + "idX", EntityType.BODY)));
    adds = append(adds, qCreatedBy(id + "idX", EntityType.BODY));
    opBoolean(context, id + "u", { "tools" : qUnion(concatenateArrays([[crown], adds])), "operationType" : BooleanOperationType.UNION });
    const pIn = vector(0, 0, 368.25) * millimeter;
    // 9. cuts
    var cuts = [];
    k = 0;
    for (var sg in [-1, 1])
    {
        // brace tie: wall slots + saddle groove in one cutter, y 7.25..12.75, z 378.2..380.5
        cuts = append(cuts, s2Box(context, id + ("tc" ~ k), vector(-40, sg * 7.25, 378.2), vector(40, sg * 12.75, 380.5)));
        for (var sx in [-1, 1])
        {
            const xo = 13 + c;
            // wall outer-face groove 1.0 deep, from z 379.0 (inside the slot band) to above the top
            cuts = append(cuts, s2Box(context, id + ("gv" ~ k ~ "_" ~ (sx + 1)), vector(sx * (xo - 1), sg * 7.25, 379.0), vector(sx * (xo + 3), sg * 12.75, top + 1)));
            // wall-top notch 1.3 deep
            cuts = append(cuts, s2Box(context, id + ("nt" ~ k ~ "_" ~ (sx + 1)), vector(sx * (xi - 1), sg * 7.25, top - 1.3), vector(sx * (xo + 1), sg * 12.75, top + 1)));
            // finger slit 1.0 wide at |y| 13.75, down to z 384
            cuts = append(cuts, s2Box(context, id + ("sl" ~ k ~ "_" ~ (sx + 1)), vector(sx * 5, sg * 13.25, 384), vector(sx * 25, sg * 14.25, top + 1)));
            // stay-tie slot through the panel, 2.0 (x) at x +-21, z 359.5..365.0
            s2Poly(context, id + ("psS" ~ k ~ "_" ~ (sx + 1)), plane(vector(sx * 21 - 1, 0, 0) * millimeter, X, Y),
                [vector(sg * (s2Pz(359.5, c) - 5), 359.5), vector(sg * (s2Pz(359.5, c) + 1), 359.5), vector(sg * (s2Pz(365, c) + 1), 365), vector(sg * (s2Pz(365, c) - 5), 365)]);
            cuts = append(cuts, s2Extr(context, id + ("ps" ~ k ~ "_" ~ (sx + 1)), id + ("psS" ~ k ~ "_" ~ (sx + 1)), X, 2));
        }
        // panel inner-face tie recess 1.5 deep, x -21.5..21.5 (ends inside the slots), z 359.0..365.5
        s2Poly(context, id + ("prS" ~ k), plane(vector(-21.5, 0, 0) * millimeter, X, Y),
            [vector(sg * (s2Pz(359, c) - 5), 359), vector(sg * (s2Pz(359, c) - 2.5), 359), vector(sg * (s2Pz(365.5, c) - 2.5), 365.5), vector(sg * (s2Pz(365.5, c) - 5), 365.5)]);
        cuts = append(cuts, s2Extr(context, id + ("pr" ~ k), id + ("prS" ~ k), X, 43));
        k += 1;
    }
    cuts = append(cuts, s2Half(context, id + "trP", 40, true));
    cuts = append(cuts, s2Half(context, id + "trN", -40, false));
    opBoolean(context, id + "cut", { "targets" : qContainsPoint(qAllModifiableSolidBodies(), pIn), "tools" : qUnion(cuts), "operationType" : BooleanOperationType.SUBTRACTION });
    return { "pIn" : pIn, "nTxt" : nTxt, "top" : top, "lf" : lf, "xi" : xi };
}

function s2FmtB(b is Box3d) returns string
{
    const a = b.minCorner / millimeter;
    const c = b.maxCorner / millimeter;
    return "x" ~ roundToPrecision(a[0], 1) ~ ".." ~ roundToPrecision(c[0], 1) ~ " y" ~ roundToPrecision(a[1], 1) ~ ".." ~ roundToPrecision(c[1], 1) ~
        " z" ~ roundToPrecision(a[2], 1) ~ ".." ~ roundToPrecision(c[2], 1);
}

annotation { "Feature Type Name" : "Rear S2 saddle coupons" }
export const rearS2SaddleCoupons = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const mm = millimeter;
        const Y = vector(0, 1, 0);
        // name, c, lip, float, offline volume (s2_coupon_model.py, 0.25 voxel), plate offset (x, z)
        const V = [["S2-A", 0.5, 1.2, 0.4, 41.72, 0, 0], ["S2-B", 1.0, 1.2, 0.4, 42.16, 95, 0],
                   ["S2-C", 1.5, 1.6, 0.4, 42.61, 0, -65], ["S2-D", 1.0, 0.8, 1.2, 42.37, 95, -65]];
        var fail = "";
        var rep = "REAR-S2-COUPONS";
        var bodies = [];
        var vTot = 0;
        var i = 0;
        for (var v in V)
        {
            const r = s2BuildCoupon(context, id + ("c" ~ i), v[0], v[1], v[2], v[3]);
            const q = qContainsPoint(qAllModifiableSolidBodies(), r.pIn);
            const nq = size(evaluateQuery(context, q));
            var vol = 0;
            var bb = "";
            var probes = "";
            if (nq != 1)
                fail = fail ~ " " ~ v[0] ~ ":n=" ~ nq;
            else
            {
                vol = evVolume(context, { "entities" : q }) / (centimeter ^ 3);
                bb = s2FmtB(evBox3d(context, { "topology" : q, "tight" : true }));
                if (abs(vol - v[4]) > 0.08 * v[4])
                    fail = fail ~ " " ~ v[0] ~ ":vol";
                // probes (coupon still at the origin)
                const c = v[1];
                const lipIn = size(evaluateQuery(context, qContainsPoint(q, vector(r.xi - 0.5 * v[2], 0, r.lf + 0.6) * mm)));
                const slitOut = size(evaluateQuery(context, qContainsPoint(q, vector(r.xi + 1.5 + 0.2, 13.75, r.top - 3) * mm)));
                const notchOut = size(evaluateQuery(context, qContainsPoint(q, vector(r.xi + 1.5, 10, r.top - 0.6) * mm)));
                const wallIn = size(evaluateQuery(context, qContainsPoint(q, vector(r.xi + 1.5, 3, r.top - 0.6) * mm)));
                const sadIn = size(evaluateQuery(context, qContainsPoint(q, vector(0, 0, 379.5) * mm)));
                const grvOut = size(evaluateQuery(context, qContainsPoint(q, vector(0, 10, 379.5) * mm)));
                var tieOpen = 0;
                for (var sg in [-1, 1])
                {
                    const h = evRaycast(context, { "entities" : q, "ray" : line(vector(-45, sg * 10, 379.35) * mm, vector(1, 0, 0)), "closest" : true });
                    if (size(h) == 0)
                        tieOpen += 1;
                }
                var stayOpen = 0;
                for (var sg in [-1, 1])
                {
                    for (var sx in [-1, 1])
                    {
                        const y0 = s2Pz(362.25, c) - 4 - 3;
                        const h = evRaycast(context, { "entities" : q, "ray" : line(vector(sx * 21, sg * y0, 362.25) * mm, sg * Y), "closest" : true });
                        if (size(h) == 0 || h[0].distance > 9 * mm)
                            stayOpen += 1;
                    }
                }
                probes = " lip=" ~ lipIn ~ " slit=" ~ (1 - slitOut) ~ " notch=" ~ (1 - notchOut) ~ " wall=" ~ wallIn ~ " saddle=" ~ sadIn ~ " groove=" ~ (1 - grvOut) ~
                    " tie=" ~ tieOpen ~ "/2 stay=" ~ stayOpen ~ "/4 txt=" ~ r.nTxt;
                if (lipIn != 1 || slitOut != 0 || notchOut != 0 || wallIn != 1 || sadIn != 1 || grvOut != 0 || tieOpen != 2 || stayOpen != 4)
                    fail = fail ~ " " ~ v[0] ~ ":probes";
                setProperty(context, { "entities" : q, "propertyType" : PropertyType.NAME, "value" : "Rear " ~ v[0] ~ " saddle coupon (c " ~ c ~ ", lip " ~ v[2] ~ ", float " ~ v[3] ~ ")" });
                opTransform(context, id + ("mv" ~ i), { "bodies" : q, "transform" : transform(vector(v[5], 0, v[6]) * mm) });
                vTot += vol;
            }
            rep = rep ~ " | " ~ v[0] ~ " c=" ~ v[1] ~ " lip=" ~ v[2] ~ " float=" ~ v[3] ~ " vol=" ~ roundToPrecision(vol, 2) ~ " (exp " ~ v[4] ~ ") " ~ bb ~ probes;
            i += 1;
        }
        const all = qAllModifiableSolidBodies();
        const nAll = size(evaluateQuery(context, all));
        if (nAll != 4)
            fail = fail ~ " nAll=" ~ nAll;
        const plate = evBox3d(context, { "topology" : all, "tight" : true });
        const pd = (plate.maxCorner - plate.minCorner) / mm;
        if (pd[0] > 245 || pd[2] > 245)
            fail = fail ~ " plate";
        var nClash = 0;
        const list = evaluateQuery(context, all);
        for (var a = 0; a < size(list); a += 1)
        {
            for (var b = a + 1; b < size(list); b += 1)
            {
                nClash += size(evCollision(context, { "tools" : list[a], "targets" : list[b] }));
            }
        }
        if (nClash > 0)
            fail = fail ~ " clash";
        var nTiny = 0;
        var tinyTxt = "";
        for (var f in evaluateQuery(context, qOwnedByBody(all, EntityType.FACE)))
        {
            const ar = evArea(context, { "entities" : f }) / (mm * mm);
            if (ar < 0.5)
            {
                nTiny += 1;
                if (nTiny <= 6)
                {
                    const fb = evBox3d(context, { "topology" : f, "tight" : true });
                    const cc = (fb.minCorner + fb.maxCorner) / 2 / mm;
                    tinyTxt = tinyTxt ~ " (" ~ roundToPrecision(cc[0], 1) ~ "," ~ roundToPrecision(cc[1], 1) ~ "," ~ roundToPrecision(cc[2], 1) ~ " a" ~ roundToPrecision(ar, 3) ~ ")";
                }
            }
        }
        rep = rep ~ " | plate XZ " ~ roundToPrecision(pd[0], 1) ~ "x" ~ roundToPrecision(pd[2], 1) ~ " height(Y) " ~ roundToPrecision(pd[1], 1) ~
            " clash=" ~ nClash ~ " tiny<0.5mm2=" ~ nTiny ~ tinyTxt ~ " volTot=" ~ roundToPrecision(vTot, 2) ~ " print@4=" ~ roundToPrecision(vTot * 1000 / 4 / 3600, 2) ~ "h";
        if (fail != "")
            throw regenError(rep ~ " FAIL:" ~ fail);
        println(rep ~ " PASS");
    }, {});
