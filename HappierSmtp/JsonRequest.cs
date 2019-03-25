using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappierSmtp
{
    public class JsonRequest
    {
        public JsonRequestPayload payload { get; set; }
        public string eventType { get; set; }
        public DateTime date { get; set; }
    }

    public class JsonRequestPayload
    {
        public string subject { get; set; }
        public string templateName { get; set; }
        public string body { get; set; }
        public string[] to { get; set; }
        public string[] cc { get; set; }
        public string[] bc { get; set; }
        public string from { get; set; }
        public InlineAttachment[] inlineAttachments { get; set; }
        public Attachment[] attachments { get; set; }
        public Dictionary<string, string> props { get; set; }

        public class InlineAttachment
        {
            public string fileName { get; set; }
            public string mediaType { get; set; }
            public string contentId { get; set; }
        }
        public class Attachment
        {
            public string fileName { get; set; }
            public string fileTitle { get; set; }
        }
    }
}
