using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.EditorAssistant.Models;
using Umbraco.EditorAssistant.Services;

namespace Umbraco.EditorAssistant.Components;

[ViewComponent]
public sealed class EditorAssistantViewComponent(IEditorAssistantService editorAssistantService, IUmbracoContextAccessor umbracoContextAccessor,
    IOptions<EditorAssistantOptions> options) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (options.Value.DisableResponseCaching)
        {
            HttpContext.Response.Headers.CacheControl = "private, no-store, max-age=0";
            HttpContext.Response.Headers.Pragma = "no-cache";
            HttpContext.Response.Headers.Expires = "0";
        }

        if (!umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext))
            return Content(string.Empty);

        IPublishedContent? currentContent = umbracoContext.PublishedRequest?.PublishedContent;
        EditorAssistantViewModel? model = await editorAssistantService.CreateModelAsync(HttpContext, currentContent, HttpContext.RequestAborted);

        return model is null ? Content(string.Empty) : View(model);
    }
}
