namespace RequestManagementDTO
{
    public class UpdateStatusDTO
    {
        public RequestStatus NewStatus { get; set; }
        public string RowVersion { get; set; } = string.Empty;
        public string? ChangedBy { get; set; }
    }
}
