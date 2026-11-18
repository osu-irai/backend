using FluentValidation;

namespace irai.Features.Requests.Validators;

public class DeleteSelfRequestValidator : AbstractValidator<int?>
{
    public DeleteSelfRequestValidator()
    {
        RuleFor(x => x)
            .NotNull();
    }
}
