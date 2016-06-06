
using System;
using System.Diagnostics;
using System.Net;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using CommonLibrary;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace WorkerAdmin
{
    public class WorkerRole : RoleEntryPoint
    {
        private CloudQueue imagesQueue;
        private CloudBlobContainer imagesBlobContainer;
        private JobsViewDbContext db = JobsViewDbContext.Instance;

        public override void Run()
        {
            Trace.TraceInformation("ContosoAdsWorker entry point called");
            CloudQueueMessage msg = null;
            while (true)
            {
                try
                {
                    msg = this.imagesQueue.GetMessage();
                    if (msg != null)
                    {
                        ProcessQueueMessage(msg);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (StorageException e)
                {
                    if (msg != null && msg.DequeueCount > 5)
                    {
                        this.imagesQueue.DeleteMessage(msg);
                        Trace.TraceError("Deleting poison queue item: '{0}'", msg.AsString);
                    }
                    Trace.TraceError("Exception in ContosoAdsWorker: '{0}'", e.Message);
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        private void ProcessQueueMessage(CloudQueueMessage msg)
        {
            Trace.TraceInformation("Processing queue message {0}", msg);

            // Queue message contains AdId.
            var adId = int.Parse(msg.AsString);
            ad ad = db.Ads.Find(adId);
            //if (ad == null)
            //{
            //    throw new Exception(String.Format("AdId {0} not found, can't create thumbnail", adId.ToString()));
            //}
            if (ad != null)
            {
                Uri blobUri = new Uri(ad.ImageURL);
                string blobName = blobUri.Segments[blobUri.Segments.Length - 1];

                CloudBlockBlob inputBlob = this.imagesBlobContainer.GetBlockBlobReference(blobName);
                string thumbnailName = Path.GetFileNameWithoutExtension(inputBlob.Name) + "thumb.jpg";
                CloudBlockBlob outputBlob = this.imagesBlobContainer.GetBlockBlobReference(thumbnailName);

                using (Stream input = inputBlob.OpenRead())
                using (Stream output = outputBlob.OpenWrite())
                {
                    ConvertImageToThumbnailJPG(input, output);
                    outputBlob.Properties.ContentType = "image/jpeg";
                }
                Trace.TraceInformation("Generated thumbnail in blob {0}", thumbnailName);

                ad.ThumbnailURL = outputBlob.Uri.ToString();
                db.SaveChanges();
                Trace.TraceInformation("Updated thumbnail URL in database: {0}", ad.ThumbnailURL);

                // Remove message from queue.
                this.imagesQueue.DeleteMessage(msg);
            }

        }

        public void ConvertImageToThumbnailJPG(Stream input, Stream output)
        {
            int thumbnailsize = 80;
            int width;
            int height;
            var originalImage = new Bitmap(input);

            if (originalImage.Width > originalImage.Height)
            {
                width = thumbnailsize;
                height = thumbnailsize * originalImage.Height / originalImage.Width;
            }
            else
            {
                height = thumbnailsize;
                width = thumbnailsize * originalImage.Width / originalImage.Height;
            }

            Bitmap thumbnailImage = null;
            try
            {
                thumbnailImage = new Bitmap(width, height);

                using (Graphics graphics = Graphics.FromImage(thumbnailImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            }
            finally
            {
                if (thumbnailImage != null)
                {
                    thumbnailImage.Dispose();
                }
            }
        }

        // A production app would also include an OnStop override to provide for
        // graceful shut-downs of worker-role VMs.  See
        // http://azure.microsoft.com/en-us/documentation/articles/cloud-services-dotnet-multi-tier-app-storage-3-web-role/#restarts
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections.
            ServicePointManager.DefaultConnectionLimit = 12;

            // Read database connection string and open database.
            //var dbConnString = CloudConfigurationManager.GetSetting("Assignment_Group6ConnectionString");
            //db = new Assignment_Group6Context(dbConnString);

            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (JobsViewDbContext.getStorageConnectionString());

            Trace.TraceInformation("Creating images blob container");
            var blobClient = storageAccount.CreateCloudBlobClient();
            imagesBlobContainer = blobClient.GetContainerReference("images");
            if (imagesBlobContainer.CreateIfNotExists())
            {
                // Enable public access on the newly created "images" container.s
                imagesBlobContainer.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });
            }

            Trace.TraceInformation("Creating images queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            imagesQueue = queueClient.GetQueueReference("images");
            imagesQueue.CreateIfNotExists();

            Trace.TraceInformation("Storage initialized");
            return base.OnStart();
        }
    }
}
