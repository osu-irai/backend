using Microsoft.AspNetCore.Mvc.ModelBinding;
using osuRequestor.Features.Requests.Models;

namespace osuRequestor.Features.Requests.ModelBinding;

public class RequestModelBinderProvider : IModelBinderProvider
{
    #region IModelBinderProvider Members

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(PostBaseRequest) ||
            context.Metadata.ModelType == typeof(PostRequestWithName))
        {
            return new RequestModelBinder();
        }

        return null;
    }

    #endregion
}
