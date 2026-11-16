#!/usr/bin/env python3
"""Axle-frame envelope for Fox 36 / AWL HD / 38 lowers.

The numbers below were read from Fox's public user-spec sheets (not-to-scale
frame drawings). No anonymously downloadable solid of the lowers was found, so
this script does not invent an arch curve, a leg outer-diameter profile, or an
STL. If a mesh is later placed at fork_ref/incoming/*.(stl|step|stp), trimesh
(and cadquery, for STEP) will section it in the same frame and write extra
plots. Until then the three PNGs are the dimensioned envelope only.

Frame: origin at the axle centre, +Z along the stanchion axis (toward the
crown), +X forward, +Y to the rider's left. Right-handed. The stanchion axis
is parallel to Z and sits at X = -rake, because Fox rake places the axle
forward of the steerer / stanchion axis.
"""

from __future__ import annotations

import csv
import json
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
from matplotlib.patches import Circle, FancyBboxPatch

ROOT = Path(__file__).resolve().parent
PLOT_DIR = ROOT / "plots"
INCOMING = ROOT / "incoming"

# Travel columns are millimetres. Parenthetical values on the sheets are the
# same dimension with the fork fully compressed. Tolerance on A and B is the
# sheet's frame-design envelope (±5.0 mm), not a machining tolerance.
CHASSIS = {
    "my26_36_29": {
        "title": "2026 Fox 36, 29 in",
        "url": "https://tech.ridefox.com/bike/spec-sheets/3119/2026-2027-36mm-user-specifications",
        "sheet": "MY26_FOX_36_29in_User_Spec.jpg",
        "travels": (140, 150, 160),
        "A_ext": (556.0, 566.0, 576.0),
        "A_comp": (416.0, 416.0, 416.0),
        "B_ext": (527.9, 537.9, 547.9),
        "B_comp": (387.9, 387.9, 387.9),
        "rakes_mm": (44.0,),
        "rake_tol": 2.5,
        "dropout_spacing": 110.75,
        "dropout_tol": 2.0,
        "post_mount": "200 mm Boost",
        "arch_family": "MY26 generative arch (front of the lowers). Not the 2021 arch.",
    },
    "awl_hd_36_29": {
        "title": "2025 Fox AWL 36 HD, 29 in",
        "url": "https://tech.ridefox.com/bike/spec-sheets/2972/2025-2027-awl-or-awl-sport-and-awl-hd-or-awl-hd-sport-user-specifications",
        "sheet": "MY25_FOX_AWL_HD_29in_User_Spec.jpg",
        "travels": (130, 140, 150, 160),
        "A_ext": (551.3, 561.3, 571.3, 581.3),
        "A_comp": (421.6, 421.6, 421.6, 421.6),
        "B_ext": (523.5, 533.5, 543.5, 553.5),
        "B_comp": (393.6, 393.6, 393.6, 393.6),
        "rakes_mm": (44.0, 51.0),
        "rake_tol": 2.5,
        "dropout_spacing": 110.0,
        "dropout_tol": 2.0,
        "post_mount": "180 mm class (sheet still uses the pre-MY26 post, not the 200 mm MY26 pattern)",
        "arch_family": "Pre-MY26 36 chassis. A2C and post mount do not match the 2026 36.",
    },
    "y2023_36_29": {
        "title": "2023 Fox 36, 29 in",
        "url": "https://tech.ridefox.com/bike/spec-sheets/2904/2023-2025-36mm-user-specifications",
        "sheet": "2023_FOX_36_29in_USER_SPEC_RevA.jpg",
        "travels": (130, 140, 150, 160, 170),
        "A_ext": (545.0, 555.0, 565.0, 575.0, 585.0),
        "A_comp": (415.0, 415.0, 415.0, 415.0, 415.0),
        "B_ext": None,
        "B_comp": None,
        "rakes_mm": (44.0, 51.0),
        "rake_tol": 2.5,
        "dropout_spacing": 110.0,
        "dropout_tol": 2.0,
        "post_mount": "180 mm Boost",
        "arch_family": "2021 arch, crowns 4.0 mm taller than 2021 (A and compressed A both +4.0 mm).",
    },
    "y2021_36_29": {
        "title": "2021 Fox 36, 29 in",
        "url": "https://tech.ridefox.com/bike/spec-sheets/1090/2021-2022-36mm-user-specifications",
        "sheet": "2021_Fox_36_29in_USER_SPEC_RevB.jpg",
        "travels": (130, 140, 150, 160, 170),
        "A_ext": (541.0, 551.0, 561.0, 571.0, 581.0),
        "A_comp": (411.0, 411.0, 411.0, 411.0, 411.0),
        "B_ext": None,
        "B_comp": None,
        "rakes_mm": (44.0, 51.0),
        "rake_tol": 2.5,
        "dropout_spacing": 110.0,
        "dropout_tol": 2.0,
        "post_mount": "180 mm Boost",
        "arch_family": "First year of the arch shared with the 38. Not the MY26 arch.",
    },
    "y2021_38_29": {
        "title": "2021 Fox 38, 29 in (sheet still current on the 2021–2026 38 page)",
        "url": "https://tech.ridefox.com/bike/spec-sheets/1089/2021-2026-38mm-user-specifications",
        "sheet": "2021_38_29in_user_spec_web-9.12.24.jpg",
        "travels": (150, 160, 170, 180),
        "A_ext": (563.7, 573.7, 583.7, 593.7),
        "A_comp": (413.7, 413.7, 413.7, 413.7),
        "B_ext": (538.7, 548.7, 558.7, 568.7),
        "B_comp": (388.7, 388.7, 388.7, 388.7),
        "rakes_mm": (44.0, 51.0),
        "rake_tol": 2.5,
        "dropout_spacing": 110.0,
        "dropout_tol": 2.0,
        "post_mount": "180 mm direct",
        "arch_family": "2021 arch family, kept on the 38 through 2026. Stanchions are 38 mm, not 36 mm.",
    },
}

# Shared, sheet-dimensioned hardware. Confidence is discussed in the README.
HARDWARE = {
    "axle_bore_diameter_mm": 14.900,
    "axle_bore_tol_mm": 0.025,
    "hub_counterbore_diameter_mm": 21.50,
    "hub_counterbore_tol_mm": 0.40,
    "stanchion_diameter_mm": 36.0,  # product specification, not a lower-leg OD
    "crown_race_seat_diameter_mm": 39.808,
    "crown_race_seat_tol_mm": 0.030,
    "steerer_od_mm": 28.58,
    "steerer_od_tol_mm": 0.05,
    "steerer_above_crown_mm": 80.0,
    "steerer_above_crown_tol_mm": 3.0,
    "headset_stack_above_crown_race_mm": 83.0,
    "my26_post_spacing_mm": 74.17,
    "my26_post_to_dropout_face_mm": 1.02,
    "my26_post_to_dropout_face_tol_mm": 0.05,
    "my26_upper_post_to_hub_face_max_mm": 7.00,
    "my26_lower_post_to_hub_face_max_mm": 6.15,
    "my26_tire_peak_diameter_mm": 762.0,
    "my26_tire_edge_diameter_mm": 732.0,
    "my26_tire_width_mm": 69.2,
    "tire_to_crown_clearance_min_mm": 6.0,
}


def _idx(spec: dict, travel: int) -> int:
    return spec["travels"].index(travel)


def write_table(path: Path) -> None:
    rows = []
    for key, spec in CHASSIS.items():
        for i, travel in enumerate(spec["travels"]):
            a_ext = spec["A_ext"][i]
            a_comp = spec["A_comp"][i]
            b_ext = None if spec["B_ext"] is None else spec["B_ext"][i]
            b_comp = None if spec["B_comp"] is None else spec["B_comp"][i]
            rows.append(
                {
                    "chassis": key,
                    "title": spec["title"],
                    "travel_mm": travel,
                    "A_extended_mm": a_ext,
                    "A_compressed_mm": a_comp,
                    "A_minus_travel_mm": round(a_ext - a_comp, 1),
                    "B_extended_mm": "" if b_ext is None else b_ext,
                    "B_compressed_mm": "" if b_comp is None else b_comp,
                    "A_minus_B_mm": "" if b_ext is None else round(a_ext - b_ext, 1),
                    "dropout_spacing_mm": spec["dropout_spacing"],
                    "rakes_mm": " ".join(str(r) for r in spec["rakes_mm"]),
                }
            )
    with path.open("w", newline="") as fh:
        writer = csv.DictWriter(fh, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def _style():
    plt.rcParams.update(
        {
            "font.size": 10,
            "axes.titlesize": 12,
            "axes.labelsize": 11,
            "figure.facecolor": "white",
            "axes.facecolor": "white",
            "axes.edgecolor": "#222",
            "text.color": "#1a1a1a",
        }
    )


def plot_front(path: Path) -> None:
    """YZ section, viewed looking aft. 2026 36, 160 mm, 29 in."""
    spec = CHASSIS["my26_36_29"]
    i = _idx(spec, 160)
    half = spec["dropout_spacing"] / 2.0
    tire_r = HARDWARE["my26_tire_peak_diameter_mm"] / 2.0
    edge_r = HARDWARE["my26_tire_edge_diameter_mm"] / 2.0
    half_w = HARDWARE["my26_tire_width_mm"] / 2.0

    fig, ax = plt.subplots(figsize=(11.2, 8.2))
    # Tire front silhouette: peak at centre, edge diameter at the sidewall.
    y = np.linspace(-half_w, half_w, 200)
    # Rounded crown between the published peak and edge radii.
    z_top = edge_r + (tire_r - edge_r) * np.cos(np.pi * y / (2 * half_w)) ** 2
    ax.fill_between(y, 0, z_top, color="#d9d9d9", alpha=0.85, label="Max 29 in tire (762 peak / 732 edge / 69.2 wide)")
    ax.plot(y, z_top, color="#555", lw=1.2)
    ax.plot([-half_w, -half_w, half_w, half_w], [0, edge_r, edge_r, 0], color="#555", lw=0.6, ls=":")

    # Dropout inner faces and axle bore. Faces are dimensioned; the leg OD is not,
    # so the faces are drawn as short ticks at the axle, not as full leg walls.
    for s in (-1, 1):
        ax.plot([s * half, s * half], [-18, 28], color="#0b5cab", lw=2.0)
    ax.annotate(
        f"inside dropout faces\n{spec['dropout_spacing']:.2f} ± {spec['dropout_tol']:.0f} mm",
        xy=(-half, 22),
        xytext=(-78, 70),
        arrowprops=dict(arrowstyle="->", color="#0b5cab"),
        color="#0b5cab",
        ha="center",
    )
    bore_r = HARDWARE["axle_bore_diameter_mm"] / 2.0
    ax.add_patch(Circle((0, 0), bore_r, fill=False, ec="#111", lw=1.4))
    ax.plot(0, 0, "k+", ms=8)
    ax.annotate(
        f"axle bore Ø {HARDWARE['axle_bore_diameter_mm']:.3f} ± {HARDWARE['axle_bore_tol_mm']:.3f}\n(datum E)",
        xy=(bore_r, 0),
        xytext=(28, -55),
        arrowprops=dict(arrowstyle="->", color="#111"),
        fontsize=9,
    )

    a_ext, a_comp = spec["A_ext"][i], spec["A_comp"][i]
    b_ext, b_comp = spec["B_ext"][i], spec["B_comp"][i]
    labels = (
        (a_ext, "#9b1c1c", "-", f"A extended  {a_ext:.1f}"),
        (b_ext, "#6d28d9", "-", f"B extended  {b_ext:.1f}   (A−B = {a_ext - b_ext:.1f})"),
        (a_comp, "#c2410c", "--", f"A compressed  {a_comp:.1f}"),
        (b_comp, "#7c3aed", "--", f"B compressed  {b_comp:.1f}"),
    )
    for z, color, ls, text in labels:
        ax.axhline(z, color=color, lw=1.15, ls=ls)
        ax.text(72, z + (4 if "extended" in text else -7), text, color=color, fontsize=8, va="center")

    gap = b_comp - tire_r
    ax.text(
        155,
        190,
        "Not on the sheet, so not drawn:\n"
        "arch underside curve\n"
        "leg outer-diameter profile\n"
        "leg centre spacing\n"
        "brace depth and rear-face tilt\n\n"
        f"B compressed sits {gap:.1f} mm above\n"
        "the max tire peak. The sheet's\n"
        "minimum tire-to-crown gap is 6 mm,\n"
        "so B is consistent with the crown\n"
        "underside, not with the arch.\n"
        "The arch is fixed to the axle;\n"
        "its Z is unpublished.",
        fontsize=8,
        color="#7c2d12",
        va="center",
        bbox=dict(boxstyle="round", fc="#fffbeb", ec="#f59e0b", pad=0.4),
    )

    ax.set_xlim(-150, 250)
    ax.set_ylim(-80, 620)
    ax.set_aspect("equal")
    ax.set_xlabel("+Y  rider's left  (mm)")
    ax.set_ylabel("+Z  stanchion axis, axle at 0  (mm)")
    ax.set_title("Front section  ·  2026 Fox 36  29 in  160 mm\nviewed looking aft (−X). Arch curve is not drawn.")
    ax.grid(True, lw=0.3, color="#ddd")
    ax.legend(loc="upper left", frameon=False, fontsize=8)
    fig.tight_layout()
    fig.savefig(path, dpi=140)
    plt.close(fig)


def plot_side(path: Path) -> None:
    spec = CHASSIS["my26_36_29"]
    i = _idx(spec, 160)
    rake = 44.0
    tire_r = HARDWARE["my26_tire_peak_diameter_mm"] / 2.0
    fig, ax = plt.subplots(figsize=(8.4, 10.2))

    theta = np.linspace(0, np.pi, 300)
    ax.plot(tire_r * np.sin(theta), tire_r * np.cos(theta), color="#555", lw=1.2, label="Max tire crown, R = 381 mm (Ø 762)")
    ax.fill_between(tire_r * np.sin(theta), 0, tire_r * np.cos(theta), color="#e5e5e5", alpha=0.8)

    # Stanchion axis. Axle is forward of it by the rake.
    ax.axvline(-rake, color="#0b5cab", lw=1.4, label=f"stanchion axis  X = −{rake:.0f} mm (44 mm rake)")
    ax.annotate(
        "",
        xy=(0, 40),
        xytext=(-rake, 40),
        arrowprops=dict(arrowstyle="<->", color="#0b5cab"),
    )
    ax.text(-rake / 2, 48, "rake 44 ± 2.5", ha="center", color="#0b5cab", fontsize=9)

    a_ext, a_comp = spec["A_ext"][i], spec["A_comp"][i]
    b_ext, b_comp = spec["B_ext"][i], spec["B_comp"][i]
    for z, color, name in (
        (a_ext, "#9b1c1c", "A"),
        (a_comp, "#c2410c", "A comp"),
        (b_ext, "#6d28d9", "B"),
        (b_comp, "#7c3aed", "B comp"),
    ):
        ax.plot([-rake - 18, -rake + 18], [z, z], color=color, lw=2.0)
        ax.text(-rake + 22, z, f"{name}  {z:.1f}", color=color, va="center", fontsize=8)

    # Crown moves down the axis by the travel. Arch does not: it is on the lowers.
    ax.annotate(
        "",
        xy=(-rake, a_comp),
        xytext=(-rake, a_ext),
        arrowprops=dict(arrowstyle="<->", color="#9b1c1c"),
    )
    ax.text(-rake - 8, (a_ext + a_comp) / 2, "160 mm\ntravel", ha="right", va="center", color="#9b1c1c", fontsize=8)

    ax.plot(0, 0, "k+", ms=10)
    ax.text(8, -16, "axle", fontsize=9)
    ax.text(
        30,
        250,
        "Brace fore-aft depth,\nrear-face tilt, and the\ngap from brace to crown\nare not dimensioned.\n\nThe arch is fixed in\nthis axle frame.\nThe crown moves.",
        fontsize=9,
        color="#92400e",
        bbox=dict(boxstyle="round", fc="#fffbeb", ec="#f59e0b"),
    )
    ax.set_xlim(-120, 420)
    ax.set_ylim(-40, 640)
    ax.set_aspect("equal")
    ax.set_xlabel("+X forward  (mm)")
    ax.set_ylabel("+Z along stanchion axis  (mm)")
    ax.set_title("Side section  ·  2026 Fox 36  29 in  160 mm  ·  44 mm rake\nOrigin at the axle. Stanchion axis is the vertical blue line.")
    ax.grid(True, lw=0.3, color="#ddd")
    ax.legend(loc="upper right", frameon=False, fontsize=8)
    fig.tight_layout()
    fig.savefig(path, dpi=140)
    plt.close(fig)


def plot_top(path: Path) -> None:
    spec = CHASSIS["my26_36_29"]
    half = spec["dropout_spacing"] / 2.0
    rake = 44.0
    fig, ax = plt.subplots(figsize=(10.2, 6.4))
    # Axle, a line through the origin along Y.
    ax.plot([-half - 15, half + 15], [0, 0], color="#111", lw=3, solid_capstyle="round", label="axle (datum E)")
    ax.plot(0, 0, "k+", ms=10)
    for s in (-1, 1):
        ax.plot([s * half, s * half], [-8, 8], color="#0b5cab", lw=2)
    ax.annotate(
        f"dropout inner faces  {spec['dropout_spacing']:.2f} ± 2 mm",
        xy=(half, 0),
        xytext=(20, -38),
        arrowprops=dict(arrowstyle="->", color="#0b5cab"),
        color="#0b5cab",
    )
    # Steerer / stanchion axis footprint.
    ax.plot(0, -rake, "o", color="#0b5cab", ms=8)
    ax.annotate(
        "stanchion axis\nX = −44 mm, Y = 0\n(44 mm behind the axle)",
        xy=(0, -rake),
        xytext=(-78, -82),
        arrowprops=dict(arrowstyle="->", color="#0b5cab"),
        color="#0b5cab",
        ha="center",
        fontsize=8,
    )
    # Unknown brace footprint, deliberately not sized.
    ax.add_patch(
        FancyBboxPatch(
            (-40, 8),
            80,
            36,
            boxstyle="round,pad=0.4,rounding_size=2",
            fc="#fffbeb",
            ec="#f59e0b",
            lw=1.2,
            ls="--",
        )
    )
    ax.text(
        0,
        26,
        "brace footprint\nNOT DIMENSIONED\n(no fore-aft depth)",
        ha="center",
        va="center",
        fontsize=9,
        color="#92400e",
    )
    ax.set_xlim(-100, 100)
    ax.set_ylim(-100, 60)
    ax.set_aspect("equal")
    ax.set_xlabel("+Y rider's left  (mm)")
    ax.set_ylabel("+X forward  (mm)")
    ax.set_title("Top section  ·  2026 Fox 36  29 in\nLooking down the stanchion axis. Brace outline is a label, not a measured shape.")
    ax.grid(True, lw=0.3, color="#ddd")
    ax.legend(loc="upper right", frameon=False)
    fig.tight_layout()
    fig.savefig(path, dpi=140)
    plt.close(fig)


def try_mesh() -> dict:
    """Section a mesh if one was dropped in incoming/. Never fabricate one."""
    hits = []
    if INCOMING.exists():
        hits = sorted(INCOMING.glob("*.stl")) + sorted(INCOMING.glob("*.step")) + sorted(INCOMING.glob("*.stp"))
    report = {"mesh_found": bool(hits), "files": [str(p) for p in hits]}
    if not hits:
        report["note"] = "No mesh in fork_ref/incoming. Envelope plots only."
        return report
    import trimesh

    mesh_path = hits[0]
    if mesh_path.suffix.lower() in {".step", ".stp"}:
        report["note"] = "STEP present but cadquery/OCP was not required to run; STL path is preferred."
        try:
            import cadquery as cq

            shape = cq.importers.importStep(str(mesh_path))
            report["step_solids"] = len(shape.solids().vals())
        except Exception as exc:  # noqa: BLE001
            report["step_error"] = str(exc)
        return report
    mesh = trimesh.load(mesh_path, force="mesh")
    report["vertices"] = int(len(mesh.vertices))
    report["extents_mm"] = [float(v) for v in mesh.extents]
    report["watertight"] = bool(mesh.is_watertight)
    return report


def plot_fender_clearance(stl_path: Path, out: Path) -> dict:
    """Section the CC BY-NC MY26 fender. This is a clearance part, not the fork."""
    import trimesh

    mesh = trimesh.load(stl_path, force="mesh")
    z_flange = 55.0
    lines = trimesh.intersections.mesh_plane(
        mesh, plane_normal=[0, 0, 1], plane_origin=[0, 0, z_flange]
    )
    fig, ax = plt.subplots(figsize=(8.2, 7.2))
    for seg in lines:
        ax.plot(seg[:, 0], seg[:, 1], color="#333", lw=0.7)
    # Published hole pairs, measured from circular sections of this STL.
    holes = [(-45.5, -61.5), (45.5, -61.5)]
    for x, y in holes:
        ax.add_patch(Circle((x, y), 1.70, fill=False, ec="#9b1c1c", lw=1.4))
    ax.plot([-45.5, 45.5], [-61.5, -61.5], color="#9b1c1c", lw=0.8)
    ax.text(0, -68, "lower pair  91.0 mm  ·  Ø 3.40", ha="center", color="#9b1c1c", fontsize=8)
    # R60 construction circle of the fender's own inner crown (not the fork).
    theta = np.linspace(0, np.pi, 200)
    cx, cy, radius = -0.1, -55.7, 59.9
    ax.plot(cx + radius * np.cos(theta), cy + radius * np.sin(theta), color="#0b5cab", lw=1.0, ls="--")
    ax.text(
        -58,
        -8,
        "inner crown\nR = 59.9 mm\n(fender, not fork)",
        color="#0b5cab",
        fontsize=8,
        ha="left",
    )
    ax.set_aspect("equal")
    ax.set_xlabel("fender X, lateral (mm)")
    ax.set_ylabel("fender Y (mm)")
    ax.set_title("MY26 fender section, 55 mm along its length\nClearance shell by sjc115 — not in the axle frame")
    ax.grid(True, lw=0.3, color="#ddd")
    fig.tight_layout()
    fig.savefig(out, dpi=140)
    plt.close(fig)
    return {
        "source_stl": str(stl_path),
        "licence": "CC BY-NC 4.0",
        "author": "sjc115",
        "url": "https://www.printables.com/model/1753735-2026-fox-36-fork-fender-mud-guard-my26",
        "note": "Clearance fender, not the fork. Outline is built to round numbers (100 mm wide, R≈60 mm). Hole centres are the only features likely copied from the crown.",
        "lower_hole_pair_mm": [[-45.5, -61.5], [45.5, -61.5]],
        "lower_pair_spacing_mm": 91.0,
        "lower_hole_diameter_mm": 3.40,
        "upper_hole_pair_mm": [[-30.0, 0.0], [30.0, 0.0]],
        "upper_pair_spacing_mm": 60.0,
        "upper_hole_diameter_mm": 3.50,
        "upper_pair_station_mm": [4.0, 6.0],
        "lower_pair_station_mm": [53.0, 56.0],
        "inner_crown_radius_mm": 59.9,
        "inner_crown_fit_residual_mm": 0.13,
        "outer_width_mm": 100.0,
        "length_mm": 242.3,
    }


def main() -> None:
    _style()
    PLOT_DIR.mkdir(parents=True, exist_ok=True)
    write_table(ROOT / "dimensions.csv")
    plot_front(PLOT_DIR / "front_section.png")
    plot_side(PLOT_DIR / "side_section.png")
    plot_top(PLOT_DIR / "top_section.png")
    mesh_report = try_mesh()
    fender_candidates = [
        Path("/tmp/fox_downloads/unz/fender.stl"),
        INCOMING / "fender.stl",
    ]
    fender = next((p for p in fender_candidates if p.exists()), None)
    if fender is not None:
        info = plot_fender_clearance(fender, PLOT_DIR / "fender_mount_section.png")
        (ROOT / "fender_clearance.json").write_text(json.dumps(info, indent=2) + "\n")
        mesh_report["fender"] = info["url"]
    (ROOT / "mesh_report.json").write_text(json.dumps(mesh_report, indent=2) + "\n")
    print("wrote", PLOT_DIR)
    print(json.dumps(mesh_report, indent=2))


if __name__ == "__main__":
    main()
