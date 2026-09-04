using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.EditorAssistant.Services;
using Umbraco.EditorAssistant.Models;
using Umbraco.EditorAssistant.Components;

namespace Umbraco.EditorAssistant.Composers;

public sealed class EditorAssistantComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services
            .AddOptions<EditorAssistantOptions>()
            .BindConfiguration(EditorAssistantOptions.SectionName)
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.BackofficePath) &&
                            settings.BackofficePath.StartsWith('/'),
                "EditorAssistant:BackofficePath must begin with '/'.")
            .ValidateOnStart();

        builder.Services.AddScoped<IEditorAssistantService, EditorAssistantService>();
        builder.Services.AddTransient<ITagHelperComponent, EditorAssistantTagHelperComponent>();
    }
}
