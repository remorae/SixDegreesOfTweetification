using System.Threading.Tasks;

namespace SixDegrees.Services
{
    /// <summary>
    /// Interface for any class which can send emails.
    /// </summary>
    public interface IEmailSender
    {
        Task SendEmailAsync(string address, string subject, string message);
    }
}
