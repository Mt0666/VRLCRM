using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Models;

namespace VRLCRM.Controllers;

public class TablesController : Controller
{
  public IActionResult Basic() => View();
}
