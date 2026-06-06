using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Models;

namespace VRLCRM.Controllers;

public class FormLayoutsController : Controller
{
public IActionResult Horizontal() => View();
public IActionResult Vertical() => View();
}
