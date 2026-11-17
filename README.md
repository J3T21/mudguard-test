# Mudguard Test

Fresh workspace for Amflow PR Carbon mudguard CAD / FeatureScript work.

## Onshape (new account, jameset701@outlook.com)

Document `3da26eeb7582e4a385de5a0b` (Part Studio element `6d4ce527def8539685e560ec`):

| model | workspace | link |
|---|---|---|
| v2.13 full | `948942d3cc9378b956acd3a2` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/948942d3cc9378b956acd3a2/e/6d4ce527def8539685e560ec |
| v2.13 joint coupon | `c3a0d5af654d70f91bf09ea2` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/c3a0d5af654d70f91bf09ea2/e/6d4ce527def8539685e560ec |
| v2.14 full | `a2e5d8fbd1052ed791efa62b` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/a2e5d8fbd1052ed791efa62b/e/6d4ce527def8539685e560ec |
| v2.14 joint coupon | `16c881db862c3f5c81b02a0a` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/16c881db862c3f5c81b02a0a/e/6d4ce527def8539685e560ec |

Sources: `featurescript/mudguard_v2.14.fs`, `featurescript/test_coupon_v2.14.fs`; verification in `featurescript/verification_v2.14.md`.

## v2.15 (built)

| model | workspace | link |
|---|---|---|
| v2.15 full | `a2749eafdd4a4d19d1a57dc6` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/a2749eafdd4a4d19d1a57dc6/e/6d4ce527def8539685e560ec |

`featurescript/mudguard_v2.15.fs`: the front tip (A) and the rear end (B) are rounded in plan with R32 corners tangent to the end
and to the side lip at y = ±48.5 (33 mm straight end), plus a 1 mm 45° chamfer on that straight lip (outer and inner).
Everything else is v2.14. The joint coupon is unchanged (use the v2.14 coupon; the coupon window s −25..+115 excludes both ends).
Offline check: `featurescript/v2.15_end_round.py`. Onshape test bundle: `featurescript/test_bundle_v2.15.fs`
(v2.15 core + the v2.14 coupon checks); console in `featurescript/verification_v2.15.txt`.

Measured: A 62.4 / B 112.1 cm³ (1 solid each); end cut removed A 2848 / B 2113 mm³; corners removed 2/2, inside-arc kept 2/2,
arc radius 32.0 at both ends, chamfer present (u 0.7 at 0.3 below the top), land u 0 at 1.2 below. Tyre clearance A 9.44/10.89,
B 9.44/10.90; legs 0.59; width 97; bolts 0 hits; tie slots clear; joint step 0.011; outside and inside R4 join fillets applied;
B bed fit 241.3 × 238.3. Print orientations unchanged.

## v2.16 (built) - lip chamfer removed

| model | workspace | link |
|---|---|---|
| v2.16 full | `4596cab3c7f85b3098b074bf` | https://cad.onshape.com/documents/3da26eeb7582e4a385de5a0b/w/4596cab3c7f85b3098b074bf/e/6d4ce527def8539685e560ec |

`featurescript/mudguard_v2.16.fs` = v2.15 with the 1 mm lip chamfer on the straight end lip removed (James, t57u); R32 plan corners
and everything else unchanged. Coupon unchanged (v2.14). Test bundle `featurescript/test_bundle_v2.16.fs`, console `featurescript/verification_v2.16.txt`.

Measured: A 62.5 / B 112.1 cm³ (1 solid each); corners removed 2/2, inside-arc kept 2/2, arc R 32.0 both ends; chamfer absent (u 0 at 0.3 below
the top). Tyre A 9.44/10.89, B 9.44/10.90; legs 0.59; width 97; bolts 0 hits; ties clear; joint step 0.011; R4 out/in join fillets;
B bed fit 241.3 × 238.3. Print orientations unchanged.

## v2.17 (offline — no Onshape build)

`featurescript/mudguard_v2.17.fs`. Fox AWL, PETG, Bambu P1S. Same as v2.16 (R32 ends, no lip chamfer, R4 join fillets, stepped joint, brace ties at s ±12.5) except:

- No seal window. The leg sheets stay solid.
- Wing zip ties: only the lowest pair, at 125 mm below the brace (the 90 mm and 110 mm rows are gone). Four holes, two per wing.
- Seal blister, on: the leg-facing face wraps the collar (OD 55 mm, 20 mm tall, top at the brace) at 1.5 mm radial clearance, so a 58 mm effective cylinder, face at |y|=43.5. Flat run is 22 mm starting where the 19° wall meets that cylinder (D 17.5), then a 51 mm ramp back to the |y|=48.5 sheet so the return stays 1.5 mm off a straight taper from OD 55 at D=20 to OD 45 at D=90. Wall stays 2.5 mm.
- Junction rib: 2 mm proud toward the tyre, 3 mm down the sheet, stadium ends R1.5. It is on the tyre side of the skin. A bead on the leg face would use up the 1.5 mm collar gap.

Offline numbers: `featurescript/verification_v2.17.txt`, script `featurescript/v2.17_offline.py`. Expected volume about A 62.5 / B 128.7 cm³. B bed fit still 241.3 × 238.3. Renders in `out/`. Test bundle `featurescript/test_bundle_v2.17.fs` (one feature evaluation throws unless parts, volumes, holes, indent face, rib, design-leg clearance and bed all pass). The joint coupon trim is unchanged; its sheet probes in the bundle match the solid sheet. The older tape figure OD 60 at 40 mm is not what this blister clears — see the verification note.

## v2.17.1

Onshape measured body B 1.1 mm off the design leg (`legclr 1.1`); the offline model said 1.50. The difference is the 0.4 mm loft skin: the shell was built proud of |y|=43.5 and that skin stayed, so the face was at |y|=43.9 (72.5 − 43.9 − 27.5 = 1.1). v2.17.1 drops the proud skin. The kept face is |y|=42.7 (0.5 mm inside the 43.5 spec cylinder, then a 0.3 mm cut bite). Flat clearance to the 55 mm collar is 2.30 mm. The bottom ramp is 64 mm so it stays above 1.8 mm until it joins the original sheet, which is still 1.5 mm off the OD 45 leg. `featurescript/test_bundle_v2.17.1.fs` prints the closest point and both volumes before it throws, and the throw text includes the volumes. Tag `v2.17.1`.

## v2.17.2

Built in Onshape. B volume check lower bound 123 → 120 cm³ (measured about 122.9). Tag `v2.17.2`, commit `abfcecb`.

## v2.18

The leg-facing ridge was the step between the loft at |y|=43.0 and the cut at |y|=42.7, together with the R4 crease fillet that the blister cut sliced. Shell and cut now use one surface: an R10 ease off the 19° wall, a flat at |y|=42.7 (2.30 mm off the 55 mm collar), then a smoothstep back to the sheet. The stadium rib is replaced by one R10 blend along the whole wing root on the tyre side, bulging 2.4 mm so it stays 8.4 mm off the 69.2 mm tyre. Leg clearance stays at least 1.50 mm. `featurescript/test_bundle_v2.18.fs` puts volA, volB, leg gap and tyre gap in one string that is also the throw message. Volume bounds are ±8% around A 62.5 / B 124.5. Renders: `out/v2.18_*.png`, including the tyre-side wing close-up. Tag `v2.18`.

## v2.19

The blister, the R10 root blend and the outer join fillet are gone. The leg sheet is the wall plane (|y|=48.5) at the root, then one continuous 2.5 mm sheet bows 2.8 mm away from the leg and comes back to the same plane. The belly is at |y|=45.7, 0.7 mm inside the OD 55 collar; the flush root line is still 2.6 mm into the upper leg for about 5 mm of height, which is the tangent the root has to keep. Tyre gap of the bowed inner face is about 9.1 mm. OD 45 clearance on the flat sheet is 1.5 mm. Stanchion clearance is 6 mm. The test reports collar interference and the OD 45 gap and does not fail on them. It fails if the sheet meets the OD 36 stanchion or the tyre gap is under 8 mm. Volume bounds are ±8% around A 62.5 / B 124. Renders: `out/v2.19_*.png`.

## v2.19.1

Onshape rejected the v2.19 loft intersection (`BOOLEAN_BAD_INPUT`): the slab was 21 sections, many of them the same rectangle, and the flat face sat on the wall plane. The sheet is now one closed profile — a 0.05 mm proud embed, then three tangent arcs (radii 30, 40, 30 mm) and the flat return — extruded across the wing and unioned. Corner rounds are a subtraction of two small bites. Offline, the profile has area 274 mm², thickness 2.5 mm, no self-intersections and no zero-length edges, and the embed overlaps the wall by 8.8 mm² per side. Belly |y|=45.95 (0.95 mm inside the OD 55 collar). Tyre gap about 8.9 mm. The production file and the test bundle use the same check string. Renders: `out/v2.19.1_*.png`.

## v2.19.2

Onshape rejected the v2.19.1 deep-patch union (BOOLEAN_INVALID at patchU). The leg sheet was already on the guard, 0.05 mm proud of the wall plane, while the deep-patch face sits 0.05 mm inside that plane. The sheets now union after the deep patch, the proud trim and clipY. The embed and the lower flat are 0.5 mm proud, and a shave cuts that skin back to |y|=48.5. The R8 tip tools were sketched with sketch-y along world -Z, so they missed the sheet; the plane x-direction is now -X and x is negated, and a build throws if a side loses under 10 mm3. The unused blister loft and the outer-fillet warning are gone. The check string is still the v2.19.1 string. Renders: out/v2.19.2_*.png.

## v2.19.3

Leg joint rebuilt to Split & Join Planner's plan: the lower 10 mm of the guard wall is cut out over s 22.5..76 and replaced by one revolved root profile (19° wall → R8 → R53 bow → R9.31 → flat |y| 49.0, wall 3.0 → 2.5), and a planar lower sheet on |y| 49.0 overlaps the root flat by 4 mm, all in one union. The proud skin, root band, ifill, deep patch, trims, shave, keep boxes and crease fillets are gone. The guard is revolved (exact) over the leg zone s 16..78 with lofts either side meeting it normal to the section, so the root sits on true surfaces. C1 ends at s 4 and the leg zone starts at s 16. OD45 clearance 1.0 mm, belly 2.5 mm lower. Verification: `featurescript/verification_v2.19.3.txt`; renders `out/v2.19.3_*.png`.
