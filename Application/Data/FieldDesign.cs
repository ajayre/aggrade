namespace AgGrade.Data
{
    public class FieldDesign
    {
        public string SurveyFileName { get; set; } = string.Empty;

        public int MainSlopeDirection { get; set; }
        public double MainSlope { get; set; }
        public double CrossSlope { get; set; }
        public double CutFillRatio { get; set; }
        public double ImportToField { get; set; }
        public double ExportFromField { get; set; }
    }
}
