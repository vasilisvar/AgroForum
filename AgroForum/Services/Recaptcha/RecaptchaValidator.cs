using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroForum.Options;
using Microsoft.Extensions.Options;

namespace AgroForum.Services.Recaptcha;

public sealed class RecaptchaValidator : IRecaptchaValidator
{
    private static readonly TimeSpan MaximumTokenAge = TimeSpan.FromMinutes(2);

    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RecaptchaValidator> _logger;

    public RecaptchaValidator(
        HttpClient httpClient,
        IOptions<RecaptchaOptions> options,
        TimeProvider timeProvider,
        ILogger<RecaptchaValidator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RecaptchaValidationResult> ValidateAsync(
        string? token,
        string expectedAction,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return RecaptchaValidationResult.Skipped();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("reCAPTCHA rejected action {Action}: response token was missing.", expectedAction);
            return RecaptchaValidationResult.Failure();
        }

        try
        {
            using var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = token
            });

            using var response = await _httpClient.PostAsync(
                _options.VerificationEndpoint,
                requestContent,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "reCAPTCHA verification endpoint returned HTTP {StatusCode} for action {Action}.",
                    (int)response.StatusCode,
                    expectedAction);
                return RecaptchaValidationResult.Failure();
            }

            var verification = await response.Content.ReadFromJsonAsync<RecaptchaVerificationResponse>(
                cancellationToken: cancellationToken);

            if (verification == null || !verification.Success)
            {
                _logger.LogWarning(
                    "reCAPTCHA rejected action {Action}. Error codes: {ErrorCodes}",
                    expectedAction,
                    verification?.ErrorCodes == null ? "none" : string.Join(",", verification.ErrorCodes));
                return RecaptchaValidationResult.Failure();
            }

            if (!string.Equals(verification.Action, expectedAction, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "reCAPTCHA action mismatch. Expected {ExpectedAction}, received {ActualAction}.",
                    expectedAction,
                    verification.Action);
                return RecaptchaValidationResult.Failure();
            }

            if (verification.Score < _options.MinimumScore)
            {
                _logger.LogWarning(
                    "reCAPTCHA score {Score} was below the configured threshold for action {Action}.",
                    verification.Score,
                    expectedAction);
                return RecaptchaValidationResult.Failure();
            }

            var allowedHostnames = _options.AllowedHostnames
                .Where(hostname => !string.IsNullOrWhiteSpace(hostname))
                .Select(hostname => hostname.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (allowedHostnames.Count > 0 && !allowedHostnames.Contains(verification.Hostname))
            {
                _logger.LogWarning(
                    "reCAPTCHA hostname {Hostname} was not allowed for action {Action}.",
                    verification.Hostname,
                    expectedAction);
                return RecaptchaValidationResult.Failure();
            }

            var tokenAge = _timeProvider.GetUtcNow() - verification.ChallengeTimestamp;
            if (verification.ChallengeTimestamp == default ||
                tokenAge < TimeSpan.FromSeconds(-30) ||
                tokenAge > MaximumTokenAge)
            {
                _logger.LogWarning("reCAPTCHA token timestamp was invalid for action {Action}.", expectedAction);
                return RecaptchaValidationResult.Failure();
            }

            return RecaptchaValidationResult.Success(verification.Score);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("reCAPTCHA verification timed out for action {Action}.", expectedAction);
            return RecaptchaValidationResult.Failure();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "reCAPTCHA verification failed for action {Action}.", expectedAction);
            return RecaptchaValidationResult.Failure();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "reCAPTCHA returned an invalid response for action {Action}.", expectedAction);
            return RecaptchaValidationResult.Failure();
        }
    }

    private sealed class RecaptchaVerificationResponse
    {
        public bool Success { get; init; }
        public double Score { get; init; }
        public string Action { get; init; } = string.Empty;

        [JsonPropertyName("challenge_ts")]
        public DateTimeOffset ChallengeTimestamp { get; init; }

        public string Hostname { get; init; } = string.Empty;

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; init; }
    }
}
