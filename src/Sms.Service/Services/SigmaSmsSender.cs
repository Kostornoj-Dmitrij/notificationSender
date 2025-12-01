using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sms.Service.Models;
using Sms.Service.Services;
using Sms.Service.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

public class SigmaSmsSender : ISmsSender, IDisposable
{
    private readonly SmsSettings _smsSettings;
    private readonly ILogger<SigmaSmsSender> _logger;
    private readonly HttpClient _httpClient;
    private string _currentToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    public SigmaSmsSender(SmsSettings smsSettings, ILogger<SigmaSmsSender> logger)
    {
        _smsSettings = smsSettings;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://online.sigmasms.ru/"); // Измените базовый URL
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to send SMS to {PhoneNumber}, message length: {Length}",
                phoneNumber, message.Length);

            var token = await GetTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get SigmaSMS token for sending SMS to {PhoneNumber}", phoneNumber);
                return new SmsSendResult
                {
                    Success = false,
                    ErrorMessage = "Authentication failed"
                };
            }

            var messageId = await SendSmsInternalAsync(token, phoneNumber, message, cancellationToken);

            if (messageId.HasValue)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}, ExternalId: {ExternalId}",
                    phoneNumber, messageId.Value);
            }

            return new SmsSendResult
            {
                Success = messageId.HasValue,
                ExternalId = messageId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber} via SigmaSMS", phoneNumber);
            return new SmsSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<Guid?> SendSmsInternalAsync(string token, string phoneNumber, string message, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/sendings");
        request.Headers.Add("Authorization", token);

        var requestBody = new
        {
            recipient = phoneNumber,
            type = "sms",
            payload = new
            {
                sender = _smsSettings.Sender,
                text = message
            }
        };

        var json = JsonConvert.SerializeObject(requestBody);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogDebug("SigmaSMS response: {StatusCode} - {Content}", response.StatusCode, responseContent);

        if (response.IsSuccessStatusCode)
        {
            var result = JsonConvert.DeserializeAnonymousType(responseContent, new
            {
                Id = default(Guid?),
                Recipient = default(string),
                Status = default(string),
                Error = default(string)
            });

            if (result?.Id != null)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}, SigmaSMS ID: {MessageId}",
                    phoneNumber, result.Id);
                return result.Id;
            }
            else if (!string.IsNullOrEmpty(result?.Error))
            {
                _logger.LogError("SigmaSMS returned error: {Error}", result.Error);
            }
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Token expired, refreshing and retrying...");
            _currentToken = string.Empty;
            var newToken = await GetTokenAsync(cancellationToken, forceRefresh: true);

            if (!string.IsNullOrEmpty(newToken))
            {
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", newToken);

                response = await _httpClient.SendAsync(request, cancellationToken);
                responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeAnonymousType(responseContent, new
                    {
                        Id = default(Guid?),
                        Recipient = default(string),
                        Status = default(string),
                        Error = default(string)
                    });

                    if (result?.Id != null)
                    {
                        _logger.LogInformation("SMS sent successfully after token refresh to {PhoneNumber}, ID: {MessageId}",
                            phoneNumber, result.Id);
                        return result.Id;
                    }
                }
            }
        }

        _logger.LogError("Failed to send SMS to {PhoneNumber}. Response: {StatusCode} - {Content}",
            phoneNumber, response.StatusCode, responseContent);
        return null;
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrEmpty(_currentToken) && !IsTokenExpired(_currentToken))
        {
            return _currentToken;
        }

        await _tokenSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && !string.IsNullOrEmpty(_currentToken) && !IsTokenExpired(_currentToken))
            {
                return _currentToken;
            }

            var authResult = await AuthenticateAsync(cancellationToken);
            if (authResult != null)
            {
                _currentToken = authResult;
                _tokenExpiry = GetTokenExpiry(_currentToken);
                _logger.LogInformation("SigmaSMS token refreshed, expires at {Expiry}", _tokenExpiry);
                return _currentToken;
            }

            return string.Empty;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<string?> AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authRequest = new
            {
                username = _smsSettings.Username,
                password = _smsSettings.Password
            };

            var json = JsonConvert.SerializeObject(authRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Используем полный путь как в официальном примере
            var request = new HttpRequestMessage(HttpMethod.Post, "api/login")
            {
                Content = content
            };

            _logger.LogInformation("Authenticating with SigmaSMS...");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Authentication response: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonConvert.DeserializeObject<SigmaAuthResponse>(responseContent);
                return authResponse?.Token;
            }

            _logger.LogError("SigmaSMS authentication failed: {StatusCode} - {Content}",
                response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SigmaSMS authentication");
            return null;
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            var jwt = new JwtSecurityToken(token);
            return jwt.ValidTo < DateTime.UtcNow.AddMinutes(10);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT token, considering it expired");
            return true;
        }
    }

    private DateTime GetTokenExpiry(string token)
    {
        try
        {
            var jwt = new JwtSecurityToken(token);
            return jwt.ValidTo;
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _tokenSemaphore?.Dispose();
    }
}