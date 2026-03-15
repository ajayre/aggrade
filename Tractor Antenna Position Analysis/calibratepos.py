"""
Calibrate GPS antenna offset from two opposing poses (pole-in-line method).

Inputs:
  T: rear track width (cm) - distance between left and right rear hub centers
  heading1, heading2: tractor heading in degrees (e.g. from IMU) at pose 1 and 2
  lat1, lon1: antenna GPS position (lat, lon) at pose 1
  lat2, lon2: antenna GPS position (lat, lon) at pose 2

  (No ground point G needed: the hub-on-pole constraint only requires that the
   same point on the pole was used in both poses; that fixes t_2 - t_1 and the
   solution does not use G or any distance D.)

Output:
  Antenna offset (x_a, y_a) in cm in tractor frame:
    - origin at rear axle center
    - y forward, x right (left = negative x)
    - left hub at (-T/2, 0), right hub at (+T/2, 0)
"""
import argparse
import math
import os


def _radii_m(lat0_rad):
    """Meridian and parallel radii (m) at latitude (rad). WGS84."""
    a = 6378137.0
    f = 1 / 298.257223563
    e2 = 2 * f - f * f
    sin_lat0 = math.sin(lat0_rad)
    N = a / math.sqrt(1 - e2 * sin_lat0 * sin_lat0)
    M = N * (1 - e2) / (1 - e2 * sin_lat0 * sin_lat0)
    return M, N


def latlon_to_local_m(lat_deg, lon_deg, lat0_deg, lon0_deg):
    """Convert (lat, lon) to local flat (east, north) in meters. WGS84 approx."""
    deg2rad = math.pi / 180
    lat0 = lat0_deg * deg2rad
    lon0 = lon0_deg * deg2rad
    lat = lat_deg * deg2rad
    lon = lon_deg * deg2rad
    M, N = _radii_m(lat0)
    north = M * (lat - lat0)
    east = N * math.cos(lat0) * (lon - lon0)
    return east, north


def local_m_to_latlon(east_m, north_m, lat0_deg, lon0_deg):
    """Convert local (east, north) in meters to (lat_deg, lon_deg). WGS84 approx."""
    deg2rad = math.pi / 180
    lat0 = lat0_deg * deg2rad
    lon0 = lon0_deg * deg2rad
    M, N = _radii_m(lat0)
    lat_rad = lat0 + north_m / M
    lon_rad = lon0 + east_m / (N * math.cos(lat0))
    return math.degrees(lat_rad), math.degrees(lon_rad)


def rotation_2d(angle_deg):
    """Rotation matrix (2x2) for angle in degrees, counterclockwise."""
    c = math.cos(math.radians(angle_deg))
    s = math.sin(math.radians(angle_deg))
    return [[c, -s], [s, c]]


def solve_antenna_offset_cm(T_cm, heading1_deg, heading2_deg, lat1, lon1, lat2, lon2, G_cm=None):
    """
    Solve for antenna offset p_a = (x_a, y_a) in cm.

    Convention: pose 1 = left hub on pole ref, pose 2 = right hub on same ref.
    Tractor frame: y forward, x right; left hub (-T/2, 0), right hub (+T/2, 0).

    If G_cm is given (east_cm, north_cm) in the same local frame as antenna positions
    (origin = mean of pos1, pos2), the second pose is corrected to exactly 180° from
    the first by rotating P2 and t2 around G, then the 0°/180° solution is used.
    This corrects for operator heading error.
    """
    # Local flat origin = mean of the two antenna positions
    lat0 = (lat1 + lat2) / 2
    lon0 = (lon1 + lon2) / 2
    e1, n1 = latlon_to_local_m(lat1, lon1, lat0, lon0)
    e2, n2 = latlon_to_local_m(lat2, lon2, lat0, lon0)
    P1 = (e1 * 100, n1 * 100)   # cm
    P2 = (e2 * 100, n2 * 100)   # cm

    # World frame: east = first component, north = second. Tractor +y = forward.
    R1 = rotation_2d(-heading1_deg)
    R2 = rotation_2d(-heading2_deg)

    h_A = (-T_cm / 2, 0)
    h_B = (T_cm / 2, 0)

    def mat_vec(M, v):
        return (M[0][0] * v[0] + M[0][1] * v[1], M[1][0] * v[0] + M[1][1] * v[1])

    t1 = (-R1[0][0] * h_A[0] - R1[0][1] * h_A[1], -R1[1][0] * h_A[0] - R1[1][1] * h_A[1])
    t2 = (-R2[0][0] * h_B[0] - R2[0][1] * h_B[1], -R2[1][0] * h_B[0] - R2[1][1] * h_B[1])

    # Optional: correct for heading error using G. Do NOT rotate measured P2. Assume
    # the second pose's heading should have been 180° and place both axle centers
    # so the hub is at G: t1 = G - R1*h_A, t2 = G - R2_corrected*h_B. Then heading
    # error does not create a spurious antenna offset.
    if G_cm is not None:
        heading2_deg = heading1_deg + 180
        R2 = rotation_2d(-heading2_deg)
        t1 = (
            G_cm[0] - (R1[0][0] * h_A[0] + R1[0][1] * h_A[1]),
            G_cm[1] - (R1[1][0] * h_A[0] + R1[1][1] * h_A[1]),
        )
        t2 = (
            G_cm[0] - (R2[0][0] * h_B[0] + R2[0][1] * h_B[1]),
            G_cm[1] - (R2[1][0] * h_B[0] + R2[1][1] * h_B[1]),
        )

    diff_P = (P1[0] - P2[0], P1[1] - P2[1])
    diff_t = (t1[0] - t2[0], t1[1] - t2[1])
    rhs = (diff_P[0] - diff_t[0], diff_P[1] - diff_t[1])
    R1_minus_R2 = [
        [R1[0][0] - R2[0][0], R1[0][1] - R2[0][1]],
        [R1[1][0] - R2[1][0], R1[1][1] - R2[1][1]],
    ]
    det = R1_minus_R2[0][0] * R1_minus_R2[1][1] - R1_minus_R2[0][1] * R1_minus_R2[1][0]
    inv = [
        [R1_minus_R2[1][1] / det, -R1_minus_R2[0][1] / det],
        [-R1_minus_R2[1][0] / det, R1_minus_R2[0][0] / det],
    ]
    x_a_cm = inv[0][0] * rhs[0] + inv[0][1] * rhs[1]
    y_a_cm = inv[1][0] * rhs[0] + inv[1][1] * rhs[1]
    return x_a_cm, y_a_cm


def main():
    parser = argparse.ArgumentParser(
        description="Compute antenna offset (cm) from T, headings, and two antenna lat/lon."
    )
    parser.add_argument("--T", type=float, required=True, help="Rear track width (mm)")
    parser.add_argument("--heading1", type=float, required=True, help="Heading pose 1 (deg)")
    parser.add_argument("--heading2", type=float, required=True, help="Heading pose 2 (deg)")
    parser.add_argument("--pos1", type=float, nargs=2, required=True, metavar=("LAT", "LON"),
                        help="Antenna lat lon at pose 1")
    parser.add_argument("--pos2", type=float, nargs=2, required=True, metavar=("LAT", "LON"),
                        help="Antenna lat lon at pose 2")
    parser.add_argument("--G", type=float, default=None, metavar="D_MM",
                        help="Hub for 180° correction: distance D_MM from origin to hub along pole (pose 1 axle). Origin = mean of antenna positions.")
    args = parser.parse_args()

    lat1, lon1 = args.pos1
    lat2, lon2 = args.pos2
    T_cm = args.T / 10.0  # mm -> cm
    G_cm = None
    if args.G is not None:
        D_mm = args.G
        th_rad = math.radians(args.heading1)
        G_cm = (D_mm / 10.0 * math.cos(th_rad), -D_mm / 10.0 * math.sin(th_rad))

    x_a, y_a = solve_antenna_offset_cm(
        T_cm, args.heading1, args.heading2,
        lat1, lon1, lat2, lon2,
        G_cm=G_cm,
    )
    print(f"Antenna offset (tractor frame, cm): x_a = {x_a:.3f},  y_a = {y_a:.3f}")
    print(f"  (x: right positive, left negative; y: forward positive)")


def _proof_examples():
    """
    Prove correct output with two concrete examples (zero GPS/hub error).
    Forward model: given T, p_a, theta1, theta2, compute antenna positions P1, P2
    in world (hub point = origin), then convert to lat/lon and run solver.
    """
    def mat_vec(M, v):
        return (M[0][0] * v[0] + M[0][1] * v[1], M[1][0] * v[0] + M[1][1] * v[1])

    lat0, lon0 = 45.0, -122.0  # arbitrary reference

    # ---- Example 1: North/South (0° / 180°), antenna 10 cm right, 40 cm forward ----
    T1 = 200.0  # cm
    x_a1, y_a1 = 10.0, 40.0  # cm
    h1_deg, h2_deg = 0.0, 180.0
    R1 = rotation_2d(-h1_deg)
    R2 = rotation_2d(-h2_deg)
    h_A = (-T1 / 2, 0)
    h_B = (T1 / 2, 0)
    t1 = (-R1[0][0] * h_A[0] - R1[0][1] * h_A[1], -R1[1][0] * h_A[0] - R1[1][1] * h_A[1])
    t2 = (-R2[0][0] * h_B[0] - R2[0][1] * h_B[1], -R2[1][0] * h_B[0] - R2[1][1] * h_B[1])
    p_a1 = (x_a1, y_a1)
    P1_cm = (mat_vec(R1, p_a1)[0] + t1[0], mat_vec(R1, p_a1)[1] + t1[1])
    P2_cm = (mat_vec(R2, p_a1)[0] + t2[0], mat_vec(R2, p_a1)[1] + t2[1])
    # Convert to meters then to lat/lon (origin = hub point; script uses mean of pos1/pos2 as origin)
    e1_m, n1_m = P1_cm[0] / 100, P1_cm[1] / 100
    e2_m, n2_m = P2_cm[0] / 100, P2_cm[1] / 100
    lat1, lon1 = local_m_to_latlon(e1_m, n1_m, lat0, lon0)
    lat2, lon2 = local_m_to_latlon(e2_m, n2_m, lat0, lon0)
    x_out, y_out = solve_antenna_offset_cm(T1, h1_deg, h2_deg, lat1, lon1, lat2, lon2)
    err1 = abs(x_out - x_a1) + abs(y_out - y_a1)
    assert err1 < 0.001, f"Example 1: expected ({x_a1}, {y_a1}), got ({x_out}, {y_out})"
    print("Example 1 (N/S, 10 cm right, 40 cm fwd): OK")

    # ---- Example 2: East/West (90° / 270°), antenna 5 cm left, 35 cm forward ----
    T2 = 180.0
    x_a2, y_a2 = -5.0, 35.0
    h1_deg, h2_deg = 90.0, 270.0
    R1 = rotation_2d(-h1_deg)
    R2 = rotation_2d(-h2_deg)
    h_A = (-T2 / 2, 0)
    h_B = (T2 / 2, 0)
    t1 = (-R1[0][0] * h_A[0] - R1[0][1] * h_A[1], -R1[1][0] * h_A[0] - R1[1][1] * h_A[1])
    t2 = (-R2[0][0] * h_B[0] - R2[0][1] * h_B[1], -R2[1][0] * h_B[0] - R2[1][1] * h_B[1])
    p_a2 = (x_a2, y_a2)
    P1_cm = (mat_vec(R1, p_a2)[0] + t1[0], mat_vec(R1, p_a2)[1] + t1[1])
    P2_cm = (mat_vec(R2, p_a2)[0] + t2[0], mat_vec(R2, p_a2)[1] + t2[1])
    e1_m, n1_m = P1_cm[0] / 100, P1_cm[1] / 100
    e2_m, n2_m = P2_cm[0] / 100, P2_cm[1] / 100
    lat1, lon1 = local_m_to_latlon(e1_m, n1_m, lat0, lon0)
    lat2, lon2 = local_m_to_latlon(e2_m, n2_m, lat0, lon0)
    x_out, y_out = solve_antenna_offset_cm(T2, h1_deg, h2_deg, lat1, lon1, lat2, lon2)
    err2 = abs(x_out - x_a2) + abs(y_out - y_a2)
    assert err2 < 0.001, f"Example 2: expected ({x_a2}, {y_a2}), got ({x_out}, {y_out})"
    print("Example 2 (E/W, 5 cm left, 35 cm fwd): OK")
    print("Proof: both examples recover antenna offset to <0.001 cm (zero GPS/hub error).")
    _draw_proof_diagram()


def _draw_proof_diagram():
    """Draw a clearer, to-scale top view of the two proof examples with tractor bodies."""
    try:
        import matplotlib.pyplot as plt
        import matplotlib.patches as patches
    except ImportError:
        print("(matplotlib not found; skipping scale diagram)")
        return

    def mat_vec(M, v):
        return (M[0][0] * v[0] + M[0][1] * v[1], M[1][0] * v[0] + M[1][1] * v[1])

    def draw_tractor(ax, axle_center, heading_deg, body_length_cm=250, body_width_cm=200, label=None):
        """
        Draw a simple tractor footprint:
        - rectangle centered some distance forward of axle center
        - a line showing the tractor centerline (forward direction)
        """
        # Heading: 0 = north, 90 = east. Convert to radians CCW from +x.
        theta = math.radians(90 - heading_deg)
        c, s = math.cos(theta), math.sin(theta)
        # Center of body a bit ahead of axle center (e.g. 1/3 body length)
        offset_fwd = body_length_cm / 3.0
        body_cx = axle_center[0] + s * offset_fwd
        body_cy = axle_center[1] + c * offset_fwd
        # Rectangle corners (local frame, +y forward)
        dx = body_width_cm / 2.0
        dy = body_length_cm / 2.0
        # Build rotation matrix from local to world
        R = [[c, -s], [s, c]]
        # Local rectangle corner offsets
        local_corners = [
            (-dx, -dy),
            (dx, -dy),
            (dx, dy),
            (-dx, dy),
        ]
        world_corners = [
            (body_cx + R[0][0] * x + R[0][1] * y, body_cy + R[1][0] * x + R[1][1] * y)
            for (x, y) in local_corners
        ]
        poly = patches.Polygon(world_corners, closed=True, fill=False, edgecolor="tab:blue", linewidth=1.5)
        ax.add_patch(poly)
        # Draw centerline arrow from axle center forward
        fwd_len = dy + offset_fwd
        cx2 = axle_center[0] + s * fwd_len
        cy2 = axle_center[1] + c * fwd_len
        ax.annotate(
            "",
            xy=(cx2, cy2),
            xytext=axle_center,
            arrowprops=dict(arrowstyle="->", color="tab:blue", linewidth=1.5),
        )
        if label:
            ax.text(body_cx, body_cy, label, color="tab:blue", fontsize=8, ha="center", va="center")

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(10, 6))
    for ax in (ax1, ax2):
        ax.set_aspect("equal")
        ax.axhline(0, color="k", linewidth=0.5)
        ax.axvline(0, color="k", linewidth=0.5)
        ax.grid(True, alpha=0.3)

    # ---- Example 1: N/S, T=200, p_a=(10, 40), hub at origin ----
    T1, x_a1, y_a1 = 200.0, 10.0, 40.0
    h_A = (-T1 / 2, 0)
    h_B = (T1 / 2, 0)
    R1 = rotation_2d(0)      # heading 0° (north)
    R2 = rotation_2d(180)    # heading 180° (south)
    t1 = (-R1[0][0] * h_A[0] - R1[0][1] * h_A[1], -R1[1][0] * h_A[0] - R1[1][1] * h_A[1])
    t2 = (-R2[0][0] * h_B[0] - R2[0][1] * h_B[1], -R2[1][0] * h_B[0] - R2[1][1] * h_B[1])
    P1 = (mat_vec(R1, (x_a1, y_a1))[0] + t1[0], mat_vec(R1, (x_a1, y_a1))[1] + t1[1])
    P2 = (mat_vec(R2, (x_a1, y_a1))[0] + t2[0], mat_vec(R2, (x_a1, y_a1))[1] + t2[1])
    # Tractor bodies for both poses, sharing the same hub at (0,0)
    draw_tractor(ax1, t1, 0.0, label="pose 1")
    draw_tractor(ax1, t2, 180.0, label="pose 2")
    # Rear axle line (centerline at axle) and hub
    ax1.plot([h_A[0], h_B[0]], [h_A[1], h_B[1]], "-", color="brown", linewidth=2, label="rear axle")
    ax1.plot(0, 0, "ko", markersize=8, label="hub (both poses)")
    # Antenna positions
    ax1.plot(P1[0], P1[1], "g^", markersize=8, label="antenna P1")
    ax1.plot(P2[0], P2[1], "gv", markersize=8, label="antenna P2")
    ax1.set_xlabel("east (cm)")
    ax1.set_ylabel("north (cm)")
    ax1.set_title("Example 1: N/S 0° & 180°\nT=200 cm, p_a=(10, 40) cm")
    ax1.legend(loc="upper right", fontsize=7)
    ax1.set_xlim(-150, 150)
    ax1.set_ylim(-80, 80)

    # ---- Example 2: E/W, T=180, p_a=(-5, 35) ----
    T2, x_a2, y_a2 = 180.0, -5.0, 35.0
    h_A = (-T2 / 2, 0)
    h_B = (T2 / 2, 0)
    R1 = rotation_2d(-90)    # heading 90° east -> rotation(-90) in our mat convention
    R2 = rotation_2d(-270)   # heading 270° west -> rotation(-270)
    t1 = (-R1[0][0] * h_A[0] - R1[0][1] * h_A[1], -R1[1][0] * h_A[0] - R1[1][1] * h_A[1])
    t2 = (-R2[0][0] * h_B[0] - R2[0][1] * h_B[1], -R2[1][0] * h_B[0] - R2[1][1] * h_B[1])
    P1 = (mat_vec(R1, (x_a2, y_a2))[0] + t1[0], mat_vec(R1, (x_a2, y_a2))[1] + t1[1])
    P2 = (mat_vec(R2, (x_a2, y_a2))[0] + t2[0], mat_vec(R2, (x_a2, y_a2))[1] + t2[1])
    draw_tractor(ax2, t1, 90.0, label="pose 1")
    draw_tractor(ax2, t2, 270.0, label="pose 2")
    ax2.plot([h_A[0], h_B[0]], [h_A[1], h_B[1]], "-", color="brown", linewidth=2, label="rear axle")
    ax2.plot(0, 0, "ko", markersize=8, label="hub (both poses)")
    ax2.plot(P1[0], P1[1], "g^", markersize=8, label="antenna P1")
    ax2.plot(P2[0], P2[1], "gv", markersize=8, label="antenna P2")
    ax2.set_xlabel("east (cm)")
    ax2.set_ylabel("north (cm)")
    ax2.set_title("Example 2: E/W 90° & 270°\nT=180 cm, p_a=(-5, 35) cm")
    ax2.legend(loc="upper right", fontsize=7)
    ax2.set_xlim(-150, 150)
    ax2.set_ylim(-80, 80)

    plt.suptitle("Calibration proof: top-down tractor view (cm). Hub at origin.", fontsize=11)
    plt.tight_layout()
    out = os.path.join(os.path.dirname(__file__), "calibratepos_proof_diagram.png")
    plt.savefig(out, dpi=150, bbox_inches="tight")
    print(f"Scale diagram saved: {out}")
    plt.close()


if __name__ == "__main__":
    import sys
    if "--proof" in sys.argv:
        _proof_examples()
    else:
        main()
