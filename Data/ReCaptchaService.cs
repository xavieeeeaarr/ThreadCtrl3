using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ThrdCtrl2.Data
{
    public class ReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public ReCaptchaService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _secretKey = config["ReCaptcha:SecretKey"];
        }

        public async Task<(bool Success, string ErrorCodes)> VerifyWithErrorsAsync(string response)
        {
            if (string.IsNullOrEmpty(response)) return (false, "No response from widget");
            if (string.IsNullOrEmpty(_secretKey) || _secretKey.Contains("YOUR_REAL")) return (true, "");

            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _secretKey),
                    new KeyValuePair<string, string>("response", response)
                });

                var httpResponse = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonResponse);
                    
                    if (result != null && result.Success) return (true, "");
                    
                    string errors = result?.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "Unknown error";
                    return (false, errors);
                }
                return (false, "Google API unreachable");
            }
            catch (System.Exception ex)
            {
                return (false, "Exception: " + ex.Message);
            }
        }

        public async Task<bool> VerifyAsync(string response)
        {
            var result = await VerifyWithErrorsAsync(response);
            return result.Success;
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }
}
