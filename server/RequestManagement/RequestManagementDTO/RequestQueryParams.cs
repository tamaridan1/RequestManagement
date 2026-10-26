namespace RequestManagementDTO
{
    public class RequestQueryParams
    {
        // דפדוף
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // חיפוש טקסטואלי
        public string? Search { get; set; }

        // סינון
        public RequestStatus? Status { get; set; }
        public RequestPriority? Priority { get; set; }
        public string? OrganizationName { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        // מיון
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }
}
