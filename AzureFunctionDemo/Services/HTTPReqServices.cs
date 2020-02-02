using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Services
{
    public class HTTPReqServices
    {
        ILogger _logger;
        private readonly HttpClient _HttpClient;
        private readonly AzureServiceTokenProvider _azureServiceTokenProvider;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        public HTTPReqServices(ILogger logger)
        {
            _logger = logger;
            _HttpClient = new HttpClient();
            _azureServiceTokenProvider = new AzureServiceTokenProvider();
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
               HttpStatusCode.RequestTimeout, // 408
               HttpStatusCode.InternalServerError, // 500
               HttpStatusCode.BadGateway, // 502
               HttpStatusCode.ServiceUnavailable, // 503
               HttpStatusCode.GatewayTimeout // 504
            };
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrInner<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                  .WaitAndRetryAsync(new[]
                  {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(8)
                  });
        }

        public async Task<bool> CallApiAsync(string url, string authURL)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("url is empty or null", nameof(url));
            }
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            string responseString;
            bool successful;
            try
            {
                HttpResponseMessage response;
                response = await _retryPolicy.ExecuteAsync(async () =>
                         await CreateAndSendMessageAsync(url, authURL)
                    );
                responseString = await response.Content.ReadAsStringAsync();
                successful = response.IsSuccessStatusCode;
                if (successful)
                {
                    _logger.LogInformation(responseString);
                }
                else
                {
                    _logger.LogError(responseString);
                }
                return successful;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message.ToString());
                if (ex.Message.Contains("One or more errors"))
                {
                    _logger.LogError(ex.InnerException.Message);
                }
                return false;
            }
        }

        private async Task<HttpResponseMessage> CreateAndSendMessageAsync(string url, string authURL)
        {
            HttpResponseMessage response;
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(authURL))
            {
                var token = await GetTokenAsync(authURL);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            response = await _HttpClient.SendAsync(requestMessage);
            return response;
        }

        public async Task<string> GetTokenAsync(string url)
        {
            string accessToken = await _azureServiceTokenProvider.GetAccessTokenAsync(url);
            return accessToken;
        }
    }
}
