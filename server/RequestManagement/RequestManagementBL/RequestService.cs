using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RequestManagementBL.Exceptions;
using RequestManagementBL.Validators;
using RequestManagementDAL;
using RequestManagementDTO;

namespace RequestManagementBL
{
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RequestService> _logger;

        private const string SummaryCacheKey = "requests_summary";

        private static readonly SemaphoreSlim _summaryLock = new(1, 1);

        public RequestService(IRequestRepository repository, IMemoryCache cache, ILogger<RequestService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PagedResult<RequestDTO>> GetRequestsAsync(RequestQueryParams queryParams, CancellationToken cancellationToken)
        {
            var result = await _repository.GetPagedAsync(queryParams, cancellationToken);

            var dtos = result.Items.Select(MapToDto).ToList();

            return new PagedResult<RequestDTO>
            {
                Items = dtos,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<RequestSummaryDTO> GetSummaryAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(SummaryCacheKey, out RequestSummaryDTO? cached) && cached is not null)
                return cached;

            await _summaryLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(SummaryCacheKey, out RequestSummaryDTO? cachedAfterLock) && cachedAfterLock is not null)
                    return cachedAfterLock;

                var byStatus = await _repository.GetCountByStatusAsync(cancellationToken);
                var byPriority = await _repository.GetCountByPriorityAsync(cancellationToken);

                var summary = new RequestSummaryDTO
                {
                    CountByStatus = byStatus,
                    CountByPriority = byPriority
                };

                _cache.Set(SummaryCacheKey, summary, TimeSpan.FromMinutes(2));

                return summary;
            }
            finally
            {
                _summaryLock.Release();
            }
        }

        private void InvalidateSummaryCache()
        {
            _cache.Remove(SummaryCacheKey);
        }

        public Task ReseedAsync(CancellationToken cancellationToken)
        {
            return _repository.ReseedAsync(cancellationToken);
        }

        public async Task<RequestDTO> UpdateStatusAsync(int id, UpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetEntityByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"פנייה עם מזהה {id} לא נמצאה.");

            if (!StatusTransitionValidator.IsValid(entity.Status, dto.NewStatus))
                throw new ValidationException($"מעבר מסטטוס {entity.Status} ל-{dto.NewStatus} אינו מותר.");

            byte[] rowVersion;
            try
            {
                rowVersion = Convert.FromBase64String(dto.RowVersion);
            }
            catch (FormatException)
            {
                throw new ValidationException("RowVersion לא תקין.");
            }

            var history = new RequestStatusHistory
            {
                RequestId = entity.Id,
                PreviousStatus = entity.Status,
                NewStatus = dto.NewStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = dto.ChangedBy
            };

            entity.Status = dto.NewStatus;
            entity.UpdatedAt = DateTime.UtcNow;

            var success = await _repository.UpdateStatusAsync(entity, history, rowVersion, cancellationToken);
            if (!success)
                throw new ConflictException("הפנייה עודכנה על ידי משתמש אחר. יש לרענן ולנסות שוב.");

            InvalidateSummaryCache();

            _logger.LogInformation("Status updated: RequestId={RequestId}, {From} -> {To}, ChangedBy={ChangedBy}", entity.Id, history.PreviousStatus, history.NewStatus, dto.ChangedBy ?? "unknown");

            return MapToDto(entity);
        }

        public async Task<List<StatusHistoryDTO>> GetHistoryAsync(int requestId, CancellationToken cancellationToken)
        {
            var history = await _repository.GetHistoryAsync(requestId, cancellationToken);
            return history.Select(h => new StatusHistoryDTO
            {
                Id = h.Id,
                RequestId = h.RequestId,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedBy
            }).ToList();
        }

        public async Task<BulkUpdateResultDTO> BulkUpdateStatusAsync(BulkUpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            var result = new BulkUpdateResultDTO { TotalRequested = dto.Items.Count };

            foreach (var item in dto.Items)
            {
                var itemResult = new BulkItemResultDTO { Id = item.Id };
                try
                {
                    var single = new UpdateStatusDTO
                    {
                        NewStatus = dto.NewStatus,
                        RowVersion = item.RowVersion,
                        ChangedBy = dto.ChangedBy
                    };
                    await UpdateStatusAsync(item.Id, single, cancellationToken);
                    itemResult.Success = true;
                }
                catch (Exception ex)
                {
                    itemResult.Success = false;
                    itemResult.Error = ex.Message;
                }

                result.Results.Add(itemResult);
            }

            result.SucceededCount = result.Results.Count(r => r.Success);
            result.FailedCount = result.Results.Count(r => !r.Success);

            _logger.LogInformation(
                "Bulk status update: Requested={Total}, Succeeded={Succeeded}, Failed={Failed}",
                result.TotalRequested, result.SucceededCount, result.FailedCount);

            return result;
        }

        private static RequestDTO MapToDto(RequestEntity e)
        {
            return new RequestDTO
            {
                Id = e.Id,
                Title = e.Title,
                OrganizationName = e.OrganizationName,
                Status = e.Status,
                Priority = e.Priority,
                AssignedTo = e.AssignedTo,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                RowVersion = Convert.ToBase64String(e.RowVersion)
            };
        }
    }
}
