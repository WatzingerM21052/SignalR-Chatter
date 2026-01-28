namespace ChatterBackend.Controllers;

public record struct OkStatus(bool IsOk, string Val, string? Error = null);

[Route("[controller]/[action]")]
[ApiController]
public class ValuesController : ControllerBase
{ 
  [HttpGet]
  public OkStatus Dummy()
  {
    this.Log();
    return new OkStatus(true, $"{DateTime.Now:HH:mm:ss}");
  }
}
