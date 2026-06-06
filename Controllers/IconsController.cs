using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Models;

namespace VRLCRM.Controllers;

public class IconsController : Controller
{
  public IActionResult RiIcons() => View();
}
