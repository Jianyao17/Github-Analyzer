namespace GithubAnalyzer.WebApi.Models.Emails;

/// <summary>
/// Abstract base class for composing emails, inspired by Laravel's Mailable.
/// Each concrete implementation specifies its subject, template, and dynamic placeholder values.
/// </summary>
public abstract class Mailable
{
    /// <summary>
    /// The email subject line.
    /// </summary>
    public abstract string Subject { get; }

    /// <summary>
    /// The HTML template file name (without extension) located in Resources/Emails/.
    /// </summary>
    public abstract string TemplateName { get; }

    /// <summary>
    /// Returns a dictionary of placeholder keys and their replacement values.
    /// Keys correspond to <c>{{Key}}</c> tokens in the HTML template.
    /// </summary>
    public abstract Dictionary<string, string> GetPlaceholders();

    /// <summary>
    /// Returns the merged placeholders including common values such as the current year.
    /// Subclass placeholders take precedence over base values.
    /// </summary>
    public Dictionary<string, string> BuildPlaceholders()
    {
        var placeholders = new Dictionary<string, string>
        {
            ["AppName"] = "Github Analyzer",
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        // Merge subclass-specific placeholders (overrides base if key collides)
        foreach (var (key, value) in GetPlaceholders())
        {
            placeholders[key] = value;
        }

        return placeholders;
    }
}
