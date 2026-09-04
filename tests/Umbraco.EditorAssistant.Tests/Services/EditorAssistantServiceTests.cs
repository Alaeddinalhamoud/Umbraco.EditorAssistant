using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.EditorAssistant.Models;
using Umbraco.EditorAssistant.Services;
using Xunit;

namespace Umbraco.EditorAssistant.Tests.Services;

public sealed class EditorAssistantServiceTests
{
    [Fact]
    public async Task CreateModelAsync_ReturnsNull_WhenBackofficeUserIsNotAuthenticated()
    {
        DefaultHttpContext context = CreateHttpContext(AuthenticateResult.NoResult());
        IPublishedContent content = CreateContent(Guid.NewGuid(), "Home");
        EditorAssistantService sut = CreateService();

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, content);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateModelAsync_ReturnsNull_WhenCurrentContentIsMissing()
    {
        DefaultHttpContext context = CreateHttpContext(CreateSuccessfulAuthentication());
        EditorAssistantService sut = CreateService();

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateModelAsync_ReturnsNull_WhenContentKeyIsEmpty()
    {
        DefaultHttpContext context = CreateHttpContext(CreateSuccessfulAuthentication());
        IPublishedContent content = CreateContent(Guid.Empty, "Home");
        EditorAssistantService sut = CreateService();

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, content);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateModelAsync_CreatesDefaultEditUrl_ForAuthenticatedUser()
    {
        Guid contentKey = Guid.Parse("D8D2D57A-0B5E-48B4-8E5D-E72A747F5738");
        DefaultHttpContext context = CreateHttpContext(CreateSuccessfulAuthentication());
        IPublishedContent content = CreateContent(contentKey, "About us");
        EditorAssistantService sut = CreateService();

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, content);

        Assert.NotNull(result);
        Assert.Equal("About us", result.ContentName);
        Assert.Equal(
            "/umbraco/section/content/workspace/document/edit/d8d2d57a-0b5e-48b4-8e5d-e72a747f5738",
            result.EditUrl);
    }

    [Fact]
    public async Task CreateModelAsync_IncludesPathBase_AndNormalizesCustomBackofficePath()
    {
        Guid contentKey = Guid.Parse("DA2749C8-DBD1-48E7-B977-86AD814A48D1");
        DefaultHttpContext context = CreateHttpContext(CreateSuccessfulAuthentication());
        context.Request.PathBase = "/website";
        IPublishedContent content = CreateContent(contentKey, "Contact");
        EditorAssistantService sut = CreateService("/management/");

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, content);

        Assert.NotNull(result);
        Assert.Equal(
            "/website/management/section/content/workspace/document/edit/da2749c8-dbd1-48e7-b977-86ad814a48d1",
            result.EditUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateModelAsync_UsesDefaultBackofficePath_WhenConfiguredPathIsBlank(string backofficePath)
    {
        Guid contentKey = Guid.Parse("4DF69DB7-7378-40D7-B67C-D6B35C2E3E0E");
        DefaultHttpContext context = CreateHttpContext(CreateSuccessfulAuthentication());
        IPublishedContent content = CreateContent(contentKey, "News");
        EditorAssistantService sut = CreateService(backofficePath);

        EditorAssistantViewModel? result = await sut.CreateModelAsync(context, content);

        Assert.NotNull(result);
        Assert.StartsWith("/umbraco/section/content/workspace/document/edit/", result.EditUrl);
    }

    [Fact]
    public async Task CreateModelAsync_Throws_WhenCancellationIsRequested()
    {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        DefaultHttpContext context = new();
        EditorAssistantService sut = CreateService();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.CreateModelAsync(context, null, cancellation.Token));
    }

    [Fact]
    public async Task CreateModelAsync_Throws_WhenHttpContextIsNull()
    {
        EditorAssistantService sut = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.CreateModelAsync(null!, null));
    }

    private static EditorAssistantService CreateService(string backofficePath = "/umbraco")
    {
        EditorAssistantOptions settings = new() { BackofficePath = backofficePath };
        return new EditorAssistantService(Options.Create(settings));
    }

    private static DefaultHttpContext CreateHttpContext(AuthenticateResult authenticationResult)
    {
        Mock<IAuthenticationService> authenticationService = new();
        authenticationService
            .Setup(service => service.AuthenticateAsync(
                It.IsAny<HttpContext>(),
                Constants.Security.BackOfficeExposedAuthenticationType))
            .ReturnsAsync(authenticationResult);

        ServiceProvider services = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = services };
    }

    private static AuthenticateResult CreateSuccessfulAuthentication()
    {
        ClaimsIdentity identity = new(
            [new Claim(ClaimTypes.NameIdentifier, "editor")],
            Constants.Security.BackOfficeExposedAuthenticationType);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(
            principal,
            Constants.Security.BackOfficeExposedAuthenticationType);

        return AuthenticateResult.Success(ticket);
    }

    private static IPublishedContent CreateContent(Guid key, string name)
    {
        Mock<IPublishedContent> content = new();
        content.SetupGet(item => item.Key).Returns(key);
        content.SetupGet(item => item.Name).Returns(name);
        return content.Object;
    }
}
