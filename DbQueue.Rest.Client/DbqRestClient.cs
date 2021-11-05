using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace DbQueue
{
    public class DbqRestClient : IDbqBoth, IDisposable
    {
        public DbqRestClient(string address, DbqRestClientSettings? settings = null)
        {
            _address = address;
            _settings = settings ?? new();
            _client = new(CreateClient);
            StackMode = _settings.StackMode;
            LockTimeout = _settings.LockTimeout;
        }

        private readonly string _address;
        private readonly DbqRestClientSettings _settings;
        private readonly Lazy<HttpClient> _client;


        public HttpRequestHeaders DefaultRequestHeaders => _client.Value.DefaultRequestHeaders;
        public bool StackMode { get; set; }
        public TimeSpan LockTimeout { get; set; }

        public void Dispose()
        {
            if (_client.IsValueCreated) _client.Value.Dispose();
        }

        public async Task Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default)
        {
            var type = Join(types);
            var url = @$"dbq/{QueueStack()}/{Encode(queue)}?{QueryArgs(new()
            {
                { "type", type?.str },
                { "separator", type?.sep },
            })}";

            using var response = await _client.Value.DeleteAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            var url = $"dbq/{QueueStack()}/{Encode(queue)}/count";
            using var response = await _client.Value.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<long>(content);
        }

        public async Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default)
        {
            var queue = Join(queues);
            var url = @$"dbq/{QueueStack()}/{Encode(queue?.str)}?{QueryArgs(new()
            {
                { "type", type },
                { "availableAfter", availableAfter },
                { "removeAfter", removeAfter },
                { "separator", queue?.sep },
            })}";

            using var ms = new MemoryStream();

            while (await data.MoveNextAsync())
                ms.Write(data.Current);

            ms.Position = 0;

            using var content = new StreamContent(ms, 4096);

            using var response = await _client.Value.PostAsync(url, content, cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            var url = $"dbq/{QueueStack()}/{Encode(queue)}/peek?index={index}";
            var response = await _client.Value.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
                return null;

            response.EnsureSuccessStatusCode();

            var data = GetEnumerator(response, cancellationToken);
            await data.MoveNextAsync(); // start enumerate for connect dispose

            return data;
        }

        public async Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> Pop(string queue, CancellationToken cancellationToken = default)
        {
            var url = $"dbq/{QueueStack()}/{Encode(queue)}?useAck=true&lockTimeout={(int)LockTimeout.TotalMilliseconds}";
            var response = await _client.Value.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
                return null;

            response.EnsureSuccessStatusCode();

            var ackKey = response.Headers.TryGetValues("ack-key", out var a) ? a.FirstOrDefault() : null;

            if (ackKey == null)
                throw new Exception("the response does not contain an ack-key");

            var commited = false;

            var data = GetEnumerator(response, cancellationToken);
            await data.MoveNextAsync(); // start enumerate for connect dispose

            return new DbqAck<IAsyncEnumerator<byte[]>>(data,
                commit: async () =>
                {
                    await Commit(ackKey, true, cancellationToken);
                    commited = true;
                },
                dispose: async () =>
                {
                    if (!commited)
                        await Commit(ackKey, false);

                    response.Dispose();
                });
        }

        private async Task Commit(string ackKey, bool result, CancellationToken cancellationToken = default)
        {
            var url = @$"dbq/ack/{ackKey}";

            using var response = result
                ? await _client.Value.PostAsync(url, null, cancellationToken)
                : await _client.Value.DeleteAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();
        }



        private static async IAsyncEnumerator<byte[]> GetEnumerator(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                yield return BytesEmpty;

                using var content = await response.Content.ReadAsStreamAsync();

                var buffer = new byte[4096];
                var count = 0;

                while ((count = await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    var chunk = new byte[count];
                    Array.Copy(buffer, 0, chunk, 0, count);
                    yield return chunk;
                }
            }
            finally
            {
                response.Dispose();
            }
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient(_settings.Credentials != null
                    ? new HttpClientHandler { Credentials = _settings.Credentials }
                    : new HttpClientHandler { UseDefaultCredentials = true });

            client.BaseAddress = new Uri(_address);

            foreach (var kvp in _settings.DefaultRequestHeaders)
                client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);

            return client;
        }

        private string QueueStack()
        {
            return StackMode ? "stack" : "queue";
        }

        private static string QueryArgs(Dictionary<string, object?> args)
        {
            return string.Join("&", args
                .Select(x => x.Value == null ? null : $"{x.Key}={QueryValue(x.Value)}")
                .Where(x => x != null));
        }

        private static string? QueryValue(object obj)
        {
            var type = obj.GetType();

            var val = type == typeof(DateTime) ? ((DateTime)obj).ToUniversalTime().ToString("o")
                : obj is string ? obj as string
                : obj.ToString();

            return Encode(val);
        }

        private static string? Encode(string? val)
        {
            return val == null ? null : HttpUtility.UrlEncode(val);
        }

        private static (string sep, string str)? Join(IEnumerable<string>? values = null)
        {
            if (values?.Any() != true)
                return null;

            var separator = new[] { ",", ";", "|", "-", "_", ".", " " }.FirstOrDefault(x => !values.Any(xx => xx.Contains(x)))
                ?? throw new ArgumentException($"Could not find correct separator for enumerated values: {string.Join(",", values)}");

            return (separator, string.Join(separator, values));
        }

        private static readonly byte[] BytesEmpty = new byte[0];
    }
}
