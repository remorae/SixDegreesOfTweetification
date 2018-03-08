using System.ComponentModel.DataAnnotations;

namespace SixDegrees.Model.AccountViewModel
{
    public class ExternalLoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
