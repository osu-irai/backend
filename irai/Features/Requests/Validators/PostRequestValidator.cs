using FluentValidation;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.Validators;

public class PostRequestValidator : AbstractValidator<PostBaseRequest>
{
    public PostRequestValidator()
    {
        RuleFor(x => x)
            .NotNull();
        When(x => x is not null, () =>
        {
            RuleFor(x => x!.DestinationId)
                .NotNull();
            RuleFor(x => x!.BeatmapId)
                .NotNull();
        });
    }
}
