using WebApplication1.Service.Interface;
using WebApplication1.Repository.Interface;
using WebApplication1.Repository;
using WebApplication1.DTO.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace WebApplication1.Service
{
    public class EmailSenderService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ITemplateRepository _templateRepository;
        private readonly EmailSettings _emailSettings;
        public EmailSenderService(IConfiguration configuration, ITemplateRepository templateRepository, IOptions<EmailSettings> emailSettings)
        {
            _configuration = configuration;
            _templateRepository = templateRepository;
            _emailSettings = emailSettings.Value;
        }

        public async Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var _email = new MimeMessage();
                _email.From.Add(MailboxAddress.Parse(_emailSettings.Username));
                _email.To.Add(MailboxAddress.Parse(email));
                _email.Subject = subject;
                _email.Body = new TextPart(TextFormat.Html) { Text = message };

                using (var smtp = new SmtpClient())
                {
                    // Connect with STARTTLS; adjust SecureSocketOptions as needed.
                    await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                    await smtp.SendAsync(_email);
                    await smtp.DisconnectAsync(true);
                }
                
                return ServiceResult<bool>.Success(true, "Email sent successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Failed to send email: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<string>> RenderTemplateAsync(string templateName, IDictionary<string, string> tokens)
        {
            try
            {
                // Retrieve the raw template
                var template = await _templateRepository.GetTemplateAsync(templateName);
                if (string.IsNullOrWhiteSpace(template))
                {
                    return ServiceResult<string>.Failure($"Template '{templateName}' not found.");
                }

                // Replace each token in the template (e.g., {{USER_NAME}})
                foreach (var token in tokens)
                {
                    template = template.Replace($"{{{{{token.Key}}}}}", token.Value);
                }
                return ServiceResult<string>.Success(template);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure($"Failed to render template: {ex.Message}");
            }
        }
    }
}
