using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.EditorAssistant.Services;
using Umbraco.EditorAssistant.Models;

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
    }
}
