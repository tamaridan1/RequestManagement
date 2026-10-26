using Microsoft.EntityFrameworkCore;
using RequestManagementDTO;

namespace RequestManagementDAL
{
    public class RequestRepository : IRequestRepository
    {
        private readonly AppDbContext _context;

        public RequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<RequestEntity>> GetPagedAsync(RequestQueryParams p, CancellationToken cancellationToken)
        {
            
            IQueryable<RequestEntity> query = _context.Requests.AsNoTracking();

            if (p.Status.HasValue)
                query = query.Where(r => r.Status == p.Status.Value);

            if (p.Priority.HasValue)
                query = query.Where(r => r.Priority == p.Priority.Value);

            if (!string.IsNullOrWhiteSpace(p.OrganizationName))
                query = query.Where(r => r.OrganizationName == p.OrganizationName);

            if (!string.IsNullOrWhiteSpace(p.AssignedTo))
                query = query.Where(r => r.AssignedTo == p.AssignedTo);

            if (p.CreatedFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= p.CreatedFrom.Value);

            if (p.CreatedTo.HasValue)
                query = query.Where(r => r.CreatedAt <= p.CreatedTo.Value);

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var term = p.Search.Trim();
                query = query.Where(r =>
                    r.Title.Contains(term) ||
                    r.OrganizationName.Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, p.SortBy, p.SortDescending);

            var items = await query
                .Skip((p.PageNumber - 1) * p.PageSize)
                .Take(p.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<RequestEntity>
            {
                Items = items,
                PageNumber = p.PageNumber,
                PageSize = p.PageSize,
                TotalCount = totalCount
            };
        }

        private static IQueryable<RequestEntity> ApplySorting(IQueryable<RequestEntity> query, string sortBy, bool descending)
        {
            return sortBy?.ToLower() switch
            {
                "priority"     => descending ? query.OrderByDescending(r => r.Priority) : query.OrderBy(r => r.Priority),
                "status"       => descending ? query.OrderByDescending(r => r.Status)   : query.OrderBy(r => r.Status),
                "title"        => descending ? query.OrderByDescending(r => r.Title)    : query.OrderBy(r => r.Title),
                "organizationname" => descending ? query.OrderByDescending(r => r.OrganizationName) : query.OrderBy(r => r.OrganizationName),
                _              => descending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            };
        }

        public async Task<Dictionary<RequestStatus, int>> GetCountByStatusAsync(CancellationToken cancellationToken)
        {
            return await _context.Requests
                .AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        public async Task<Dictionary<RequestPriority, int>> GetCountByPriorityAsync(CancellationToken cancellationToken)
        {
            return await _context.Requests
                .AsNoTracking()
                .GroupBy(r => r.Priority)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        public Task ReseedAsync(CancellationToken cancellationToken)
        {
            return DbInitializer.ReseedAsync(_context, cancellationToken);
        }

        public async Task<RequestEntity?> GetEntityByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<bool> UpdateStatusAsync(RequestEntity entity, RequestStatusHistory history, byte[] rowVersion, CancellationToken cancellationToken)
        {
            _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;

            _context.StatusHistory.Add(history);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        public async Task<List<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken)
        {
            return await _context.StatusHistory
                .AsNoTracking()
                .Where(h => h.RequestId == requestId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
