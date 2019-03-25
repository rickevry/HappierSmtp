using System;
using System.Net.Mail;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappierSmtp
{
    public class Client
    {

        public class ClientSettings
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string BlobConnection { get; set; }
            public string BlobContainer { get; set; }
        }
        
        private SmtpClient getSmtpClient(ClientSettings settings)
        {
            SmtpClient smtpClient = null;

            if (!string.IsNullOrEmpty(settings.Host) && settings.Port > 0) {
                smtpClient = new SmtpClient(settings.Host, settings.Port);
            } else if (!string.IsNullOrEmpty(settings.Host)) {
                smtpClient = new SmtpClient(settings.Host);
            } else {
                smtpClient = new SmtpClient();
            }

            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(settings.Username, settings.Password);
                smtpClient.Credentials = credentials;
            }
            return smtpClient;
        }

        private object LoadBlob(string connectionString, string containerName, string fileName, bool binary)
        {
            if ((connectionString != null) && (containerName != null) || (fileName != null))
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                if (blockBlob.Exists())
                {
                    if (binary)
                    {
                        MemoryStream ms = new MemoryStream();
                        blockBlob.DownloadToStream(ms);
                        return ms.GetBuffer();
                    }
                    else
                    {
                        return blockBlob.DownloadText();
                    }
                }
            }

            return null;
        }


        private void addInline(MailMessage mailMessage, MailCmd.InlineAttachment[] attachments, ClientSettings settings)
        {
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment != null)
                    {
                        byte[] imageBytes = LoadBlob(settings.BlobConnection, settings.BlobContainer, attachment.FileName, true) as byte[];
                        var inlineLogo = new Attachment(new System.IO.MemoryStream(imageBytes), attachment.MediaType);
                        inlineLogo.ContentId = attachment.ContentId;
                        inlineLogo.ContentDisposition.Inline = true;
                        inlineLogo.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                        mailMessage.Attachments.Add(inlineLogo);
                    }
                }
            }
        }

        private void addAttachments(MailMessage mailMessage, MailCmd.Attachment[] attachments, ClientSettings settings)
        {
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment != null)
                    {
                        System.Net.Mime.ContentType contentType = new System.Net.Mime.ContentType();
                        contentType.MediaType = System.Net.Mime.MediaTypeNames.Application.Octet;
                        contentType.Name = attachment.FileTitle;

                        byte[] imageBytes = LoadBlob(settings.BlobConnection, settings.BlobContainer, attachment.FileName, true) as byte[];
                        if (imageBytes != null)
                        {
                            var inlineLogo = new Attachment(new System.IO.MemoryStream(imageBytes), contentType);
                            mailMessage.Attachments.Add(inlineLogo);
                        }
                    }
                }
            }
        }

        private void addReceivers(MailAddressCollection collection, string[] strings)
        {
            if (strings != null)
            {
                foreach (string s in strings)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        collection.Add(s);
                    }
                }
            }
        }

        private string parseTemplate(string template, Dictionary<string, string> props)
        {
            if (props != null)
            {
                foreach (var prop in props.Keys)
                {
                    string value = props[prop];
                    string stringValue = (value != null) ? value : "";
                    template = template.Replace("{" + prop + "}", stringValue);
                }
            }
            return template;
        }
        private string loadMailTemplate(string mailTemplateName, ClientSettings settings)
        {
            object data = LoadBlob(settings.BlobConnection, settings.BlobContainer, mailTemplateName, false);
            return (data != null) ? data.ToString() : null;
        }

        private string getBody(Dictionary<string, string> props, string body, string mailTemplateName, ClientSettings settings)
        {
            if (!String.IsNullOrEmpty(body))
            {
                if (!String.IsNullOrEmpty(mailTemplateName))
                {
                    string template = loadMailTemplate(mailTemplateName, settings);
                    if (!string.IsNullOrEmpty(template))
                    {
                        return parseTemplate(template, props);
                    }
                }
                return body.ToString();
            }
            return "";
        }

        ClientSettings _settings;

        public Client(ClientSettings props)
        {
            this._settings = props;
        }
        public MailCmd Deserialize(string json)
        {
            MailCmd cmd = JsonConvert.DeserializeObject<MailCmd>(json);
            return cmd;
        }

        public void Send(MailCmd cmd)
        {
            SmtpClient client = getSmtpClient(this._settings);

            if (cmd.Props!=null && !cmd.Props.ContainsKey("body"))
            {
                cmd.Props.Add("body", cmd.Body);
            }

            string htmlBody = getBody(cmd.Props, cmd.Body, cmd.TemplateName, this._settings);

            var mailMessage = new MailMessage
            {
                Subject = cmd.Subject,
                Body = htmlBody,
                IsBodyHtml = true,
                From = string.IsNullOrEmpty(cmd.From) ? null : new MailAddress(cmd.From)
            };

            // add inline attachments
            addInline(mailMessage, cmd.InlineAttachments, this._settings);

            // add attachments
            addAttachments(mailMessage, cmd.Attachments, this._settings);

            // add receivers
            addReceivers(mailMessage.To, cmd.To);
            addReceivers(mailMessage.CC,  cmd.CC);
            addReceivers(mailMessage.Bcc, cmd.Bcc);

            client.Send(mailMessage);
        }
    }
}


// inlineAttachments