namespace Bsmarter.Controllers
{
    public static partial class ControllersExtentions
    {
        public static WebApplication MapUploadController(this WebApplication app)
        {
            var groupName = "Upload";

            // Upload files (multipart/form-data). Field name: files
            app.MapPost("/api/upload", ControllersExtentions.UploadFilesAsync)
                .WithTags(groupName)
                .WithSummary(nameof(ControllersExtentions.UploadFilesAsync))
                .WithDescription("""
                Upload one or more files using multipart/form-data.
                The form field name must be 'files'.
                """)
                .DisableAntiforgery()
                .Produces(200)
                .Produces(400);

            return app;
        }
    }
}
