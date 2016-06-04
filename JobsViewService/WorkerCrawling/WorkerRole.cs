using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using CommonLibrary;
using System.Data.Entity;
using HtmlAgilityPack;
using System.Web;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WorkerCrawling
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private readonly JobsViewDbContext db = JobsViewDbContext.Instance;
        private CloudQueue queue1;

        public override void Run()
        {
            Trace.TraceInformation("GetJobWorker is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();

            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse
                (RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));


            Trace.TraceInformation("Creating images queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue1 = queueClient.GetQueueReference("queue1");
            queue1.CreateIfNotExists();

            Trace.TraceInformation("Storage initialized");
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            Trace.TraceInformation("GetJobWorker has been started");

            return base.OnStart();
        }

        public override void OnStop()
        {
            Trace.TraceInformation("GetJobWorker is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("GetJobWorker has stopped");
        }

        public void getJob()
        {
            string url = "http://www.vietnamworks.com/tim-viec-lam/tat-ca-viec-lam";
            HtmlWeb web = new HtmlWeb();
            HtmlDocument hdoc = web.Load(url);
            HtmlNode[] nodes = hdoc.DocumentNode.SelectNodes("//a[contains(@class,'job-title')]").ToArray();

            // Use this code for test.
            //HtmlNode item = nodes[1];

            foreach (HtmlNode item in nodes)
            {
                String Title = "";
                String Text = "";
                String Html = "";
                String Link = "";
                String cateName = "";

                Title = item.InnerText;

                string urlChild = item.GetAttributeValue("href", "");

                HtmlWeb webChild = new HtmlWeb();
                HtmlDocument docChild = webChild.Load(urlChild);
                HtmlNode nodeChild = docChild.DocumentNode.SelectSingleNode("//div[@id='job-description']");
                HtmlNode nodeChild2 = docChild.DocumentNode.SelectSingleNode("//div[contains(@class,'push-top-xs')]/a");

                cateName = nodeChild2.InnerText;
                Text = nodeChild.InnerText;
                Html = nodeChild.OuterHtml.Trim();
                Link = urlChild.Trim();
                if (db.Documents.Any(d => d.Link == Link) == false)
                {
                    Document doc = new Document();
                    doc.CategoryId = getCategory(HttpUtility.HtmlDecode(cateName));
                    doc.Title = HttpUtility.HtmlDecode(Title);
                    doc.Text = HttpUtility.HtmlDecode(Text);
                    doc.Html = Html;
                    doc.Link = Link;

                    db.Documents.Add(doc);
                    db.SaveChanges();
                }
            }
        }

        public void insertCategory()
        {
            string url = "http://www.vietnamworks.com/tim-viec-lam/tat-ca-viec-lam/";
            HtmlNode.ElementsFlags.Remove("option");
            HtmlDocument html = new HtmlWeb() { AutoDetectEncoding = true }.Load(url);

            var root = html.DocumentNode;

            var nodes = root.SelectNodes("//select[contains(@id,'cateListMainSearch')]/option").Skip(1)
                .Select(n => new
                {
                    id = n.Attributes["value"].Value,
                    value = HttpUtility.HtmlDecode(n.InnerText)
                }).ToList();
            foreach (var item in nodes)
            {
                if (db.Categories.Any(c => c.Name == item.value) == false)
                {
                    Category cate = new Category();
                    cate.CategoryId = int.Parse(item.id);
                    cate.Name = item.value;
                    db.Categories.Add(cate);
                    db.SaveChanges();
                };
            }
        }

        public int getCategory(string name)
        {
            Category cate = new Category();

            cate = db.Categories
                   .Where(b => b.Name == name)
                   .FirstOrDefault();

            if (cate == null)
            {
                return 0;
            }
            return cate.CategoryId;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            insertCategory();
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                getJob();
                CloudQueueMessage message = new CloudQueueMessage("crawl successfully");
                queue1.AddMessage(message);
                Trace.TraceInformation("Working");
                await Task.Delay(100000000);
            }
        }
    }
}
