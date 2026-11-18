using FluentValidation;

namespace irai.Features.Requests.Validators;

public class GetRequestsValidator : AbstractValidator<int?>
{
    public GetRequestsValidator()
    {
        RuleFor(x => x)
            .NotNull()
            .WithMessage("Request player ID is null");
    }
}
