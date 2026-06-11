using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VRLCRM.Controllers;

[Authorize]
public class DashboardsController : Controller
{
  public IActionResult Index() => View();
}
