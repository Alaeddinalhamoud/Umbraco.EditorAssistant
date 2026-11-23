using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.EditorAssistant.Models;

namespace Umbraco.EditorAssistant.Services;

public sealed class EditorAssistantService(IOptions<EditorAssistantOptions> options) : IEditorAssistantService
{
    // Umbraco 18 moved Name to IPublishedElement. Resolve its declaring interface
    // at runtime so the same package binary also works with Umbraco 17.
    private static readonly System.Reflection.PropertyInfo ContentNameProperty =
        typeof(IPublishedContent).GetProperty(nameof(IPublishedContent.Name))
        ?? typeof(IPublishedElement).GetProperty(nameof(IPublishedContent.Name))
        ?? throw new InvalidOperationException("The installed Umbraco version does not expose a published content name.");

    private readonly EditorAssistantOptions _options = options.Value;

    public async Task<EditorAssistantViewModel?> CreateModelAsync(HttpContext httpContext, IPublishedContent? currentContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        cancellationToken.ThrowIfCancellationRequested();

        AuthenticateResult authentication = await httpContext.AuthenticateAsync(Constants.Security.BackOfficeExposedAuthenticationType);

        if (!authentication.Succeeded || authentication.Principal?.Identity?.IsAuthenticated != true || currentContent is null || currentContent.Key == Guid.Empty)
            return null;

        string backofficePath = NormalizeBackofficePath(_options.BackofficePath);
        string applicationPath = httpContext.Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
        string contentKey = currentContent.Key.ToString("D").ToLowerInvariant();
        string editUrl = $"{applicationPath}{backofficePath}/section/content/workspace/document/edit/{contentKey}";

        return new EditorAssistantViewModel(editUrl, (string?)ContentNameProperty.GetValue(currentContent) ?? string.Empty);
    }

    #region Private Methods
    private static string NormalizeBackofficePath(string path)
    {
        string normalized = string.IsNullOrWhiteSpace(path) ? "/umbraco" : path.Trim();

        if (!normalized.StartsWith('/'))
            normalized = $"/{normalized}";

        return normalized.TrimEnd('/');
    }
    #endregion
}
