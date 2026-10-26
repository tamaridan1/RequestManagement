using FluentValidation;
using RequestManagementDTO;

namespace RequestManagementBL.Validators
{
    public class RequestQueryParamsValidator : AbstractValidator<RequestQueryParams>
    {
        private static readonly string[] AllowedSortFields =
            { "createdat", "priority", "status", "title", "organizationname" };

        public RequestQueryParamsValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("מספר העמוד חייב להיות גדול מ-0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("גודל העמוד חייב להיות בין 1 ל-100.");

            RuleFor(x => x.CreatedFrom)
                .LessThanOrEqualTo(x => x.CreatedTo!.Value)
                .When(x => x.CreatedFrom.HasValue && x.CreatedTo.HasValue)
                .WithMessage("תאריך ההתחלה חייב להיות לפני או שווה לתאריך הסוף.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("סטטוס לא חוקי.");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .When(x => x.Priority.HasValue)
                .WithMessage("עדיפות לא חוקית.");

            RuleFor(x => x.SortBy)
                .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                                AllowedSortFields.Contains(sortBy.ToLower()))
                .WithMessage("שדה מיון לא חוקי.");
        }
    }
}
