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

## v2.15 (source only, not built in Onshape)

`featurescript/mudguard_v2.15.fs`. The front tip and the rear end are rounded in plan: R32 corners tangent to the end and to the side-wall lip at y = ±48.5, leaving a 33 mm straight end, plus a 1 mm 45° chamfer on that straight lip (outer and inner). Sections, the flange joint, bolts, tie slots, leg sheets and the print faces are unchanged. `featurescript/test_coupon_v2.15.fs` is a copy of the v2.14 coupon; the coupon window (s −25..+115) does not include either end. Offline checks are in `featurescript/v2.15_end_round.py`.

Not uploaded: the Onshape MCP session authenticated as jameset701@gmail.com, not the document account jameset701@outlook.com, so no `test_feature` or `create_geometry` call was made.
