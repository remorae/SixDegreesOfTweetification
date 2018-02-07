using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SixDegrees.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {

        [HttpPost("[action]")]
        public Boolean Login([FromBody] LoginCredentials creds)
        {

            if (creds.username.Equals("Tom") && creds.password.Equals("Capaul"))
            {
                return true;
            }

            return false;
        }

        public class LoginCredentials
        {

            public string username { get; set; }
            public string password { get; set; }
        }

    }



}