using Microsoft.AspNetCore.Mvc.ModelBinding;
using osuRequestor.DTO.Requests;

namespace osuRequestor.Extensions;

public class RequestModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(PostBaseRequest) ||
            context.Metadata.ModelType == typeof(PostRequestWithName)) return new RequestModelBinder();

        return null;
    }
}