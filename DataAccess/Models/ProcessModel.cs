namespace DataAccess.Models
{
    public class ProcessModel
    {
        public int ProcessId { get; set; }
        public string OperationName { get; set; }
        public DateTime ProcessDate { get; set; }
        /*public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }*/
        public int NumberOfEmailsInProcess { get; set; }
        public int SuccessCount { get; set; }
        public int ProxyErrorCount { get; set; }
        public int HttpErrorCount { get; set; }
        public int OthersCount { get; set; }
        public List<SpamCountModel> SpamCounts { get; set; } = new List<SpamCountModel>();
    }
    public class SpamCountModel
    {
        public int SpamCountId { get; set; }
        public int ProcessId { get; set; }
        public string Email { get; set; }
        public int SpamCount { get; set; }
        public DateTime CountDate { get; set; }
    }
}