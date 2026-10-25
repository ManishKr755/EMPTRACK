using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMPTRACK.Controllers;

[Authorize]
public class ConcernMessagesController : Controller
{
    // Replies are not part of the approved workflow. Historical ConcernMessage
    // rows and their schema remain untouched and readable in concern details.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMessage() => Forbid();
}
