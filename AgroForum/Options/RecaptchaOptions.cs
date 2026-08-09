namespace AgroForum.Options;

public sealed class RecaptchaOptions
{
    public const string SectionName = "Recaptcha";

    public bool Enabled { get; set; }

    public string SiteKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public double MinimumScore { get; set; } = 0.5;

    public string VerificationEndpoint { get; set; } = "https://www.google.com/recaptcha/api/siteverify";

    public string[] AllowedHostnames { get; set; } = Array.Empty<string>();
}
