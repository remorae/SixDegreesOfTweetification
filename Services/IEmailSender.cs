using System.Threading.Tasks;

namespace SixDegrees.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
