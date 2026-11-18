using FluentValidation;
using irai.Features.Requests.Models;

namespace irai.Features.Requests.Validators;

public class PostRequestWithNameValidator : AbstractValidator<PostRequestWithName?>
{
    public PostRequestWithNameValidator()
    {
        RuleFor(x => x)
            .NotNull();
        When(x => x is not null, () =>
        {
            RuleFor(x => x!.DestinationName)
                .NotNull();
            RuleFor(x => x!.BeatmapId)
                .NotNull();
        });
    }
}
