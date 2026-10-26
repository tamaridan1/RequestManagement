namespace RequestManagementDTO
{
    public class RequestSummaryDTO
    {
        public Dictionary<RequestStatus, int> CountByStatus { get; set; } = new();
        public Dictionary<RequestPriority, int> CountByPriority { get; set; } = new();
    }
}