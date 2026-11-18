FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Shell that fits around the rear brace. The cavity is the brace arch.
// The skin is 2.5 mm thick and the skirt runs 45 mm down the stay, 20 mm out.
// STLs come from featurescript/brace_arch_coupons.py.

const CROWN_X = [
            -30.00, -27.50, -25.00, -22.50, -20.00, -17.50, -15.00, -12.50,
            -10.00, -7.50, -5.00, -2.50, 0.00, 2.50, 5.00, 7.50, 10.00, 12.50,
            15.00, 17.50, 20.00, 22.50, 25.00, 27.50, 30.00
        ];
const CROWN_Y = [
            0.00, 1.01, 1.98, 2.89, 3.72, 4.46, 5.09, 5.60, 6.00, 6.26, 6.40,
            6.40, 6.28, 6.05, 5.70, 5.25, 4.73, 4.13, 3.49, 2.83, 2.17, 1.55,
            0.99, 0.52, 0.19
        ];

const WALL = 2.5;
const DEPTH = 16;
const FLARE = 20;
const SKIRT = 45;
const FLARE_N = 16;
const TYRE_W = 58;
const SEAT_W = 48;
const HANDLE = 8;
const BRACE_THK = 12;

function crownMin() returns number
{
    var ymin = CROWN_Y[0];
    for (var i = 1; i < size(CROWN_Y); i += 1)
        if (CROWN_Y[i] < ymin)
            ymin = CROWN_Y[i];
    return ymin;
}

function addClosed(sk is Sketch, pts is array)
{
    for (var i = 0; i < size(pts) - 1; i += 1)
        skLineSegment(sk, "e" ~ i, { "start" : pts[i] * millimeter, "end" : pts[i + 1] * millimeter });
}

function flarePts(origin is Vector, tangent is Vector, side is number) returns array
{
    var heading = tangent / norm(tangent);
    const dth = (side > 0 ? -1 : 1) * (82 * PI / 180) / FLARE_N;
    const c = cos(dth);
    const s = sin(dth);
    const step = (FLARE + SKIRT) / FLARE_N;
    var p = origin;
    var pts = [];
    for (var i = 0; i < FLARE_N; i += 1)
    {
        heading = vector(c * heading[0] - s * heading[1], s * heading[0] + c * heading[1]);
        p = p + step * heading;
        pts = append(pts, p);
    }
    return pts;
}

function contactPts(width is number, clearance is number) returns array
{
    const scale = width / 60;
    var cav = [];
    for (var i = 0; i < size(CROWN_X); i += 1)
        cav = append(cav, vector(CROWN_X[i] * scale, CROWN_Y[i] - clearance));
    const n = size(cav);
    const left = flarePts(cav[0], cav[0] - cav[1], -1);
    const right = flarePts(cav[n - 1], cav[n - 1] - cav[n - 2], 1);
    var pts = [];
    for (var i = size(left) - 1; i >= 0; i -= 1)
        pts = append(pts, left[i]);
    for (var i = 0; i < n; i += 1)
        pts = append(pts, cav[i]);
    for (var i = 0; i < size(right); i += 1)
        pts = append(pts, right[i]);
    return pts;
}

function offsetTyre(pts is array) returns array
{
    var out = [];
    for (var i = 0; i < size(pts); i += 1)
    {
        var t;
        if (i == 0)
            t = pts[1] - pts[0];
        else if (i == size(pts) - 1)
            t = pts[i] - pts[i - 1];
        else
            t = pts[i + 1] - pts[i - 1];
        t = t / norm(t);
        out = append(out, pts[i] + WALL * vector(t[1], -t[0]));
    }
    return out;
}

function shellPoints(width is number, clearance is number) returns array
{
    // Outer skin left to right, then the brace face back to the start.
    const contact = contactPts(width, clearance);
    const outer = offsetTyre(contact);
    var pts = outer;
    for (var i = size(contact) - 1; i >= 0; i -= 1)
        pts = append(pts, contact[i]);
    pts = append(pts, outer[0]);
    return pts;
}

function sectionSketch(context is Context, id is Id, z is number, pts is array)
{
    const pl = plane(vector(0, 0, z) * millimeter, vector(0, 0, 1), vector(1, 0, 0));
    const sk = newSketchOnPlane(context, id, { "sketchPlane" : pl });
    addClosed(sk, pts);
    skSolve(sk);
}

function makeCoupon(context is Context, id is Id, clearance is number, label is string)
{
    const tyre = TYRE_W - 2 * clearance;
    const seat = SEAT_W - 2 * clearance;
    // Handle stays full width, then the 12 mm brace thickness tapers to the seat face.
    const zOuter = [0, HANDLE, HANDLE + BRACE_THK];
    const wOuter = [tyre, tyre, seat];
    var outerQ = [];
    for (var i = 0; i < 3; i += 1)
    {
        sectionSketch(context, id + ("o" ~ i), zOuter[i], shellPoints(wOuter[i], clearance));
        outerQ = append(outerQ, qSketchRegion(id + ("o" ~ i)));
    }
    opLoft(context, id + "out", { "profileSubqueries" : outerQ, "bodyType" : ToolBodyType.SOLID });

    // Four zip slots, two pairs, long axis lateral. Cut through the crown only.
    const slotX = [-16, -8, 4, 11];
    const slotZ = [14, 14, 14, 14];
    var slotQs = [];
    for (var k = 0; k < 4; k += 1)
    {
        fCuboid(context, id + ("slot" ~ k), {
                    "corner1" : vector(slotX[k] - 3, -2, slotZ[k] - 1.2) * millimeter,
                    "corner2" : vector(slotX[k] + 3, 10, slotZ[k] + 1.2) * millimeter });
        slotQs = append(slotQs, qCreatedBy(id + ("slot" ~ k), EntityType.BODY));
    }
    opBoolean(context, id + "slots", {
                "tools" : qUnion(slotQs),
                "targets" : qCreatedBy(id + "out", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });

    // ID marks sit on the tyre side of the shell, clear of the brace face.
    const nBar = { "C0" : 1, "C1" : 2, "C2" : 3 }[label];
    const ySkin = 6.4 - clearance - WALL;
    var markQs = [];
    for (var k = 0; k < nBar; k += 1)
    {
        const z0 = 1.2 + k * 2.2;
        fCuboid(context, id + ("markI" ~ k), {
                    "corner1" : vector(-6, ySkin - 1.2, z0) * millimeter,
                    "corner2" : vector(6, ySkin + 0.4, z0 + 1.4) * millimeter });
        markQs = append(markQs, qCreatedBy(id + ("markI" ~ k), EntityType.BODY));
        const x0 = -8 + k * 6;
        fCuboid(context, id + ("markE" ~ k), {
                    "corner1" : vector(x0, ySkin - 1.2, -1.2) * millimeter,
                    "corner2" : vector(x0 + 3, ySkin + 0.6, 0) * millimeter });
        markQs = append(markQs, qCreatedBy(id + ("markE" ~ k), EntityType.BODY));
    }
    opBoolean(context, id + "marks", {
                "tools" : qUnion(append(markQs, qCreatedBy(id + "out", EntityType.BODY))),
                "operationType" : BooleanOperationType.UNION });

    setProperty(context, {
                "entities" : qCreatedBy(id + "out", EntityType.BODY),
                "propertyType" : PropertyType.NAME,
                "value" : "Brace coupon " ~ label ~ " clearance " ~ toString(clearance) ~ " mm" });
}

annotation { "Feature Type Name" : "Brace scan-fit coupons C0-C2" }
export const braceArchCoupons = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const clearances = [0, 0.4, 0.8];
        const labels = ["C0", "C1", "C2"];
        for (var i = 0; i < 3; i += 1)
        {
            makeCoupon(context, id + ("c" ~ i), clearances[i], labels[i]);
            opTransform(context, id + ("tx" ~ i), {
                        "bodies" : qCreatedBy(id + ("c" ~ i) + "out", EntityType.BODY),
                        "transform" : transform(vector(i * 140, 0, 0) * millimeter) });
        }
        println("brace shell C0-C2: cavity 58 to 48, skirt 45 down and 20 out, wall 2.5");
    }, {});
