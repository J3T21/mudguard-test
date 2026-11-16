# Mudguard V2 – Fox AWL HD Sport 36 mm (Amflow PR Carbon), 2-piece zip-tie front guard

FeatureScript (Onshape, std 3112) built with the Onshape FeatureScript MCP.

- `mudguard_v2.fs` – production feature **Mudguard V2 (Fox AWL 36)**; every parameter has a default.
- `mudguard_v2.1.fs` – same as mudguard_v2.fs (v2.1 snapshot); `mudguard_v2.0.fs` – first version.
- `stage1_shell.fs` – stage-1 shell-only test feature (intermediate).
- Onshape: https://cad.onshape.com/documents/b4a53e8390655572002075ff/w/5ce0853d809914970d796e9a/e/5b67a4974a5ea64603396eec (v2.1; v2.0 in workspace 626735a52f1ff5d642470481)

Coordinates: origin = axle, +Z along stanchion, +X forward, Y lateral; s = arc length on the outer top
centreline, s=0 brace centreline, s<0 forward. Outer top radius = PH1 - padThk (393.5).

Outputs two parts: **Guard Front A** (blade + brace saddle + 8 mm flange with captive M3 nyloc slots and
alignment groove) and **Guard Rear B** (leg ears + tail + 4 mm flange with tongue and gussets).
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
- Brace tie bosses 14.6 x 10.4 (overlap flange A side face, closes the slit).
- Leg ear R4 root fillets (parameter earFillet): only 10 of 22 junction edges fillet; rest left sharp.
- Extra loft stations s=-145, -95, -82.5: front width 110.3 (was 112.6).
- Known issue: brace tie slots at y=+/-30 cut into the R6 corner; outboard ~3 mm of each slot is blind (skin remains). See PR notes.
