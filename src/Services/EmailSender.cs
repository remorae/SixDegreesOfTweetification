using System.Threading.Tasks;

namespace SixDegrees.Services
{
    /// <summary>
    /// Sends emails to users.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        /// <summary>
        /// Generates a task to send an email with the given subject and message to the given address.
        /// </summary>
        /// <remarks>
        /// NOT IMPLEMENTED.
        /// </remarks>
        /// <param name="address">Where to send the email.</param>
        /// <param name="subject">The email's subject line.</param>
        /// <param name="message">The content of the email.</param>
        /// <returns>A task to send the email.</returns>
        public Task SendEmailAsync(string address, string subject, string message)
        {
            return Task.CompletedTask;
        }
    }
}
