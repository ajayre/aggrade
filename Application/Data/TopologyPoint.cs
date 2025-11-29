namespace AgGrade.Data
{
    public class TopologyPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double ExistingElevation { get; set; }
        public double ProposedElevation { get; set; }
        public double CutFill { get; set; }
        public string? Code { get; set; }
        public string? Comments { get; set; }
        public double X { get; set; } // Local coordinate system
        public double Y { get; set; } // Local coordinate system
    }
}
