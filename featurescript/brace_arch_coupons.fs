FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Under-brace arch fit coupons A1–A4. Offline design; paste into Onshape only when exporting.
// No API usage required — STLs come from featurescript/brace_arch_coupons.py.
//
// Outer width 88 mm (~1 mm/side vs ~90 mm cavity at tyre-top height).
// Final-style C1: walls at ±41 (82 mm), R18 top-corner fillets (production C1),
// circular crown tangent to both fillets with the given rise at x=0.
// Wall 2.5 mm; length 50 mm; depth 30 mm.
// Rises: A1 2.5, A2 4.0, A3 5.5, A4 7.0 mm.
// Sketch: x = lateral, y = outward (crown +y). Extrude along +Z.

function crownRadiusTangent(halfChord is number, rise is number, cornerR is number) returns number
{
    // Fillet centres at (±(halfChord - cornerR), -cornerR).
    // |C - F| = R - cornerR with C = (0, rise - R) ⇒ R = (hx² + (rise+cornerR)² - cornerR²) / (2 rise)
    const hx = halfChord - cornerR;
    return (hx * hx + (rise + cornerR) * (rise + cornerR) - cornerR * cornerR) / (2 * rise);
}

function makeCoupon(context is Context, id is Id, rise is number, label is string)
{
    const mm = millimeter;
    const outerW = 88;
    const chord = 82;
    const cornerR = 18;
    const wall = 2.5;
    const depth = 30;
    const length = 50;
    const halfC = chord / 2;
    const halfO = outerW / 2;
    const R = crownRadiusTangent(halfC, rise, cornerR);
    const cy = rise - R;
    const fx = halfC - cornerR; // |fillet centre x|
    const fy = -cornerR;

    // Crown–fillet tangency point (left): C + (R / |F-C|) * (F - C)
    const Fx = -fx;
    const F = vector(Fx, fy);
    const C = vector(0, cy);
    const FC = F - C;
    const tp = C + (R / norm(FC)) * FC; // left tangent point
    const tpR = vector(-tp[0], tp[1]);

    const pl = plane(vector(0, 0, 0) * mm, vector(0, 0, 1), vector(1, 0, 0));

    // --- Outer solid U: flare → wall → R18 → crown → R18 → wall → flare ---
    const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : pl });
    const aWallL = PI;
    const aCrownL = atan2(tp[1] - fy, tp[0] - Fx);
    const aMidL = 0.5 * (aWallL + aCrownL);
    const aCrownR = atan2(tpR[1] - fy, tpR[0] - fx);
    const aWallR = 0;
    const aMidR = 0.5 * (aCrownR + aWallR);

    skLineSegment(sk, "ol", {
                "start" : vector(-halfO, -depth) * mm,
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

    // --- Inner void: offset ~wall (concentric crown + inset fillets/walls) ---
    const Ri = R - wall;
    const rCi = cornerR - wall;
    const halfCi = halfC - wall;
    const halfOi = halfO - wall;
    const fxi = halfCi - rCi;
    const fyi = -rCi;
    const cyi = cy; // same crown centre; radius shrunk
    // Recompute inner tangency with concentric crown and inset fillet.
    const Fi = vector(-fxi, fyi);
    const Ci = vector(0, cyi);
    const FCi = Fi - Ci;
    // Use |Ci-Fi| based point at radius Ri along Fi-Ci
    const tpi = Ci + (Ri / norm(FCi)) * FCi;
    const tpiR = vector(-tpi[0], tpi[1]);

    const aWallLi = PI;
    const aCrownLi = atan2(tpi[1] - fyi, tpi[0] + fxi);
    const aMidLi = 0.5 * (aWallLi + aCrownLi);
    const aCrownRi = atan2(tpiR[1] - fyi, tpiR[0] - fxi);
    const aMidRi = 0.5 * (aCrownRi + 0);

    const ski = newSketchOnPlane(context, id + "ski", { "sketchPlane" : pl });
    skLineSegment(ski, "il", {
                "start" : vector(-halfOi, -depth + wall) * mm,
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
                "end" : vector(halfOi, -depth + wall) * mm });
    skLineSegment(ski, "iff", {
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
                "value" : "Brace coupon " ~ label ~ " rise " ~ toString(rise) ~ " mm R18" });
}

annotation { "Feature Type Name" : "Brace arch coupons A1-A4 (88 mm, R18)" }
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
        println("brace coupons: outerW=88 chord=82 cornerR=18 wall=2.5 length=50 depth=30 rises=" ~ toString(rises));
        println("print STLs offline: python featurescript/brace_arch_coupons.py (zero Onshape API calls)");
    }, { "buildAll" : true, "variant" : 1 });
