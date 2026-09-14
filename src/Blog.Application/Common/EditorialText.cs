namespace Blog.Application.Common;

public static class EditorialText
{
    public static string WithoutAiDashes(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        return value
            .Replace(" — ", ": ", StringComparison.Ordinal)
            .Replace(" – ", ": ", StringComparison.Ordinal)
            .Replace("—", ": ", StringComparison.Ordinal)
            .Replace("–", "-", StringComparison.Ordinal);
    }
}
