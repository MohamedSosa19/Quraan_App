using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Quraan.Api.Logging;

/// <summary>
/// T180 — Serilog enricher that redacts personally-identifiable / sensitive
/// values from the rendered message and from any string-valued properties on
/// the LogEvent. Targets:
///   • Authorization / Cookie / Set-Cookie headers (full value)
///   • Bearer tokens
///   • <c>password=...</c> / <c>"password":"..."</c> form/JSON fragments
///   • Email addresses
///
/// This is a *defense-in-depth* layer: the request-logging middleware does not
/// log bodies and headers are not enriched into the log event by default. The
/// enricher exists so that any future third-party enricher or hand-written log
/// statement that accidentally captures a header / body fragment still gets
/// scrubbed before it reaches the sink. (FR-038, SC-010, Principle V)
/// </summary>
public sealed partial class PiiStrippingEnricher : ILogEventEnricher
{
    public const string Redaction = "[REDACTED]";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        // Walk every scalar/string property and rewrite if needed.
        var keys = logEvent.Properties.Keys.ToArray();
        foreach (var key in keys)
        {
            if (logEvent.Properties[key] is ScalarValue { Value: string s })
            {
                var redacted = Redact(s);
                if (!ReferenceEquals(redacted, s))
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(key, redacted));
                }
            }
        }
    }

    /// <summary>Public for unit-testing the redaction rules.</summary>
    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var work = value;

        // Authorization: <scheme> <token>  →  Authorization: [REDACTED]
        work = AuthorizationHeader().Replace(work, $"Authorization: {Redaction}");

        // Cookie: foo=bar  →  Cookie: [REDACTED]
        work = CookieHeader().Replace(work, $"Cookie: {Redaction}");

        // Bearer eyJhbGciOi... → Bearer [REDACTED]
        work = BearerToken().Replace(work, $"Bearer {Redaction}");

        // password=secret (form) / "password":"secret" (json) → password=[REDACTED]
        work = PasswordValue().Replace(work, m => m.Groups[1].Value + Redaction + m.Groups[3].Value);

        // user@domain.tld  →  [REDACTED]
        work = Email().Replace(work, Redaction);

        return work;
    }

    [GeneratedRegex(@"Authorization:\s*\S+(\s+\S+)?", RegexOptions.IgnoreCase)]
    private static partial Regex AuthorizationHeader();

    [GeneratedRegex(@"Cookie:\s*[^\r\n]+", RegexOptions.IgnoreCase)]
    private static partial Regex CookieHeader();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9._\-+/=]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerToken();

    // Captures the password-key prefix in (1), the value in (2), and any closing
    // quote/punctuation in (3) so the replacement preserves form/JSON shape.
    [GeneratedRegex(@"(""?password""?\s*[:=]\s*""?)([^""&\s,}]+)("")?", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordValue();

    [GeneratedRegex(@"\b[\w.+-]+@[\w-]+(\.[\w-]+)+\b")]
    private static partial Regex Email();
}
