
using Microsoft.Extensions.Configuration;

namespace RootsIdentity.Models
{
    public class ResetPasswordMessage : EmailNotificationMessage
    {
        private IConfiguration? _config;

        public ResetPasswordMessage(string uri, string htmlTemplateFolder, IConfiguration? config)
            : base(uri, htmlTemplateFolder)
        {
            _config = config;
        }

        private string BodyTemplateFileName
        {
            get
            {
                string key = "Messages:ResetPassword:BodyTemplateFileName";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        public override void Build()
        {
            string key = "Messages:ResetPassword:Subject";
            string defVal = "";
            string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
            _subject = val == null? defVal : val;
            ParseBody(BodyTemplateFileName);
        }
    }
}
