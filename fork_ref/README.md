# Fox 36 lower reference

Reference geometry for modelling a front mudguard around a Fox 36 arch and lower legs. The frame used everywhere below is:

- origin at the axle centre
- +Z along the stanchion axis, toward the crown
- +X forward
- +Y to the rider's left

The stanchion axis is parallel to Z and sits at `X = −rake`, because Fox rake puts the axle ahead of the steerer.

No solid of the lowers is in this folder. Fox's public user-spec sheets are frame envelopes, explicitly not to scale, and they do not dimension the arch curve, the leg outer diameter, or the brace depth. The one free mesh that could be downloaded without an account is a clearance fender, not the fork. Its licence is CC BY-NC 4.0, and it is not the lowers, so the STL is not committed. The download URL is below.

Regenerate the plots with `python3 fork_ref/extract_envelope.py` (numpy, matplotlib, trimesh). If `fender.stl` from the Printables model is present at `/tmp/fox_downloads/unz/fender.stl` or `fork_ref/incoming/fender.stl`, the fender section is redrawn from the mesh.

## Which fork

The Amflow PL Carbon is specified with a **2026 Fox 36** (Performance, GRIP, or Factory, GRIP X2), 160 mm, 15×110, 44 mm rake, and the frame geometry table lists a 570 mm fork length. That is the fork a mudguard for this bike has to clear.

It is not an AWL. The 2025 AWL 36 HD is a different chassis: taller axle-to-crown, the pre-2026 post mount, and the arch generation from 2021 rather than the March 2025 redesign. A 2021–2025 Fox 36 does share one arch with the Fox 38 of those years. The 2026 Fox 36 does not. Fox's own mud-guard procedure still covers the 38 through 2026 and the 36 only through 2025.

## Sources

| Source | What is there | Licence / access | Used |
| --- | --- | --- | --- |
| [2026–2027 36 mm user spec](https://tech.ridefox.com/bike/spec-sheets/3119/2026-2027-36mm-user-specifications) | Dimensioned chassis envelope, 29 in sheet `MY26_FOX_36_29in_User_Spec.jpg` | Fox publishes it for frame fitment. The sheet is marked proprietary; the JPEGs are not copied into this repo. Numbers below were read off the sheet. | Primary envelope |
| [2025–2027 AWL / AWL HD user spec](https://tech.ridefox.com/bike/spec-sheets/2972/2025-2027-awl-or-awl-sport-and-awl-hd-or-awl-hd-sport-user-specifications) | 2025 AWL 36 HD 29 in | Same | Comparison |
| [2023–2025 36 mm user spec](https://tech.ridefox.com/bike/spec-sheets/2904/2023-2025-36mm-user-specifications) | 2023 36, 29 in | Same | Comparison |
| [2021–2022 36 mm user spec](https://tech.ridefox.com/bike/spec-sheets/1090/2021-2022-36mm-user-specifications) | 2021 36, 29 in | Same | Comparison |
| [2021–2026 38 mm user spec](https://tech.ridefox.com/bike/spec-sheets/1089/2021-2026-38mm-user-specifications) | 2021 38, 29 in sheet, still the sheet on that page | Same | Last-resort comparison |
| [2021–2025 36 / 2021–2026 38 mud-guard installation](https://tech.ridefox.com/bike/service-procedures/1054/2021-2025-36mm-and-2021-2026-38mm-mud-guard-installation) | How the pre-MY26 guard attaches: two screws into the back of the brace, two more on the leg pressure-release ports | Fox service page | Qualitative only |
| [Printables 1753735, sjc115, MY26 fender](https://www.printables.com/model/1753735-2026-fox-36-fork-fender-mud-guard-my26) | `fender.stl`, a clearance shell the author says uses the four 3 mm holes on the back of the crown | **CC BY-NC 4.0**. Downloaded with no account. STL not committed. | Clearance section only |
| [Sketchfab Fox 36, tho.blo](https://sketchfab.com/3d-models/fox-36-c8304b77214e48998c99b3625ed27ba9) | Scandy Pro phone scan, 4.6 M triangles | `isDownloadable: false`. No licence offered. | Not usable |
| GrabCAD, Thingiverse, Thangs, Cults3D, TraceParts, Onshape public search, GitHub | Service tools, volume spacers, paid or login-gated fenders, stylised display-bike lowers | GrabCAD, Thingiverse, Cults and TraceParts require an account to download. No account was created. MakerWorld's Fox 36 "lowers" are a display-bike remix whose licence forbids redistribution; they are not a measured fork. No public Onshape document of a Fox 36 turned up. | Not used as geometry |

Press check on the tables: a 2021 launch note said the 29 in 38 is 2.7 mm taller than the 2021 36. The sheets give 573.7 − 571.0 = 2.7 mm at 160 mm. The 2023 parts page says the new crown is 4 mm taller; the 2023 sheet is 575.0 versus 571.0.

## Axle-to-crown, 29 in

`A` is the axle-to-crown datum. The value in parentheses on the sheet, shown here as compressed, is the same dimension at full compression. On every row, `A_extended − A_compressed` matches the travel to within 0.3 mm, so both ends of `A` are the axle and a point on the crown/steerer assembly.

`B` is the second vertical datum. It shortens by the same travel, so its upper end is also on the crown assembly, not on the arch. The arch is part of the lowers and cannot change its distance from the axle when the fork compresses. `A − B` is 28.1 mm on the 2026 36, 27.8 mm on the AWL HD, and 25.0 mm on the 38. That is a crown stack, not an arch height.

Tolerance on `A` and `B` is the sheet's ±5.0 mm frame envelope (medium rider pressure), not a machining tolerance.

| Chassis | Travel | A extended | A compressed | B extended | B compressed | A − B |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 2026 Fox 36 | 140 | 556.0 | 416.0 | 527.9 | 387.9 | 28.1 |
| 2026 Fox 36 | 150 | 566.0 | 416.0 | 537.9 | 387.9 | 28.1 |
| 2026 Fox 36 | 160 | 576.0 | 416.0 | 547.9 | 387.9 | 28.1 |
| 2025 AWL 36 HD | 150 | 571.3 | 421.6 | 543.5 | 393.6 | 27.8 |
| 2025 AWL 36 HD | 160 | 581.3 | 421.6 | 553.5 | 393.6 | 27.8 |
| 2023 Fox 36 | 160 | 575.0 | 415.0 | — | — | — |
| 2021 Fox 36 | 160 | 571.0 | 411.0 | — | — | — |
| 2021 Fox 38 | 160 | 573.7 | 413.7 | 548.7 | 388.7 | 25.0 |

Full travel columns are in `dimensions.csv`. The 2021 and 2023 36 sheets, as cropped, tabulate `A` only.

Amflow's geometry table says fork length 570 mm. Fox's 2026 160 mm datum `A` is 576.0 mm. Those are not the same callout. Design to Fox's `A` unless Amflow publishes the datum.

## Hardware that is dimensioned

Read from the 2026 36 29 in sheet unless noted.

| Feature | Value | Confidence |
| --- | --- | --- |
| Axle bore, datum E | Ø 14.900 ± 0.025 mm | High. Same callout on the AWL HD sheet. |
| Hub counterbore for the 15×110 end cap | Ø 21.50 ± 0.40 mm | Medium. Read from the dropout detail; easy to confuse with a nearby callout. |
| Inside dropout faces | 110.75 ± 2.00 mm (2026 36). 110 ± 2 mm on the 2021 36, AWL HD, and 38 | Medium for the 110.75 leading digits (the tight crop showed `…5 ± 2.00` on a span drawn across the dropouts). High that this is hub spacing, not leg-centre spacing, and that the ±2 mm is the floating-axle allowance. |
| Rake | 44 ± 2.5 mm on the 2026 36 29 in sheet drawn here. 44 and 51 mm on the older 36 and on the 38. | High |
| Crown-race seat | Ø 39.808 ± 0.030 mm | High |
| Steerer outside diameter | Ø 28.58 ± 0.05 mm | High |
| Steerer above the crown on the sheet | 80 ± 3 mm. Note 10 also requires the upper headset cup 83.0 mm above the crown race. | High that both numbers are on the sheet; they are a frame envelope, not a measured stick-out of a particular fork. |
| 200 mm Boost post, hole spacing | 74.17 mm between the two M6 hole centres | Medium. Read from the post-mount detail. |
| Post-hole centre to inside dropout face | 1.02 ± 0.05 mm | Medium |
| Upper / lower post to inside hub face | 7.00 mm max / 6.15 mm max | Medium |
| Max 29 in tire the sheet will pass | 762 mm peak diameter, 732 mm edge diameter, 69.2 mm wide. Anything over 2.4 in wide needs 6.0 mm tire-to-crown clearance at full compression. A mudguard may use some of that. | High |
| Stanchion diameter | 36 mm (38 mm on the 38) | High as a product spec. This is not the lower-leg outside diameter. |

At 160 mm on the 2026 36, compressed `B` is 387.9 mm and the max tire peak is 381 mm, so the crown datum finishes 6.9 mm above the biggest tire the sheet allows. That matches the 6 mm tire-to-crown note and is the main reason `B` is being read as the underside of the crown.

## What a mudguard still does not have

These are the quantities the arch model needs. They are not on the Fox sheets, and they were not measured from a fork solid.

| Quantity | Result | Confidence |
| --- | --- | --- |
| Lower-leg outside diameter from the brace down 150 mm | Not published. Sheets draw the legs as plain cylinders and say the drawing is not to scale. | None |
| Leg centre spacing | Not published. Do not use the 110 mm dropout figure as a centre distance. | None |
| Inner face-to-face gap of the legs | Not published. The dropout faces are 110–110.75 mm apart at the axle. Above that, the casting is undescribed. | None |
| Arch underside, front view: width, radius, height above the axle | Not dimensioned. See the fender note. | None for a fork-metal number |
| Brace fore-aft depth and rear-face tilt | Not dimensioned. On 2021–2025 36s and 2021–2026 38s the guard screws into two threaded holes in the back of the brace (2 mm or 2.5 mm hex, 0.5 N·m) and the leg bleeders take the other two fasteners. The rear face is flat enough to be a bolt plane. No angle was measured. The 2026 36 does not use that interface. | Qualitative only for the old arch. None for the 2026 arch. |
| Gap from the brace rear face to the legs and up to the crown | The arch is fixed in the axle frame. The crown moves down by the travel. Minimum gap is at full compression, when `B` is at 387.9 mm (2026 36, 160 mm). The arch top has to be below that. Its actual height is not on the sheet, so the gap is unknown. | The crown motion is high. The gap number is none. |

## Fender clearance shell

`plots/fender_mount_section.png` is one station of [sjc115's MY26 fender](https://www.printables.com/model/1753735-2026-fox-36-fork-fender-mud-guard-my26), sectioned 55 mm along the STL's long axis. It is a modelled clearance part. The outer width is exactly 100.0 mm and the inner crown fits a circle of radius 59.9 mm with 0.13 mm residual, so those are construction numbers, not a scan of the casting.

Two pairs of holes, both cylinders parallel to the STL's long axis, both about a 3.2–3.5 mm clearance for an M3:

| Pair | Where along the fender | Centres in the fender's XY | Spacing | Hole diameter |
| --- | --- | --- | --- | ---: |
| Upper | 4–6 mm | (±30.0, 0.0) | 60.0 mm | 3.50 mm |
| Lower | 53–56 mm | (±45.5, −61.5) | 91.0 mm | 3.40 mm |

The author says these four holes are the crown's. They are not coplanar: the pairs are about 50 mm apart along the fender. Treat the spacings as a starting guess for the 2026 crown hole pattern, and check them on a fork before cutting metal. Confidence: medium-low.

The fender is not in the axle frame. Nothing in the STL locates the axle, so this section was not transformed onto the envelope plots.

## Plots

- `plots/front_section.png` — 2026 36, 160 mm, 29 in, viewed looking aft. Tire envelope, dropout faces, axle bore, `A` and `B` extended and compressed. The arch is not drawn.
- `plots/side_section.png` — same fork, 44 mm rake. Stanchion axis at `X = −44`. Tire crown radius 381 mm. Crown travel marked.
- `plots/top_section.png` — axle and dropout faces. The brace box is a label, not a measured footprint.
- `plots/fender_mount_section.png` — fender station described above.

## Model fidelity

There is no model of the lowers in this repo, so there is no decimated STL. Fidelity of the tabulated chassis numbers is high where two sheets and a published delta agree (the 4.0 mm crown step in 2023, the 2.7 mm 38-versus-36 step). Fidelity of anything about the arch casting, the leg profile, or the brace cross-section is none. The fender adds a low-fidelity clearance picture of a 2026 crown mount and nothing you should machine to.
