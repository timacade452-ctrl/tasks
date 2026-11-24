using Microsoft.Extensions.Options;

namespace StudyFlow.Services;

public class FileValidationService : IFileValidationService
{
    private readonly FileStorageOptions _options;

    public FileValidationService(IOptions<FileStorageOptions> options) => _options = options.Value;

    public FileValidationResult Validate(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return FileValidationResult.Fail("Choose a non-empty file to upload.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            return FileValidationResult.Fail(
                $"This file is larger than the {_options.MaxFileSizeBytes / (1024 * 1024)} MB limit.");
        }

        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return FileValidationResult.Fail("The uploaded file has an invalid name.");
        }

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        if (!FileTypes.AllowedExtensions.Contains(extension))
        {
            return FileValidationResult.Fail("Only PDF, Word (.doc/.docx) and PowerPoint (.ppt/.pptx) files are accepted.");
        }

        try
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[8];
            var read = stream.Read(header);
            if (read < 4)
            {
                return FileValidationResult.Fail("The file appears to be empty or corrupted.");
            }

            var isPdf = header[..4].SequenceEqual("%PDF"u8);
            var isZip = read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
            var isOle = read >= 8 && header[0] == 0xD0 && header[1] == 0xCF &&
                        header[2] == 0x11 && header[3] == 0xE0 && header[4] == 0xA1 &&
                        header[5] == 0xB1 && header[6] == 0x1A && header[7] == 0xE1;

            var validSignature = extension switch
            {
                ".pdf" => isPdf,
                ".docx" or ".pptx" => isZip,
                ".doc" or ".ppt" => isOle,
                _ => false
            };

            return validSignature
                ? FileValidationResult.Ok(extension)
                : FileValidationResult.Fail(
                    $"The file content does not match the {extension} extension. Please upload a valid document.");
        }
        catch (Exception)
        {
            return FileValidationResult.Fail("The file could not be read. Please try another file.");
        }
    }
}
