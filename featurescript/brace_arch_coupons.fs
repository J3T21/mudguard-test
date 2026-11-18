FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Under-brace arch fit coupons A1–A4. Offline design; paste into Onshape only when exporting.
// No API usage required — STLs come from featurescript/brace_arch_coupons.py.
//
// Outer width 88 mm (~1 mm/side vs ~90 mm cavity at tyre-top height).
// Crown = circular arc over an 82 mm chord; walls vertical; wall 2.5 mm;
// length 50 mm; depth 30 mm. Rises: A1 2.5, A2 4.0, A3 5.5, A4 7.0 mm.
// Sketch: x = lateral, y = outward (crown +y). Extrude along +Z.

function crownRadius(halfChord is number, rise is number) returns number
{
    return (halfChord * halfChord + rise * rise) / (2 * rise);
}

function makeCoupon(context is Context, id is Id, rise is number, label is string)
{
    const mm = millimeter;
    const outerW = 88;
    const chord = 82;
    const wall = 2.5;
    const depth = 30;
    const length = 50;
    const halfC = chord / 2;
    const halfO = outerW / 2;
    const R = crownRadius(halfC, rise);
    const cy = rise - R; // circle centre y (below chord)
    const Ri = R - wall;
    const halfOi = halfO - wall;

    // Inner chord half-width where the concentric arc meets y = 0:
    // x^2 + (0 - cy)^2 = Ri^2  =>  x = sqrt(Ri^2 - cy^2)
    const halfCi = sqrt(Ri * Ri - cy * cy);

    const pl = plane(vector(0, 0, 0) * mm, vector(0, 0, 1), vector(1, 0, 0));

    // --- Outer solid U ---
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : pl });
    skLineSegment(sk, "ol", {
                "start" : vector(-halfO, -depth) * mm,
                "end" : vector(-halfC, 0) * mm });
    skArc(sk, "oa", {
                "start" : vector(-halfC, 0) * mm,
                "mid" : vector(0, rise) * mm,
                "end" : vector(halfC, 0) * mm });
    skLineSegment(sk, "or", {
                "start" : vector(halfC, 0) * mm,
                "end" : vector(halfO, -depth) * mm });
    skLineSegment(sk, "of", {
                "start" : vector(halfO, -depth) * mm,
                "end" : vector(-halfO, -depth) * mm });
    skSolve(sk);
    opExtrude(context, id + "ex", {
                "entities" : qSketchRegion(id + "sk"),
                "direction" : vector(0, 0, 1),
                "endBound" : BoundingType.BLIND,
                "endDepth" : length * mm });

    // --- Inner void (concentric crown, walls inset by wall) ---
    const ski = newSketchOnPlane(context, id + "ski", { "sketchPlane" : pl });
    skLineSegment(ski, "il", {
                "start" : vector(-halfOi, -depth + wall) * mm,
                "end" : vector(-halfCi, 0) * mm });
    skArc(ski, "ia", {
                "start" : vector(-halfCi, 0) * mm,
                "mid" : vector(0, cy + Ri) * mm,
                "end" : vector(halfCi, 0) * mm });
    skLineSegment(ski, "ir", {
                "start" : vector(halfCi, 0) * mm,
                "end" : vector(halfOi, -depth + wall) * mm });
    skLineSegment(ski, "if", {
                "start" : vector(halfOi, -depth + wall) * mm,
                "end" : vector(-halfOi, -depth + wall) * mm });
    skSolve(ski);
    opExtrude(context, id + "exi", {
                "entities" : qSketchRegion(id + "ski"),
                "direction" : vector(0, 0, 1),
                "endBound" : BoundingType.BLIND,
                "endDepth" : (length + 2) * mm });
    opTransform(context, id + "ti", {
                "bodies" : qCreatedBy(id + "exi", EntityType.BODY),
                "transform" : transform(vector(0, 0, -1) * mm) });
    opBoolean(context, id + "cut", {
                "tools" : qCreatedBy(id + "exi", EntityType.BODY),
                "targets" : qCreatedBy(id + "ex", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION });

    // ID nubs on the inner floor: 1..4 boxes for A1..A4 (matches Python STLs).
    const nNub = { "A1" : 1, "A2" : 2, "A3" : 3, "A4" : 4 }[label];
    var nubQs = [];
    for (var k = 0; k < nNub; k += 1)
    {
        const z0 = 8 + k * 8;
        fCuboid(context, id + ("nub" ~ k), {
                    "corner1" : vector(-6, -depth + wall, z0) * mm,
                    "corner2" : vector(-2, -depth + wall + 0.6, z0 + 4) * mm });
        nubQs = append(nubQs, qCreatedBy(id + ("nub" ~ k), EntityType.BODY));
    }
    if (size(nubQs) > 0)
    {
        opBoolean(context, id + "nubu", {
                    "tools" : qUnion(append(nubQs, qCreatedBy(id + "ex", EntityType.BODY))),
                    "operationType" : BooleanOperationType.UNION });
    }

    setProperty(context, {
                "entities" : qCreatedBy(id + "ex", EntityType.BODY),
                "propertyType" : PropertyType.NAME,
                "value" : "Brace coupon " ~ label ~ " rise " ~ toString(rise) ~ " mm" });
}

annotation { "Feature Type Name" : "Brace arch coupons A1-A4 (88 mm)" }
export const braceArchCoupons = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Build all four coupons" }
        definition.buildAll is boolean;
        annotation { "Name" : "Single variant (if not all): 1=A1 .. 4=A4" }
        isInteger(definition.variant, { (unitless) : [1, 1, 4] } as IntegerBoundSpec);
    }
    {
        const rises = [2.5, 4.0, 5.5, 7.0];
        const labels = ["A1", "A2", "A3", "A4"];
        if (definition.buildAll)
        {
            for (var i = 0; i < 4; i += 1)
            {
                makeCoupon(context, id + ("c" ~ i), rises[i], labels[i]);
                // Space along X so they do not overlap in the Part Studio.
                opTransform(context, id + ("tx" ~ i), {
                            "bodies" : qCreatedBy(id + ("c" ~ i) + "ex", EntityType.BODY),
                            "transform" : transform(vector((i - 1.5) * 100, 0, 0) * millimeter) });
            }
        }
        else
        {
            const i = definition.variant - 1;
            makeCoupon(context, id + "one", rises[i], labels[i]);
        }
        println("brace coupons: outerW=88 chord=82 wall=2.5 length=50 depth=30 rises=" ~ toString(rises));
        println("print STLs offline: python featurescript/brace_arch_coupons.py (zero Onshape API calls)");
    }, { "buildAll" : true, "variant" : 1 });
