namespace RequestManagementDTO
{
    public class BulkUpdateStatusDTO
    {
        public RequestStatus NewStatus { get; set; }
        public string? ChangedBy { get; set; }
        public List<BulkItemDTO> Items { get; set; } = new();
    }

    public class BulkItemDTO
    {
        public int Id { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
