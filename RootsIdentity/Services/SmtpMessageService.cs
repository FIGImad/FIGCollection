using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using FIGCommon.Utilities;
using FIGCommon.Interfaces;


namespace RootsIdentity.Providers
{
    public class SmtpMessageService : IMessageService
    {
        protected readonly IConfiguration _config;
        protected readonly ILogger<SmtpMessageService> _logger;
        public SmtpMessageService(ILogger<SmtpMessageService> logger,
                             IConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #region Accessors
        private string SmtpHost
        {
            get
            {
                string key = "Smtp:Host";
                string defVal = "";
                return (_config[key] == null ? defVal : _config[key]) ?? defVal;
            }
        }
        private int SmtpPort
        {
            get
            {
                string key = "Smtp:Port";
                return int.TryParse(_config[key], out int port) ? port : 0;
            }
        }
        private bool SmtpUseSSL
        {
            get
            {
                string key = "Smtp:UseSsl";
                return bool.TryParse(_config[key], out bool port) ? port : false;
            }
        }
        private string SmtpUsername
        {
            get
            {
                string key = "Smtp:Username";
                string defVal = "";
                string val = (_config[key] == null ? defVal : _config[key]) ?? defVal;
                return ProtectedDataUtil.Unprotect(val) ?? val;
            }
        }
        private string SmtpPassword
        {
            get
            {
                string key = "Smtp:Password";
                string defVal = "";
                string val = (_config[key] == null ? defVal : _config[key]) ?? defVal;
                return ProtectedDataUtil.Unprotect(val) ?? val;
            }
        }
        private int SmtpTimeoutSeconds
        {
            get
            {
                string key = "Smtp:TimeoutSeconds";
                return int.TryParse(_config[key], out int val) ? val : 0;
            }
        }
        private string SmtpEmailFromAddress
        {
            get
            {
                string key = "Smtp:EmailFromAddress";
                string defVal = "";
                return (_config[key] == null ? defVal : _config[key]) ?? defVal;
            }
        }
        private string SmtpEmailFromName
        {
            get
            {
                string key = "Smtp:EmailFromName";
                string defVal = "";
                return (_config[key] == null ? defVal : _config[key]) ?? defVal;
            }
        }
        #endregion Accessors

        public async Task Send(string email, string subject, string messageBody)
        {
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(SmtpEmailFromName, SmtpEmailFromAddress));
            var addresses = email.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var address in addresses)
            {
                message.To.Add(new MailboxAddress(address, address));
            }
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = messageBody };

            var client = new SmtpClient();

            var secureSocketOption = SmtpUseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            try
            {
                await client.ConnectAsync(SmtpHost, SmtpPort, secureSocketOption);
                if (SmtpUsername.Length > 0)  // assume unauthenticaed smtp server if username is empty
                {
                    await client.AuthenticateAsync(SmtpUsername, SmtpPassword, default(CancellationToken));
                }
                await client.SendAsync(message, default(CancellationToken));
                await client.DisconnectAsync(true, default(CancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                throw;
            }
        }
    }
}
