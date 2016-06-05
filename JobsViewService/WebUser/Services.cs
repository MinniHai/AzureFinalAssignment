using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;

namespace WebUser
{
    public class Services
    {
        public static Db _db = null;

        public IOrderedEnumerable<KeyValuePair<int, float>> GetSuggestDocId(int docId, List<int> docIdList)
        {

            _db = Db.Instance;
            var resultList = new List<float>();
            var tfidfDocIdList = _db.GetTfidf(docId);
            //Hashtable hashTableDocIDList = new Hashtable();
            var dictionaryDocIdList = new Dictionary<int, float>();
            foreach (var docIdRequest in docIdList)
            {
                float tmp = 0;
                var tfidfDocIdRequestList = _db.GetTfidf(docIdRequest);
                for (var i = 0; i < (tfidfDocIdList.Count()); i++)
                {
                    tmp += (float)Math.Pow((tfidfDocIdList[i] - tfidfDocIdRequestList[i]), 2);
                }
                resultList.Add((float)Math.Sqrt(tmp));
                //hashTableDocIDList.Add(docIDRequest, (float)Math.Sqrt(tmp));
                dictionaryDocIdList.Add(docIdRequest, (float)Math.Sqrt(tmp));
            }
            var items = from pair in dictionaryDocIdList orderby pair.Value ascending select pair;
            return items;
        }

        // return list <docID,so lan xuat hien cua keyword( map with user keyword prefer) trong docID>
        public IEnumerable<KeyValuePair<int, int>> GetListByPreference(string userId)
        {
            _db = Db.Instance;
            const int beginValue = 1;
            var preferenceKeyList = _db.getUserReferenceByUserId(userId);

            var docKeywords = _db.GetlistdocKeyword();


            var listOfDocByPre = new Dictionary<int, int>();

            foreach (string preWord in preferenceKeyList)
                foreach (var docWord in docKeywords)
                {
                    if (preWord.ToUpper().Equals(docWord.Keyword.ToUpper()))
                        if (listOfDocByPre.ContainsKey(docWord.DocId))
                        {
                            listOfDocByPre[docWord.DocId] += 1;
                        }
                        else
                        {
                            listOfDocByPre.Add(docWord.DocId, beginValue);
                        }
                }
            //short top
            var items = from pair in listOfDocByPre orderby pair.Value descending select pair;

            return items;
        }

        public List<ads> GetListAdsByDocId(int docId)
        {
            _db = Db.Instance;

            var adslist = _db.GetAllAds();

            var docKeywords = _db.GetlistdocKeywordByDocId(docId);

            var listOfAdsId = new List<ads>();

            foreach (var keyword in docKeywords)
            {
                foreach (var ads in adslist)
                {
                    if (keyword.Keyword.ToString().ToLower().Equals(ads.Keyword.ToString().ToLower()))
                    {
                        listOfAdsId.Add(ads);
                    }
                }
            }
            return listOfAdsId;
        }
    }
}