using System.Web;
using System.Web.Mvc;

namespace ASP_NET_Security_Roles
{
		public class FilterConfig
		{
				public static void RegisterGlobalFilters(GlobalFilterCollection filters)
				{
						filters.Add(new HandleErrorAttribute());
				}
		}
}
