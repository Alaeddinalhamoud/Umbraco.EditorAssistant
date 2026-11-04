using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
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

public sealed class EditorAssistantTagHelperComponentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Body_RendersOnlyForEditors_AndPreventsSharedCaching(bool authenticated)
    {
        Mock<IPublishedContent> content = new();
        Mock<IPublishedRequest> request = new();
        request.SetupGet(x => x.PublishedContent).Returns(content.Object);
        Mock<IUmbracoContext> umbracoContext = new();
        umbracoContext.SetupGet(x => x.PublishedRequest).Returns(request.Object);
        IUmbracoContext? resolvedContext = umbracoContext.Object;
        Mock<IUmbracoContextAccessor> accessor = new();
        accessor.Setup(x => x.TryGetUmbracoContext(out resolvedContext)).Returns(true);

        DefaultHttpContext httpContext = new();
        Mock<IEditorAssistantService> service = new();
        service.Setup(x => x.CreateModelAsync(httpContext, content.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticated ? new EditorAssistantViewModel("/umbraco/edit/test", "News & events") : null);
        EditorAssistantTagHelperComponent component = new(service.Object, accessor.Object,
            new HttpContextAccessor { HttpContext = httpContext }, Options.Create(new EditorAssistantOptions()));
        TagHelperContext context = new("body", new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        TagHelperOutput output = new("body", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await component.ProcessAsync(context, output);

        Assert.Equal("private, no-store, max-age=0", httpContext.Response.Headers.CacheControl.ToString());
        if (authenticated)
        {
            Assert.Contains("href=\"/umbraco/edit/test\"", output.PostContent.GetContent());
            Assert.Contains("News &amp; events", output.PostContent.GetContent());
        }
        else
        {
            Assert.Empty(output.PostContent.GetContent());
        }
        service.VerifyAll();
    }
}
