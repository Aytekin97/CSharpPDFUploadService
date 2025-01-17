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
            // Initialize the AmazonS3Client with credentials from settings
            _s3Client = new AmazonS3Client(settings.AWSAccessKey, settings.AWSSecretKey, Amazon.RegionEndpoint.USEast1);
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
