# Mudguard V2 – Fox AWL HD Sport 36 mm (Amflow PR Carbon), 2-piece zip-tie front guard

FeatureScript (Onshape, std 3112) built with the Onshape FeatureScript MCP.

- `mudguard_v2.fs` – production feature **Mudguard V2 (Fox AWL 36)**; every parameter has a default.
- `mudguard_v2.1.fs` – same as mudguard_v2.fs (v2.1 snapshot); `mudguard_v2.0.fs` – first version.
- `stage1_shell.fs` – stage-1 shell-only test feature (intermediate).
- Onshape v2.7 (current): https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/3305653c27a2034bd2967e06/e/5b67a4974a5ea64603396eec
- Onshape v2.6: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/c72993a9e3ca77a1844350a9/e/5b67a4974a5ea64603396eec
- Onshape v2.5: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/cebfd1f1e404c8fa591d005d/e/5b67a4974a5ea64603396eec
- Onshape v2.4: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/f7a8708351f8db57d71d95a0/e/5b67a4974a5ea64603396eec
- Onshape v2.3: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/6e34d14a5ae5e724004e2dd1/e/5b67a4974a5ea64603396eec
- Onshape v2.2: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/9f4eddef365d02564c3ecaad/e/5b67a4974a5ea64603396eec
- Onshape (older): https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/5ce0853d809914970d796e9a/e/5b67a4974a5ea64603396eec (v2.1; v2.0 in workspace 626735a52f1ff5d642470481)

Coordinates: origin = axle, +Z along stanchion, +X forward, Y lateral; s = arc length on the outer top
centreline, s=0 brace centreline, s<0 forward. Outer top radius = PH1 - padThk (393.5).

Outputs two parts: **Guard Front A** (blade + brace saddle + 8 mm flange with captive M3 nyloc slots and
alignment groove) and **Guard Rear B** (integral flat leg wings + tail + 4 mm flange with tongue and gussets).
Mating plane at s = PH3/2 + padThk + flangeAThk = 20.5 mm. 3x M3 teardrop holes at y = -15/0/+15,
7 mm above the outer top surface.

Hardware: 3x M3x12 ISO 7380/DIN 912 A2, 3x DIN 125 washers, 3x DIN 985 nyloc, 4.8 mm zip ties.

Export STL/STEP from Onshape: right-click each part > Export.

## Printing note – teardrops
The M3 bolt-hole teardrops point **+Y**. Print both parts **on their side with the -Y side on the bed**
(A turned ~27/63 deg on the plate to fit). For other orientations change "Teardrop tip angle".

## v2.1 changes (Print Checker review)
- Tongue centred 1.3 mm above top (0.3-2.3), groove 0.05-2.55; nyloc slot floor 3.8 -> 1.25 mm web, no breakthrough.
- Nyloc slot 5.95 x 4.5 (walls 1.5 mating / 2.0 brace side); parameters nutWidth / nutThk.
- Brace tie bosses 14.6 x 10.4 (overlap flange A side face, closes the slit). (v2.4: back to 14 x 10.4, no longer near flange A.)
- Leg ear R4 root fillets (parameter earFillet): only 10 of 22 junction edges fillet; rest left sharp.
- Extra loft stations s=-145, -95, -82.5: front width 110.3 (was 112.6).
- (fixed in v2.2) brace tie slots at y=+/-30 were blind at the outboard ~3 mm.

## v2.2 changes
- Brace tie slot cut depth is now parameter tieCutDepth (default 20 mm): full 6 mm slot open through the R6 corner at both rows (verified by radial ray traces); 4 mm clear of flange A.
- Ear root fillets disabled (earFillet = 0): leg-ear saddles are being replaced by flat zip-tie wings in the next revision.

## v2.3 changes – integral flat leg wings (replaces the leg-ear saddles)
- One flat 2.5 mm wing per side (wingThk), parallel to the wheel, centred s=+45 (legTieOffset), leg-side face at
  |y| = PH2/2 - padThk (48.5), tyre-side face 46.0. Paddle 40 wide (s+25..+65), root s+21..+77 at d=12, tip d=112,
  i.e. 100 mm of wing below the root. Joined to the 19 deg side wall by a solid wedge joggle (d 12..28).
- Tie slots 6.0 x 2.4 (long side along the leg) at s+34/+56, rows d 48/74/100 (use 48 and 100, 74 spare),
  0.8 mm strap channels on the tyre side. Put zip-tie heads on the rear-outboard side of the leg.
- All wing values are parameters (wingThk, wingWidth, wingRootFront/Rear, wingRootDepth, wingFlareDepth,
  wingTipDepth, wingCornerR, wingJoggleDepth, wingRow1-3, wingSlotPitch, strapChannel).
- Tyre clearance to the wing plates 11.4 mm (69.2 mm max tyre) / 14.25 mm (63.5 mm Assegai) per side.
- Print B on its side (one wing flat on the bed, -Y down), supports under the shell and the upper wing.
- render_view_v2.3.fs = wrapper feature that rotates the model 135 deg for the behind/below renders.

## v2.4 changes - long rear segment, flange between the brace tie rows
- Split flipped: **frontLen 165** (blade s -165..0), **tailLen 240** (s 0..+240), total 405 unchanged.
  Part A (front) = s -165..+20.5, part B (rear, with the wings) = s +20.5..+240, so B is the longer piece.
- Stations: tip zone + kicker anchored to the tip, the 110 mm plateau shortened, flare to the 78 mm waist anchored
  to the brace (s -70..-15), 84 mm at the legs, tail tapers to 80 x 22 at s+240. Front max width 110.2.
- Brace tie rows are parameters: braceTieFront (14 -> s=-14, part A) and braceTieRear (29 -> s=+29, part B).
- Flange stack s +12.5..+24.5 now lies between the two tie rows, towards the rear row: A's front face butts the
  brace rear face, rear-row slot sits 3.3 mm behind B's flange. The flanges are only 46 wide (|y|<=23), so the
  rear strap (|y| 27..33) runs down past the flange ends instead of through them.
- Trade-off: the rear tie no longer hugs the brace rear face; it runs from the brace top-rear edge back ~18 mm
  to the slot. Fore-aft location now comes from flange A bearing on the brace rear face plus the front tie
  hugging the brace front face; the rear tie mainly pulls B (and the join) up against the brace. The rear
  strap loop is ~20 mm longer - use 300 mm+ ties.
- Wings unchanged at s=+45 measured from the brace centre (the leg position is a fork dimension and does not
  move with the tie row), i.e. 16 mm behind the rear tie row.
- Bounding boxes: A 182.5 x 110.2 x 72.9, B 206.4 x 97.0 x 128.0 - both fit 250 on their side without rotation.
- render_view_v2.4.fs = behind/below render wrapper.

## v2.5 changes - fitted to James's measured legs
- Leg model from measurements (below the brace underside): inner gap 85 @ 40 mm, 100 @ 90 mm and below; OD 60 / 45;
  centre spacing 145. New params legGapUpper (85), legTaperTop (40), legTaperEnd (90); PH2 = 100, PH6 = 45.
- Wings: still flat 2.5 mm plates at |y| 46.0-48.5, centred on the leg axis (s+45). Slot rows now 85 / 110 / 125 mm
  below the brace (params are measured from the brace underside), tip 135 mm. Above ~75 mm the plate is relieved
  around the tapered leg (+1.5 mm pad) and joins the guard through a strip behind the leg (root s+50..+85).
  A tilted paddle following the taper was tried first but left only 5.4 mm to a 69.2 mm tyre at the ~50 mm pinch.
- Guard width at the legs 84 -> 76 (legZoneWidth) so the walls clear the 85 mm gap.
- Clearances: wings 11.4 / 14.25, part B min 9.09 / 10.31 (max tyre / Assegai). B 206.4 x 149.5 x 97 - fits on its side.
- Brace geometry unchanged (photos not available to the build agent) - see refs/fork_photos.md.

## v2.6 changes - near-constant width
- Width 82 mm along the whole 405 mm (params guardWidth / frontWidth / tailWidth, all 82). The 110 mm front flare is gone.
- 82 is the maximum at the leg axis: measured 85 mm leg gap minus 1.5 mm each side. Walls are 32 mm deep under the brace
  and between the legs so the top flat under the brace stays PH5 = 60.
- Leg model: gap held at 85 from the brace underside to 40 mm down (new param legGapTop, an assumption - confirm on the bike).
- Clearances: A 8.28, B 8.72 mm to a 69.2 mm tyre; wings 11.4. A 182.5 x 82 x 71.1, B 206.6 x 97 x 149.5 - both fit 250.
- Reference widths: refs/width_research_v2.6.md.

## v2.7 changes
- PH3 (brace fore-aft depth) 22 -> 20 from James's ruler photo; flange stack moves 1 mm forward (mating plane s=19.5).
- Front brace tie row moved back to s=-12.5 (slot edge 1.3 mm ahead of the brace face); rear row s=+28.
- Width: 90 mm front blade and tail like the Mudhugger EVO/FRX, 82 mm under the brace and through the leg zone (s -15..+60),
  transitions 90->86->82 (s -70..-15) and 82->88->90 (s +60..+129).
- Tyre clearance A 8.69 / B 8.50 mm (69.2 tyre). A 181.5 x 90 x 71.1, B 207.5 x 97 x 149.6 - both fit 250.

## v2.8 changes - Mudhugger-style flush leg sheets
- Leg mounts simplified: flat 2.5 mm vertical sheets carry on flush from the guard side wall in the lower-leg inner-face plane (outer |y| 48.5), s 17..73 (56 wide, centred on the leg at s 45), down to 135 mm below the brace. Strap slot pairs at 90/110/125 mm. The design relies on the plastic flexing.
- A rectangular seal-bulge window (s 25..65, 12..84 mm below the brace) goes through the wall and sheet, leaving 8 mm strips in front and behind.
- Because the sheet is flush with the inner-leg plane, the guard is wider round the tyre: 97 mm in the leg zone, front and tail (v2.7: 82 / 90). The saddle under the brace is unchanged (82 mm, 60 top flat).
- Tyre clearance (69.2 max / Assegai): A 8.17/9.70, B 11.40/14.25, sheets 11.22/12.94 (v2.7: A 8.69/10.22, B 8.50/10.04).
- A 181.5 x 97.1 x 71.1; B 208.5 x 98.5 x 149.6 (208.5 x 149.6 on its side, so it fits the P1S).
- Files: `mudguard_v2.8.fs`, `render_view_v2.8.fs`, `verification_v2.8.txt`, `renders/mudguard_v2.8_*`.

## v2.9 changes - longer rear
- The rear segment behind the brace goes from 240 to 315 mm (arc from the brace centre). The front stays at 165, giving rear:front 1.91, close to the Mudhugger EVO's 1.84 but with a total of 480 rather than 525.
- 315 is the longest tail that still lets B print on its side (-Y down, sheets and flange as before), turned about 27° on the bed: footprint 244.0 x 241.3, height 98.5. 320 mm would need 248.4.
- Parts: A 181.5 x 97.1 x 71.1 (arc 184.5); B 265.5 x 98.5 x 149.6 (arc 295.5).
- Widths, sheets, window, ties, flange and clearances are unchanged from v2.8.
- Down-tube clearance at full compression is only an estimate (~20 mm nominal at s 93-168 behind the brace). Check on the bike. The extra length beyond 240 curves away from the frame.
- Files: `mudguard_v2.9.fs`, `verification_v2.9.txt`, `renders/mudguard_v2.9_*`.
