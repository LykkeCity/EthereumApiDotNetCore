using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.EthereumCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Error()
        {
            return Redirect("/swagger/ui");
        }
    }
}
