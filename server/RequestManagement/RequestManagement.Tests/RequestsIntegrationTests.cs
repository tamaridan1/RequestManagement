using Microsoft.EntityFrameworkCore;
using RequestManagementDAL;
using RequestManagementDTO;

namespace RequestManagement.Tests
{
    public class RequestsIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RequestRepository _repository;

        public RequestsIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())  // DB ייחודי לכל בדיקה
                .Options;

            _context = new AppDbContext(options);
            _repository = new RequestRepository(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            _context.Requests.AddRange(
                new RequestEntity { Title = "פנייה א", OrganizationName = "חברה 1", Status = RequestStatus.New, Priority = RequestPriority.High, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[] { 1 } },
                new RequestEntity { Title = "פנייה ב", OrganizationName = "חברה 2", Status = RequestStatus.InProgress, Priority = RequestPriority.Low, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[] { 2 } },
                new RequestEntity { Title = "פנייה ג", OrganizationName = "חברה 1", Status = RequestStatus.New, Priority = RequestPriority.Medium, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[] { 3 } }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetPaged_ReturnsAllItems_WithCorrectTotal()
        {
            var query = new RequestQueryParams { PageNumber = 1, PageSize = 10 };

            var result = await _repository.GetPagedAsync(query, CancellationToken.None);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task GetPaged_FilterByStatus_ReturnsOnlyMatching()
        {
            var query = new RequestQueryParams { PageNumber = 1, PageSize = 10, Status = RequestStatus.New };

            var result = await _repository.GetPagedAsync(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, r => Assert.Equal(RequestStatus.New, r.Status));
        }

        [Fact]
        public async Task GetPaged_SearchByOrganization_ReturnsMatching()
        {
            var query = new RequestQueryParams { PageNumber = 1, PageSize = 10, Search = "חברה 1" };

            var result = await _repository.GetPagedAsync(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
