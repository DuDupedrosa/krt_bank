using FluentValidation;
using krt_api.Core.Accounts.Dtos;
using Maoli;

namespace krt_api.Core.Accounts.Validators
{
    public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
    {
        public CreateAccountDtoValidator()
        {
            RuleFor(dto => dto.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(dto => dto.CPF)
                .NotEmpty().WithMessage("CPF is required.")
                .Length(11).WithMessage("CPF must be exactly 11 characters long.")
                .Matches(@"^\d{11}$").WithMessage("CPF must contain only numbers.")
                .Must((dto, cpf) => cpf != null && Cpf.Validate(cpf));
        }
    }
}
