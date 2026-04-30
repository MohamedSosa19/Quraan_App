using FluentAssertions;
using Quraan.Api.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace Quraan.UnitTests.Logging;

/// <summary>
/// T180 — Verifies the PII-stripping Serilog enricher redacts the value categories
/// listed in FR-038 (auth headers, bearer tokens, password fragments, emails) and
/// leaves benign content alone.
/// </summary>
public sealed class PiiStrippingEnricherTests
{
    [Theory]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig", "Authorization: [REDACTED]")]
    [InlineData("authorization: Basic dXNlcjpwYXNz", "Authorization: [REDACTED]")]
    [InlineData("Cookie: session=abc; theme=dark", "Cookie: [REDACTED]")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.body.sig", "Bearer [REDACTED]")]
    [InlineData("password=hunter2&next=/", "password=[REDACTED]&next=/")]
    [InlineData("{\"password\":\"hunter2\",\"email\":\"a@b.com\"}", "{\"password\":\"[REDACTED]\",\"email\":\"[REDACTED]\"}")]
    [InlineData("contact: jane.doe+tag@example.co.uk for help", "contact: [REDACTED] for help")]
    public void Redact_strips_known_pii_patterns(string input, string expected)
    {
        var actual = PiiStrippingEnricher.Redact(input);
        actual.Should().Be(expected);
    }

    [Fact]
    public void Redact_leaves_neutral_strings_untouched()
    {
        const string input = "GET /api/v1/surahs/1 → 200 in 12ms";
        PiiStrippingEnricher.Redact(input).Should().Be(input);
    }

    [Fact]
    public void Enrich_rewrites_string_scalar_properties_in_place()
    {
        var enricher = new PiiStrippingEnricher();
        var template = new MessageTemplateParser().Parse("login attempt");
        var props = new List<LogEventProperty>
        {
            new("Header", new ScalarValue("Authorization: Bearer secret")),
            new("Email",  new ScalarValue("user@example.com")),
            new("Path",   new ScalarValue("/api/v1/surahs/1")), // untouched
            new("Count",  new ScalarValue(42)),                  // non-string; untouched
        };
        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information,
            exception: null, messageTemplate: template, properties: props);

        enricher.Enrich(evt, new TestPropertyFactory());

        ((ScalarValue)evt.Properties["Header"]).Value.Should().Be("Authorization: [REDACTED]");
        ((ScalarValue)evt.Properties["Email"]).Value.Should().Be("[REDACTED]");
        ((ScalarValue)evt.Properties["Path"]).Value.Should().Be("/api/v1/surahs/1");
        ((ScalarValue)evt.Properties["Count"]).Value.Should().Be(42);
    }

    private sealed class TestPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }
}
