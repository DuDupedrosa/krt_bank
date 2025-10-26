using FluentValidation;
using krt_api.Core.Accounts.Dtos;

namespace krt_api.Core.Accounts.Validators
{
    public class UpdateAccountDtoValidator : AbstractValidator<UpdateAccountDto>
    {
        public UpdateAccountDtoValidator()
        {
            Include(new CreateAccountDtoValidator());

            RuleFor(dto => dto.Id)
                .NotEmpty().WithMessage("Id is required.")
                .Must(id => id != Guid.Empty).WithMessage("Id must be a valid GUID.");
        }
    }
}
