namespace RequestManagementDTO
{
    public class BulkUpdateResultDTO
    {
        public int TotalRequested { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkItemResultDTO> Results { get; set; } = new();
    }

    public class BulkItemResultDTO
    {
        public int Id { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
