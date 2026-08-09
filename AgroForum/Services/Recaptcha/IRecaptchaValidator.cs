namespace AgroForum.Services.Recaptcha;

public interface IRecaptchaValidator
{
    Task<RecaptchaValidationResult> ValidateAsync(
        string? token,
        string expectedAction,
        CancellationToken cancellationToken = default);
}
