namespace RequestManagementDTO
{
    // רשומת היסטוריה
    public class StatusHistoryDTO
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public RequestStatus PreviousStatus { get; set; }
        public RequestStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
    }
}
