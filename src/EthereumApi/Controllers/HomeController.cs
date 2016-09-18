using System.Threading.Tasks;
using EthereumApiSelfHosted.Models;
using Microsoft.AspNetCore.Mvc;
using Services;

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
