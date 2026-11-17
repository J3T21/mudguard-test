FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Rear S3.1 moulded under-brace saddle test coupons S3.1-A..D, from Split & Join Planner's sicomte_S3_spec.md (S3.1).
// Frame: rear axle at the origin, Y lateral, s = 0 (brace centreline) on +Z, +s toward +X; s is arc length at R 369.5.
// The hump section is drawn in the XZ plane with exact lines and arcs: pocket, crest and flank use the spec numbers as
// Cartesian (x, z), and the R25 / R27.5 roots are solved tangent to the true crown circles R369.5 / R367 (offline, s3geom.py).
// The brace flare (|y| 10..25.5) is built from oblique extrudes of the half profiles (pure translation per segment, so the
// faces are exact and adjacent pieces meet on identical profiles); no lofts. Coupons are cut at s -50..+50, built at the
// origin one at a time and then moved to their plate slot (plate = world XZ, print direction = world Y, lip edges down).

// profile data per inner offset (oi = 2 * o): right-half outer and inner chains from the pocket floor corner start to the
// closing ray at s 52; rows are [0 line | 1 arc, x0, z0, xm, zm, x1, z1] in mm
function s3Prof(oi is number) returns map
{
    if (oi == 1)
        return { "out" : [[1, 4, 380, 8.55591, 381.863872, 10.49901, 386.386559],
                 [0, 10.49901, 386.386559, 0, 0, 10.659304, 395.56981],
                 [1, 10.659304, 395.56981, 13.323468, 399.270566, 17.722873, 398.07115],
                 [0, 17.722873, 398.07115, 0, 0, 37.653073, 374.319263],
                 [1, 37.653073, 374.319263, 44.028142, 368.900055, 51.834318, 365.887924]],
                 "inn" : [[1, 4, 377, 10.658638, 379.724121, 13.498553, 386.334202],
                 [0, 13.498553, 386.334202, 0, 0, 13.658847, 395.517452],
                 [1, 13.658847, 395.517452, 14.324888, 396.442641, 15.424739, 396.142788],
                 [0, 15.424739, 396.142788, 0, 0, 34.995173, 372.819653],
                 [1, 34.995173, 372.819653, 42.396319, 366.631784, 51.479136, 363.380765]],
                 "c" : 4, "w" : 10.5, "hc" : [22.222386, 392.708839], "C1" : [14.658695, 395.5] };
    if (oi == 2)
        return { "out" : [[1, 4, 380, 8.906365, 382.007247, 10.998934, 386.877833],
                 [0, 10.998934, 386.877833, 0, 0, 11.150653, 395.56981],
                 [1, 11.150653, 395.56981, 13.814816, 399.270566, 18.214221, 398.07115],
                 [0, 18.214221, 398.07115, 0, 0, 38.213148, 374.237358],
                 [1, 38.213148, 374.237358, 44.345164, 368.964528, 51.839588, 365.925128]],
                 "inn" : [[1, 4, 377, 11.009093, 379.867496, 13.998477, 386.825476],
                 [0, 13.998477, 386.825476, 0, 0, 14.150196, 395.517452],
                 [1, 14.150196, 395.517452, 14.816237, 396.442641, 15.916088, 396.142788],
                 [0, 15.916088, 396.142788, 0, 0, 35.554209, 372.738987],
                 [1, 35.554209, 372.738987, 42.710832, 366.692808, 51.481861, 363.400001]],
                 "c" : 4, "w" : 11, "hc" : [22.713735, 392.708839], "C1" : [15.150044, 395.5] };
    if (oi == 3)
        return { "out" : [[1, 4, 380, 9.256819, 382.150622, 11.498858, 387.369107],
                 [0, 11.498858, 387.369107, 0, 0, 11.642002, 395.56981],
                 [1, 11.642002, 395.56981, 14.306165, 399.270566, 18.70557, 398.07115],
                 [0, 18.70557, 398.07115, 0, 0, 38.77401, 374.154515],
                 [1, 38.77401, 374.154515, 44.663811, 369.031337, 51.846574, 365.974437]],
                 "inn" : [[1, 4, 377, 11.359547, 380.01087, 14.498401, 387.31675],
                 [0, 14.498401, 387.31675, 0, 0, 14.641545, 395.517452],
                 [1, 14.641545, 395.517452, 15.307585, 396.442641, 16.407437, 396.142788],
                 [0, 16.407437, 396.142788, 0, 0, 36.114026, 372.657389],
                 [1, 36.114026, 372.657389, 43.02697, 366.75578, 51.486113, 363.430018]],
                 "c" : 4, "w" : 11.5, "hc" : [23.205083, 392.708839], "C1" : [15.641392, 395.5] };
    throw regenError("s3Prof: no profile for oi=" ~ oi);
}

function s3Rn(s is number) returns Vector
{
    const a = (s / 369.5) * radian;
    return vector(sin(a), 0, cos(a));
}

function s3T(s is number) returns Vector
{
    const a = (s / 369.5) * radian;
    return vector(cos(a), 0, -sin(a));
}

function s3P(x is number, z is number, sx is number, dx is number) returns Vector
{
    return vector(sx * (x + dx), z) * millimeter;
}

// add one chain of rows to a sketch; sx mirrors (x -> -x), dx translates outward (applied before the mirror)
function s3Chain(sk is Sketch, tag is string, rows is array, sx is number, dx is number)
{
    for (var i = 0; i < size(rows); i += 1)
    {
        const g = rows[i];
        if (g[0] == 0)
            skLineSegment(sk, tag ~ i, { "start" : s3P(g[1], g[2], sx, dx), "end" : s3P(g[5], g[6], sx, dx) });
        else
            skArc(sk, tag ~ i, { "start" : s3P(g[1], g[2], sx, dx), "mid" : s3P(g[3], g[4], sx, dx), "end" : s3P(g[5], g[6], sx, dx) });
    }
}

function s3Line(sk is Sketch, tag is string, x0 is number, z0 is number, x1 is number, z1 is number)
{
    skLineSegment(sk, tag, { "start" : vector(x0, z0) * millimeter, "end" : vector(x1, z1) * millimeter });
}

function s3Last(rows is array) returns array
{
    const g = rows[size(rows) - 1];
    return [g[5], g[6]];
}

// sketch plane parallel to XZ at lateral position y, sketch coordinates (x, z)
function s3PlaneY(y is number) returns Plane
{
    return plane(vector(0, y, 0) * millimeter, vector(0, -1, 0), vector(1, 0, 0));
}

// full hump shell section at lateral y, translated by dx on each side
function s3ShellSk(context is Context, sid is Id, y is number, P is map, dx is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : s3PlaneY(y) });
    const c = P.c + dx;
    s3Line(sk, "fo", -c, 380, c, 380);
    s3Line(sk, "fi", -c, 377, c, 377);
    for (var sx in [-1, 1])
    {
        const t = sx > 0 ? "R" : "L";
        s3Chain(sk, t ~ "o", P.out, sx, dx);
        s3Chain(sk, t ~ "i", P.inn, sx, dx);
        const po = s3Last(P.out);
        const pq = s3Last(P.inn);
        s3Line(sk, t ~ "cl", sx * (po[0] + dx), po[1], sx * (pq[0] + dx), pq[1]);
    }
    skSolve(sk);
}

// half shell section (sx = +1 rear half, -1 front half); its floor starts at absolute x = sx * xa (xa < 0, past the centre)
function s3HalfSk(context is Context, sid is Id, y is number, P is map, dx is number, sx is number, xa is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : s3PlaneY(y) });
    const xc = sx * (P.c + dx);
    s3Line(sk, "fo", sx * xa, 380, xc, 380);
    s3Line(sk, "fi", sx * xa, 377, xc, 377);
    s3Line(sk, "fe", sx * xa, 377, sx * xa, 380);
    s3Chain(sk, "o", P.out, sx, dx);
    s3Chain(sk, "i", P.inn, sx, dx);
    const po = s3Last(P.out);
    const pq = s3Last(P.inn);
    s3Line(sk, "cl", sx * (po[0] + dx), po[1], sx * (pq[0] + dx), pq[1]);
    skSolve(sk);
}

// end wall + neck skirt (|y| 25.5..28): outer profile at dx, filled down to R358
function s3EndSk(context is Context, sid is Id, y is number, P is map, dx is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : s3PlaneY(y) });
    const c = P.c + dx;
    const po = s3Last(P.out);
    const xo = po[0] + dx;
    const zb = sqrt(358 * 358 - xo * xo);
    s3Line(sk, "fo", -c, 380, c, 380);
    for (var sx in [-1, 1])
    {
        const t = sx > 0 ? "R" : "L";
        s3Chain(sk, t ~ "o", P.out, sx, dx);
        s3Line(sk, t ~ "dn", sx * xo, po[1], sx * xo, zb);
    }
    skArc(sk, "bot", { "start" : vector(-xo, zb) * millimeter, "mid" : vector(0, 358) * millimeter, "end" : vector(xo, zb) * millimeter });
    skSolve(sk);
}

function s3Extr(context is Context, eid is Id, sid is Id, dir is Vector, depth is number) returns Query
{
    opExtrude(context, eid, { "entities" : qSketchRegion(sid), "direction" : dir, "endBound" : BoundingType.BLIND, "endDepth" : depth * millimeter });
    return qCreatedBy(eid, EntityType.BODY);
}

// rectangle (y0..y1 lateral, r0..r1 radial) revolved about the axle from s0 to s1
function s3RevRect(context is Context, rid is Id, y0 is number, y1 is number, r0 is number, r1 is number, s0 is number, s1 is number) returns Query
{
    const sk = newSketchOnPlane(context, rid + "sk", { "sketchPlane" : plane(vector(0, 0, 0) * millimeter, s3T(s0), vector(0, 1, 0)) });
    s3Line(sk, "a", y0, r0, y1, r0);
    s3Line(sk, "b", y1, r0, y1, r1);
    s3Line(sk, "c", y1, r1, y0, r1);
    s3Line(sk, "d", y0, r1, y0, r0);
    skSolve(sk);
    opRevolve(context, rid + "rv", { "entities" : qSketchRegion(rid + "sk"), "axis" : line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)),
                "angleForward" : ((s1 - s0) / 369.5) * radian });
    return qCreatedBy(rid + "rv", EntityType.BODY);
}

function s3Box(context is Context, bid is Id, c1 is Vector, c2 is Vector) returns Query
{
    const lo = vector(min(c1[0], c2[0]), min(c1[1], c2[1]), min(c1[2], c2[2]));
    const hi = vector(max(c1[0], c2[0]), max(c1[1], c2[1]), max(c1[2], c2[2]));
    fCuboid(context, bid, { "corner1" : lo * millimeter, "corner2" : hi * millimeter });
    return qCreatedBy(bid, EntityType.BODY);
}

// half-space beyond the radial plane at s (removeAbove = true removes s' > s, false removes s' < s)
function s3Half(context is Context, hid is Id, s is number, removeAbove is boolean) returns Query
{
    const cs = coordSystem(vector(0, 0, 0) * millimeter, s3Rn(s), s3T(s));
    fCuboid(context, hid, { "corner1" : vector(-1000, -1000, removeAbove ? 0 : -1000) * millimeter, "corner2" : vector(1000, 1000, removeAbove ? 1000 : 0) * millimeter });
    opTransform(context, hid + "tx", { "bodies" : qCreatedBy(hid, EntityType.BODY), "transform" : toWorld(cs) });
    return qCreatedBy(hid, EntityType.BODY);
}

// stay cup (sy = +1 / -1 side): leg + R2 inside / R5 outside corner + lip, sketched at z 378 (d 2) and extruded along the
// stay direction (0, 0.3 sy, -1) down to z 356 (d 24); leg stay face 1.0 off the stay inner face y = 30 + 0.3 d
function s3Cup(context is Context, cid is Id, sy is number) returns Query
{
    const ys = 29.6;
    const yt = 29.6 - 3 * sqrt(1.09);
    const sk = newSketchOnPlane(context, cid + "sk", { "sketchPlane" : plane(vector(0, 0, 378) * millimeter, vector(0, 0, 1), vector(1, 0, 0)) });
    s3Line(sk, "a", 26, sy * ys, -17, sy * ys);
    skArc(sk, "b", { "start" : vector(-17, sy * ys) * millimeter, "mid" : vector(-17 - sqrt(2), sy * (ys + 2 - sqrt(2))) * millimeter, "end" : vector(-19, sy * (ys + 2)) * millimeter });
    s3Line(sk, "c", -19, sy * (ys + 2), -19, sy * 50);
    s3Line(sk, "d", -19, sy * 50, -22, sy * 50);
    s3Line(sk, "e", -22, sy * 50, -22, sy * (yt + 5));
    skArc(sk, "f", { "start" : vector(-22, sy * (yt + 5)) * millimeter, "mid" : vector(-17 - 2.5 * sqrt(2), sy * (yt + 5 - 2.5 * sqrt(2))) * millimeter, "end" : vector(-17, sy * yt) * millimeter });
    s3Line(sk, "g", -17, sy * yt, 26, sy * yt);
    s3Line(sk, "h", 26, sy * yt, 26, sy * ys);
    skSolve(sk);
    return s3Extr(context, cid + "x", cid + "sk", vector(0, 0.3 * sy, -1), 22 * sqrt(1.09));
}

// obround flank hole W (along y) x H (along the flank), axis along the flank normal nf, from 5 inside to 2 outside the outer surface
function s3Hole(context is Context, hid is Id, hcx is number, hcz is number, y is number, sx is number, W is number, H is number) returns Query
{
    const t = 50 * degree;
    const nf = vector(sx * sin(t), 0, cos(t));
    const org = vector(sx * hcx, y, hcz) * millimeter - nf * 5 * millimeter;
    const sk = newSketchOnPlane(context, hid + "sk", { "sketchPlane" : plane(org, nf, vector(0, 1, 0)) });
    const a = W / 2 - H / 2;
    const r = H / 2;
    s3Line(sk, "a", -a, r, a, r);
    skArc(sk, "b", { "start" : vector(a, r) * millimeter, "mid" : vector(a + r, 0) * millimeter, "end" : vector(a, -r) * millimeter });
    s3Line(sk, "c", a, -r, -a, -r);
    skArc(sk, "d", { "start" : vector(-a, -r) * millimeter, "mid" : vector(-a - r, 0) * millimeter, "end" : vector(-a, r) * millimeter });
    skSolve(sk);
    return s3Extr(context, hid + "x", hid + "sk", nf, 7);
}

// stay-tie recess on the leg's tyre face: 1.0 deep (normal), z 359..365, x -16.5..21, as an oblique slab along the stay
function s3Recess(context is Context, rid is Id, sy is number) returns Query
{
    const k = sqrt(1.09);
    const yt = 29.6 - 3 * k + 0.3 * 13;
    const sk = newSketchOnPlane(context, rid + "sk", { "sketchPlane" : plane(vector(0, 0, 365) * millimeter, vector(0, 0, 1), vector(1, 0, 0)) });
    const y0 = sy * (yt - 1.5 * k);
    const y1 = sy * (yt + 1.0 * k);
    s3Line(sk, "a", -16.5, y0, 21, y0);
    s3Line(sk, "b", 21, y0, 21, y1);
    s3Line(sk, "c", 21, y1, -16.5, y1);
    s3Line(sk, "d", -16.5, y1, -16.5, y0);
    skSolve(sk);
    return s3Extr(context, rid + "x", rid + "sk", vector(0, 0.3 * sy, -1), 6 * k);
}

function s3BuildCoupon(context is Context, id is Id, name is string, oi is number, ylip is number) returns map
{
    const P = s3Prof(oi);
    const Y = vector(0, 1, 0);
    const ysta = [10, 14, 18, 22, 25.5];
    const dxs = [0, 0.151531, 0.626136, 1.5, 4.230826];
    const xas = [-3.0, -3.0, -3.0, -4.5];
    var adds = [];
    // H0: waist |y| <= 10, one exact extrude of the full section
    s3ShellSk(context, id + "h0S", -10, P, 0);
    adds = append(adds, s3Extr(context, id + "h0", id + "h0S", Y, 20));
    // H1: flare |y| 10..25.5, 4 segments x 2 halves x 2 sides, each an oblique extrude (profile translates by the dx step)
    var n = 0;
    for (var sy in [-1, 1])
    {
        for (var j = 0; j < 4; j += 1)
        {
            const ddx = dxs[j + 1] - dxs[j];
            const dy = ysta[j + 1] - ysta[j];
            for (var sx in [-1, 1])
            {
                s3HalfSk(context, id + ("fS" ~ n), sy * ysta[j], P, dxs[j], sx, xas[j]);
                adds = append(adds, s3Extr(context, id + ("f" ~ n), id + ("fS" ~ n), vector(sx * ddx, sy * dy, 0), sqrt(ddx * ddx + dy * dy)));
                n += 1;
            }
        }
        // H2: end wall + neck skirt |y| 25.5..28
        s3EndSk(context, id + ("eS" ~ n), sy * 25.5, P, dxs[4]);
        adds = append(adds, s3Extr(context, id + ("e" ~ n), id + ("eS" ~ n), vector(0, sy, 0), 2.5));
        // crown web and stay cup
        adds = append(adds, s3RevRect(context, id + ("wb" ~ n), sy * 27.5, sy * 31.6, 366.5, 369.0, -21, 24));
        adds = append(adds, s3Cup(context, id + ("cp" ~ n), sy));
        n += 1;
    }
    // ID, embossed 0.6 on the +y skirt face (sketch 0.5 inside, extruded 1.1), centred near s -31, R ~363
    const tsk = newSketchOnPlane(context, id + "idS", { "sketchPlane" : plane(vector(0, 27.5, 0) * millimeter, vector(0, 1, 0), vector(-1, 0, 0)) });
    skText(tsk, "id", { "text" : name, "fontName" : "OpenSans-Bold.ttf", "firstCorner" : vector(25.5, 359.5) * millimeter, "secondCorner" : vector(37.5, 364.5) * millimeter });
    skSolve(tsk);
    opExtrude(context, id + "idX", { "entities" : qSketchRegion(id + "idS", true), "direction" : Y, "endBound" : BoundingType.BLIND, "endDepth" : 1.1 * millimeter });
    const nTxt = size(evaluateQuery(context, qCreatedBy(id + "idX", EntityType.BODY)));
    adds = append(adds, qCreatedBy(id + "idX", EntityType.BODY));
    opBoolean(context, id + "u", { "tools" : qUnion(adds), "operationType" : BooleanOperationType.UNION });
    // cuts
    var cuts = [];
    const hc = P.hc;
    for (var sx in [-1, 1])
    {
        const t = sx > 0 ? "R" : "L";
        cuts = append(cuts, s3Hole(context, id + ("hP" ~ t), hc[0], hc[1], 8, sx, 6.0, 2.6));
        cuts = append(cuts, s3Hole(context, id + ("hN" ~ t), hc[0], hc[1], -8, sx, 5.5, 2.2));
        // crest notch 6.0 x 1.0 on the -8 tie
        cuts = append(cuts, s3Box(context, id + ("nt" ~ t), vector(sx * (P.C1[0] - 3.5), -11, 398.5), vector(sx * (P.C1[0] + 3.5), -5, 401)));
    }
    for (var sy in [-1, 1])
    {
        const t = sy > 0 ? "P" : "N";
        cuts = append(cuts, s3Box(context, id + ("sh" ~ t), vector(19.7, sy * 29, 359), vector(22.3, sy * 36.5, 365)));
        cuts = append(cuts, s3Recess(context, id + ("rc" ~ t), sy));
        cuts = append(cuts, s3Box(context, id + ("gv" ~ t), vector(-23.5, sy * 38, 359), vector(-21.2, sy * 48, 365)));
        cuts = append(cuts, s3Box(context, id + ("le" ~ t), vector(-24, sy * ylip, 354), vector(-17, sy * 60, 380)));
    }
    cuts = append(cuts, s3Half(context, id + "trP", 50, true));
    cuts = append(cuts, s3Half(context, id + "trN", -50, false));
    const pIn = vector(0, 0, 378.5) * millimeter;
    opBoolean(context, id + "cut", { "targets" : qContainsPoint(qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID), pIn), "tools" : qUnion(cuts), "operationType" : BooleanOperationType.SUBTRACTION });
    return { "pIn" : pIn, "nTxt" : nTxt };
}

function s3FmtB(b is Box3d) returns string
{
    const a = b.minCorner / millimeter;
    const c = b.maxCorner / millimeter;
    return "x" ~ roundToPrecision(a[0], 1) ~ ".." ~ roundToPrecision(c[0], 1) ~ " y" ~ roundToPrecision(a[1], 1) ~ ".." ~ roundToPrecision(c[1], 1) ~
        " z" ~ roundToPrecision(a[2], 1) ~ ".." ~ roundToPrecision(c[2], 1);
}

annotation { "Feature Type Name" : "Rear S3.1 saddle coupons" }
export const rearS31SaddleCoupons = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const mm = millimeter;
        // name, oi = 2 * inner offset, lip edge |y|, offline volume (s3vox.py, 0.25 voxel), plate move (x, y, z); D is raised
        // 4 in y so all four lip edges sit on the same bed plane y = -43
        const V = [["S3.1-A", 1, 43.0, 44.10, 200, 0, 0, "A"], ["S3.1-B", 2, 43.0, 44.13, 315, 0, 0, "B"],
                   ["S3.1-C", 3, 43.0, 44.17, 200, 0, -60, "C"], ["S3.1-D", 2, 47.0, 44.62, 315, 4, -60, "D"]];
        // probe points (coupon at the origin) and expected inside(1)/outside(0) string, from probes.py on the offline model
        const PR = [[[0, 0, 378.5], [0, 0, 390], [12.07, 0, 391], [-12.07, 0, 391], [19.5, 0, 385], [21.073, 0, 391.745], [21.073, 8, 391.745], [-21.073, -8, 391.745], [14.659, -8, 399], [14.659, 8, 399], [0, 27, 370], [19.5, 24, 385], [10, 30.4, 370], [10, -30.4, 370], [-20.5, 40, 370], [-20.5, -40, 370], [-20.5, 44, 370], [-20.5, 42, 370], [21, 32.8, 362], [0, 31.77, 362], [0, 32.9, 362], [-21.6, 40, 362], [-21.6, 40, 370], [0, 40, 370], [-18.25, 40, 370], [0, 28.9, 367.75], [51, 0, 368], [0, 27, 390], [10, 34.75, 355.5], [10, 34.75, 356.5]],
                    [[0, 0, 378.5], [0, 0, 390], [12.57, 0, 391], [-12.57, 0, 391], [19.5, 0, 385], [21.565, 0, 391.745], [21.565, 8, 391.745], [-21.565, -8, 391.745], [15.15, -8, 399], [15.15, 8, 399], [0, 27, 370], [19.5, 24, 385], [10, 30.4, 370], [10, -30.4, 370], [-20.5, 40, 370], [-20.5, -40, 370], [-20.5, 44, 370], [-20.5, 42, 370], [21, 32.8, 362], [0, 31.77, 362], [0, 32.9, 362], [-21.6, 40, 362], [-21.6, 40, 370], [0, 40, 370], [-18.25, 40, 370], [0, 28.9, 367.75], [51, 0, 368], [0, 27, 390], [10, 34.75, 355.5], [10, 34.75, 356.5]],
                    [[0, 0, 378.5], [0, 0, 390], [13.07, 0, 391], [-13.07, 0, 391], [19.5, 0, 385], [22.056, 0, 391.745], [22.056, 8, 391.745], [-22.056, -8, 391.745], [15.641, -8, 399], [15.641, 8, 399], [0, 27, 370], [19.5, 24, 385], [10, 30.4, 370], [10, -30.4, 370], [-20.5, 40, 370], [-20.5, -40, 370], [-20.5, 44, 370], [-20.5, 42, 370], [21, 32.8, 362], [0, 31.77, 362], [0, 32.9, 362], [-21.6, 40, 362], [-21.6, 40, 370], [0, 40, 370], [-18.25, 40, 370], [0, 28.9, 367.75], [51, 0, 368], [0, 27, 390], [10, 34.75, 355.5], [10, 34.75, 356.5]],
                    [[0, 0, 378.5], [0, 0, 390], [12.57, 0, 391], [-12.57, 0, 391], [19.5, 0, 385], [21.565, 0, 391.745], [21.565, 8, 391.745], [-21.565, -8, 391.745], [15.15, -8, 399], [15.15, 8, 399], [0, 27, 370], [19.5, 24, 385], [10, 30.4, 370], [10, -30.4, 370], [-20.5, 40, 370], [-20.5, -40, 370], [-20.5, 48, 370], [-20.5, 46, 370], [21, 32.8, 362], [0, 31.77, 362], [0, 32.9, 362], [-21.6, 40, 362], [-21.6, 40, 370], [0, 40, 370], [-18.25, 40, 370], [0, 28.9, 367.75], [51, 0, 368], [0, 27, 390], [10, 34.75, 355.5], [10, 34.75, 356.5]]];
        const PX = [[1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1], [1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1], [1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1], [1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1]];
        var fail = "";
        var rep = "REAR-S3.1-COUPONS";
        var vTot = 0;
        var vs = "";
        var i = 0;
        for (var v in V)
        {
            const r = s3BuildCoupon(context, id + ("c" ~ i), v[0], v[1], v[2]);
            const own = qBodyType(qCreatedBy(id + ("c" ~ i), EntityType.BODY), BodyType.SOLID);
            const nOwn = size(evaluateQuery(context, own));
            const q = qContainsPoint(own, r.pIn);
            const nq = size(evaluateQuery(context, q));
            var vol = 0;
            var bb = "";
            var pr = "";
            if (nq != 1 || nOwn != 1)
                fail = fail ~ " " ~ v[7] ~ ":n" ~ nq ~ "/" ~ nOwn;
            else
            {
                vol = evVolume(context, { "entities" : q }) / (centimeter ^ 3);
                bb = s3FmtB(evBox3d(context, { "topology" : q, "tight" : true }));
                if (abs(vol - v[3]) > 0.08 * v[3])
                    fail = fail ~ " " ~ v[7] ~ ":vol";
                const pts = PR[i];
                var nBad = 0;
                var bad = "";
                for (var k = 0; k < size(pts); k += 1)
                {
                    const got = size(evaluateQuery(context, qContainsPoint(q, vector(pts[k][0], pts[k][1], pts[k][2]) * mm)));
                    pr = pr ~ got;
                    if (got != PX[i][k])
                    {
                        nBad += 1;
                        if (nBad <= 4)
                            bad = bad ~ (nBad > 1 ? "," : "") ~ k;
                    }
                }
                if (nBad > 0)
                    fail = fail ~ " " ~ v[7] ~ ":p" ~ bad ~ (nBad > 4 ? "+" ~ (nBad - 4) : "");
                setProperty(context, { "entities" : q, "propertyType" : PropertyType.NAME,
                            "value" : "Rear " ~ v[0] ~ " saddle coupon (o " ~ (v[1] / 2) ~ ", lip |y| " ~ v[2] ~ ")" });
                opTransform(context, id + ("mv" ~ i), { "bodies" : q, "transform" : transform(vector(v[4], v[5], v[6]) * mm) });
                vTot += vol;
            }
            rep = rep ~ " | " ~ v[0] ~ " o=" ~ (v[1] / 2) ~ " lip=" ~ v[2] ~ " vol=" ~ roundToPrecision(vol, 2) ~ " (exp " ~ v[3] ~ ") " ~ bb ~ " p=" ~ pr ~ " txt=" ~ r.nTxt;
            vs = vs ~ " " ~ v[7] ~ "=" ~ roundToPrecision(vol, 2);
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
            throw regenError("S3 FAIL:" ~ fail ~ " | vol" ~ vs ~ " | plate " ~ roundToPrecision(pd[0], 1) ~ "x" ~ roundToPrecision(pd[2], 1) ~ " clash=" ~ nClash ~ " tiny=" ~ nTiny);
        println(rep ~ " PASS");
    }, {});
