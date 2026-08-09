namespace AgroForum.Services.PostImages
{
    public sealed class PostImageStorage
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private const string PublicPathPrefix = "/uploads/posts/";

        private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp"
            };

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PostImageStorage> _logger;

        public PostImageStorage(
            IWebHostEnvironment environment,
            ILogger<PostImageStorage> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<PostImageSaveResult> SaveAsync(
            IFormFile image,
            CancellationToken cancellationToken = default)
        {
            if (image.Length == 0)
            {
                return PostImageSaveResult.Failure("Choose a non-empty image file.");
            }

            if (image.Length > MaxFileSizeBytes)
            {
                return PostImageSaveResult.Failure("The image must be 5 MB or smaller.");
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType))
            {
                return PostImageSaveResult.Failure("Upload a JPEG, PNG, or WebP image.");
            }

            if (!string.Equals(image.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                return PostImageSaveResult.Failure("The selected file does not match its image type.");
            }

            await using var input = image.OpenReadStream();
            var header = new byte[12];
            var bytesRead = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            if (!HasValidSignature(extension, header, bytesRead))
            {
                return PostImageSaveResult.Failure("The selected file is not a valid JPEG, PNG, or WebP image.");
            }

            var uploadsDirectory = GetUploadsDirectory();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsDirectory, storedFileName);

            try
            {
                Directory.CreateDirectory(uploadsDirectory);

                await using var output = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await output.WriteAsync(header.AsMemory(0, bytesRead), cancellationToken);
                await input.CopyToAsync(output, cancellationToken);

                return PostImageSaveResult.Success($"{PublicPathPrefix}{storedFileName}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                DeleteFileIfPresent(fullPath);
                throw;
            }
            catch (Exception exception)
            {
                DeleteFileIfPresent(fullPath);
                _logger.LogError(exception, "Unable to store uploaded forum image.");
                return PostImageSaveResult.Failure("The image could not be stored. Please try again.");
            }
        }

        public Task DeleteAsync(string imagePath)
        {
            if (!imagePath.StartsWith(PublicPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var fileName = Path.GetFileName(imagePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Task.CompletedTask;
            }

            DeleteFileIfPresent(Path.Combine(GetUploadsDirectory(), fileName));
            return Task.CompletedTask;
        }

        private string GetUploadsDirectory()
        {
            var webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRoot, "uploads", "posts");
        }

        private static bool HasValidSignature(string extension, byte[] header, int bytesRead)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF,
                ".png" => bytesRead >= 8
                    && header.AsSpan(0, 8).SequenceEqual(
                        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => bytesRead >= 12
                    && header[0] == 0x52
                    && header[1] == 0x49
                    && header[2] == 0x46
                    && header[3] == 0x46
                    && header[8] == 0x57
                    && header[9] == 0x45
                    && header[10] == 0x42
                    && header[11] == 0x50,
                _ => false
            };
        }

        private static void DeleteFileIfPresent(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    public sealed record PostImageSaveResult(string? ImagePath, string? ErrorMessage)
    {
        public bool IsSuccess => ImagePath != null;

        public static PostImageSaveResult Success(string imagePath) => new(imagePath, null);

        public static PostImageSaveResult Failure(string errorMessage) => new(null, errorMessage);
    }
}
