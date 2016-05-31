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

namespace WorkerCrawling
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        int id = 1;

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

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("GetJobWorker has been started");

            return result;
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
            using (var db = new JobsViewDbContext())
            {
                Document doc = new Document();
                string url = "http://www.vietnamworks.com/tim-viec-lam/tat-ca-viec-lam";
                HtmlWeb web = new HtmlWeb();
                HtmlDocument hdoc = web.Load(url);
                HtmlNode[] nodes = hdoc.DocumentNode.SelectNodes("//a[contains(@class,'job-title')]").ToArray();
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
            using (var db = new JobsViewDbContext())
            {

                String name = "";

                Document doc = new Document();
                string url = "http://www.vietnamworks.com/tim-viec-lam/tat-ca-viec-lam";
                HtmlWeb web = new HtmlWeb();
                HtmlDocument hdoc = web.Load(url);
                HtmlNode[] nodes = hdoc.DocumentNode.SelectNodes("//a[contains(@class,'job-title')]").ToArray();

                foreach (HtmlNode item in nodes)
                {

                    string urlChild = item.GetAttributeValue("href", "");

                    HtmlWeb webChild = new HtmlWeb();
                    HtmlDocument docChild = webChild.Load(urlChild);
                    HtmlNode nodeChild = docChild.DocumentNode.SelectSingleNode("//div[contains(@class,'push-top-xs')]/a");
                    name = nodeChild.InnerText;

                    Category cateExist = new Category();

                    if (db.Categories.Any(c => c.Name == name) == false)
                    {
                        Category cate = new Category();
                        cate.CategoryId = id;
                        cate.Name = HttpUtility.HtmlDecode(name);
                        db.Categories.Add(cate);
                        db.SaveChanges();
                        id++;
                    };

                }
            }
        }

        public int getCategory(string name)
        {
            using (var db = new JobsViewDbContext())
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
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                insertCategory();
                getJob();
                Trace.TraceInformation("Working");
                await Task.Delay(100000000);
            }
        }
    }
}
