using Microsoft.AspNetCore.Mvc;

namespace LibraryMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Statistics()
        {
            return View();
        }
    }
}
