# Spec: 3D-printed two-piece FRONT mudguard, zip-tie mount
Fox AWL HD Sport (36 mm) · Amflow PR Carbon (non-Pro) · Bambu P1S. Reference Analyst, 16 Nov 2026 (Europe/London)

**Tags.** KNOWN = published or measured, with the source. JAMES = James's figure from his last print. 3DD = reported in 3D Designer's research (`/workspace/mudguard/research.md`); I checked the Amflow tyre and fork entries myself. EST = my estimate, with the reasoning. **PH-n** = a placeholder for a measurement James has not taken yet. Treat every PH as a CAD parameter, not a fixed value. Ignore the old 34 mm CAD in `/workspace/mudguard/`.

## 0. Coordinate system (use this in the CAD)
- **Fork frame.** The origin is the axle centre. +Z runs along the stanchion axis, towards the crown. +X points forward (the brace side). Y is lateral.
- **Arc coordinate `s`.** Measured in mm along the guard's outer top centreline. **s = 0 is the brace centreline** (half-way between its front and back faces at the underside). **s < 0 is forward** (the front blade). **s > 0 is rearward** (towards the crown and the tail).
- **R_top.** The radius of the guard's outer top surface about the axle. **R_top = PH-1 − pad thickness** (the guard hangs from the brace underside).

## 1. Overview
- **Object.** A long front-wheel spray guard that runs in an arc concentric with the tyre. It mounts to the **underside of the fork brace (arch)** with 2 zip ties over the brace, and to **both lower legs** with 2 zip ties, about 45 mm behind the brace ties (JAMES).
- **Construction.**
  - It is printed in two pieces: a **front piece** (front blade plus the brace saddle) and a **rear piece** (leg ears plus the tail).
  - The pieces join at a **transverse flange perpendicular to the top surface**, just behind the brace and between the left and right brace-tie slots, with **3× M3**.
  - The front flange face butts against the brace back face. That stops the guard rotating forward under braking and mud load, and the brace hides the join.
- **What each reference contributes.**
  - **Sico Amflow** (440 mm, 131 g, zip ties; KNOWN, sicomtbproducts.com/product/amflow-pl). This is the primary shape reference: scanned to the Amflow, **waisted/saddled under the brace and then flared**, a rounded deep U-section, and tie holes in two clusters on the waist (from its photos).
  - **Cube Acid Long**: **405 mm length; 80 mm wide under the arch, flaring to 125 mm** (KNOWN, tweekscycles.com/cube-acid-mud-shield-front-mudguard-4000313). This sets the length target and the waist-to-flare widths. It also contributes the rubber pads against the fork.
  - **Mudhugger EVO Long**: 525 mm, 90 mm wide, 120 g. The page splits the length as "Rear extension 340 mm, front extension 185 mm" (KNOWN, themudhugger.co.uk). It contributes the deep U-section, the trapezoid saddle under the arch, the **subtle tip kicker** and multiple rows of brace-tie slots.
  - **Mudhugger FRX**: 435 × 90 mm, about 80 g (KNOWN, same site). It shows the flat floor to **avoid**.
- **Fork and tyre.**
  - The fork is a FOX AWL HD Sport: 36 mm stanchions, 160 mm travel, 15×110 Boost, 44 mm offset. **No guard mount** ("MUD GUARD MOUNT: N/A", ridefox.com/products/fox-36-awl-rail; fork spec from amflowbikes.com/pr-carbon/specs). KNOWN.
  - The front tyre is a Maxxis Assegai 29×2.50 (KNOWN, Amflow specs).
  - Fox maximum is 29×2.6: **762 mm peak diameter, 69.2 mm width**. Any tyre wider than 2.4" needs **≥ 8 mm tyre-to-fender** clearance at full compression (3DD, from Fox user-spec sheet 1).
  - Fox A-dimension is 585 mm extended and 425 mm compressed (3DD, same sheet).

## 2. Bounding boxes (EST; they follow PH-1 and PH-3)
Assumes R_top ≈ 393 mm, which gives 1° of arc ≈ 6.86 mm. Front piece = s −240 → +16 (256 mm of arc). Rear piece = s +16 → +165 (149 mm of arc).

| Piece | Arc | Chord | Sagitta | Max width | Height (chord on bed, incl. section depth + flange) | Fits 256³? |
|---|---|---|---|---|---|---|
| Front | 256 mm (37.3°) | **≈ 251 mm** | ≈ 21 mm | 110 mm | ≈ 80 mm | **Yes, along X with about 5 mm spare.** If you lengthen it, rotate it to the diagonal: (half-chord + half-width at the ends) must stay ≤ 181 mm. |
| Rear | 149 mm (21.7°) | ≈ 148 mm | ≈ 7 mm | ≈ 84 mm shell; ≈ 104 mm over the ears | ≈ 75 mm (ears hang about 30 mm below the top) | Yes, with a large margin |
| Assembled | 405 mm (59°) | ≈ 387 mm | ≈ 52 mm | 110 mm | ≈ 100 mm | No. That is why it is split. |

**Print orientation (EST).** Print as worn: chord on the bed, U opening down, tree supports inside the arch. That keeps the layer lines running along the arc, so the part is strong in bending. Do **not** stand it on the cut face: it fits (about 250 mm tall), but the layers would then lie across the arc and it would snap at the brace.

## 3. Length, arc and radius
| Item | Value | Tag |
|---|---|---|
| Total arc length | **405 mm** | KNOWN target (Cube Long). |
| Front blade (s = 0 → tip) | **240 mm** (35.0° ahead of the brace) | EST. Long front blade, like Cube and the EVO. This is the most the front piece can have and still fit the bed. |
| Tail (s = 0 → end) | **165 mm** (24.1° behind the brace) | EST. This is the remainder of the 405 mm. **Trim to 120–140 mm if the down-tube check fails** (OQ-9). |
| Join plane | s = +PH-3/2 + 1.5 pad + 8 (front flange) ≈ **+20.5 mm** | EST, from PH-3 (updated v2.1: flange A 8 mm). |
| Brace position in the fork frame | Brace centre about 30 mm forward of the stanchion axis, about 4.4° ahead of +Z | EST. Fox 36-family arches sit in front of the legs; scaled from 36 mm stanchion photos. |
| Angular coverage | About **59°** in total: tip at about 39° forward of +Z, tail end at about 20° behind | EST |
| Tyre outer radius | **≈ 375 mm** (±5) | EST. A 29×2.5 Assegai on a 30 mm rim is typically about 745–755 mm across. Fox caps it at 381 mm (3DD/Fox). |
| Radial clearance, tyre to guard inside | **16 mm nominal; 12 mm minimum** | EST. Mudhugger's "index finger between brace and tyre" rule is about 15–18 mm; Fox requires ≥ 8 mm. |
| **Guard inner radius** | R_tyre + clearance = **391 mm** | EST |
| **Guard outer top radius R_top** | **393.5 mm** (wall 2.5 mm) | EST. **Must equal PH-1 − 1.5 mm pad.** |
| Profile | Constant radius (concentric) from the tip to the tail end. **Tip kicker:** the last 25 mm bends 12° outward (away from the tyre). | EST (EVO "subtle kicker") |

If the measured PH-1 gives an inner clearance under 10 mm, thin the waist floor to 2.0 mm and/or shorten the saddle. Do not drop below 8 mm (Fox).

## 4. Cross-sections
Widths are outer dimensions. Depth = how far the side-wall bottom edge sits below the outer top, measured radially.

| Station | s (mm) | Outer width | Depth (wrap) | Shape | Tag |
|---|---|---|---|---|---|
| Front tip | −240 | **100** | **30** | Rounded U: 50 mm flat-ish top crown (R 150) blending into R 22 corners, walls 10° out from vertical. Kicker lip. | EST. Narrower than the max so it does not look like a moto fender; matches the EVO/Sico tips. |
| Front max (constant zone) | −170 → −70 | **110** | **42** | Same U. The side-wall bottom edges sit about 24 mm below the tyre crown, i.e. at the Assegai's widest shoulder (about 64 mm); that leaves about 21 mm of mud room per side. | EST. Between EVO 90 and Cube 125. |
| Flare transition | −70 → −15 | 110 → 78 | 42 → 26 | G2 blend. The flare must finish **before** the brace front face (s ≈ −11) so the waist passes under it. | EST |
| **Under the brace (waist)** | −15 → +18 | **78** (≤ 80) | **26** | **Trapezoid:** top flat **60 mm** (PH-5), side walls 19° from vertical down to 78 mm at the bottom, **R 6** fillets at the top corners. Alternative if PH-5 shows a curved underside: a single arc **R 42 mm** over a 78 mm chord, giving 26 mm of depth. Inner width ≈ 73 mm, enough for the 2.5" Assegai and tight for a 2.6 (69.2) at the wall bottoms. | **Width limit JAMES (70–80 max flat)**; Cube 80 mm under the arch (KNOWN); the rest EST. |
| Legs zone | +18 → +60 | **84** | 26 | Trapezoid as at the waist, plus the leg ears (§5). | EST. Must be ≤ PH-2 − 2 × (about 8 mm gap). |
| Rear tail | +60 → +165 | 84 → **80** | 26 → **22** | Rounded U, R 40. Ends square with a 3 mm rolled-down lip. | EST |

The top surface at the waist must sit flat against the brace underside. If PH-5 shows the brace underside is arched or V-shaped, copy that shape on the top over s = ±(PH-3/2 + 3).

## 5. Zip-tie slots
- **Slot sizes.**
  - For 4.8 mm ties (about 1.3 mm thick): **6.0 × 2.4 mm** (long axis transverse to the tie path), 0.4 mm entry chamfers. These also pass 3.6 mm ties.
  - For 3.6 mm-only ties: 4.4 × 1.8 mm.
  - Recommendation: 4.8 mm UV-stable black nylon ties, so use 6.0 × 2.4 mm everywhere.
- **Slot bosses.** Every slot gets a 3.5 mm-thick local pad (shell 2.5 mm + 1 mm raised on the outside), at least 4 mm of material around the slot, and R 1 edges.

| Group | Count | Position (s; lateral y) | Notes | Tag |
|---|---|---|---|---|
| Brace ties, front row | 2 | s = −(PH-3/2 + 3) ≈ −14; y = ±30 | The tie goes up through here, over the brace top and down behind it. | EST |
| Brace ties, optional second front row | 2 | s ≈ −22; y = ±30 | Adjustment, like the EVO's three rows. Leave it skinned over (0.4 mm) and punch it out if needed. | EST |
| Brace ties, rear row | 2 | s = +(PH-3/2 + 3) ≈ +14; y = ±30 | In the **front piece**, just outboard of the flange (flange spans y = ±23). | EST |
| Leg ties | 4–6 (2–3 per wing) | Wing centre at **s = +45** (JAMES); slot pairs at s = +34 / +56, rows d = 48 / 74 / 100 | **SUPERSEDED, see `wings.md`** | JAMES / EST |

- **Leg wings (rear piece, ×2): SUPERSEDED.** James rejected the v2 R 24 saddle ears. Use the flat bolt-on wings in **`wings.md`**: 40 × 100 mm paddle, 3 mm PETG, |y| 45.5–48.5, tip 112 mm below the guard top, 2× M3 CSK to an integral stub on part B.
- **Pads against the fork.** All of these sit on Heli-tape on the fork.
  - Allow **1.5 mm** of adhesive EPDM or neoprene (or a TPU 95A printed pad) on the waist top over s = −14 → +14.
  - **1.5 mm** on each wing's leg face (see `wings.md`).
  - **1.0 mm** on the front flange face.
  - The geometry already subtracts these pad thicknesses.

## 6. Join flange
| Item | Value | Tag |
|---|---|---|
| Position | Front face at s = +PH-3/2 + 1.5 (brace back face + pad), y = ±23, rising **perpendicular to the top surface** | JAMES concept; values EST |
| Height above the outer top surface | **14 mm** (PH: ≤ PH-4 − 3 mm, and ≥ 6 mm below the crown at full compression; see OQ-8) | EST |
| Width | 46 mm, top corners R 5 | EST |
| Front-piece flange | **8 mm** thick (updated v2.1 to match join plan and mudguard_v2.fs; carries captive nyloc slots) | EST |
| Rear-piece flange | **4 mm** thick | EST |
| M3 holes | 3× **Ø3.4** clearance at y = −15, 0, +15, centred **7 mm** above the outer top | EST |
| Nuts | **Hex traps in the front flange, opening on the brace face**: 5.8 mm AF × 2.6 mm deep (fits the nut flush, 0.2 mm recessed). Bolts go in from the rear (crown side). This keeps the brace face flat. | EST |
| Bolts | M3 × 10 button head (4 + 3.4 + 2.4 ≈ 9.8 → flush). An alternative is M3 heat-set inserts (Ø4.0 hole) in the front flange. | EST |
| Below the top | The flange continues as an **internal rim** around the inside of the U, 5 mm deep, down both side walls, so the side walls are joined too. Optionally add 1× M3 low on each side wall (James to decide). | EST |
| Location | 2× Ø3 × 3 mm pins on the rear flange; Ø3.3 holes in the front | EST |
| Fillets | R 3 at the root on the rear (crown) side. **0.5 mm chamfer only on the brace side**, so the flange face can sit fully on the brace back face; match the brace's bottom-back edge radius if PH-4 shows one. | EST |

## 7. Wall, ribs, material
- **Wall thickness.**
  - Shell **2.5 mm** (6 × 0.42 mm lines).
  - **3.0 mm** across the waist (s −20 → +20) and the leg zone.
  - Edges get a 1.5 mm rolled bead (stiffness, no knife edge).
- **Ribs.**
  - One **external** spine rib, 4 mm wide × 2 mm tall, on the top centreline from s = −200 to −20; it stiffens the long blade.
  - No internal ribs (they collect mud).
  - Optional internal 6 mm gusset at each ear root.
- **Fillets and chamfers.** R 6 at the waist top corners; R 4 at the ear roots; R 3 at the flange root (rear side); 0.4 mm chamfers on the slots; the tip kicker gets an R 10 transition.
- **Material.**
  - **PETG** is the default: tough, takes a little flex, prints easily on the P1S. Use 4–6 walls, 25 % gyroid infill (the parts are mostly walls anyway), and tree supports with a 0.2 mm Z gap.
  - **ASA** is the option if the bike lives outside or in a hot car (better UV and heat). Print it enclosed with the door closed. It is slightly more brittle in cold impacts.
  - **TPU 95A** for the pads, or a clip-on tip flap.
  - Avoid PLA (creeps or warps in the sun) and CF-filled filament (brittle at the brace).

## 8. KNOWN vs EST
| Parameter | Value | Tag / source |
|---|---|---|
| Fork model, stanchion | FOX AWL HD Sport, 36 mm | KNOWN (amflowbikes.com/pr-carbon/specs; ridefox.com) |
| Travel, axle, offset | 160 mm, 15×110, 44 mm | KNOWN (Amflow specs) |
| Guard mount on fork | None | KNOWN (ridefox.com "N/A"; Mudhugger bolt-on compatibility page: "AWL 29 Not Compatible no bolt holes") |
| A-dimension | 585 extended / 425 compressed | 3DD (Fox user spec, sheet 1) |
| Front tyre | Maxxis Assegai 29×2.50 | KNOWN (Amflow specs) |
| Fox maximum tyre | 762 mm diameter, 69.2 mm width, ≥ 8 mm to fender | 3DD (Fox user spec, note 5) |
| Tyre outer radius | 375 mm | EST |
| Total length | 405 mm | KNOWN target (Cube Long, Tweeks) |
| Cube widths | 80 under the arch → 125 mm | KNOWN (Tweeks) |
| EVO Long | 525 mm (340 rear / 185 front ext.), 90 mm | KNOWN (themudhugger.co.uk) |
| FRX | 435 × 90 mm | KNOWN (themudhugger.co.uk) |
| Sico Amflow front | 440 mm, 131 g | KNOWN (sicomtbproducts.com) |
| Brace-to-leg tie offset | ~45 mm | JAMES |
| Maximum flat width under the brace | 70–80 mm | JAMES |
| Flange concept, 3× M3 | as described | JAMES |
| Front blade / tail split | 240 / 165 mm | EST |
| Clearance / R_inner / R_top | 16 / 391 / 393.5 mm | EST |
| All section widths and depths in §4 | — | EST |
| Slot sizes, positions, flange dimensions | — | EST |
| PH-1 axle → brace underside | **395 mm** | PLACEHOLDER (R_tyre + 16 + 2.5 + 1.5) |
| PH-2 inner face-to-face of the lower legs at guard height | **100 mm** | PLACEHOLDER (Boost 36 lowers, about 145 mm c-c minus a 45 mm OD) |
| PH-3 brace fore-aft depth at its underside | **22 mm** | PLACEHOLDER (scaled from 36 mm stanchion photos) |
| PH-4 brace back-face height | **25 mm** | PLACEHOLDER |
| PH-5 brace underside flat width / shape | **60 mm flat** | PLACEHOLDER |
| PH-6 lower-leg OD at the ear height | **45 mm** | PLACEHOLDER |
| PH-7 brace face tilt vs the fork axis | **0°** | PLACEHOLDER (parametrise) |

## 9. Open questions: what James should measure
Take these with the wheel in, the fork at full extension, and a steel rule plus a card template.
1. **PH-1, axle to brace underside.** Measure from the axle centre (centre of the thru-axle end) to the brace underside at the centre, parallel to the stanchions. Also measure tyre crown to brace underside (that gap minus 4 mm = the available clearance).
2. **Tyre radius.** Measure the tyre diameter at the tread centre with a long rule across the wheel (or tape around the circumference ÷ π), then halve it.
3. **PH-5, brace underside profile.** Press a strip of card or a profile gauge up under the brace and trace it. Note the flat width and any V or arch shape.
4. **PH-3 and PH-4, brace size.** Measure fore-aft depth at the underside with calipers, and the height of the back face.
5. **PH-2 and PH-6, legs.** Take the inside face-to-face distance between the lower legs at 0–40 mm below the brace, and each leg's OD there (calipers).
6. **Tie paths.** Confirm the ~45 mm is the **fore-aft (along-the-arc) distance from the brace centre to the leg centreline** (that is how this spec reads it), not a lateral distance.
7. **PH-7, brace tilt.** Measure the angle of the brace front and back faces relative to the stanchion axis, with a phone inclinometer or a set square against the stanchion.
8. **Crown clearance.** Let the air out (or remove the air spring per Fox) and compress fully. Measure the crown underside to the brace top and to the guard flange position. The flange top and the tail must keep **≥ 6 mm**.
9. **Down-tube clearance.** At full compression, turn the bars to full lock both ways. Check the 165 mm tail against the down tube and the battery cover; the tail end sits about 24° behind the brace.
10. **Central brace hole.** Is there one, and what are its diameter and depth? (Optional locating peg only; never the primary mount.)
11. **Join on the side walls.** Is the top-only 3× M3 joint enough, or does James want 1× M3 per side wall as well?

## v2.4 update
- Split: frontLen 165 (s -165..0), tailLen 240 (s 0..+240). Part A s -165..+20.5, part B (wings) s +20.5..+240.
- Brace tie rows: front s=-14 (A), rear s=+29 (B, 3.3 mm behind B's flange). Flange stack s +12.5..+24.5, |y|<=23.
- Wings stay centred s=+45 from brace centre.

## v2.5 update - measured legs (James)
- PH2 = 100 (inner gap at/below 90 mm under the brace); legGapUpper 85 at 40 mm; PH6 = 45 (OD at ties); OD 60 at 40 mm; centre 145.
- Wing tie rows 85 / 110 / 125 mm below the brace underside, tip 135. Wings relieved around the tapered leg above ~75 mm.
- Guard width at legs 76 (was 84). Brace PH3/PH4/PH5 still placeholders - photos not yet read (see fork_photos.md).
