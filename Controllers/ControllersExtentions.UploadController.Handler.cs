namespace Bsmarter.Controllers;
public static partial class ControllersExtentions
{
    public static async Task<IResult> UploadFilesAsync(HttpRequest request, IConfiguration config)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Expected multipart/form-data.");

        var form = await request.ReadFormAsync();
        var files = form.Files;

        if (files is null || files.Count == 0)
            return Results.BadRequest("No files received. Use form field name 'files'.");

        // Configure via appsettings.json: Uploads:RootPath (e.g. C:\\UploadedFiles)
        var uploadRoot = config["Uploads:RootPath"];
        if (string.IsNullOrWhiteSpace(uploadRoot))
            uploadRoot = @"C:\UploadedFiles";

        Directory.CreateDirectory(uploadRoot);

        var baseUrl = $"{request.Scheme}://{request.Host}";
        var uploaded = new List<object>(files.Count);

        foreach (var file in files)
        {
            if (file.Length <= 0)
                continue;

            var originalName = file.FileName;
            var safeFileName = SanitizeFileName(originalName);
            var savedFileName = GetUniqueFileName(uploadRoot, safeFileName);
            var fullPath = Path.Combine(uploadRoot, savedFileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // Note: saving outside wwwroot means not publicly accessible unless you also map static files for this folder.
            uploaded.Add(new
            {
                originalName,
                fileName = savedFileName,
                size = file.Length,
                contentType = file.ContentType,
                path = fullPath,
                // placeholder URLs (only valid if you expose the folder)
                relativeUrl = $"/uploads/{Uri.EscapeDataString(savedFileName)}",
                url = $"{baseUrl}/uploads/{Uri.EscapeDataString(savedFileName)}"
            });
        }

        return Results.Ok(new { count = uploaded.Count, files = uploaded });
    }

    static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

    static string GetUniqueFileName(string directoryPath, string fileName)
    {
        var fullPath = Path.Combine(directoryPath, fileName);
        if (!System.IO.File.Exists(fullPath))
            return fileName;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        if (baseName.Length > 80)
            baseName = baseName[..80];

        return $"{baseName}_{Guid.NewGuid():N}{ext}";
    }
}