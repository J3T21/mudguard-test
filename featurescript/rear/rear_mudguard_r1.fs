FeatureScript 3112;
import(path : "onshape/std/common.fs", version : "3112.0");

// Rear mudguard r1, Amflow PR Carbon, layout 1c from the Split & Join Planner.
// One custom feature. Origin is the rear axle, +X forward, +Z up, +Y rider's left.
// phi is from +X toward +Z (90 deg at top dead centre). s is arc length on the
// outer top, s = 0 on the seatstay brace centreline, +s toward TDC.
// Offline check (no Onshape): featurescript/rear/rear_r1_model.py
// Expected at the defaults: parts=2 volA=48.07 volB=114.19 minTyreGap=15.57
// neckW=52 bedA=123.5x123.6 bedB=247.7x248.0 slotAlignErr=0

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
        const q = (s >= -20) ? def.lapWall : def.wall;
        rows = append(rows, [s, q, sec.h + bottomExtra]);
    }
    return rows;
}

function numDef(definition is map) returns map
{
    return {
        "R" : definition.outerR / millimeter,
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
    const sA = [sF, -110, -85, -55, -37, -20, -10, 0, 10, sJ];
    const aSolid = loftProfiles(context, id + "aOut", def, rowsOf(sA, 0, h));
    const aHole = loftProfiles(context, id + "aHole", def, rowsWallA(def, sA, 1));
    subtractTool(context, id + "aSub", aSolid, aHole);
    // shoulder filler just ahead of s=-20, overlapping the inner wall by 0.4 mm
    var shRows = [];
    for (var s in [-23, -21.5, -20])
    {
        const sec = sectionAt(def, s);
        shRows = append(shRows, [s, def.wall - 0.4, sec.h]);
    }
    const shoulder = loftProfiles(context, id + "sh", def, shRows);
    unionInto(context, id + "shU", bodyAt(context, pA), shoulder);

    // ----- B shell, bridge, tongue -----
    const sB = [sJ, 40, 60, 85, 120, 160, 200, 240, 280, 310, sT];
    const bSolid = loftProfiles(context, id + "bOut", def, rowsOf(sB, 0, h));
    const bHole = loftProfiles(context, id + "bHole", def, rowsOf(sB, def.wall, h + 1));
    subtractTool(context, id + "bSub", bSolid, bHole);

    const sTg = [-20, -10, 0, 10, sJ + 1.5];
    const tgSolid = loftProfiles(context, id + "tgOut", def, rowsOf(sTg, qTongue, h));
    const tgHole = loftProfiles(context, id + "tgHole", def, rowsOf(sTg, qTongueIn, h + 1));
    subtractTool(context, id + "tgSub", tgSolid, tgHole);

    // thick bridge sJoin .. sJoin+6 so the tongue (depth 3.2..6.2) joins the 2.5 mm shell
    const sBr = [sJ, sJ + 6];
    const brSolid = loftProfiles(context, id + "brOut", def, rowsOf(sBr, 0, h));
    const brHole = loftProfiles(context, id + "brHole", def, rowsOf(sBr, qTongueIn + 0.4, h + 1));
    subtractTool(context, id + "brSub", brSolid, brHole);

    unionInto(context, id + "bU1", bodyAt(context, pB), tgSolid);
    unionInto(context, id + "bU2", bodyAt(context, pB), brSolid);

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
    const ss = [-20, -10, 0, 10, 20.6];
    for (var i = 0; i < size(ss); i += 1)
    {
        const fr = arcFrame(def, ss[i]);
        const sid = id + ("sk" ~ i);
        const sk = newSketchOnPlane(context, sid, { "sketchPlane" : fr.plane });
        const mm = millimeter;
        // two rectangles, one loop each is not a single region if drawn together without being separate.
        // draw one rectangle per groove by making a single profile per side via two sketches stacked in the loft pairs.
        skLineSegment(sk, "a", { "start" : vector(20 - 1.8, 1.5) * mm, "end" : vector(20 + 1.8, 1.5) * mm });
        skLineSegment(sk, "b", { "start" : vector(20 + 1.8, 1.5) * mm, "end" : vector(20 + 1.8, 3.05) * mm });
        skLineSegment(sk, "c", { "start" : vector(20 + 1.8, 3.05) * mm, "end" : vector(20 - 1.8, 3.05) * mm });
        skLineSegment(sk, "d", { "start" : vector(20 - 1.8, 3.05) * mm, "end" : vector(20 - 1.8, 1.5) * mm });
        skSolve(sk);
        profs = append(profs, qSketchRegion(sid));
    }
    opLoft(context, id + "loft", { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    var profs2 = [];
    for (var j = 0; j < size(ss); j += 1)
    {
        const fr2 = arcFrame(def, ss[j]);
        const sid2 = id + ("skn" ~ j);
        rectSketch(context, sid2, fr2.plane, -20 - 1.8, 1.5, -20 + 1.8, 3.05);
        profs2 = append(profs2, qSketchRegion(sid2));
    }
    opLoft(context, id + "loft2", { "profileSubqueries" : profs2, "bodyType" : ToolBodyType.SOLID });
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft", EntityType.BODY), qCreatedBy(id + "loft2", EntityType.BODY)]);
}

function addRails(context is Context, id is Id, def is map, qTop is number) returns Query
{
    // 3.0 wide, 1.2 proud of the tongue outer, 0.3 embedded, 4 mm lead-in
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
            const bot = qTop + 0.3;
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
    // 6.2 wide x 1.3 deep on the tongue underside, flush seat for the tie strand
    const y0 = qInner - 1.3;
    const ss = [-14, 0, 14];
    for (var side = 0; side < 2; side += 1)
    {
        const y = (side == 0) ? 12 : -12;
        var profs = [];
        for (var i = 0; i < size(ss); i += 1)
        {
            const fr = arcFrame(def, ss[i]);
            rectSketch(context, id + ("sk" ~ side ~ i), fr.plane, y - 3.1, y0, y + 3.1, qInner + 0.2);
            profs = append(profs, qSketchRegion(id + ("sk" ~ side ~ i)));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function addBraceBosses(context is Context, id is Id, def is map) returns Query
{
    // 1 mm proud on the +s side of each brace slot, on A's outer skin
    var tools = [];
    var n = 0;
    for (var s in [14, -14])
    {
        const sBoss = s + 1.35 + 2;
        const fr = arcFrame(def, sBoss);
        for (var y in [12, -12])
        {
            rectSketch(context, id + ("sk" ~ n), fr.plane, y - 4, -1.0, y + 4, 0.3);
            tools = append(tools, extrudeRegion(context, id + ("ex" ~ n), id + ("sk" ~ n), fr.tang, 4));
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
                // 3 mm wide, 3 mm proud, 0.6 mm embedded so the union is solid
                rectSketch(context, id + ("sk" ~ n ~ k), fr.plane, y - 1.5, -3, y + 1.5, 0.6);
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
        rectSketch(context, id + ("sk" ~ n), fr.plane, -18, -0.4, 18, 0.15);
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
    const origin = fr.origin - fr.tang * (sign * 0.3 * millimeter);
    const pl = plane(origin, fr.tang, vector(0, 1, 0));
    rectSketch(context, id + "sk", pl, -half, -lip, half, 0.3);
    return extrudeRegion(context, id + "ex", id + "sk", fr.tang * sign, lip + 0.3);
}

function addHem(context is Context, id is Id, def is map) returns Query
{
    // 1.8 mm lip along both lower edges of the skirt
    const ss = [def.sFront, -110, -85, -55, -20];
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        var profs = [];
        for (var i = 0; i < size(ss); i += 1)
        {
            const sec = sectionAt(def, ss[i]);
            const fr = arcFrame(def, ss[i]);
            const xIn = sg * (sec.a - 1.2);
            const xLip = sg * (sec.a + 0.6);
            const x0 = min(xIn, xLip);
            const x1 = max(xIn, xLip);
            rectSketch(context, id + ("sk" ~ side ~ i), fr.plane, x0, sec.h - 1.8, x1, sec.h);
            profs = append(profs, qSketchRegion(id + ("sk" ~ side ~ i)));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function cutTailCorners(context is Context, id is Id, def is map)
{
    // R5 in plan where the squared end meets each side wall
    const sEnd = def.sTail;
    const sMid = sEnd - 2.5;
    const sec = sectionAt(def, sEnd);
    const a = sec.a;
    const fr = arcFrame(def, sMid);
    const origin = fr.radial * ((def.R + 6) * millimeter);
    const pl = plane(origin, fr.radial, fr.tang);
    // sketch x along +s, sketch y = -world Y
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        const sk = newSketchOnPlane(context, id + ("sk" ~ side), { "sketchPlane" : pl });
        const mm = millimeter;
        // fillet centre at s = sEnd-5, |y| = a-5. In this plane sMid maps to sketch x = 0.
        const cx = (sEnd - 5) - sMid;
        const cy = -sg * (a - 5);
        const cornerX = sEnd - sMid;
        const cornerY = -sg * a;
        const c45 = 0.70710678118;
        // arc from the side wall to the end plane, then the two straight edges of the offcut
        const arcEnd = vector(cornerX, cy);
        const arcStart = vector(cx, cornerY);
        if (sg == 1)
        {
            skArc(sk, "a", { "start" : arcStart * mm, "mid" : vector(cx + 5 * c45, cy - 5 * c45) * mm, "end" : arcEnd * mm });
            skLineSegment(sk, "e", { "start" : arcEnd * mm, "end" : vector(cornerX, cornerY) * mm });
            skLineSegment(sk, "s", { "start" : vector(cornerX, cornerY) * mm, "end" : arcStart * mm });
        }
        else
        {
            skArc(sk, "a", { "start" : arcStart * mm, "mid" : vector(cx + 5 * c45, cy + 5 * c45) * mm, "end" : arcEnd * mm });
            skLineSegment(sk, "e", { "start" : arcEnd * mm, "end" : vector(cornerX, cornerY) * mm });
            skLineSegment(sk, "s", { "start" : vector(cornerX, cornerY) * mm, "end" : arcStart * mm });
        }
        skSolve(sk);
        const tool = extrudeRegion(context, id + ("ex" ~ side), id + ("sk" ~ side), -fr.radial, def.depth + 14);
        subtractTool(context, id + ("cut" ~ side), bodyAt(context, worldPt(def, 150, 0, 1)), tool);
    }
}

function addTabs(context is Context, id is Id, def is map) returns Query
{
    // flat tabs following the stay inner face, 3 mm thick, 3 mm side clearance.
    // Lower end is d=44: a 3 mm tab at the requested d=55 cannot keep a 15 mm tyre gap.
    const phi = def.phi0 * radian;
    const radial = vector(cos(phi), 0, sin(phi));
    const tang = vector(-sin(phi), 0, cos(phi));
    const ds = [8, 20, 32, 44];
    for (var side = 0; side < 2; side += 1)
    {
        const sg = (side == 0) ? 1 : -1;
        var profs = [];
        for (var i = 0; i < size(ds); i += 1)
        {
            const d = ds[i];
            const yo = stayGap(d) / 2 - 3;
            const origin = radial * ((def.R + def.pad - d) * millimeter);
            // plane normal is radial; sketch x = tang, sketch y = radial x tang = -Y
            const pl = plane(origin, radial, tang);
            const y0 = -sg * yo;
            const y1 = -sg * 16; // 16 mm from centre, buried in the shell wall
            rectSketch(context, id + ("sk" ~ side ~ i), pl, -16, min(y0, y1), 16, max(y0, y1));
            profs = append(profs, qSketchRegion(id + ("sk" ~ side ~ i)));
        }
        opLoft(context, id + ("loft" ~ side), { "profileSubqueries" : profs, "bodyType" : ToolBodyType.SOLID });
    }
    opDeleteBodies(context, id + "del", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
    return qUnion([qCreatedBy(id + "loft0", EntityType.BODY), qCreatedBy(id + "loft1", EntityType.BODY)]);
}

function cutTabSlots(context is Context, id is Id, def is map)
{
    // one 6.0 (along the stay) x 2.4 slot per station, both stays. Stations d=20 and d=40.
    const phi = def.phi0 * radian;
    const radial = vector(cos(phi), 0, sin(phi));
    const tang = vector(-sin(phi), 0, cos(phi));
    var n = 0;
    for (var d in [20, 40])
    {
        for (var sg in [1, -1])
        {
            const yo = stayGap(d) / 2 - 3;
            const origin = radial * ((def.R + def.pad - d) * millimeter) + vector(0, sg * (yo - 8), 0) * millimeter;
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

annotation { "Feature Type Name" : "Rear Mudguard R1 (Amflow PR)" }
export const rearMudguardR1 = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Outer top radius" }
        isLength(definition.outerR, { (millimeter) : [340, 390, 430] } as LengthBoundSpec);
        annotation { "Name" : "Wall" }
        isLength(definition.wall, { (millimeter) : [1.6, 2.5, 4] } as LengthBoundSpec);
        annotation { "Name" : "Lap and mount wall" }
        isLength(definition.lapWall, { (millimeter) : [2, 3, 5] } as LengthBoundSpec);
        annotation { "Name" : "Section depth" }
        isLength(definition.depth, { (millimeter) : [12, 22, 40] } as LengthBoundSpec);
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
        annotation { "Name" : "Brace underside to tyre (estimate, not used to set R)" }
        isLength(definition.braceGap, { (millimeter) : [20, 38, 60] } as LengthBoundSpec);
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
        "outerR" : 390 * millimeter, "wall" : 2.5 * millimeter, "lapWall" : 3 * millimeter, "depth" : 22 * millimeter,
        "neckWidth" : 52 * millimeter, "wideWidth" : 94 * millimeter, "neckCorner" : 4 * millimeter, "wideCorner" : 12 * millimeter,
        "phiBrace" : 33.6 * degree, "tyreRadius" : 350 * millimeter, "tyreWidth" : 63.5 * millimeter,
        "padThk" : 1.5 * millimeter, "braceGap" : 38 * millimeter, "lapClear" : 0.2 * millimeter,
        "sFront" : -135 * millimeter, "sJoin" : 20 * millimeter, "sTail" : 330 * millimeter
    });
