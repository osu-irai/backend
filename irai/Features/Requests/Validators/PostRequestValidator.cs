using FluentValidation;
using irai.Features.Requests.Models;

namespace irai.Features.Requests.Validators;

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
