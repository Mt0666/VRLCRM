using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Models;

namespace VRLCRM.Controllers;

public class AuthController : Controller
{
  public IActionResult ForgotPasswordBasic() => View();
  public IActionResult LoginBasic() => View();
  public IActionResult RegisterBasic() => View();
}
