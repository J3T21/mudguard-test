FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Scan-fit brace saddles C0–C2. The crown is a smooth fit to the bridge
// underside in the cropped Kiri scan, mapped onto the 60 mm tyre-side mouth.
// The coupon is 1 mm per side inside that mouth, tapers to 48 mm over the
// 12 mm brace thickness, and carries an 8 mm handle on the tyre face. The
// plastic flap is ignored. STLs come from featurescript/brace_arch_coupons.py.

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
