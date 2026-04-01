# General Algorithm for Computing Cut/Fill to a Planar Sloping Surface

## Problem Definition

You are given:

- A set of already-defined 2 ft x 2 ft bins
- For each bin, a known center point `(x_i, y_i)`
- For each bin, a known initial elevation `z_init(i)`
- A required **main slope** `s_m`
- A required **cross slope** `s_c`
- A required **main slope heading** `theta`
- A desired **cut/fill ratio** `R_target`

You want to compute:

- A final planar surface with the required main slope and cross slope
- A vertical offset of that plane such that the desired cut/fill ratio is achieved
- For each bin, the cut or fill depth required to move from the initial surface to the final surface

## Key Idea

Once the main slope, cross slope, and heading are fixed, the orientation of the final plane is fully determined.

The only remaining degree of freedom is the plane's vertical offset. That means the problem reduces to:

1. Define the plane orientation from slope and heading
2. Slide the plane up or down vertically
3. Find the vertical offset that gives the desired cut/fill ratio

This is a one-dimensional root-finding problem.

## 1. Define the Final Plane

The final plane can be written as:

```text
z_final(x, y) = a*x + b*y + c
```

Where:

- `a` and `b` are determined by the required slopes and heading
- `c` is a vertical offset that must be solved for

## 2. Compute Plane Coefficients from Main Slope, Cross Slope, and Heading

Let:

- `s_m` = main slope in ft/ft
- `s_c` = cross slope in ft/ft
- `theta` = heading angle of the main slope direction

Define the unit vector in the main slope direction:

```text
u = (cos(theta), sin(theta))
```

Define the unit vector in the cross-slope direction, perpendicular to the main slope:

```text
v = (-sin(theta), cos(theta))
```

Then the plane is:

```text
z_final(x, y) = s_m * (x*cos(theta) + y*sin(theta))
              + s_c * (-x*sin(theta) + y*cos(theta))
              + c
```

Expanding gives:

```text
a = s_m*cos(theta) - s_c*sin(theta)
b = s_m*sin(theta) + s_c*cos(theta)
```

So the final plane is:

```text
z_final(x, y) = a*x + b*y + c
```

At this point, `a` and `b` are fixed. Only `c` remains unknown.

## 3. Compute Cut/Fill Depth at Each Bin

For each bin `i`, compute the signed elevation difference:

```text
d_i(c) = z_init(i) - z_final(x_i, y_i)
```

Interpretation:

- `d_i > 0` means cut
- `d_i < 0` means fill

Define:

```text
cut_i(c)  = max(d_i(c), 0)
fill_i(c) = max(-d_i(c), 0)
```

These are depths, not volumes.

If each bin has area `A`, then volume per bin is:

```text
V_cut_i(c)  = cut_i(c)  * A
V_fill_i(c) = fill_i(c) * A
```

For 2 ft x 2 ft bins:

```text
A = 4 ft^2
```

## 4. Compute Total Cut and Fill Volumes

Sum over all bins:

```text
V_cut(c)  = sum(V_cut_i(c))
V_fill(c) = sum(V_fill_i(c))
```

These are the total cut and fill volumes for a given vertical offset `c`.

## 5. Define the Target Ratio Condition

Let the desired cut/fill ratio be:

```text
R_target = V_cut / V_fill
```

You want to choose `c` so that:

```text
V_cut(c) / V_fill(c) = R_target
```

A numerically convenient equivalent form is:

```text
f(c) = V_cut(c) - R_target * V_fill(c)
```

Solve for:

```text
f(c) = 0
```

That is the core equation.

## 6. Why This Works

As `c` increases, the final plane moves upward.

That generally causes:

- total cut to decrease
- total fill to increase

So `f(c)` is monotonic in normal grading situations. That makes the problem well suited to a simple bracketing root finder such as bisection.

## 7. Solve for the Vertical Offset Using Bisection

### Step 1: Choose Bounds

Pick `c_low` and `c_high` such that `f(c_low)` and `f(c_high)` have opposite signs.

For example:

- a low plane position that creates mostly cut
- a high plane position that creates mostly fill

Then:

```text
f(c_low) * f(c_high) < 0
```

### Step 2: Iterate

Repeat:

```text
c_mid = (c_low + c_high) / 2
```

Compute `f(c_mid)`.

If `f(c_mid)` has the same sign as `f(c_low)`, replace `c_low` with `c_mid`.
Otherwise replace `c_high` with `c_mid`.

### Step 3: Stop

Stop when either:

- `abs(f(c_mid))` is below a specified tolerance, or
- `abs(c_high - c_low)` is below a specified tolerance

Then take:

```text
c_star = (c_low + c_high) / 2
```

This is the required vertical offset.

## 8. Final Per-Bin Results

Once `c_star` is known, compute for each bin:

```text
z_final(i) = a*x_i + b*y_i + c_star
d_i        = z_init(i) - z_final(i)
cut_i      = max(d_i, 0)
fill_i     = max(-d_i, 0)
```

You can store either:

- separate `cut_i` and `fill_i`, or
- a signed value `d_i` where positive means cut and negative means fill

## 9. Pseudocode

```text
input:
    x[i], y[i]          # bin center coordinates
    z_init[i]           # initial elevation at each bin
    A                   # bin area (4 ft^2 for 2 ft x 2 ft bins)
    s_m                 # main slope (ft/ft)
    s_c                 # cross slope (ft/ft)
    theta               # main slope heading
    R_target            # desired cut/fill ratio
    tolerance

compute:
    a = s_m*cos(theta) - s_c*sin(theta)
    b = s_m*sin(theta) + s_c*cos(theta)

function volumes(c):
    total_cut = 0
    total_fill = 0

    for i in all bins:
        z_final = a*x[i] + b*y[i] + c
        d = z_init[i] - z_final

        if d > 0:
            total_cut += d * A
        else:
            total_fill += (-d) * A

    return total_cut, total_fill

function f(c):
    total_cut, total_fill = volumes(c)
    return total_cut - R_target * total_fill

choose c_low and c_high such that f(c_low) and f(c_high) have opposite signs

while abs(c_high - c_low) > tolerance:
    c_mid = 0.5 * (c_low + c_high)

    if f(c_mid) == 0:
        break
    elif f(c_mid) has same sign as f(c_low):
        c_low = c_mid
    else:
        c_high = c_mid

c_star = 0.5 * (c_low + c_high)

for i in all bins:
    z_final[i] = a*x[i] + b*y[i] + c_star
    d[i] = z_init[i] - z_final[i]
    cut[i] = max(d[i], 0)
    fill[i] = max(-d[i], 0)

output:
    z_final[i], d[i], cut[i], fill[i], c_star
```

## 10. Notes

### Slope Units

Make sure all slopes are converted to ft/ft before using them.

Examples:

- percent slope: `s = percent / 100`
- angle in degrees: `s = tan(angle)`

### Heading Units

Make sure `theta` is in radians if using standard trig functions in most programming languages.

### Cut/Fill Ratio Edge Cases

If `V_fill(c)` is near zero, the ratio `V_cut / V_fill` becomes unstable. The solver should guard against this if extreme offsets are possible.

Using the root form

```text
f(c) = V_cut(c) - R_target * V_fill(c)
```

is generally better behaved than directly comparing ratios.

### Shrink/Swell or Compaction Factors

If the desired cut/fill ratio must account for soil behavior, modify the volume equation before solving.

For example, if fill requires an adjusted conversion factor, solve against adjusted fill volume instead of raw geometric fill volume.

## 11. Summary

The general solution is:

1. Compute the plane orientation from main slope, cross slope, and heading
2. Express the final grading surface as `z_final(x, y) = a*x + b*y + c`
3. Treat `c` as the only unknown
4. For any trial `c`, compute total cut and total fill from the bins
5. Solve for the `c` that gives the desired cut/fill ratio
6. Use that `c` to compute final cut/fill depth for every bin

This reduces the grading problem to a straightforward one-parameter numerical solve.
