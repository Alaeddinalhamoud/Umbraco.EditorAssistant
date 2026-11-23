using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.EditorAssistant.Components;
using Umbraco.EditorAssistant.Models;
using Umbraco.EditorAssistant.Services;
using Xunit;

namespace Umbraco.EditorAssistant.Tests.Components;

public sealed class EditorAssistantViewComponentTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task InvokeAsync_RendersOnlyForEditors_OnActiveRequests_AndPreventsSharedCaching(bool authenticated, bool requestAborted)
    {
        Mock<IPublishedContent> content = new();
        Mock<IPublishedRequest> request = new();
        request.SetupGet(x => x.PublishedContent).Returns(content.Object);
        Mock<IUmbracoContext> umbracoContext = new();
        umbracoContext.SetupGet(x => x.PublishedRequest).Returns(request.Object);
        IUmbracoContext? resolvedContext = umbracoContext.Object;
        Mock<IUmbracoContextAccessor> accessor = new();
        accessor.Setup(x => x.TryGetUmbracoContext(out resolvedContext)).Returns(true);

        using CancellationTokenSource cancellation = new();
        if (requestAborted)
            cancellation.Cancel();

        DefaultHttpContext httpContext = new() { RequestAborted = cancellation.Token };
        Mock<IEditorAssistantService> service = new();
        service.Setup(x => x.CreateModelAsync(httpContext, content.Object, cancellation.Token))
            .Returns((HttpContext _, IPublishedContent? _, CancellationToken token) =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(authenticated ? new EditorAssistantViewModel("/umbraco/edit/test", "News & events") : null);
            });
        EditorAssistantViewComponent component = new(service.Object, accessor.Object, Options.Create(new EditorAssistantOptions()))
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = httpContext }
            }
        };

        IViewComponentResult result = await component.InvokeAsync();

        Assert.Equal("private, no-store, max-age=0", httpContext.Response.Headers.CacheControl.ToString());
        if (authenticated && !requestAborted)
        {
            var view = Assert.IsType<ViewViewComponentResult>(result);
            Assert.Equal(new EditorAssistantViewModel("/umbraco/edit/test", "News & events"), view.ViewData!.Model);
        }
        else
        {
            Assert.Empty(Assert.IsType<ContentViewComponentResult>(result).Content);
        }
        service.VerifyAll();
    }
}
