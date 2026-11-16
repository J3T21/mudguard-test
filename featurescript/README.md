# Mudguard V2 – Fox AWL HD Sport 36 mm (Amflow PR Carbon), 2-piece zip-tie front guard

FeatureScript (Onshape, std 3112) built with the Onshape FeatureScript MCP.

- `mudguard_v2.fs` – production feature **Mudguard V2 (Fox AWL 36)**; every parameter has a default.
- `mudguard_v2.1.fs` – same as mudguard_v2.fs (v2.1 snapshot); `mudguard_v2.0.fs` – first version.
- `stage1_shell.fs` – stage-1 shell-only test feature (intermediate).
- Onshape v2.4 (current): https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/f7a8708351f8db57d71d95a0/e/5b67a4974a5ea64603396eec
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
