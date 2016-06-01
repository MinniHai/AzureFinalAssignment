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
using Microsoft.WindowsAzure.Storage.Queue;
using CommonLibrary;
using System.Data.Entity;
using System.Collections;
using Microsoft.Azure;

namespace WorkerSplitting
{
    public class WorkerRole : RoleEntryPoint
    {

        private CloudQueue myQueue;
        private JobsViewDbContext db;
        //private ArrayList dbKeyWords;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            Trace.TraceInformation("Test Slack");
            CloudQueueMessage msg = null;
            //documentsQueue.DeleteMessage
            while (true)
            {
                try
                {
                    msg = this.myQueue.GetMessage();
                    //Trace.TraceInformation("Get queue message for {0}---------------------------------------------", msg.AsString);
                    if (msg != null && msg.AsString.Equals("letsgo"))
                    {
                        ProcessQueueMessage();
                        myQueue.DeleteMessage(msg);
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
                        this.myQueue.DeleteMessage(msg);
                        Trace.TraceError("Deleting poison queue item: '{0}'", msg.AsString);
                    }
                    Trace.TraceError("Exception in Keyword Splitter Worker: '{0}'", e.Message);
                    System.Threading.Thread.Sleep(5000);
                }
            }

        }

        private void ProcessQueueMessage()
        {
            calculateTF();
            calculateTFTDF();
            //db.Dispose();                       
        }

        private void calculateTFTDF()
        {
            //Lay het cac record trong bang document
            var listDocuments = db.Documents.ToList();

            //Dem xem co tat ca bao nhieu document
            double totalDocCount = listDocuments.Count;

            var listDoc_KeyWord = db.Document_Keyword.ToList();
            foreach (var item in listDoc_KeyWord)
            {
                int keyID = item.KeywordId;

                //Tim tat ca cac doc co chua keywordID trung voi keyID
                double relatedDocCount = db.Document_Keyword.Where(d => d.KeywordId.Equals(keyID)).ToList().Count;
                double idf = Math.Log10(totalDocCount / relatedDocCount);
                if (item.TF != null)
                {
                    double tf_idf = (double)item.TF * idf;
                    Trace.TraceInformation("Calculate TFTDF ......................................");
                    item.TFIDF = tf_idf;

                    //Update field tfidf
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        private void calculateTF()
        {
            //Lay het cac record trong bang document
            var listDocuments = db.Documents.ToList();

            //Duyet tung record vua lay ra
            foreach (var doc in listDocuments)
            {
                //Tim xem trong bang doc_keyword co record nao trung id voi doc dang xet ko
                var existedDoc = db.Document_Keyword.Where(d => d.DocumentId.Equals(doc.DocumentId)).FirstOrDefault();

                //Kiem tra xem doc nay tinh TF-TDF chua, neu chua thi moi lam, khong thi thoi
                if (existedDoc == null)
                {
                    string text = doc.Text;

                    //Su dung ham de tach keyword, Hashtable: key: keyword, value: so lan xuat hien cua keyword do
                    Hashtable keyWordsTable = getKeyWords(text);

                    //Tim max occurence cua 1 document
                    ICollection wordOccurenceList = keyWordsTable.Values;
                    double maxOccurence = 1;
                    foreach (var i in wordOccurenceList)
                    {
                        double j = Double.Parse(i.ToString());
                        if (j > maxOccurence)
                        {
                            maxOccurence = j;
                        }
                    }

                    //Lay ra tap hop cac keyword trong hashtable
                    ICollection keyWordsList = keyWordsTable.Keys;

                    //Duyet tung keyword
                    foreach (var item in keyWordsList)
                    {

                        var result = db.Keywords.Where(k => k.Keyword_.Equals(item.ToString())).FirstOrDefault();
                        //Kiem tra xem keyword da ton tai trong bang keywords hay chua, neu chua thi moi add vo
                        if (result == null)
                        {
                            db.Keywords.Add(new Keyword { Keyword_ = item.ToString() });
                            db.SaveChanges();

                        }

                        double occurence = Double.Parse(keyWordsTable[item].ToString());

                        //Lay ID cua record vua moi add vao bang keyword
                        var newKeyWord = db.Keywords.Where(k => k.Keyword_.Equals(item.ToString())).FirstOrDefault();//SQl ko phan bik hoa thuong
                        //int fixedID = 0;
                        //foreach(var i in newKeyWord)
                        //{
                        //    var tmp = db.doc_keyword.Where(d => d.DocId.Equals(doc.Id) && d.KeywordId.Equals(i.Id));
                        //    if (tmp == null)
                        //    {
                        //        fixedID = i.Id;
                        //    }
                        //}

                        //Kiem tra trung Id
                        Trace.TraceInformation("docId: '{0}'", doc.DocumentId);
                        Trace.TraceInformation("keyWordID: '{0}'", newKeyWord.KeywordId);

                        db.Document_Keyword.Add(new Document_Keyword
                        {
                            DocumentId = doc.DocumentId,
                            KeywordId = newKeyWord.KeywordId,
                            // TF = (so lan xuat hien cua keyword do)/(so lan xuat hien nhieu nhat)
                            TF = occurence / maxOccurence
                        });
                        db.SaveChanges();

                    }

                }
            }
        }

        private Hashtable getKeyWords(string rawString)
        {

            ArrayList list = new ArrayList();
            string copy = rawString;
            //Lay key word cua nhung co in hoa
            for (int i = 0; i < rawString.Length; i++)
            {

                //gap tu viet hoa, chay cho toi khi gap "(khoang trang)(chu thuong)"
                if (rawString.ElementAt(i) >= 65 && rawString.ElementAt(i) <= 90)
                {
                    int startIndex = i;

                    while (true)
                    {
                        int endIndex = i;
                        if (rawString.ElementAt(i) == '.' || rawString.ElementAt(i) == ',' || rawString.ElementAt(i) == ')' || rawString.ElementAt(i) == ';' || rawString.ElementAt(i) == ':' || rawString.ElementAt(i) == '"')
                        {
                            endIndex = endIndex - 1;
                            string subString = rawString.Substring(startIndex, endIndex - startIndex + 1);
                            list.Add(subString);
                            copy = copy.Replace(subString, "");
                            break;
                        }
                        if (i == rawString.Length - 1)
                        {
                            string subString = rawString.Substring(startIndex, endIndex - startIndex + 1);
                            list.Add(subString);
                            copy = copy.Replace(subString, "");
                            break;
                        }
                        if (i == rawString.Length - 2 && rawString.ElementAt(i + 1) == ' ')
                        {
                            string subString = rawString.Substring(startIndex, endIndex - startIndex + 1);
                            list.Add(subString);
                            copy = copy.Replace(subString, "");
                            break;
                        }
                        if (i <= rawString.Length - 3 && rawString.ElementAt(i + 1) == ' ' && (rawString.ElementAt(i + 2) < 65 || rawString.ElementAt(i + 2) > 90))
                        {
                            string subString = rawString.Substring(startIndex, endIndex - startIndex + 1);
                            list.Add(subString);
                            copy = copy.Replace(subString, "");
                            break;
                        }
                        i++;
                    }
                }
                //remain = remain + word.Substring(begin, end - begin + 1);

            }
            string[] capitalWords = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                capitalWords[i] = list[i].ToString();
                //Console.WriteLine(upercaseWords[i]);
            }


            //Lay key word cua nhung ko co in hoa
            char[] seperator = { ' ', ':', '"', '!', '(', ')', '.', ',' };
            string[] words = copy.Split(seperator);
            //string[] capitalWords = filterName(rawString);

            Hashtable docKeyWords = new Hashtable();

            foreach (string cap in capitalWords)
            {
                string tmp = cap.ToLower();
                tmp = removeStopWord(tmp).Trim();
                if (tmp.Length > 1 && !isStopWord(tmp))
                {
                    if (docKeyWords.ContainsKey(tmp))
                    {
                        docKeyWords[tmp] = (int)docKeyWords[tmp] + 1;
                    }
                    else
                    {
                        docKeyWords.Add(tmp, (int)1);
                    }
                }
            }


            foreach (string word in words)
            {
                string s = word.Trim().ToLower();

                //Neu s la keyword
                if (s.Length > 1 && !isStopWord(s))
                {
                    //add s vao hashtable
                    if (docKeyWords.ContainsKey(s))
                    {
                        docKeyWords[s] = (int)docKeyWords[s] + 1;
                    }
                    else
                    {
                        docKeyWords.Add(s, (int)1);
                    }
                }
            }
            return docKeyWords;
        }

        private bool isStopWord(string word)
        {
            for (int i = 0; i < StopWords.stopWords.Length; i++)
            {
                if (word.ToLower() == StopWords.stopWords.ElementAt(i))
                {
                    return true;
                }
            }
            return false;
        }

        private string removeStopWord(string word)
        {
            string[] list = word.Split(' ');
            string result = "";
            foreach (string s in list)
            {
                if (!isStopWord(s))
                {
                    result = result + s + " ";
                }
            }
            return result;
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            //var dbConnectionString = CloudConfigurationManager.GetSetting("DBConnectionString");
            db = new JobsViewDbContext();

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            Trace.TraceInformation("Creating documents queue");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            myQueue = queueClient.GetQueueReference("message");

            myQueue.CreateIfNotExists();
            Trace.TraceInformation("Queue initialized");
            return base.OnStart();
        }
    }
}
