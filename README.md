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
