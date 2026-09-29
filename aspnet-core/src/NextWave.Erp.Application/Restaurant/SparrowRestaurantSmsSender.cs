using Abp.Dependency;
using Abp.UI;
using NextWave.Erp.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public class SparrowRestaurantSmsSender(IAppConfigurationAccessor configuration) : IRestaurantSmsSender, ITransientDependency
    {
        private static readonly HttpClient Http = new();

        public async Task SendAsync(string number, string message)
        {
            var token = configuration.Configuration["SparrowSms:Token"];
            var sender = configuration.Configuration["SparrowSms:SenderId"];
            var endpoint = configuration.Configuration["SparrowSms:ApiUrl"] ?? "https://api.sparrowsms.com/v2/sms/";
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(sender))
                throw new UserFriendlyException("Sparrow SMS credentials and an approved sender ID must be configured");

            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("from", sender),
                new KeyValuePair<string, string>("to", number),
                new KeyValuePair<string, string>("text", message)
            });
            using var response = await Http.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new UserFriendlyException($"Sparrow SMS returned HTTP {(int)response.StatusCode}");

            try
            {
                using var json = JsonDocument.Parse(body);
                if (!json.RootElement.TryGetProperty("response_code", out var responseCode) || responseCode.GetInt32() != 200)
                    throw new UserFriendlyException("Sparrow SMS did not accept the message");
            }
            catch (JsonException)
            {
                throw new UserFriendlyException("Sparrow SMS returned an invalid response");
            }
        }
    }
}
