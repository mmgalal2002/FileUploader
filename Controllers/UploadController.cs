using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FileUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Post(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files received");

            var uploadPath = Path.Combine(_env.WebRootPath, "Images");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Build a base URL like: https://host[:port]
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var uploaded = new List<object>(files.Count);

            foreach (var file in files)
            {
                if (file.Length <= 0)
                    continue;

                var originalName = file.FileName;
                var safeFileName = SanitizeFileName(originalName);

                var savedFileName = GetUniqueFileName(uploadPath, safeFileName);
                var filePath = Path.Combine(uploadPath, savedFileName);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var relativeUrl = $"/Images/{Uri.EscapeDataString(savedFileName)}";
                var publicUrl = baseUrl + relativeUrl;

                uploaded.Add(new
                {
                    originalName,
                    fileName = savedFileName,
                    size = file.Length,
                    contentType = file.ContentType,
                    url = publicUrl,
                    relativeUrl
                });
            }

            return Ok(new { count = uploaded.Count, files = uploaded });
        }

        private static string SanitizeFileName(string fileName)
        {
            // Strip any path info and replace invalid chars
            var name = Path.GetFileName(fileName);

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            // Avoid empty filenames
            if (string.IsNullOrWhiteSpace(name))
                name = "file";

            return name;
        }

        private static string GetUniqueFileName(string directoryPath, string fileName)
        {
            // If it doesn't exist, keep it.
            var fullPath = Path.Combine(directoryPath, fileName);
            if (!System.IO.File.Exists(fullPath))
                return fileName;

            // Otherwise generate: name_{guid}.ext
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);

            // Keep baseName manageable
            if (baseName.Length > 80)
                baseName = baseName[..80];

            return $"{baseName}_{Guid.NewGuid():N}{ext}";
        }
    }
}