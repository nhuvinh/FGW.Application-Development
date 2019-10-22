using System.Web;
using System.Web.Mvc;

namespace MvcMovie.Controllers
{
		public class HelloWorldController : Controller
		{
				// 
				// GET: /HelloWorld/ 

				public ActionResult Index()
				{
						ViewBag.Body = "This is Body";
						return View();
				}

				// 
				// GET: /HelloWorld/Welcome/ 

				public string Welcome(string name, int id)
				{
						return HttpUtility.HtmlEncode("Hello " + name + ", NumTimes is: " + id);
				}
		}
}