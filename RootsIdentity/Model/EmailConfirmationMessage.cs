using Microsoft.Extensions.Configuration;
using System;


namespace RootsIdentity.Models
{
    public class EmailConfirmationMessage : EmailNotificationMessage
    {
        private enum Tag
        {
            EXPIRY_HOURS_TAG
        }

        private IConfiguration? _config;

        public EmailConfirmationMessage(string uri, string htmlTemplateFolder, IConfiguration? config)
            : base(uri, htmlTemplateFolder)
        {
            _config = config;
        }

        private string BodyTemplateFileName
        {
            get
            {
                string key = "Messages:ConfirmEmail:BodyTemplateFileName";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private int ValidationTokenExpiryHours
        {
            get
            {
                string key = "ValidationTokenExpiryHours";
                string defVal = "0";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return Int32.Parse(val == null ? defVal : val);
            }
        }

        
        public override void Build()
        {
            string key = "Messages:ConfirmEmail:Subject";
            string defVal = "";
            string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];

            _subject = val == null ? defVal : val;
            ParseBody(BodyTemplateFileName);
            _body = _body.Replace(Tag.EXPIRY_HOURS_TAG.ToString(), ValidationTokenExpiryHours.ToString());
        }
    }
}
