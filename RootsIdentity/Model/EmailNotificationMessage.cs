using System.IO;


namespace RootsIdentity.Models
{
    public abstract class EmailNotificationMessage
    {
        private static string URI_TAG = "URI_TAG";

        protected string _uri;
        protected string _subject = "";
        protected string _body = "";
        protected string _htmlTemplateFolder;

        public string Subject
        {
            get { return _subject; }
        }

        public string Body
        {
            get { return _body; }
        }

        public EmailNotificationMessage(string uri, string htmlTemplateFolder)
        {
            _uri = uri;
            _htmlTemplateFolder = htmlTemplateFolder;
        }

        public abstract void Build();

        protected void ParseBody(string bodyFileName)
        {
            var body = System.IO.File.ReadAllText(Path.Combine(_htmlTemplateFolder, bodyFileName), new System.Text.UTF8Encoding());
            _body = body.Replace(URI_TAG, _uri);
        }
    }
}
