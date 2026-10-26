using RequestManagementDTO;

namespace RequestManagementDAL
{
    public interface IRequestRepository
    {
        // שליפת עמוד של פניות לפי פרמטרי חיפוש/סינון/מיון
        Task<PagedResult<RequestEntity>> GetPagedAsync(RequestQueryParams queryParams, CancellationToken cancellationToken);

        // Aggregation 1: ספירת פניות לפי סטטוס
        Task<Dictionary<RequestStatus, int>> GetCountByStatusAsync(CancellationToken cancellationToken);

        // Aggregation 2: ספירת פניות לפי עדיפות
        Task<Dictionary<RequestPriority, int>> GetCountByPriorityAsync(CancellationToken cancellationToken);

        // יצירה מחדש של נתוני הבדיקה
        Task ReseedAsync(CancellationToken cancellationToken);

        Task<RequestEntity?> GetEntityByIdAsync(int id, CancellationToken cancellationToken);

        Task<bool> UpdateStatusAsync(RequestEntity entity, RequestStatusHistory history, byte[] rowVersion, CancellationToken cancellationToken);

        // שליפת היסטוריית שינויים של פנייה
        Task<List<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);
    }
}
