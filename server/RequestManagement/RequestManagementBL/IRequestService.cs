using RequestManagementDTO;

namespace RequestManagementBL
{
    public interface IRequestService
    {
        Task<PagedResult<RequestDTO>> GetRequestsAsync(RequestQueryParams queryParams, CancellationToken cancellationToken);

        Task<RequestSummaryDTO> GetSummaryAsync(CancellationToken cancellationToken);

        Task ReseedAsync(CancellationToken cancellationToken);

        Task<RequestDTO> UpdateStatusAsync(int id, UpdateStatusDTO dto, CancellationToken cancellationToken);

        Task<List<StatusHistoryDTO>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);

        Task<BulkUpdateResultDTO> BulkUpdateStatusAsync(BulkUpdateStatusDTO dto, CancellationToken cancellationToken);
    }
}
