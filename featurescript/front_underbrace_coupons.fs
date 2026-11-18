FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Front mudguard under-brace coupon A4 (quick verify). Offline STLs from
// featurescript/front_underbrace_coupons.py — paste only when exporting.
// Top wall-to-wall 88 mm with R18 fillets; walls continue so the floor is
// 96 mm (+4 mm each side). Rise 7.0 mm. Extrusion 20 mm. Four ID notches.

function crownRadiusTangent(halfChord is number, rise is number, cornerR is number) returns number
{
    const hx = halfChord - cornerR;
    return (hx * hx + (rise + cornerR) * (rise + cornerR) - cornerR * cornerR) / (2 * rise);
}

annotation { "Feature Type Name" : "Front under-brace A4 (88→96, 20 mm)" }
export const frontUnderbraceA4 = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        const mm = millimeter;
        const topW = 88;
        const botW = 96;
        const cornerR = 18;
        const wall = 2.5;
        const depth = 30;
        const length = 20;
        const rise = 7.0;
        const halfT = topW / 2;
        const halfB = botW / 2;
        const R = crownRadiusTangent(halfT, rise, cornerR);
        const cy = rise - R;
        const fx = halfT - cornerR;
        const fy = -cornerR;
        const Fx = -fx;
        const F = vector(Fx, fy);
        const C = vector(0, cy);
        const FC = F - C;
        const tp = C + (R / norm(FC)) * FC;
        const tpR = vector(-tp[0], tp[1]);
        const aMidL = 0.5 * (PI + atan2(tp[1] - fy, tp[0] - Fx));
        const aMidR = 0.5 * (atan2(tpR[1] - fy, tpR[0] - fx) + 0);

        const pl = plane(vector(0, 0, 0) * mm, vector(0, 0, 1), vector(1, 0, 0));
        const sk = newSketchOnPlane(context, id + "sk", { "sketchPlane" : pl });
        skLineSegment(sk, "ol", {
                    "start" : vector(-halfB, -depth) * mm,
                    "end" : vector(-halfT, -cornerR) * mm });
        skArc(sk, "ofl", {
                    "start" : vector(-halfT, -cornerR) * mm,
                    "mid" : vector(Fx + cornerR * cos(aMidL), fy + cornerR * sin(aMidL)) * mm,
                    "end" : tp * mm });
        skArc(sk, "oa", {
                    "start" : tp * mm,
                    "mid" : vector(0, rise) * mm,
                    "end" : tpR * mm });
        skArc(sk, "ofr", {
                    "start" : tpR * mm,
                    "mid" : vector(fx + cornerR * cos(aMidR), fy + cornerR * sin(aMidR)) * mm,
                    "end" : vector(halfT, -cornerR) * mm });
        skLineSegment(sk, "or", {
                    "start" : vector(halfT, -cornerR) * mm,
                    "end" : vector(halfB, -depth) * mm });
        skLineSegment(sk, "of", {
                    "start" : vector(halfB, -depth) * mm,
                    "end" : vector(-halfB, -depth) * mm });
        skSolve(sk);
        opExtrude(context, id + "ex", {
                    "entities" : qSketchRegion(id + "sk"),
                    "direction" : vector(0, 0, 1),
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length * mm });

        // Simple rectangular void for the U (good enough for paste-export).
        const Ri = R - wall;
        const rCi = cornerR - wall;
        const halfTi = halfT - wall;
        const halfBi = halfB - wall;
        const fxi = halfTi - rCi;
        const fyi = -rCi;
        const Fi = vector(-fxi, fyi);
        const Ci = vector(0, cy);
        const FCi = Fi - Ci;
        const tpi = Ci + (Ri / norm(FCi)) * FCi;
        const tpiR = vector(-tpi[0], tpi[1]);
        const aMidLi = 0.5 * (PI + atan2(tpi[1] - fyi, tpi[0] + fxi));
        const aMidRi = 0.5 * (atan2(tpiR[1] - fyi, tpiR[0] - fxi) + 0);

        const ski = newSketchOnPlane(context, id + "ski", { "sketchPlane" : pl });
        skLineSegment(ski, "il", {
                    "start" : vector(-halfBi, -depth + wall) * mm,
                    "end" : vector(-halfTi, -rCi) * mm });
        skArc(ski, "ifl", {
                    "start" : vector(-halfTi, -rCi) * mm,
                    "mid" : vector(-fxi + rCi * cos(aMidLi), fyi + rCi * sin(aMidLi)) * mm,
                    "end" : tpi * mm });
        skArc(ski, "ia", {
                    "start" : tpi * mm,
                    "mid" : vector(0, cy + Ri) * mm,
                    "end" : tpiR * mm });
        skArc(ski, "ifr", {
                    "start" : tpiR * mm,
                    "mid" : vector(fxi + rCi * cos(aMidRi), fyi + rCi * sin(aMidRi)) * mm,
                    "end" : vector(halfTi, -rCi) * mm });
        skLineSegment(ski, "ir", {
                    "start" : vector(halfTi, -rCi) * mm,
                    "end" : vector(halfBi, -depth + wall) * mm });
        skLineSegment(ski, "iff", {
                    "start" : vector(halfBi, -depth + wall) * mm,
                    "end" : vector(-halfBi, -depth + wall) * mm });
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

        var markQs = [];
        for (var k = 0; k < 4; k += 1)
        {
            const z0 = 2 + k * 4;
            fCuboid(context, id + ("nub" ~ k), {
                        "corner1" : vector(-8, -depth + wall, z0) * mm,
                        "corner2" : vector(-3, -depth + wall + 1.0, z0 + 2.5) * mm });
            markQs = append(markQs, qCreatedBy(id + ("nub" ~ k), EntityType.BODY));
        }
        opBoolean(context, id + "marks", {
                    "tools" : qUnion(append(markQs, qCreatedBy(id + "ex", EntityType.BODY))),
                    "operationType" : BooleanOperationType.UNION });

        setProperty(context, {
                    "entities" : qCreatedBy(id + "ex", EntityType.BODY),
                    "propertyType" : PropertyType.NAME,
                    "value" : "Front under-brace A4 88to96 rise 7" });
        println("front A4: topW=88 bottomW=96 rise=7 cornerR=18 length=20 four ID notches");
    }, {});
