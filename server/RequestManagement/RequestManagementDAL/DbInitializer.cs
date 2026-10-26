using Microsoft.EntityFrameworkCore;
using RequestManagementDTO;

namespace RequestManagementDAL
{
    public static class DbInitializer
    {
        private const int TotalRecords = 100_000;
        private const int BatchSize = 5_000;

        public static void Seed(AppDbContext context)
        {
            if (context.Requests.Any())
                return;

            GenerateAndInsert(context);
        }

        public static async Task ReseedAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Requests", cancellationToken);

            GenerateAndInsert(context);
        }

        private static void GenerateAndInsert(AppDbContext context)
        {
            var random = new Random();
            var now = DateTime.UtcNow;

            var statuses = Enum.GetValues<RequestStatus>();
            var priorities = Enum.GetValues<RequestPriority>();

            var batch = new List<RequestEntity>(BatchSize);

            for (int i = 1; i <= TotalRecords; i++)
            {
                var createdAt = now.AddDays(-random.Next(1, 365));

                batch.Add(new RequestEntity
                {
                    Title = $"פנייה בנושא {i}",
                    OrganizationName = $"חברה {random.Next(1, 101)}",
                    Status = statuses[random.Next(statuses.Length)],
                    Priority = priorities[random.Next(priorities.Length)],
                    AssignedTo = $"מטפל {random.Next(1, 21)}",
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });

                if (batch.Count == BatchSize)
                {
                    context.Requests.AddRange(batch);
                    context.SaveChanges();
                    context.ChangeTracker.Clear();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                context.Requests.AddRange(batch);
                context.SaveChanges();
            }
        }
    }
}
