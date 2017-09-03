using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappierSmtp
{
    public class MailCmd
    {
        public class InlineAttachment
        {
            public string FileName { get; set; }
            public string MediaType { get; set; }
            public string ContentId { get; set; }

        }

        public class Attachment
        {
            public string FileName { get; set; }
            public string FileTitle { get; set; }
        }

        public string Subject { get; set; }
        public string TemplateName { get; set; }
        public string Body{ get; set; }
        public string[] To { get; set; }
        public string[] CC { get; set; }
        public string[] Bcc { get; set; }
        public string From { get; set; }
        public InlineAttachment[] InlineAttachments { get; set; }
        public Attachment[] Attachments { get; set; }

        

    }
}
