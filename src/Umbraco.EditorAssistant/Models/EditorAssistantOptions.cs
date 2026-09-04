namespace Umbraco.EditorAssistant.Models;

public sealed class EditorAssistantOptions
{
    public const string SectionName = "EditorAssistant";

    public string BackofficePath { get; set; } = "/umbraco";

    public bool DisableResponseCaching { get; set; } = true;
}
