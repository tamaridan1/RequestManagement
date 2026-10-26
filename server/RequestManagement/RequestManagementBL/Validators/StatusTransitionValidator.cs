using RequestManagementDTO;

namespace RequestManagementBL.Validators
{
    public static class StatusTransitionValidator
    {
        private static readonly Dictionary<RequestStatus, RequestStatus[]> Allowed = new()
        {
            [RequestStatus.New]        = new[] { RequestStatus.InProgress, RequestStatus.Waiting },
            [RequestStatus.InProgress] = new[] { RequestStatus.Waiting, RequestStatus.Completed },
            [RequestStatus.Waiting]    = new[] { RequestStatus.InProgress, RequestStatus.Completed },
            [RequestStatus.Completed]  = Array.Empty<RequestStatus>()
        };

        public static bool IsValid(RequestStatus from, RequestStatus to) =>
            from != to && Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }
}
