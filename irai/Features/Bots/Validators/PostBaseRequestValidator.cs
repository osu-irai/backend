using FluentValidation;
using irai.Features.Requests.Models;

namespace irai.Features.Bots.Validators;

public class PostBaseRequestValidator : AbstractValidator<PostBaseRequest?>
{
    public PostBaseRequestValidator()
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
