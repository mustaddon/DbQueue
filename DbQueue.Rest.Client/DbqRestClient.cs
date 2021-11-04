using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        private readonly string _address;
        private readonly DbqRestClientSettings _settings;
        private readonly Lazy<HttpClient> _client;


        public HttpRequestHeaders DefaultRequestHeaders => _client.Value.DefaultRequestHeaders;

        public bool StackMode { get; set; }

        public void Dispose()
        {
            if (_client.IsValueCreated) _client.Value.Dispose();
        }

        public async Task Clear(string queue, IEnumerable<string>? types = null, CancellationToken cancellationToken = default)
        {
            var type = JoinEncode(types);
            var response = await _client.Value.DeleteAsync($"dbq/queue/{Encode(queue)}?type={type?.str}&separator={type?.sep}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<long> Count(string queue, CancellationToken cancellationToken = default)
        {
            var response = await _client.Value.GetAsync($"dbq/queue/{HttpUtility.UrlEncode(queue)}/count");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<long>(content);
        }

        public Task<IAsyncEnumerator<byte[]>?> Peek(string queue, long index = 0, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IDbqAcknowledgement<IAsyncEnumerator<byte[]>>?> Pop(string queue, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Push(IEnumerable<string> queues, IAsyncEnumerator<byte[]> data, string? type = null, DateTime? availableAfter = null, DateTime? removeAfter = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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

        private static string Encode(string? val)
        {
            return val == null ? string.Empty : HttpUtility.UrlEncode(val);
        }

        private static (string sep, string str)? JoinEncode(IEnumerable<string>? values = null)
        {
            if (values?.Any() != true)
                return null;

            var separator = new[] { ",", ";", "|", "-", "." }.FirstOrDefault(x => !values.Any(xx => xx.Contains(x)))
                ?? throw new ArgumentException($"Could not find separator for enumerated value: {string.Join(",", values)}");

            return (Encode(separator), Encode(string.Join(separator, values)));
        }
    }
}
