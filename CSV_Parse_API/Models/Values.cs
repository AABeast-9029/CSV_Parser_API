namespace CSV_Parse_API.Models
{
    public class Values
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double ExecutionTime { get; set; }
        public double Value { get; set; }
        public string FileName { get; set; }
        public Nullable<int> Results_Id { get; set; }
    }
}
