using FluentValidation;
using RequestManagementDTO;

namespace RequestManagementBL.Validators
{
    public class BulkUpdateStatusValidator : AbstractValidator<BulkUpdateStatusDTO>
    {
        public BulkUpdateStatusValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage("סטטוס לא חוקי.");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("יש לשלוח לפחות פנייה אחת.")
                .Must(items => items.Count <= 100)
                .WithMessage("ניתן לעדכן עד 100 פניות בבקשה אחת.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.Id).GreaterThan(0).WithMessage("מזהה פנייה לא חוקי.");
                item.RuleFor(i => i.RowVersion).NotEmpty().WithMessage("RowVersion חסר.");
            });
        }
    }
}
