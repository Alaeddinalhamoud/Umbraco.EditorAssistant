using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.EditorAssistant.Services;
using Umbraco.EditorAssistant.Models;

namespace Umbraco.EditorAssistant.Components;

/// <summary>
/// Adds the package stylesheet to head and the editor link to body without
/// requiring changes to the consuming application's layouts or views.
/// </summary>
public sealed class EditorAssistantTagHelperComponent(IEditorAssistantService editorAssistantService, IUmbracoContextAccessor umbracoContextAccessor,
    IHttpContextAccessor httpContextAccessor, IOptions<EditorAssistantOptions> options) : TagHelperComponent
{
    public override int Order => int.MaxValue;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.Equals(context.TagName, "head", StringComparison.OrdinalIgnoreCase))
        {
            string pathBase = GetPathBase();
            output.PostContent.AppendHtml($"<link rel=\"stylesheet\" href=\"{pathBase}/App_Plugins/EditorAssistant.Umbraco/css/editor-assistant.css\" />");
            return;
        }

        if (!string.Equals(context.TagName, "body", StringComparison.OrdinalIgnoreCase))
            return;

        if (options.Value.DisableResponseCaching)
        {
            HttpContext httpContext = GetHttpContext();
            httpContext.Response.Headers.CacheControl = "private, no-store, max-age=0";
            httpContext.Response.Headers.Pragma = "no-cache";
            httpContext.Response.Headers.Expires = "0";
        }

        if (!umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext? umbracoContext))
            return;

        IPublishedContent? currentContent = umbracoContext.PublishedRequest?.PublishedContent;
        EditorAssistantViewModel? model = await editorAssistantService.CreateModelAsync(GetHttpContext(), currentContent, GetHttpContext().RequestAborted);

        if (model is null)
            return;

        string editUrl = HtmlEncoder.Default.Encode(model.EditUrl);
        string contentName = HtmlEncoder.Default.Encode(model.ContentName);

        output.PostContent.AppendHtml($$"""
            <a class="editor-assistant"
               href="{{editUrl}}"
               target="_blank"
               aria-label="Edit {{contentName}} in Umbraco (opens in a new tab)"
               title="Edit {{contentName}} in Umbraco (opens in a new tab)"
               rel="nofollow noopener noreferrer">
                <svg class="editor-assistant__icon" viewBox="0 0 24 24" width="20" height="20" aria-hidden="true" focusable="false">
                    <path fill="currentColor" d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25Zm17.71-10.04a1.003 1.003 0 0 0 0-1.42l-2.5-2.5a1.003 1.003 0 0 0-1.42 0l-1.96 1.96 3.75 3.75 2.13-1.79Z"></path>
                </svg>
                <span class="editor-assistant__label">Editor Assistant</span>
            </a>
            """);
    }

    #region Private methods
    private string GetPathBase()
        => GetHttpContext().Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;

    private HttpContext GetHttpContext()
        => httpContextAccessor.HttpContext
           ?? throw new InvalidOperationException("Editor Assistant requires an active HTTP request.");
    #endregion
}
