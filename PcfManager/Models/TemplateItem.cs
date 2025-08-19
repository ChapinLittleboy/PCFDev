namespace PcfManager.Models;

// TemplateItem.cs
public sealed class TemplateItem
{
    public string Name { get; init; } = default!;
    public string Path { get; init; } = default!;  // full path (private) or relative path (public)
}

