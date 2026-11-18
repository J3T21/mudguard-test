FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Under-brace arch fit coupons A1–A4. Offline design; paste into Onshape only when exporting.
// No API usage required — STLs come from featurescript/brace_arch_coupons.py.
//
// Outer width 88 mm, vertical walls (no floor flare) so wall→R18 is G1.
// Production-style R18 top-corner fillets; circular crown tangent to both fillets.
// Wall 2.5 mm; length 50 mm; depth 30 mm.
// Rises: A1 2.5, A2 4.0, A3 5.5, A4 7.0 mm.
// Sketch: x = lateral, y = outward (crown +y). Extrude along +Z.

function crownRadiusTangent(halfChord is number, rise is number, cornerR is number) returns number
{
    // Fillet centres at (±(halfChord - cornerR), -cornerR).
    // |C - F| = R - cornerR with C = (0, rise - R)
    const hx = halfChord - cornerR;
    return (hx * hx + (rise + cornerR) * (rise + cornerR) - cornerR * cornerR) / (2 * rise);
}

function makeCoupon(context is Context, id is Id, rise is number, label is string)
{
    const mm = millimeter;
    const outerW = 88;
    const cornerR = 18;
    const wall = 2.5;
    const depth = 30;
    const length = 50;
    const halfC = outerW / 2; // vertical walls at ±44
    const R = crownRadiusTangent(halfC, rise, cornerR);
    const cy = rise - R;
    const fx = halfC - cornerR; // |fillet centre x| = 26
    const fy = -cornerR;

    // Crown–fillet tangency (left): C + (R / |F-C|) * (F - C)
    const Fx = -fx;
    const F = vector(Fx, fy);
    const C = vector(0, cy);
    const FC = F - C;
    const tp = C + (R / norm(FC)) * FC;
    const tpR = vector(-tp[0], tp[1]);

    const aWallL = PI;
    const aCrownL = atan2(tp[1] - fy, tp[0] - Fx);
    const aMidL = 0.5 * (aWallL + aCrownL);
    const aCrownR = atan2(tpR[1] - fy, tpR[0] - fx);
    const aMidR = 0.5 * (aCrownR + 0);

    const pl = plane(vector(0, 0, 0) * mm, vector(0, 0, 1), vector(1, 0, 0));

    // Outer: vertical wall → R18 → crown → R18 → wall (G1 throughout)
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : pl });
    skLineSegment(sk, "ol", {
                "start" : vector(-halfC, -depth) * mm,
                "end" : vector(-halfC, -cornerR) * mm });
    skArc(sk, "ofl", {
                "start" : vector(-halfC, -cornerR) * mm,
                "mid" : vector(Fx + cornerR * cos(aMidL), fy + cornerR * sin(aMidL)) * mm,
                "end" : tp * mm });
    skArc(sk, "oa", {
                "start" : tp * mm,
                "mid" : vector(0, rise) * mm,
                "end" : tpR * mm });
    skArc(sk, "ofr", {
                "start" : tpR * mm,
                "mid" : vector(fx + cornerR * cos(aMidR), fy + cornerR * sin(aMidR)) * mm,
                "end" : vector(halfC, -cornerR) * mm });
    skLineSegment(sk, "or", {
                "start" : vector(halfC, -cornerR) * mm,
                "end" : vector(halfC, -depth) * mm });
    skLineSegment(sk, "of", {
                "start" : vector(halfC, -depth) * mm,
                "end" : vector(-halfC, -depth) * mm });
    skSolve(sk);
    opExtrude(context, id + "ex", {
                "entities" : qSketchRegion(id + "sk"),
                "direction" : vector(0, 0, 1),
                "endBound" : BoundingType.BLIND,
                "endDepth" : length * mm });

    // Inner void: concentric / inset by wall
    const Ri = R - wall;
    const rCi = cornerR - wall;
    const halfCi = halfC - wall;
    const fxi = halfCi - rCi;
    const fyi = -rCi;
    const cyi = cy;
    const Fi = vector(-fxi, fyi);
    const Ci = vector(0, cyi);
    const FCi = Fi - Ci;
    const tpi = Ci + (Ri / norm(FCi)) * FCi;
    const tpiR = vector(-tpi[0], tpi[1]);
    const aMidLi = 0.5 * (PI + atan2(tpi[1] - fyi, tpi[0] + fxi));
    const aMidRi = 0.5 * (atan2(tpiR[1] - fyi, tpiR[0] - fxi) + 0);

    const ski = newSketchOnPlane(context, id + "ski", { "sketchPlane" : pl });
    skLineSegment(ski, "il", {
                "start" : vector(-halfCi, -depth + wall) * mm,
                "end" : vector(-halfCi, -rCi) * mm });
    skArc(ski, "ifl", {
                "start" : vector(-halfCi, -rCi) * mm,
                "mid" : vector(-fxi + rCi * cos(aMidLi), fyi + rCi * sin(aMidLi)) * mm,
                "end" : tpi * mm });
    skArc(ski, "ia", {
                "start" : tpi * mm,
                "mid" : vector(0, cyi + Ri) * mm,
                "end" : tpiR * mm });
    skArc(ski, "ifr", {
                "start" : tpiR * mm,
                "mid" : vector(fxi + rCi * cos(aMidRi), fyi + rCi * sin(aMidRi)) * mm,
                "end" : vector(halfCi, -rCi) * mm });
    skLineSegment(ski, "ir", {
                "start" : vector(halfCi, -rCi) * mm,
                "end" : vector(halfCi, -depth + wall) * mm });
    skLineSegment(ski, "iff", {
                "start" : vector(halfCi, -depth + wall) * mm,
                "end" : vector(-halfCi, -depth + wall) * mm });
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
                "value" : "Brace coupon " ~ label ~ " rise " ~ toString(rise) ~ " mm R18" });
}

annotation { "Feature Type Name" : "Brace arch coupons A1-A4 (88 mm, R18 G1)" }
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
        println("brace coupons: outerW=88 vertical walls cornerR=18 wall=2.5 length=50 depth=30 rises=" ~ toString(rises));
        println("print STLs offline: python featurescript/brace_arch_coupons.py (zero Onshape API calls)");
    }, { "buildAll" : true, "variant" : 1 });
