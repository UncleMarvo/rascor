using Microsoft.Maui.ApplicationModel.Communication;
using Rascor.App.Helpers;
using System.Diagnostics;

namespace Rascor.App.Services
{
    public class RegistrationEmailService
    {
        private readonly string _registrationEmail = "donal@quantumbuild.ai";

        public async Task<bool> SendRegistrationEmailAsync(string deviceUserId, string deviceIdentifier)
        {
            try
            {
                Debug.WriteLine("=== REGISTRATION EMAIL DEBUG ===");
                Debug.WriteLine($"Device User ID: {deviceUserId}");
                Debug.WriteLine($"Device Identifier: {deviceIdentifier}");

                // Check if email is supported
                bool isSupported = Email.Default.IsComposeSupported;
                Debug.WriteLine($"Email.IsComposeSupported: {isSupported}");

                if (!isSupported)
                {
                    Debug.WriteLine("ERROR: Email composition not supported on this device");
                    return false;
                }

                var emailBody =
                    "Please register my device for RASCOR Site Attendance.\n\n" +
                    "=== DEVICE INFORMATION ===\n" +
                    $"User ID: {deviceUserId}\n" +
                    $"Device: {deviceIdentifier}\n\n" +
                    "=== INSTRUCTIONS ===\n" +
                    "Do not modify this email - just press Send.\n" +
                    "My email address will be used for identification.";

                var message = new EmailMessage
                {
                    Subject = "RASCOR Device Registration",
                    Body = emailBody,
                    To = new List<string> { _registrationEmail }
                };

                Debug.WriteLine("Attempting to compose email...");
                await Email.Default.ComposeAsync(message);
                Debug.WriteLine("Email composition completed (or user cancelled)");

                // Mark as sent
                PreferencesHelper.HasSentRegistrationEmail = true;

                return true;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Debug.WriteLine($"ERROR: Email not supported: {fnsEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: Email exception: {ex.GetType().Name}");
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}