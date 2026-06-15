using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GithubAnalyzer.WebApi.Config;
using GithubAnalyzer.WebApi.Interfaces;
using GithubAnalyzer.WebApi.Models.Emails;

namespace GithubAnalyzer.WebApi.Services.Email;

/// <summary>
/// Abstract base class for all email provider implementations.
/// Handles template loading, layout merging, and placeholder substitution —
/// leaving only the actual send logic to each concrete provider.
/// </summary>
public abstract class BaseEmailService : IEmailService
{
    // Template cache shared across all instances (thread-safe)
    private static readonly ConcurrentDictionary<string, string> _templateCache = new();

    protected readonly MailConfig Config;
    protected readonly ILogger Logger;

    private readonly string _templateDirectory;

    protected BaseEmailService(MailConfig config, ILogger logger, IWebHostEnvironment env)
    {
        _templateDirectory = Path.Combine(env.ContentRootPath, "Resources", "Emails");
        Config = config;
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(string toEmail, Mailable mailable, CancellationToken ct = default)
    {
        var htmlBody = await BuildBodyAsync(mailable, ct);

        // When email sending is disabled, log instead of sending
        if (!Config.IsEnabled)
        {
            Logger.LogInformation(
                "Email sending is disabled. Simulated sending to {ToEmail}.\nSubject: {Subject}\nBody:\n{HtmlBody}",
                toEmail, mailable.Subject, htmlBody);
            return;
        }

        await SendCoreAsync(toEmail, mailable.Subject, htmlBody, ct);
    }

    /// <summary>
    /// Provider-specific send logic. Called only when <see cref="MailConfig.IsEnabled"/> is <c>true</c>.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="subject">Resolved email subject.</param>
    /// <param name="htmlBody">Fully rendered HTML body (template + placeholders applied).</param>
    /// <param name="ct">Cancellation token.</param>
    protected abstract Task SendCoreAsync(string toEmail, string subject, string htmlBody, CancellationToken ct);

    /// <summary>
    /// Loads the HTML template, merges it into the layout, and substitutes all placeholders.
    /// Results are cached in-memory to avoid repeated disk reads.
    /// </summary>
    protected async Task<string> BuildBodyAsync(Mailable mailable, CancellationToken ct)
    {
        var htmlBody = await LoadTemplateAsync(mailable.TemplateName, ct);
        var placeholders = mailable.BuildPlaceholders();

        foreach (var (key, value) in placeholders)
        {
            htmlBody = htmlBody.Replace($"{{{{{key}}}}}", value);
        }

        return htmlBody;
    }

    /// <summary>
    /// Loads the HTML template from disk, merging it into <c>_Layout.html</c> if present.
    /// Templates are cached after the first load.
    /// </summary>
    private async Task<string> LoadTemplateAsync(string templateName, CancellationToken ct)
    {
        if (_templateCache.TryGetValue(templateName, out var cached))
            return cached;

        var templatePath = Path.Combine(_templateDirectory, $"{templateName}.html");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException(
                $"Email template '{templateName}' not found at: {templatePath}");
        }

        var templateContent = await File.ReadAllTextAsync(templatePath, ct);

        var layoutPath = Path.Combine(_templateDirectory, "_Layout.html");
        var finalHtml = templateContent;

        if (File.Exists(layoutPath))
        {
            var layoutContent = await File.ReadAllTextAsync(layoutPath, ct);
            finalHtml = layoutContent.Replace("{{RenderBody}}", templateContent);

            // Resolve CSS variables dynamically before inlining
            finalHtml = ResolveCssVariables(finalHtml);

            // Inline CSS automatically using PreMailer.Net
            var inlineResult = PreMailer.Net.PreMailer.MoveCssInline(finalHtml,
              removeStyleElements: true, removeComments: true);

            finalHtml = inlineResult.Html;
        }

        _templateCache.TryAdd(templateName, finalHtml);
        return finalHtml;
    }

    /// <summary>
    /// Resolves CSS variables defined in the <c>:root</c> block
    /// and replaces all occurrences of <c>var(--variable)</c> globally.
    /// </summary>
    private static string ResolveCssVariables(string html)
    {
        var variables = new Dictionary<string, string>();
        var rootMatch = Regex.Match(html, @":root\s*\{([^}]+)\}");
        
        if (rootMatch.Success)
        {
            var varMatches = Regex.Matches(rootMatch.Groups[1].Value, @"(--[a-zA-Z0-9-]+)\s*:\s*([^;]+);");
            foreach (Match match in varMatches)
            {
                variables[match.Groups[1].Value] = match.Groups[2].Value.Trim();
            }
        }

        var resolvedHtml = html;
        foreach (var (varName, varValue) in variables)
        {
            resolvedHtml = Regex.Replace(resolvedHtml, 
                $@"var\(\s*{Regex.Escape(varName)}(?:\s*,\s*[^)]+)?\)", 
                varValue);
        }

        // Remove :root block to prevent ExCSS parsing errors
        resolvedHtml = Regex.Replace(resolvedHtml, @":root\s*\{[^}]+\}", string.Empty);
        
        // Ensure no leftover var() causes parsing errors
        resolvedHtml = Regex.Replace(resolvedHtml, @"var\([^)]+\)", "inherit");

        return resolvedHtml;
    }
}
