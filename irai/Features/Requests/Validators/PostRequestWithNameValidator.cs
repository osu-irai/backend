using FluentValidation;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.Validators;

public class PostRequestWithNameValidator : AbstractValidator<PostRequestWithName?>
{
    public PostRequestWithNameValidator()
    {
        RuleFor(x => x).NotNull();
        When(x => x is not null, () =>
        {
            RuleFor(x => x!.DestinationName).NotNull();
            RuleFor(x => x!.BeatmapId).NotNull();
        });
    }
}
