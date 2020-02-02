using System;
using AzureFunctionDemo.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
namespace AzureFunctionDemo
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task RunAsync([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var authURL = Environment.GetEnvironmentVariable("AuthURL");
            var urlString = Environment.GetEnvironmentVariable("CallUrl");
            HTTPReqServices httpCaller = new HTTPReqServices(log);
            await httpCaller.CallApiAsync(urlString, authURL);
        }
    }
}
