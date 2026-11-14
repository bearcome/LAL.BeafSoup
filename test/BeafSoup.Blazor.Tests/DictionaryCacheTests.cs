using System.Net;
using System.Text;
using BeafSoup.Blazor.Shared.Services;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class DictionaryCacheTests
{
    [Fact]
    public async Task EnsureLoadedAsync_retries_after_empty_or_failed_load()
    {
        var handler = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "concept.drink": {
                        "conceptId": "b09e8e3b-4776-10de-703b-7ecf794cec46",
                        "pos": "Verb",
                        "kind": "Word",
                        "de-DE": { "lexemes": [] }
                      }
                    }
                    """, Encoding.UTF8, "application/json"),
            });
        var cache = new DictionaryCache(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5280/"),
        });

        await cache.EnsureLoadedAsync(CancellationToken.None);
        cache.AllEntries.Should().BeEmpty();
        await cache.EnsureLoadedAsync(CancellationToken.None);

        cache.AllEntries.Should().ContainSingle();
        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task FindPrimaryAudioUrl_按词元或形态命中并返回主词元音频()
    {
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "concept.coffee": {
                    "conceptId": "b09e8e3b-4776-10de-703b-7ecf794cec46",
                    "pos": "Noun",
                    "kind": "Word",
                    "de-DE": {
                      "lexemes": [{
                        "lemma": "Kaffee",
                        "displayWord": "der Kaffee",
                        "isPrimary": true,
                        "forms": { "declension": ["Kaffee", "Kaffees"] },
                        "audioUrl": "de-DE/der-Kaffee-Mia.mp3"
                      }]
                    }
                  }
                }
                """, Encoding.UTF8, "application/json"),
        });
        var cache = new DictionaryCache(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5280/"),
        });
        await cache.EnsureLoadedAsync(CancellationToken.None);

        // 原型与变格形态都能解析到同一主词元音频
        cache.FindPrimaryAudioUrl("Kaffee", "de-DE").Should().Be("de-DE/der-Kaffee-Mia.mp3");
        cache.FindPrimaryAudioUrl("Kaffees", "de-DE").Should().Be("de-DE/der-Kaffee-Mia.mp3");
        cache.FindPrimaryAudioUrl("Wasser", "de-DE").Should().BeNull();
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
