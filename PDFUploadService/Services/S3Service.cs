using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using PDFUploadService.Models;
using Microsoft.Extensions.Options;

namespace PDFUploadService.Services
{
    public interface IS3Service
    {
        Task<(bool Exists, string FileUrl)> CheckFileExistsAsync(string bucketName, string key);
        Task<string> UploadFileAsync(string bucketName, string key, IFormFile file);
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;

        public S3Service(IOptions<AwsSettings> awsOptions)
        {
            var settings = awsOptions.Value;

            // Retrieve credentials securely from environment variables
            var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

            // Throw an exception if the credentials are missing
            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("AWS credentials are not set in the environment variables.");
            }

            // Initialize the AmazonS3Client with credentials from environment variables
            _s3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.USEast1);
        }

        public async Task<(bool Exists, string FileUrl)> CheckFileExistsAsync(string bucketName, string key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };
                var response = await _s3Client.GetObjectMetadataAsync(request);
                string url = $"https://{bucketName}.s3.amazonaws.com/{key}";
                return (true, url);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null);
            }
        }

        public async Task<string> UploadFileAsync(string bucketName, string key, IFormFile file)
        {
            using var newMemoryStream = new MemoryStream();
            await file.CopyToAsync(newMemoryStream);
            newMemoryStream.Position = 0;

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = newMemoryStream,
                Key = key,
                BucketName = bucketName,
                ContentType = file.ContentType
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return $"https://{bucketName}.s3.amazonaws.com/{key}";
        }
    }
}
