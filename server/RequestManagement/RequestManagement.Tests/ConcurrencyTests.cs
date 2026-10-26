using Microsoft.EntityFrameworkCore;
using RequestManagementDAL;
using RequestManagementDTO;

namespace RequestManagement.Tests
{
    public class ConcurrencyTests : IDisposable
    {
        private readonly string _dbName = "ConcurrencyTestDb_" + Guid.NewGuid().ToString("N");
        private readonly DbContextOptions<AppDbContext> _options;

        public ConcurrencyTests()
        {
            var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True";
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();

            context.Requests.Add(new RequestEntity
            {
                Title = "פנייה לבדיקת concurrency",
                OrganizationName = "חברה",
                Status = RequestStatus.New,
                Priority = RequestPriority.Medium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        [Fact]
        public async Task ConcurrentUpdate_SecondUpdate_Fails()
        {
            int requestId;
            byte[] originalRowVersion;
            using (var setup = new AppDbContext(_options))
            {
                var r = await setup.Requests.FirstAsync();
                requestId = r.Id;
                originalRowVersion = r.RowVersion;
            }

            using (var ctx1 = new AppDbContext(_options))
            {
                var repo1 = new RequestRepository(ctx1);
                var entity1 = await repo1.GetEntityByIdAsync(requestId, CancellationToken.None);
                entity1!.Status = RequestStatus.InProgress;
                var history1 = new RequestStatusHistory { RequestId = requestId, PreviousStatus = RequestStatus.New, NewStatus = RequestStatus.InProgress, ChangedAt = DateTime.UtcNow };
                var success1 = await repo1.UpdateStatusAsync(entity1, history1, originalRowVersion, CancellationToken.None);
                Assert.True(success1); 
            }

            using (var ctx2 = new AppDbContext(_options))
            {
                var repo2 = new RequestRepository(ctx2);
                var entity2 = await repo2.GetEntityByIdAsync(requestId, CancellationToken.None);
                entity2!.Status = RequestStatus.Waiting;
                var history2 = new RequestStatusHistory { RequestId = requestId, PreviousStatus = RequestStatus.New, NewStatus = RequestStatus.Waiting, ChangedAt = DateTime.UtcNow };
                var success2 = await repo2.UpdateStatusAsync(entity2, history2, originalRowVersion, CancellationToken.None);
                Assert.False(success2);
            }
        }

        public void Dispose()
        {
            using var context = new AppDbContext(_options);
            context.Database.EnsureDeleted();
        }
    }
}
