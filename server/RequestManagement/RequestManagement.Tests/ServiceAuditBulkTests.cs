using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using RequestManagementBL;
using RequestManagementDAL;
using RequestManagementDTO;

namespace RequestManagement.Tests
{
    public class ServiceAuditBulkTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RequestService _service;

        public ServiceAuditBulkTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var repository = new RequestRepository(_context);
            var cache = new MemoryCache(new MemoryCacheOptions());

            _service = new RequestService(repository, cache, NullLogger<RequestService>.Instance);

            SeedTestData();
        }

        private void SeedTestData()
        {
            _context.Requests.AddRange(
                new RequestEntity { Id = 1, Title = "פנייה א", OrganizationName = "חברה 1", Status = RequestStatus.New, Priority = RequestPriority.High, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[] { 1 } },
                new RequestEntity { Id = 2, Title = "פנייה ב", OrganizationName = "חברה 2", Status = RequestStatus.New, Priority = RequestPriority.Low, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[] { 2 } }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task UpdateStatus_CreatesHistoryRecord()
        {
            var dto = new UpdateStatusDTO
            {
                NewStatus = RequestStatus.InProgress,
                RowVersion = Convert.ToBase64String(new byte[] { 1 }),
                ChangedBy = "דנה"
            };

            await _service.UpdateStatusAsync(1, dto, CancellationToken.None);

            var history = await _service.GetHistoryAsync(1, CancellationToken.None);
            Assert.Single(history);
            Assert.Equal(RequestStatus.New, history[0].PreviousStatus);
            Assert.Equal(RequestStatus.InProgress, history[0].NewStatus);
            Assert.Equal("דנה", history[0].ChangedBy);
        }

        [Fact]
        public async Task BulkUpdate_PartialSuccess_ReportsSuccessAndFailure()
        {
            var dto = new BulkUpdateStatusDTO
            {
                NewStatus = RequestStatus.InProgress,
                ChangedBy = "יוסי",
                Items = new List<BulkItemDTO>
                {
                    new() { Id = 1, RowVersion = Convert.ToBase64String(new byte[] { 1 }) },   // קיים - יצליח
                    new() { Id = 999, RowVersion = Convert.ToBase64String(new byte[] { 9 }) }  // לא קיים - ייכשל
                }
            };

            var result = await _service.BulkUpdateStatusAsync(dto, CancellationToken.None);

            Assert.Equal(2, result.TotalRequested);
            Assert.Equal(1, result.SucceededCount);
            Assert.Equal(1, result.FailedCount);
            Assert.Contains(result.Results, r => r.Id == 1 && r.Success);
            Assert.Contains(result.Results, r => r.Id == 999 && !r.Success);
        }

        [Fact]
        public async Task BulkUpdate_InvalidTransition_Fails()
        {
            var dto = new BulkUpdateStatusDTO
            {
                NewStatus = RequestStatus.New,
                Items = new List<BulkItemDTO>
                {
                    new() { Id = 1, RowVersion = Convert.ToBase64String(new byte[] { 1 }) }
                }
            };

            var result = await _service.BulkUpdateStatusAsync(dto, CancellationToken.None);

            Assert.Equal(0, result.SucceededCount);
            Assert.Equal(1, result.FailedCount);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
