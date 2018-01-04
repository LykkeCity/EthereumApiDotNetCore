using System.Threading.Tasks;
using EthereumApiSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.EthereumCore.Services;

namespace EthereumApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Error()
        {
            return Redirect("/swagger/ui");
        }
    }
}
