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
