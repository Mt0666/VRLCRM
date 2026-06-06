using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Models;

namespace VRLCRM.Controllers;

public class DashboardsController : Controller
{
  public IActionResult Index() => View();
}
