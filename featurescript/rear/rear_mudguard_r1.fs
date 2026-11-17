FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Rear mudguard r2.1, Amflow PR Carbon, layout 1c.
// r2.1: stay tabs are 3 mm plates with a web to B's wall behind A, grooves cut 0.5 through
// A's inner face, shoulder is an open perimeter lip, and every added solid stops >= 0.3 short
// of or runs >= 0.5 past the face it meets (no shared wall-bottom planes, no 2-section chords).
// braceGap drives the radius. Measured brace-to-tyre gap is 30 mm, so the
// brace underside is at R 380 and the lap plate outer is 380 - 1.5 = 378.5.
// Origin is the rear axle, +X forward, +Z up, +Y rider's left.
// phi is from +X toward +Z (90 deg at TDC). s is arc length on the outer top.
// Offline check: featurescript/rear/rear_r1_model.py
// Expected: REAR-R2 parts=2 volA=43.60 volB=102.31 minTyreGap=15.29 neckW=52.00
// bedBbest=243.7x243.5 slotAlignErr=0 tailS=330 degToTDC=6.45 outerR=378.5

function smooth01(u is number) returns number
{
    if (u <= 0) return 0;
    if (u >= 1) return 1;
    return u * u * (3 - 2 * u);
}

function blend(s is number, s0 is number, s1 is number, v0 is number, v1 is number) returns number
{
    if (s <= s0) return v0;
    if (s >= s1) return v1;
    return v0 + (v1 - v0) * smooth01((s - s0) / (s1 - s0));
}

// def holds plain mm / radian numbers
function sectionAt(def is map, s is number) returns map
{
    const h = def.depth;
    if (s < -55)
        return { "a" : def.wideA, "r" : def.wideR, "h" : h };
    if (s < -20)
        return { "a" : blend(s, -55, -20, def.wideA, def.neckA), "r" : blend(s, -55, -20, def.wideR, def.neckR), "h" : h };
    if (s <= 40)
        return { "a" : def.neckA, "r" : def.neckR, "h" : h };
    if (s < 85)
        return { "a" : blend(s, 40, 85, def.neckA, def.wideA), "r" : blend(s, 40, 85, def.neckR, def.wideR), "h" : h };
    return { "a" : def.wideA, "r" : def.wideR, "h" : h };
}

function arcFrame(def is map, s is number) returns map
{
    const phi = (def.phi0 + s / def.R) * radian;
    const c = cos(phi);
    const sn = sin(phi);
    const radial = vector(c, 0, sn);
    const tang = vector(-sn, 0, c);
    const origin = radial * (def.R * millimeter);
    return { "radial" : radial, "tang" : tang, "origin" : origin,
             "plane" : plane(origin, tang, vector(0, 1, 0)), "phi" : phi };
}

// Closed U in the arc plane. Sketch x = +Y, sketch y = depth inward.
// q is the inward inset (0 = outer skin). bottom is the depth of the closing edge.
function uSketch(context is Context, sid is Id, pl is Plane, a is number, rad is number, q is number, bottom is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const mm = millimeter;
    if (q >= rad - 0.001)
    {
        const x = a - q;
        skLineSegment(sk, "t", { "start" : vector(-x, q) * mm, "end" : vector(x, q) * mm });
        skLineSegment(sk, "r", { "start" : vector(x, q) * mm, "end" : vector(x, bottom) * mm });
        skLineSegment(sk, "b", { "start" : vector(x, bottom) * mm, "end" : vector(-x, bottom) * mm });
        skLineSegment(sk, "l", { "start" : vector(-x, bottom) * mm, "end" : vector(-x, q) * mm });
    }
    else
    {
        const c = a - rad;
        const ri = rad - q;
        const side = a - q;
        const c45 = cos(45 * degree);
        const s45 = sin(45 * degree);
        skLineSegment(sk, "t", { "start" : vector(-c, q) * mm, "end" : vector(c, q) * mm });
        skArc(sk, "ar", { "start" : vector(c, q) * mm, "mid" : vector(c + ri * c45, rad - ri * s45) * mm, "end" : vector(side, rad) * mm });
        skLineSegment(sk, "wr", { "start" : vector(side, rad) * mm, "end" : vector(side, bottom) * mm });
        skLineSegment(sk, "b", { "start" : vector(side, bottom) * mm, "end" : vector(-side, bottom) * mm });
        skLineSegment(sk, "wl", { "start" : vector(-side, bottom) * mm, "end" : vector(-side, rad) * mm });
        skArc(sk, "al", { "start" : vector(-side, rad) * mm, "mid" : vector(-c - ri * c45, rad + ri * s45) * mm, "end" : vector(-c, q) * mm });
    }
    skSolve(sk);
}

// rows: [s, q, bottom]
function loftProfiles(context is Context, id is Id, def is map, rows is array) returns Query
{
    var profs = [];
    for (var i = 0; i < size(rows); i += 1)
    {
        const row = rows[i];
        const sec = sectionAt(def, row[0]);
        const fr = arcFrame(def, row[0]);
        uSketch(context, id + ("sk" ~ i), fr.plane, sec.a, sec.r, row[1], row[2]);
        profs = append(profs, qSketchRegion(id + ("sk" ~ i)));
    }
    opLoft(context, id + "loft", { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    const bodies = qCreatedBy(id + "loft", EntityType.BODY);
    if (size(evaluateQuery(context, bodies)) == 0)
        throw regenError("rear r1 loft failed " ~ toString(id));
    return bodies;
}

function worldPt(def is map, s is number, y is number, depth is number) returns Vector
{
    const fr = arcFrame(def, s);
    return fr.origin + vector(0, y, 0) * millimeter - fr.radial * (depth * millimeter);
}

function bodyAt(context is Context, p is Vector) returns Query
{
    return qContainsPoint(qAllModifiableSolidBodies(), p);
}

function subtractTool(context is Context, id is Id, target is Query, tool is Query)
{
    opBoolean(context, id, { "targets" : target, "tools" : tool, "operationType" : BooleanOperationType.SUBTRACTION });
}

function unionInto(context is Context, id is Id, keep is Query, add is Query)
{
    opBoolean(context, id, { "tools" : qUnion([keep, add]), "operationType" : BooleanOperationType.UNION });
}

function stayGap(d is number) returns number
{
    if (d <= 0) return 60;
    if (d <= 50) return 60 + 30 * d / 50;
    if (d <= 100) return 90 + 10 * (d - 50) / 50;
    return 100;
}

function slotSketch(context is Context, sid is Id, pl is Plane, cx is number, cy is number, lenX is number, lenY is number, rad is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const mm = millimeter;
    const hx = lenX / 2 - rad;
    const hy = lenY / 2 - rad;
    const c45 = 0.70710678118;
    // CCW from the top edge. cy is the sketch-y centre.
    const x0 = cx - hx;
    const x1 = cx + hx;
    const y0 = cy - hy;
    const y1 = cy + hy;
    skLineSegment(sk, "t", { "start" : vector(x0, y1 + rad) * mm, "end" : vector(x1, y1 + rad) * mm });
    skArc(sk, "tr", { "start" : vector(x1, y1 + rad) * mm, "mid" : vector(x1 + rad * c45, y1 + rad * c45) * mm, "end" : vector(x1 + rad, y1) * mm });
    skLineSegment(sk, "r", { "start" : vector(x1 + rad, y1) * mm, "end" : vector(x1 + rad, y0) * mm });
    skArc(sk, "br", { "start" : vector(x1 + rad, y0) * mm, "mid" : vector(x1 + rad * c45, y0 - rad * c45) * mm, "end" : vector(x1, y0 - rad) * mm });
    skLineSegment(sk, "b", { "start" : vector(x1, y0 - rad) * mm, "end" : vector(x0, y0 - rad) * mm });
    skArc(sk, "bl", { "start" : vector(x0, y0 - rad) * mm, "mid" : vector(x0 - rad * c45, y0 - rad * c45) * mm, "end" : vector(x0 - rad, y0) * mm });
    skLineSegment(sk, "l", { "start" : vector(x0 - rad, y0) * mm, "end" : vector(x0 - rad, y1) * mm });
    skArc(sk, "tl", { "start" : vector(x0 - rad, y1) * mm, "mid" : vector(x0 - rad * c45, y1 + rad * c45) * mm, "end" : vector(x0, y1 + rad) * mm });
    skSolve(sk);
}

function rectSketch(context is Context, sid is Id, pl is Plane, x0 is number, y0 is number, x1 is number, y1 is number)
{
    const sk = newSketchOnPlane(context, sid, { "sketchPlane" : pl });
    const mm = millimeter;
    skLineSegment(sk, "a", { "start" : vector(x0, y0) * mm, "end" : vector(x1, y0) * mm });
    skLineSegment(sk, "b", { "start" : vector(x1, y0) * mm, "end" : vector(x1, y1) * mm });
    skLineSegment(sk, "c", { "start" : vector(x1, y1) * mm, "end" : vector(x0, y1) * mm });
    skLineSegment(sk, "d", { "start" : vector(x0, y1) * mm, "end" : vector(x0, y0) * mm });
    skSolve(sk);
}

function extrudeRegion(context is Context, id is Id, sid is Id, dir is Vector, depth is number) returns Query
{
    opExtrude(context, id, { "entities" : qSketchRegion(sid), "direction" : dir,
                "endBound" : BoundingType.BLIND, "endDepth" : depth * millimeter });
    opDeleteBodies(context, id + "delsk", { "entities" : qCreatedBy(sid, EntityType.BODY) });
    return qCreatedBy(id, EntityType.BODY);
}

function rowsOf(sList is array, q is number, bottom is number) returns array
{
    var rows = [];
    for (var s in sList)
        rows = append(rows, [s, q, bottom]);
    return rows;
}

function rowsWallA(def is map, sList is array, bottomExtra is number) returns array
{
    var rows = [];
    for (var s in sList)
    {
        const sec = sectionAt(def, s);
        // r2.1: smooth 2.5 -> 3.0 over s -37..-20 instead of a step between two stations
        const q = def.wall + (def.lapWall - def.wall) * smooth01((s + 37) / 17);
        rows = append(rows, [s, q, sec.h + bottomExtra]);
    }
    return rows;
}

function numDef(definition is map) returns map
{
    return {
        "R" : (definition.tyreRadius + definition.braceGap - definition.padThk) / millimeter,
        "wall" : definition.wall / millimeter,
        "lapWall" : definition.lapWall / millimeter,
        "depth" : definition.depth / millimeter,
        "neckA" : definition.neckWidth / millimeter / 2,
        "wideA" : definition.wideWidth / millimeter / 2,
        "neckR" : definition.neckCorner / millimeter,
        "wideR" : definition.wideCorner / millimeter,
        "phi0" : definition.phiBrace / radian,
        "lapClear" : definition.lapClear / millimeter,
        "sFront" : definition.sFront / millimeter,
        "sJoin" : definition.sJoin / millimeter,
        "sTail" : definition.sTail / millimeter,
        "pad" : definition.padThk / millimeter
    };
}

// r2.1: loft stations. 10 mm where the section is constant, 2.5 mm through the smoothstep blends,
// so the spline through the sections follows sectionAt (offline natural-spline check: <= 0.1 mm).
function skirtStations(sF is number) returns array
{
    var ss = [];
    var s = sF;
    while (s < -55 - 0.01)
    {
        ss = append(ss, s);
        s += 10;
    }
    for (var k = 0; k <= 14; k += 1)
        ss = append(ss, -55 + 2.5 * k);
    return ss;
}

function tailStations(sT is number) returns array
{
    var ss = [];
    for (var k = 0; k <= 18; k += 1)
        ss = append(ss, 40 + 2.5 * k);
    var s = 95;
    while (s < sT - 2.5)
    {
        ss = append(ss, s);
        s += 10;
    }
    ss = append(ss, sT);
    return ss;
}

function buildRear(context is Context, id is Id, definition is map)
{
    const def = numDef(definition);
    const R = def.R;
    const h = def.depth;
    const sF = def.sFront;
    const sJ = def.sJoin;
    const sT = def.sTail;
    const qTongue = def.lapWall + def.lapClear;          // 3.2
    const qTongueIn = qTongue + def.lapWall;             // 6.2
    const pA = worldPt(def, -80, 0, 1);
    const pB = worldPt(def, 150, 0, 1);

    // ----- A shell, sFront .. sJoin -----
    // r2.1: two lofts that meet on the identical s=-20 section. The lap loft has a constant
    // section, so its walls stay exactly at |y| 26 / 23 (a single loft through the blend stations
    // bowed them by up to 0.35 mm). The skirt loft uses 2.5 mm stations through the -55..-20 blend.
    const sSk = skirtStations(sF);
    const aSkSolid = loftProfiles(context, id + "aOut", def, rowsOf(sSk, 0, h));
    const aSkHole = loftProfiles(context, id + "aHole", def, rowsWallA(def, sSk, 1));
    subtractTool(context, id + "aSub", aSkSolid, aSkHole);
    const sLap = [-20, -10, 0, 10, sJ];
    const aLpSolid = loftProfiles(context, id + "aOutL", def, rowsOf(sLap, 0, h));
    const aLpHole = loftProfiles(context, id + "aHoleL", def, rowsOf(sLap, def.lapWall, h + 1));
    subtractTool(context, id + "aSubL", aLpSolid, aLpHole);
    unionInto(context, id + "aU", aSkSolid, aLpSolid);
    // r2.1 shoulder: perimeter lip on the crown and walls only, s -23..-20, channel open.
    // Outer inset 1.2 (1.3+ into the A wall), inner inset 6.2 (flush with the tongue inner face).
    // The side legs stop 0.5 above the wall bottom; the hole runs 1 mm past it.
    const shS = [-23, -21.5, -20];
    var shRows = [];
    var shHoleRows = [];
    for (var s in shS)
    {
        const sec = sectionAt(def, s);
        shRows = append(shRows, [s, 1.2, sec.h - 0.5]);
        shHoleRows = append(shHoleRows, [s, qTongueIn, sec.h + 1]);
    }
    const shoulder = loftProfiles(context, id + "sh", def, shRows);
    const shHole = loftProfiles(context, id + "shHole", def, shHoleRows);
    subtractTool(context, id + "shSub", shoulder, shHole);
    unionInto(context, id + "shU", bodyAt(context, pA), shoulder);

    // ----- B shell, bridge, tongue -----
    // r2.1: constant neck loft sJoin..40, then the blend and tail on 2.5 / 10 mm stations,
    // meeting on the identical s=40 section.
    const sBn = [sJ, 30, 40];
    const bnSolid = loftProfiles(context, id + "bOutN", def, rowsOf(sBn, 0, h));
    const bnHole = loftProfiles(context, id + "bHoleN", def, rowsOf(sBn, def.wall, h + 1));
    subtractTool(context, id + "bSubN", bnSolid, bnHole);
    const sB = tailStations(sT);
    const bSolid = loftProfiles(context, id + "bOut", def, rowsOf(sB, 0, h));
    const bHole = loftProfiles(context, id + "bHole", def, rowsOf(sB, def.wall, h + 1));
    subtractTool(context, id + "bSub", bSolid, bHole);
    unionInto(context, id + "bU0", bnSolid, bSolid);

    const sTg = [-20, -10, 0, 10, sJ + 1.5];
    const tgSolid = loftProfiles(context, id + "tgOut", def, rowsOf(sTg, qTongue, h));
    const tgHole = loftProfiles(context, id + "tgHole", def, rowsOf(sTg, qTongueIn, h + 1));
    subtractTool(context, id + "tgSub", tgSolid, tgHole);

    // r2.1 bridge sJoin .. sJoin+6 joins the tongue (depth 3.2..6.2) to the 2.5 mm shell.
    // It is an inset ring: outer inset 1.5 (1 mm into B's wall, no chord against B's outer skin),
    // legs stop 0.5 above the wall bottom, three stations.
    const sBr = [sJ, sJ + 3, sJ + 6];
    const brSolid = loftProfiles(context, id + "brOut", def, rowsOf(sBr, 1.5, h - 0.5));
    const brHole = loftProfiles(context, id + "brHole", def, rowsOf(sBr, qTongueIn + 0.4, h + 1));
    subtractTool(context, id + "brSub", brSolid, brHole);

    // bridge first (it overlaps the shell), then the tongue (it overlaps the bridge)
    unionInto(context, id + "bU2", bodyAt(context, pB), brSolid);
    unionInto(context, id + "bU1", bodyAt(context, pB), tgSolid);

    // ----- grooves in A, rails on B -----
    subtractTool(context, id + "gvC", bodyAt(context, pA), addGrooves(context, id + "gv", def));
    unionInto(context, id + "railU", bodyAt(context, pB), addRails(context, id + "rail", def, qTongue));

    // ----- brace slots through both layers, recess, bosses -----
    cutBraceSlots(context, id + "slA", def, bodyAt(context, pA), 2.7);
    cutBraceSlots(context, id + "slB", def, bodyAt(context, pB), 2.4);
    subtractTool(context, id + "recC", bodyAt(context, pB), addRecess(context, id + "rec", def, qTongueIn));
    unionInto(context, id + "bossU", bodyAt(context, pA), addBraceBosses(context, id + "boss", def));

    // ----- ribs, trim guides, lips -----
    unionInto(context, id + "ribAU", bodyAt(context, pA), addRibRun(context, id + "ribA", def, [[sF, -125], [-115, -105], [-95, -85], [-75, -65], [-55, -15], [15, sJ]]));
    unionInto(context, id + "ribBU", bodyAt(context, pB), addRibRun(context, id + "ribB", def, [[sJ + 6, 80], [80, 140], [140, 200], [200, 260], [260, sT - 5]]));
    unionInto(context, id + "trimU", bodyAt(context, pA), addTrims(context, id + "trim", def));
    unionInto(context, id + "lipAU", bodyAt(context, pA), addEndLip(context, id + "lipA", def, sF, 1.8, -1));
    unionInto(context, id + "lipBU", bodyAt(context, pB), addEndLip(context, id + "lipB", def, sT, 2.0, 1));
    unionInto(context, id + "hemU", bodyAt(context, pA), addHem(context, id + "hem", def));

    // ----- squared tail, R5 corners, stay tabs -----
    cutTailCorners(context, id + "r5", def);
    unionInto(context, id + "tabU", bodyAt(context, pB), addTabs(context, id + "tab", def));
    cutTabSlots(context, id + "tsl", def);

    const qA = bodyAt(context, pA);
    const qB = bodyAt(context, pB);
    setProperty(context, { "entities" : qA, "propertyType" : PropertyType.NAME, "value" : "Rear Guard A" });
    setProperty(context, { "entities" : qB, "propertyType" : PropertyType.NAME, "value" : "Rear Guard B" });
}

function addGrooves(context is Context, id is Id, def is map) returns Query
{
    // 3.6 x 1.5, open through A's rear face. Two rails' worth, lofted along the lap.
    var profs = [];
    // r2.1: s -20.5 (0.5 into the shoulder lip) to 20.6 (0.6 past A's end), depth 1.5..3.5 (0.5 through A's inner face)
    const ss = [-20.5, -10, 0, 10, 20.6];
    for (var i = 0; i < size(ss); i += 1)
    {
        const fr = arcFrame(def, ss[i]);
        const sid = id + ("sk" ~ i);
        const sk = newSketchOnPlane(context, sid, { "sketchPlane" : fr.plane });
        const mm = millimeter;
        // two rectangles, one loop each is not a single region if drawn together without being separate.
        // draw one rectangle per groove by making a single profile per side via two sketches stacked in the loft pairs.
        skLineSegment(sk, "a", { "start" : vector(20 - 1.8, 1.5) * mm, "end" : vector(20 + 1.8, 1.5) * mm });
        skLineSegment(sk, "b", { "start" : vector(20 + 1.8, 1.5) * mm, "end" : vector(20 + 1.8, 3.5) * mm });
        skLineSegment(sk, "c", { "start" : vector(20 + 1.8, 3.5) * mm, "end" : vector(20 - 1.8, 3.5) * mm });
        skLineSegment(sk, "d", { "start" : vector(20 - 1.8, 3.5) * mm, "end" : vector(20 - 1.8, 1.5) * mm });
        skSolve(sk);
        profs = append(profs, qSketchRegion(sid));
    }
    opLoft(context, id + "loft", { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    var profs2 = [];
    for (var j = 0; j < size(ss); j += 1)
    {
        const fr2 = arcFrame(def, ss[j]);
        const sid2 = id + ("skn" ~ j);
        rectSketch(context, sid2, fr2.plane, -20 - 1.8, 1.5, -20 + 1.8, 3.5);
        profs2 = append(profs2, qSketchRegion(sid2));
    }
    opLoft(context, id + "loft2", { "profileSubqueries" : profs2, "bodyType" : ToolBodyType.SOLID });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft", EntityType.BODY), qCreatedBy(id + "loft2", EntityType.BODY)]);
}

function addRails(context is Context, id is Id, def is map, qTop is number) returns Query
{
    // 3.0 wide, 1.2 proud of the tongue outer, 0.5 embedded, 4 mm lead-in
    const ss = [-20, -16, 0, 21];
    const heights = [0.2, 1.2, 1.2, 1.2];
    for (var side = 0; side < 2; side += 1)
    {
        const y = (side == 0) ? 20 : -20;
        var profs = [];
        for (var i = 0; i < size(ss); i += 1)
        {
            const fr = arcFrame(def, ss[i]);
            const sid = id + ("s" ~ side ~ "k" ~ i);
            const top = qTop - heights[i];
            const bot = qTop + 0.5;
            rectSketch(context, sid, fr.plane, y - 1.5, top, y + 1.5, bot);
            profs = append(profs, qSketchRegion(sid));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function cutBraceSlots(context is Context, id is Id, def is map, target is Query, short is number)
{
    // 6.0 along Y, `short` along s, R0.8 corners. Through both layers: the cutter starts outside and runs inward.
    var tools = [];
    var n = 0;
    for (var s in [14, -14])
    {
        for (var y in [12, -12])
        {
            const fr = arcFrame(def, s);
            // plane normal to the radius, so the sketch is the (s, Y) plane of the surface
            const origin = fr.radial * ((def.R + 2) * millimeter);
            const pl = plane(origin, fr.radial, fr.tang);
            // sketch y = radial x tang = -world Y, so a slot at +world y is negative sketch y
            slotSketch(context, id + ("sk" ~ n), pl, 0, -y, short, 6.0, 0.8);
            const tool = extrudeRegion(context, id + ("ex" ~ n), id + ("sk" ~ n), -fr.radial, 16);
            tools = append(tools, tool);
            n += 1;
        }
    }
    opBoolean(context, id + "cut", { "targets" : target, "tools" : qUnion(tools), "operationType" : BooleanOperationType.SUBTRACTION });
}

function addRecess(context is Context, id is Id, def is map, qInner is number) returns Query
{
    // r2.1: 6.6 wide (0.3 past each slot side) x 1.3 deep on the tongue underside, cut 0.5 past
    // the inner face, s -15.7..15.7 so it runs 0.5 past the far edge of each B slot
    const y0 = qInner - 1.3;
    const ss = [-15.7, 0, 15.7];
    for (var side = 0; side < 2; side += 1)
    {
        const y = (side == 0) ? 12 : -12;
        var profs = [];
        for (var i = 0; i < size(ss); i += 1)
        {
            const fr = arcFrame(def, ss[i]);
            rectSketch(context, id + ("sk" ~ side ~ i), fr.plane, y - 3.3, y0, y + 3.3, qInner + 0.5);
            profs = append(profs, qSketchRegion(id + ("sk" ~ side ~ i)));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function addBraceBosses(context is Context, id is Id, def is map) returns Query
{
    // 1 mm proud on the +s side of each brace slot, on A's outer skin.
    // r2.1: start 0.5 past the slot edge, 3 mm long, so the s=14 pair ends at 18.85 (A ends at 20).
    var tools = [];
    var n = 0;
    for (var s in [14, -14])
    {
        const sBoss = s + 1.35 + 0.5;
        const fr = arcFrame(def, sBoss);
        for (var y in [12, -12])
        {
            rectSketch(context, id + ("sk" ~ n), fr.plane, y - 4, -1.0, y + 4, 0.5);
            tools = append(tools, extrudeRegion(context, id + ("ex" ~ n), id + ("sk" ~ n), fr.tang, 3));
            n += 1;
        }
    }
    return qUnion(tools);
}

function addRibRun(context is Context, id is Id, def is map, spans is array) returns Query
{
    var made = [];
    var n = 0;
    for (var span in spans)
    {
        for (var side = 0; side < 2; side += 1)
        {
            const y = (side == 0) ? 22 : -22;
            var profs = [];
            const s0 = span[0];
            const s1 = span[1];
            const mid = (s0 + s1) / 2;
            var k = 0;
            for (var s in [s0, mid, s1])
            {
                const fr = arcFrame(def, s);
                // 3 mm wide, 3 mm proud, 1.0 mm embedded (>= 0.7 even where y=23.5 crosses the neck R4 corner)
                rectSketch(context, id + ("sk" ~ n ~ k), fr.plane, y - 1.5, -3, y + 1.5, 1.0);
                profs = append(profs, qSketchRegion(id + ("sk" ~ n ~ k)));
                k += 1;
            }
            opLoft(context, id + ("loft" ~ n), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
            made = append(made, qCreatedBy(id + ("loft" ~ n), EntityType.BODY));
            n += 1;
        }
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion(made);
}

function addTrims(context is Context, id is Id, def is map) returns Query
{
    // raised 0.6 (along s) x 0.4 guides at s = -120, -100, -80, -60
    var tools = [];
    var n = 0;
    for (var s in [-120, -100, -80, -60])
    {
        const fr = arcFrame(def, s - 0.3);
        rectSketch(context, id + ("sk" ~ n), fr.plane, -18, -0.4, 18, 0.5); // r2.1: 0.5 embed (was 0.15)
        tools = append(tools, extrudeRegion(context, id + ("ex" ~ n), id + ("sk" ~ n), fr.tang, 0.6));
        n += 1;
    }
    return qUnion(tools);
}

function addEndLip(context is Context, id is Id, def is map, s is number, lip is number, sign is number) returns Query
{
    const fr = arcFrame(def, s);
    const sec = sectionAt(def, s);
    const half = sec.a - sec.r - 1;
    // start just inside the end and extrude out past it
    // r2.1: 0.5 back into the shell along s and 0.5 radially into the crown (was 0.3 / 0.3)
    const origin = fr.origin - fr.tang * (sign * 0.5 * millimeter);
    const pl = plane(origin, fr.tang, vector(0, 1, 0));
    rectSketch(context, id + "sk", pl, -half, -lip, half, 0.5);
    return extrudeRegion(context, id + "ex", id + "sk", fr.tang * sign, lip + 0.5);
}

function addHem(context is Context, id is Id, def is map) returns Query
{
    // 1.8 mm lip along both lower edges of the skirt. r2.1: runs 0.5 below the wall bottom so it
    // does not share the wall-bottom face, and uses the shell's own stations.
    const ss = skirtStations(def.sFront); // the shell's own stations, so the hem follows the same spline
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        var profs = [];
        for (var i = 0; i < size(ss); i += 1)
        {
            const sec = sectionAt(def, ss[i]);
            const fr = arcFrame(def, ss[i]);
            const xIn = sg * (sec.a - 1.8); // r2.1: 0.6 off the shoulder lip's outer leg face (a-1.2)
            const xLip = sg * (sec.a + 0.6);
            const x0 = min(xIn, xLip);
            const x1 = max(xIn, xLip);
            rectSketch(context, id + ("sk" ~ side ~ i), fr.plane, x0, sec.h - 1.8, x1, sec.h + 0.5);
            profs = append(profs, qSketchRegion(id + ("sk" ~ side ~ i)));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function cutTailCorners(context is Context, id is Id, def is map)
{
    // R5 in plan where the squared end meets each side wall.
    // r2.1: the sketch plane is tangent at the end itself, so the tool's x=0 face is exactly the
    // end plane; the offcut runs 1 mm past the end and 1 mm past the side wall.
    const sEnd = def.sTail;
    const sec = sectionAt(def, sEnd);
    const a = sec.a;
    const fr = arcFrame(def, sEnd);
    const origin = fr.radial * ((def.R + 6) * millimeter);
    const pl = plane(origin, fr.radial, fr.tang);
    // sketch x along +s, sketch y = radial x tang = -world Y
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        const sk = newSketchOnPlane(context, id + ("sk" ~ side), { "sketchPlane" : pl });
        const mm = millimeter;
        const c45 = 0.70710678118;
        const cx = -5;
        const cy = -sg * (a - 5);
        const wallY = -sg * a;
        const outY = -sg * (a + 1);
        const arcStart = vector(cx, wallY);
        const arcEnd = vector(0, cy);
        skArc(sk, "a", { "start" : arcStart * mm, "mid" : vector(cx + 5 * c45, cy - sg * 5 * c45) * mm, "end" : arcEnd * mm });
        skLineSegment(sk, "e1", { "start" : arcEnd * mm, "end" : vector(1, cy) * mm });
        skLineSegment(sk, "e2", { "start" : vector(1, cy) * mm, "end" : vector(1, outY) * mm });
        skLineSegment(sk, "e3", { "start" : vector(1, outY) * mm, "end" : vector(cx, outY) * mm });
        skLineSegment(sk, "e4", { "start" : vector(cx, outY) * mm, "end" : arcStart * mm });
        skSolve(sk);
        const tool = extrudeRegion(context, id + ("ex" ~ side), id + ("sk" ~ side), -fr.radial, def.depth + 14);
        subtractTool(context, id + ("cut" ~ side), bodyAt(context, worldPt(def, 150, 0, 1)), tool);
    }
}

function addTabs(context is Context, id is Id, def is map) returns Query
{
    // r2.1: a 3 mm plate per stay, |y| yo-3..yo, d 8..27.5, x -16..+30 along the brace tangent.
    // The plate clears A's lap wall (|y| 26) by >= 0.4 mm. A web at x 21..30 (behind A, which ends
    // at s 20) joins it to B's neck wall: |y| 25 (1 mm into the wall) to 0.5 into the plate, d 8..18
    // (the wall bottom is at d 18.5 there, and the straight frame sits ~1.2 mm higher at x 30).
    const phi = def.phi0 * radian;
    const radial = vector(cos(phi), 0, sin(phi));
    const tang = vector(-sin(phi), 0, cos(phi));
    var made = [];
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        // plate: yo is linear in d, so two stations make it exact
        var profs = [];
        var k = 0;
        for (var d in [8, 27.5])
        {
            const yo = stayGap(d) / 2 - 3;
            const origin = radial * ((def.R + def.pad - d) * millimeter);
            // plane normal is radial; sketch x = tang, sketch y = radial x tang = -Y
            const pl = plane(origin, radial, tang);
            rectSketch(context, id + ("pk" ~ side ~ k), pl, -16, min(-sg * yo, -sg * (yo - 3)), 30, max(-sg * yo, -sg * (yo - 3)));
            profs = append(profs, qSketchRegion(id + ("pk" ~ side ~ k)));
            k += 1;
        }
        opLoft(context, id + ("plate" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
        made = append(made, qCreatedBy(id + ("plate" ~ side), EntityType.BODY));
        var wprofs = [];
        k = 0;
        for (var d in [8, 18])
        {
            const yIn = stayGap(d) / 2 - 3 - 3 + 0.5;
            const origin = radial * ((def.R + def.pad - d) * millimeter);
            const pl = plane(origin, radial, tang);
            rectSketch(context, id + ("wk" ~ side ~ k), pl, 21, min(-sg * 25, -sg * yIn), 30, max(-sg * 25, -sg * yIn));
            wprofs = append(wprofs, qSketchRegion(id + ("wk" ~ side ~ k)));
            k += 1;
        }
        opLoft(context, id + ("web" ~ side), { "profileSubqueries" : wprofs, "bodyType" : ToolBodyType.SOLID });
        made = append(made, qCreatedBy(id + ("web" ~ side), EntityType.BODY));
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion(made);
}

function cutTabSlots(context is Context, id is Id, def is map)
{
    // one 6.0 (along the stay) x 2.4 slot per station, both stays.
    // r2.1: stations d=13.5 and d=22.5 (spans 10.5..16.5 and 19.5..25.5). r2 had d=20 and 24, which overlapped.
    const phi = def.phi0 * radian;
    const radial = vector(cos(phi), 0, sin(phi));
    const tang = vector(-sin(phi), 0, cos(phi));
    var n = 0;
    for (var d in [13.5, 22.5])
    {
        for (var sg in [1, -1])
        {
            const yo = stayGap(d) / 2 - 3;
            const origin = radial * ((def.R + def.pad - d) * millimeter) + vector(0, sg * (yo - 4), 0) * millimeter; // r2.1: starts 1 mm inboard of the plate, 1 mm outside A's wall
            // extrude along +Y * sg, sketch in the plane normal to Y
            const pl = plane(origin, vector(0, sg, 0), tang);
            // sketch x along tang (s), sketch y = Y x tang = radial, so +sketch y is outward
            slotSketch(context, id + ("sk" ~ n), pl, 0, 0, 2.4, 6.0, 0.8);
            const tool = extrudeRegion(context, id + ("ex" ~ n), id + ("sk" ~ n), vector(0, sg, 0), 20);
            subtractTool(context, id + ("cut" ~ n), bodyAt(context, worldPt(def, 150, 0, 1)), tool);
            n += 1;
        }
    }
}

annotation { "Feature Type Name" : "Rear Mudguard R2 (Amflow PR)" }
export const rearMudguardR2 = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Wall" }
        isLength(definition.wall, { (millimeter) : [1.6, 2.5, 4] } as LengthBoundSpec);
        annotation { "Name" : "Lap and mount wall" }
        isLength(definition.lapWall, { (millimeter) : [2, 3, 5] } as LengthBoundSpec);
        annotation { "Name" : "Section depth" }
        isLength(definition.depth, { (millimeter) : [12, 17, 28] } as LengthBoundSpec);
        annotation { "Name" : "Neck width (outer, s -20 to +40)" }
        isLength(definition.neckWidth, { (millimeter) : [40, 52, 60] } as LengthBoundSpec);
        annotation { "Name" : "Skirt and tail width" }
        isLength(definition.wideWidth, { (millimeter) : [70, 94, 120] } as LengthBoundSpec);
        annotation { "Name" : "Neck corner radius" }
        isLength(definition.neckCorner, { (millimeter) : [2, 4, 8] } as LengthBoundSpec);
        annotation { "Name" : "Wide corner radius" }
        isLength(definition.wideCorner, { (millimeter) : [4, 12, 20] } as LengthBoundSpec);
        annotation { "Name" : "Brace angle from forward horizontal" }
        isAngle(definition.phiBrace, { (degree) : [20, 33.6, 55] } as AngleBoundSpec);
        annotation { "Name" : "Tyre radius" }
        isLength(definition.tyreRadius, { (millimeter) : [320, 350, 380] } as LengthBoundSpec);
        annotation { "Name" : "Tyre width" }
        isLength(definition.tyreWidth, { (millimeter) : [50, 63.5, 80] } as LengthBoundSpec);
        annotation { "Name" : "EVA pad on the brace" }
        isLength(definition.padThk, { (millimeter) : [0, 1.5, 4] } as LengthBoundSpec);
        annotation { "Name" : "Brace underside to tyre. Outer R = tyre + this - pad" }
        isLength(definition.braceGap, { (millimeter) : [20, 30, 50] } as LengthBoundSpec);
        annotation { "Name" : "Lap radial clearance" }
        isLength(definition.lapClear, { (millimeter) : [0, 0.2, 0.6] } as LengthBoundSpec);
        annotation { "Name" : "A front (s)" }
        isLength(definition.sFront, { (millimeter) : [-200, -135, -80] } as LengthBoundSpec);
        annotation { "Name" : "Split, A rear / B shell start (s)" }
        isLength(definition.sJoin, { (millimeter) : [10, 20, 40] } as LengthBoundSpec);
        annotation { "Name" : "B tail (s)" }
        isLength(definition.sTail, { (millimeter) : [200, 330, 400] } as LengthBoundSpec);
    }
    {
        buildRear(context, id, definition);
    }, {
        "wall" : 2.5 * millimeter, "lapWall" : 3 * millimeter, "depth" : 17 * millimeter,
        "neckWidth" : 52 * millimeter, "wideWidth" : 94 * millimeter, "neckCorner" : 4 * millimeter, "wideCorner" : 12 * millimeter,
        "phiBrace" : 33.6 * degree, "tyreRadius" : 350 * millimeter, "tyreWidth" : 63.5 * millimeter,
        "padThk" : 1.5 * millimeter, "braceGap" : 30 * millimeter, "lapClear" : 0.2 * millimeter,
        "sFront" : -135 * millimeter, "sJoin" : 20 * millimeter, "sTail" : 330 * millimeter
    });
