using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.EditorAssistant.Models;
using Umbraco.EditorAssistant.Services;

namespace Umbraco.EditorAssistant.Components;

[ViewComponent]
public sealed class EditorAssistantViewComponent(IEditorAssistantService editorAssistantService, IUmbracoContextAccessor umbracoContextAccessor,
    IOptions<EditorAssistantOptions> options, ILogger logger) : ViewComponent
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
        CancellationToken requestAborted = HttpContext.RequestAborted;

        try
        {
            EditorAssistantViewModel? model = await editorAssistantService.CreateModelAsync(HttpContext, currentContent, requestAborted);
            return model is null || requestAborted.IsCancellationRequested ? Content(string.Empty) : View(model);
        }
        catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
        {
            logger.Error("Request was canceled.");
            return Content(string.Empty);
        }
    }
}
