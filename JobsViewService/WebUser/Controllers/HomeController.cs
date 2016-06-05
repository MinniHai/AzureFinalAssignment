using System.Linq;
using System.Web.Mvc;

namespace WebUser.Controllers
{
    public class HomeController : Controller
    {
        public static Db _db = null;
        Services _services;
        // goi view index-show ra chon user 
        public ActionResult Index()
        {
            _db = Db.Instance;
            var listUser = _db.GetlistUserId();//lay list user id tu database
            ViewBag.ListOfView = listUser; // add vao view bag
            return View();// return respone index
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult HomeView(string id)
        {
            _db = Db.Instance;
            _services = new Services();
            // return list <docID,so lan xuat hien cua keyword( map with user keyword prefer) trong docID>
            var docWithReference = _services.GetListByPreference(id);
            // get entry of document
            var listPreferenceDoc = docWithReference.Select(pair => _db.getDocumentByID(pair.Key)).ToList();
            // show
            ViewBag.ListOfView = listPreferenceDoc;

            return View();
        }
        public ActionResult DocDetail(int id)
        {
            _db = Db.Instance;
            _services = new Services();
            var doc = _db.getDocumentByID(id);
            //get other document 
            var listOfOtherDoc = _db.getListDocIDnonDoc(id);
            //
            var sgDocId = _services.GetSuggestDocId(id, listOfOtherDoc);

            var listsuggestDoc = sgDocId.Select(pair => _db.getDocumentByID(pair.Key)).ToList();

            var listAds = _services.GetListAdsByDocId(id);

            ViewData["DocumentDetail"] = doc;
            ViewBag.DocumentSgList = listsuggestDoc;
            ViewBag.AdsList = listAds;

            return View();
        }
    }
}