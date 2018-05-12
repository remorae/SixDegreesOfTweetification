using System.ComponentModel.DataAnnotations;

namespace SixDegrees.Model.AccountViewModel
{
    /// <summary>
    /// Information required to log into an existing account.
    /// </summary>
    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        public bool RememberMe { get; set; }
    }
}
