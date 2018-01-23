using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
public class ValuesController : Controller
{
    public ValuesController(){
    }

    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "Hello", "World" };
    }

}