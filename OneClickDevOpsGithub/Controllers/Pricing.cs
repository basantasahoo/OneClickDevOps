using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OneClickDevOpsGithub.Controllers
{
    public class Pricing : Controller
    {
        public IActionResult ProductList(string org, string pat1)
        {
            if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(pat1))
            {
                ViewBag.POSTOrg = org;
                ViewBag.POSTPat1 = pat1;

            }
            return View();
        }

    }
}
