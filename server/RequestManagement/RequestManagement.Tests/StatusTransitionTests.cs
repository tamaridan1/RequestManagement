using RequestManagementBL.Validators;
using RequestManagementDTO;

namespace RequestManagement.Tests
{
    public class StatusTransitionTests
    {
        [Fact]
        public void New_To_InProgress_IsAllowed()
        {
            var result = StatusTransitionValidator.IsValid(RequestStatus.New, RequestStatus.InProgress);
            Assert.True(result);
        }

        [Fact]
        public void Completed_To_New_IsNotAllowed()
        {
            var result = StatusTransitionValidator.IsValid(RequestStatus.Completed, RequestStatus.New);
            Assert.False(result);
        }

        [Fact]
        public void Same_Status_IsNotAllowed()
        {
            var result = StatusTransitionValidator.IsValid(RequestStatus.New, RequestStatus.New);
            Assert.False(result);
        }
    }
}
