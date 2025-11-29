namespace AgGrade.Data;

// Individual bin with spatial and operational data
public class Bin
{
    public int X { get; set; }
    public int Y { get; set; }
    public Coordinate SouthwestCorner { get; set; }
    public Coordinate NortheastCorner { get; set; }
    public Coordinate Centroid { get; set; }
    public double CutAmountM { get; set; }
    public double FillAmountM { get; set; }
    public double ExistingElevationM { get; set; }
    public double TargetElevationM { get; set; }
}
