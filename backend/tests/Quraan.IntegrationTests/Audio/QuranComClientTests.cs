using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Quraan.Domain.Repositories;
using Quraan.Infrastructure.ExternalAudio;
using Xunit;

namespace Quraan.IntegrationTests.Audio;

/// <summary>
/// T105 — exercises QuranComClient against an in-process stub HttpMessageHandler
/// to validate the FR-013 (graceful empty) and FR-014 (502 propagation) paths
/// without depending on an external network or WireMock.
/// </summary>
public sealed class QuranComClientTests
{
    private static QuranComClient Build(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.quran.com") }, NullLogger<QuranComClient>.Instance);

    [Fact]
    public async Task Maps_verse_timings_when_upstream_returns_data()
    {
        const string body = """
        {
          "audio_files": [
            {
              "verse_timings": [
                { "verse_key": "78:1", "timestamp_from": 0,    "timestamp_to": 1500 },
                { "verse_key": "78:2", "timestamp_from": 1500, "timestamp_to": 3200 }
              ]
            }
          ]
        }
        """;
        var client = Build(StubHandler.JsonResponse(HttpStatusCode.OK, body));
        var result = await client.GetTimingsAsync(7, 78);
        result.HasTimings.Should().BeTrue();
        result.Timings.Should().HaveCount(2);
        result.Timings[0].NumberInSurah.Should().Be(1);
        result.Timings[0].FromMs.Should().Be(0);
        result.Timings[0].ToMs.Should().Be(1500);
        result.Timings[1].FromMs.Should().Be(1500);
    }

    [Fact]
    public async Task Returns_empty_with_HasTimings_false_when_upstream_returns_404_FR013()
    {
        var client = Build(StubHandler.JsonResponse(HttpStatusCode.NotFound, "{}"));
        var result = await client.GetTimingsAsync(99, 78);
        result.HasTimings.Should().BeFalse();
        result.Timings.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_audio_files_array_is_empty()
    {
        var client = Build(StubHandler.JsonResponse(HttpStatusCode.OK, "{\"audio_files\":[]}"));
        var result = await client.GetTimingsAsync(7, 78);
        result.HasTimings.Should().BeFalse();
        result.Timings.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_on_parse_error_FR013()
    {
        var client = Build(StubHandler.JsonResponse(HttpStatusCode.OK, "not json"));
        var result = await client.GetTimingsAsync(7, 78);
        result.HasTimings.Should().BeFalse();
        result.Timings.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_UpstreamAudioFailure_on_503_FR014()
    {
        var client = Build(StubHandler.JsonResponse(HttpStatusCode.ServiceUnavailable, "{}"));
        var act = async () => await client.GetTimingsAsync(7, 78);
        await act.Should().ThrowAsync<UpstreamAudioFailureException>();
    }

    [Fact]
    public async Task Throws_UpstreamAudioFailure_on_network_error_FR014()
    {
        var client = Build(StubHandler.Throws(new HttpRequestException("connect failed")));
        var act = async () => await client.GetTimingsAsync(7, 78);
        await act.Should().ThrowAsync<UpstreamAudioFailureException>();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        private StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;

        public static StubHandler JsonResponse(HttpStatusCode status, string body) =>
            new((_, _) =>
            {
                var resp = new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(resp);
            });

        public static StubHandler Throws(Exception ex) =>
            new((_, _) => Task.FromException<HttpResponseMessage>(ex));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _send(request, cancellationToken);
    }
}
