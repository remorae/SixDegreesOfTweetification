using SixDegrees.Services;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SixDegrees.Extensions
{
    /// <summary>
    /// Helper class for email senders.
    /// </summary>
    public static class EmailSenderExtensions
    {
        /// <summary>
        /// Creates a Task to send an email confirmation message using the calling IEmailSender.
        /// </summary>
        /// <param name="emailSender"></param>
        /// <param name="address">The address to send the email to.</param>
        /// <param name="link">Where the user should go to confirm their email.</param>
        /// <returns>A task to begin email confirmation.</returns>
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string address, string link)
        {
            return emailSender.SendEmailAsync(address, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }
    }
}
