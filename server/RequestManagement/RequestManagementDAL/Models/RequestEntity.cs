using System.ComponentModel.DataAnnotations;
using RequestManagementDTO;

namespace RequestManagementDAL
{
    public class RequestEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        public RequestStatus Status { get; set; }

        public RequestPriority Priority { get; set; }

        [MaxLength(100)]
        public string? AssignedTo { get; set; } 

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
