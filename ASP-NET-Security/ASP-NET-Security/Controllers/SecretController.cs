using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASP_NET_Security.Controllers
{
    public class SecretController : Controller
    {
				// GET: Secret
				[Authorize(Users = "nhuvinh.hoang@outlook.com")]
				public ContentResult Secret()
				{
						return Content("Secret informations here");
				}
				[AllowAnonymous]
				public ContentResult PublicInfo()
				{
						return Content("Public informations here");
				}
		}
}