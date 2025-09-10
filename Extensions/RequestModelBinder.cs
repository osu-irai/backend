using Microsoft.AspNetCore.Mvc.ModelBinding;
using osuRequestor.DTO.Requests;

namespace osuRequestor.Extensions;

public class RequestModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var request = bindingContext.HttpContext.Request;
        if (!(bool)request.ContentType?.Contains("application/json"))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            throw new ArgumentException();
        }

        object? model = null;
        if (await HasJsonProperty(request, "destinationName"))
        {
            model = await request.ReadFromJsonAsync<PostRequestWithName>();
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        else if (await HasJsonProperty(request, "destinationId"))
        {
            model = await request.ReadFromJsonAsync<PostBaseRequest>();
            bindingContext.Result = ModelBindingResult.Success(model);
        }

        bindingContext.Result = ModelBindingResult.Failed();
        return;
    }
    private async Task<bool> HasJsonProperty(HttpRequest request, string propertyName)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        
        return json.Contains($"\"{propertyName}\"", StringComparison.OrdinalIgnoreCase);
    }
}