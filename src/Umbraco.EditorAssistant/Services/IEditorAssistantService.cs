using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.EditorAssistant.Models;

namespace Umbraco.EditorAssistant.Services;

public interface IEditorAssistantService
{
    Task<EditorAssistantViewModel?> CreateModelAsync(HttpContext httpContext, IPublishedContent? currentContent, CancellationToken cancellationToken = default);
}
