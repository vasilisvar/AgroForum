namespace AgroForum.Services.Recaptcha;

public sealed record RecaptchaValidationResult(bool IsValid, bool IsSkipped, double? Score, string? ErrorMessage)
{
    public static RecaptchaValidationResult Success(double score) => new(true, false, score, null);

    public static RecaptchaValidationResult Skipped() => new(true, true, null, null);

    public static RecaptchaValidationResult Failure() => new(
        false,
        false,
        null,
        "We couldn't verify this request. Please try again.");
}
