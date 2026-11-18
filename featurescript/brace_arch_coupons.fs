FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Scan-fit brace saddles C0–C2. The crown is the levelled Kiri underside of
// the rear brace (3-point smooth) across the 60 mm tyre-side mouth. The
// coupon is 1 mm per side inside that mouth, tapers to 48 mm over the 12 mm
// brace thickness, and carries an 8 mm handle on the tyre face. A plastic
// flap on top of the brace is ignored. STLs come from
// featurescript/brace_arch_coupons.py — paste this in only when exporting.

const CROWN_X = [
            -30.12, -27.41, -24.10, -21.82, -19.11, -16.83, -14.55, -12.04,
            -9.74, -7.38, -4.81, -2.50, 0.14, 2.42, 4.80, 7.12, 9.58, 12.01,
            14.48, 16.83, 19.44, 21.94, 24.42, 27.33, 29.88
        ];
const CROWN_Y = [
            4.30, 2.57, 1.45, 0.32, 0.64, 0.95, 1.56, 2.15, 2.62, 2.99, 3.35,
            3.33, 3.71, 3.98, 4.71, 5.18, 5.59, 5.78, 6.13, 6.24, 6.30, 6.17,
            5.62, 5.01, 4.30
        ];

const WALL = 2.5;
const DEPTH = 16;
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

function outerPoints(width is number, clearance is number) returns array
{
    const scale = width / 60;
    const half = width / 2;
    const floor = crownMin() - clearance - DEPTH;
    const y0 = CROWN_Y[0] - clearance;
    const y1 = CROWN_Y[size(CROWN_Y) - 1] - clearance;
    var pts = [vector(-half, floor), vector(-half, y0)];
    for (var i = 0; i < size(CROWN_X); i += 1)
        pts = append(pts, vector(CROWN_X[i] * scale, CROWN_Y[i] - clearance));
    pts = append(pts, vector(half, y1));
    pts = append(pts, vector(half, floor));
    pts = append(pts, vector(-half, floor));
    return pts;
}

function innerPoints(width is number, clearance is number) returns array
{
    const scale = width / 60;
    const half = width / 2 - WALL;
    const floor = crownMin() - clearance - DEPTH + WALL;
    const y0 = CROWN_Y[0] - clearance - WALL;
    const y1 = CROWN_Y[size(CROWN_Y) - 1] - clearance - WALL;
    var pts = [vector(-half, floor), vector(-half, y0)];
    for (var i = 0; i < size(CROWN_X); i += 1)
        pts = append(pts, vector(CROWN_X[i] * scale, CROWN_Y[i] - clearance - WALL));
    pts = append(pts, vector(half, y1));
    pts = append(pts, vector(half, floor));
    pts = append(pts, vector(-half, floor));
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
        sectionSketch(context, id + ("o" ~ i), zOuter[i], outerPoints(wOuter[i], clearance));
        outerQ = append(outerQ, qSketchRegion(id + ("o" ~ i)));
    }
    opLoft(context, id + "out", { "profileSubqueries" : outerQ, "bodyType" : ToolBodyType.SOLID });

    // Inner void runs 1 mm past each end so the cut does not leave a cap.
    const zInner = [-1, HANDLE, HANDLE + BRACE_THK + 1];
    const wInner = [tyre, tyre, seat];
    var innerQ = [];
    for (var i = 0; i < 3; i += 1)
    {
        sectionSketch(context, id + ("i" ~ i), zInner[i], innerPoints(wInner[i], clearance));
        innerQ = append(innerQ, qSketchRegion(id + ("i" ~ i)));
    }
    opLoft(context, id + "inn", { "profileSubqueries" : innerQ, "bodyType" : ToolBodyType.SOLID });
    opBoolean(context, id + "shell", {
                "tools" : qCreatedBy(id + "inn", EntityType.BODY),
                "targets" : qCreatedBy(id + "out", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });

    // Four zip slots, two pairs, long axis lateral. Cut through the crown only.
    const slotX = [-11, 11, -11, 11];
    const slotZ = [5, 5, 15, 15];
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

    // ID marks: 1/2/3 bars for C0/C1/C2 on the inner handle floor + tyre-face end.
    const nBar = { "C0" : 1, "C1" : 2, "C2" : 3 }[label];
    var markQs = [];
    for (var k = 0; k < nBar; k += 1)
    {
        const z0 = 1.2 + k * 2.2;
        fCuboid(context, id + ("markI" ~ k), {
                    "corner1" : vector(-10, -DEPTH + WALL, z0) * millimeter,
                    "corner2" : vector(10, -DEPTH + WALL + 1.2, z0 + 1.4) * millimeter });
        markQs = append(markQs, qCreatedBy(id + ("markI" ~ k), EntityType.BODY));
        const x0 = -8 + k * 6;
        fCuboid(context, id + ("markE" ~ k), {
                    "corner1" : vector(x0, -DEPTH, -1.2) * millimeter,
                    "corner2" : vector(x0 + 4, -DEPTH + 1.5, 0) * millimeter });
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
                        "transform" : transform(vector(i * 80, 0, 0) * millimeter) });
        }
        println("brace coupons C0-C2: tyre 58 seat 48 length 20 depth 16 wall 2.5 clearances 0/0.4/0.8");
    }, {});
