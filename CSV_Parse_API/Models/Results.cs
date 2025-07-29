namespace CSV_Parse_API.Models
{
    public class Results
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime FirstOperationTime { get; set; }
        public double AverageExecutionTime { get; set; }
        public double AverageValue { get; set; }
        public double MedianValue { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public double TimeDelta { get; set; }
    }
}
