using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OneClickDevOpsGithub.Controllers
{
    public class DevOps : Controller
    {
        public IActionResult DevOpsBoard()
        {
            return View();
        }
    }
}
