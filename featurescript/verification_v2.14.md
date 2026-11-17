# v2.14 verification (Onshape test_feature, new account, library 3112)

Sculpted under-brace section "C1": flat top ±23, R18 corners, vertical walls at ±41 (82 wide), depth 30.49
(= leg-zone depth, removes the wall dip at the s 10→22 blend), s −15..+10. Front: 97 wide, 5° walls, R20, blend via s −35.
Tail and leg zone unchanged from v2.13 (19°, r6). Brace ties at y ±28 (±19 would put the rear slot inside A's flange).

Leg-sheet join (B): deep-loft patch over the sheet length so the outer surface there is one surface; trim guide 12 mm / floor d+11.5;
sub-1 mm edge fragments skipped for the crease / front-end fillets; final |y|>48.5 clip of B before the join fillets.

| check | v2.13 | v2.14 |
|---|---|---|
| solids | A 65.1 / B 114.3 cm³ | A 65.3 / B 114.2 cm³ (1 + 1) |
| tyre clearance (69.2 / 63.5 mm) | A 8.17/9.70, B 8.19/9.72 | A 9.44/10.89, B 9.44/10.90 |
| B width | 97.0 | 97.0, nothing beyond the leg plate |
| leg clearance | 0.59 | 0.59, no clashes |
| bolts / ties / joint step | 0 hits / clear / — | 0 hits / all clear / 0.011 mm |
| B bed fit (27° class) | 244 × 241.3 | 244 × 241.3 |
| leg plate faces | 2 (7069 mm²) | 4 (7045 mm²; two tiny residual faces; coupon 2) |
| outside crease R4 | R4 | R4 (2 × 63 mm edges) |
| inside R4 | R4 | R4 |
| sheet front-end R1 | R1 | not applied (left sharp, ≤0.2 mm step) |
| sheet rear-end R1 | failed (sharp) | not applied (left sharp) |
| trim removed | 3843 mm³ | 3978 mm³ (normal) |
| coupon | A 17.22 / B 51.96 cm³ | A 17.45 / B 51.87 cm³, probes/slots OK |

Print orientations unchanged (A on its flange face, B on its leg plate). Full console: verification_v2.14.txt.
A later variant (front-only deep trim + combined front-end rounds) restored the rear R1 but lost the outside R4, so it was not built.
