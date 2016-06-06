using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using SearchEngine.Models;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CommonLibrary;

namespace WebAdmin.Controllers
{
    [Authorize]
    public class AdController : Controller
    {
        // GET: Ad
        private JobsViewDbContext db = JobsViewDbContext.Instance;
        private CloudQueue imagesQueue;
        private static CloudBlobContainer imagesBlobContainer;

        public AdController()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse(JobsViewDbContext.getStorageConnectionString());

            // Get context object for working with blobs, and 
            // set a default retry policy appropriate for a web user interface.
            var blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the blob container.
            imagesBlobContainer = blobClient.GetContainerReference("images");

            // Get context object for working with queues, and 
            // set a default retry policy appropriate for a web user interface.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queueClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the queue.
            imagesQueue = queueClient.GetQueueReference("images");
        }
        public async Task<ActionResult> Index(int? category)
        {
            ViewBag.ListCategories = db.Categories.ToList();
            return View();
        }
        public ActionResult LoadData(JQueryDataTableParamModel param)
        {
            var adsList = db.Ads.AsQueryable();
            var count = 1;
            var search = string.IsNullOrEmpty(param.sSearch) ? "" : param.sSearch.ToLower();
            var dataSources = db.Ads.ToList();
            var totalRecords = dataSources.Count();
            var rs = dataSources.Skip(param.iDisplayStart)
                .Take(param.iDisplayLength)
                .Where(d => d.Title.ToLower().Contains(search))
                .Select(d => new IConvertible[]
                {
                    count++,
                    d.Category==null?"Dont have category":d.Category.Name,
                    d.Title,
                    String.Format("{0:#,##0}", d.Price),
                    d.ImageURL==null?"":d.ImageURL,

                    d.PostedDate.ToString(("dd/MM/yyyy")),
                    d.Phone,
                    d.Keyword==null||d.Keyword==""?"":d.Keyword,
                    d.AdId
                })
                .ToList();

            return Json(new
            {
                param.sEcho,
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = totalRecords,
                aaData = rs
            }, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> DeleteAd(int? ID)
        {
            var status = false;
            var message = "Cant delete Ad. Have some mistake. Try again!";
            try
            {
                ad ad = db.Ads.Find(ID);
                if (ad == null)
                {
                    message = "Cant find Ad.";
                }
                else
                {
                    await DeleteAdBlobsAsync(ad);
                    db.Ads.Remove(ad);
                    db.SaveChanges();
                    status = true;
                    message = "Delete Ad success!";
                }

            }
            catch (Exception ex)
            {

            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> CreateAd(ad ad)
        {
            var status = false;
            var message = "Cant Add Ad. Have some mistake. Try again!";

            try
            {
                CloudBlockBlob imageBlob = null;
                var imageFile = Request.Files["ImageAd"];
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    imageBlob = await UploadAndSaveBlobAsync(imageFile);
                    ad.ImageURL = imageBlob.Uri.ToString();
                }
                ad.PostedDate = DateTime.Now;
                db.Ads.Add(ad);
                await db.SaveChangesAsync();

                if (imageBlob != null)
                {
                    var queueMessage = new CloudQueueMessage(ad.AdId.ToString());
                    await imagesQueue.AddMessageAsync(queueMessage);
                }
                status = true;
                message = "Add Ad success!";
            }
            catch (Exception ex)
            {

            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }


        public async Task<ActionResult> DetailAd(int? ID)
        {
            ViewBag.ListCategories = db.Categories.ToList();
            if (ID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ad ad = await db.Ads.FindAsync(ID);
            if (ad == null)
            {
                return HttpNotFound();
            }
            return PartialView(ad);
        }

        public async Task<ActionResult> UpdateAd(ad ad)
        {
            var status = false;
            var message = "Cant Update Ad. Have some mistake. Try again!";

            try
            {
                var ID = 0;
                var IDAd = Request["IDAd"].ToString();
                if (int.TryParse(IDAd, out ID))
                {
                    var AdUpdate = db.Ads.Find(ID);
                    if (AdUpdate != null)
                    {
                        CloudBlockBlob imageBlob = null;
                        var imageFile = Request.Files["ImageAdEdit"];
                        if (imageFile != null && imageFile.ContentLength != 0)
                        {
                            await DeleteAdBlobsAsync(ad);
                            imageBlob = await UploadAndSaveBlobAsync(imageFile);
                            AdUpdate.ImageURL = imageBlob.Uri.ToString();
                        }
                        AdUpdate.Price = ad.Price;
                        AdUpdate.CategoryId = ad.CategoryId;
                        AdUpdate.Title = ad.Title;
                        AdUpdate.Description = ad.Description;
                        AdUpdate.Keyword = ad.Keyword;
                        AdUpdate.PostedDate = ad.PostedDate;
                        AdUpdate.Phone = ad.Phone;
                         await db.SaveChangesAsync();
                        if (imageBlob != null)
                        {
                            var queueMessage = new CloudQueueMessage(AdUpdate.AdId.ToString());
                            await imagesQueue.AddMessageAsync(queueMessage);
                        }
                        status = true;
                        message = "Update Ad success!";
                    }

                
                }
                else
                {
                    message = "Cant Update Ad. Have some mistake with Id Ad. Try again!";
                }

            }
            catch (Exception ex)
            {

            }

            return Json(new { message, status }, JsonRequestBehavior.AllowGet);
        }

        // GET: Ad/Delete/5
        //public async Task<ActionResult> Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Ad ad = await db.Ads.FindAsync(id);
        //    if (ad == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(ad);
        //}

        //// POST: Ad/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> DeleteConfirmed(int id)
        //{
        //    Ad ad = await db.Ads.FindAsync(id);

        //    await DeleteAdBlobsAsync(ad);

        //    db.Ads.Remove(ad);
        //    await db.SaveChangesAsync();
        //    Trace.TraceInformation("Deleted ad {0}", ad.AdId);
        //    return RedirectToAction("Index");
        //}

        private async Task<CloudBlockBlob> UploadAndSaveBlobAsync(HttpPostedFileBase imageFile)
        {
            Trace.TraceInformation("Uploading image file {0}", imageFile.FileName);

            string blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            // Retrieve reference to a blob. 
            CloudBlockBlob imageBlob = imagesBlobContainer.GetBlockBlobReference(blobName);
            // Create the blob by uploading a local file.
            using (var fileStream = imageFile.InputStream)
            {
                await imageBlob.UploadFromStreamAsync(fileStream);
            }

            Trace.TraceInformation("Uploaded image file to {0}", imageBlob.Uri.ToString());

            return imageBlob;
        }

        private async Task DeleteAdBlobsAsync(ad ad)
        {
            if (!string.IsNullOrWhiteSpace(ad.ImageURL))
            {
                Uri blobUri = new Uri(ad.ImageURL);
                await DeleteAdBlobAsync(blobUri);
            }
            if (!string.IsNullOrWhiteSpace(ad.ThumbnailURL))
            {
                Uri blobUri = new Uri(ad.ThumbnailURL);
                await DeleteAdBlobAsync(blobUri);
            }
        }

        private static async Task DeleteAdBlobAsync(Uri blobUri)
        {
            string blobName = blobUri.Segments[blobUri.Segments.Length - 1];
            Trace.TraceInformation("Deleting image blob {0}", blobName);
            CloudBlockBlob blobToDelete = imagesBlobContainer.GetBlockBlobReference(blobName);
            await blobToDelete.DeleteAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}