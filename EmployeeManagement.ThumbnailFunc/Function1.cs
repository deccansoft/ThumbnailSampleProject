using System;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DemoClassLibrary;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace EmployeeManagement.ThumbnailFunc
{
    public class Function1
    {
        private IConfiguration Configuration;
        public Function1(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        [FunctionName("Function1")]
        public async Task Run([QueueTrigger("thumbnailrequest", Connection = "StorageConnectionString")] BlobInformation blobInfo,
                [Blob("empimages/{BlobName}", FileAccess.Read, Connection = "StorageConnectionString")] Stream input,
                [Blob("empimages/{BlobNameWithoutExtension}_thumbnail.jpg", FileAccess.Write,  Connection = "StorageConnectionString")] BlobContainerClient containerClient)
        {
            var blobClient = containerClient.GetBlobClient($"{blobInfo.BlobNameWithoutExtension}_thumbnail.jpg");
            using (var outputStream = await blobClient.OpenWriteAsync(true))
            {
                // Generate a thumbnail from the input stream and write it to the output stream
                GenerateThumbnail(input, outputStream);
            }

            var options = new DbContextOptionsBuilder<DemoDbContext>();
            options.UseSqlServer(Configuration.GetConnectionString("DemoDbContext"));
            var db = new DemoDbContext(options.Options);
            var id = blobInfo.EmpId;
            Employee emp = db.Employees.Find(id);
            emp.ThumbnailUrl = blobClient.Uri.ToString();
            db.SaveChanges();
        }

        private static void GenerateThumbnail(Stream input, Stream output)
        {
            using (var originalImage = SKBitmap.Decode(input))
            {
                // Calculate the new dimensions while maintaining the aspect ratio
                var resizedImage = originalImage.Resize(
                    new SKImageInfo(40, 40),
                    new SKSamplingOptions());

                using (var image = SKImage.FromBitmap(resizedImage))
                using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 75)) // Quality set to 75
                {
                    data.SaveTo(output);
                }
            }
        }
    }
}
