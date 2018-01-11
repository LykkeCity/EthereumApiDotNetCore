using System.Threading.Tasks;
using Lykke.Service.EthereumCoreSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;

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
