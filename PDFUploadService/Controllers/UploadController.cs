using Microsoft.AspNetCore.Mvc;
using PDFUploadService.Services;

namespace PDFUploadService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private static readonly List<string> AllowedMimeTypes = new()
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private readonly IS3Service _s3Service;
        private readonly IConfiguration _configuration;

        public UploadController(IS3Service s3Service, IConfiguration configuration)
        {
            _s3Service = s3Service;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] string company_name, [FromForm] IFormFile file)
        {
            if (!AllowedMimeTypes.Contains(file.ContentType))
            {
                return BadRequest(new { message = "Invalid file type. Only pdf or docx allowed." });
            }

            var bucketName = _configuration["AwsSettings:BucketName"];
            var s3Key = $"{company_name}/{file.FileName}";

            // Check if the file already exists
            var (exists, fileUrl) = await _s3Service.CheckFileExistsAsync(bucketName, s3Key);
            if (exists)
            {
                return Conflict(new
                {
                    message = "File already exists in the S3 bucket.",
                    file_url = fileUrl
                });
            }

            try
            {
                var uploadedFileUrl = await _s3Service.UploadFileAsync(bucketName, s3Key, file);
                return Ok(new
                {
                    message = "File uploaded successfully",
                    file_url = uploadedFileUrl
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
