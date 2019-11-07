using ASP_NET_Security_Roles.CustomFilters;
using ASP_NET_Security_Roles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASP_NET_Security_Roles.Controllers
{
		public class ProductController : Controller
		{
				SuperMarketEntities ctx;

				public ProductController()
				{
						ctx = new SuperMarketEntities();
				}

				// GET: Product
				[AuthLog(Roles = "Manager")]
				public ActionResult Index()
				{
						var Products = ctx.ProductMasters.ToList();
						return View(Products);
				}

				[AuthLog(Roles = "Manager")]
				public ActionResult Create()
				{
						var Product = new ProductMaster();
						return View(Product);
				}




				[HttpPost]
				public ActionResult Create(ProductMaster p)
				{
						ctx.ProductMasters.Add(p);
						ctx.SaveChanges();
						return RedirectToAction("Index");
				}
				[AuthLog(Roles = "Sales")]
				public ActionResult SaleProduct()
				{
						var Products = ctx.ProductMasters.ToList();
						return View(Products);
				}

		}
}