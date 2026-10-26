using RequestManagementBL.Validators;
using RequestManagementDTO;

namespace RequestManagement.Tests
{
    public class ValidationTests
    {
        private readonly RequestQueryParamsValidator _validator = new();

        [Fact]
        public void PageSize_TooLarge_IsInvalid()
        {
            var query = new RequestQueryParams { PageNumber = 1, PageSize = 5000 };

            var result = _validator.Validate(query);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RequestQueryParams.PageSize));
        }

        [Fact]
        public void PageNumber_Zero_IsInvalid()
        {
            var query = new RequestQueryParams { PageNumber = 0, PageSize = 20 };

            var result = _validator.Validate(query);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidParams_PassValidation()
        {
            var query = new RequestQueryParams { PageNumber = 1, PageSize = 20 };

            var result = _validator.Validate(query);

            Assert.True(result.IsValid);
        }
    }
}
