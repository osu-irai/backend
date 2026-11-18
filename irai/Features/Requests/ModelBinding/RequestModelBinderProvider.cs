using irai.Features.Requests.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace irai.Features.Requests.ModelBinding;

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
