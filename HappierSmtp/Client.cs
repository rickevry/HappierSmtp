using System;
using System.Net.Mail;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappierSmtp
{
    public class Client
    {

        object _settings = null;

        private object GetValueFromAnonymousType(object dataitem, string itemkey)
        {
            JObject jobj = dataitem as JObject;
            if (jobj != null)
            {
                JArray array = jobj[itemkey] as JArray;
                if (array != null) return array;
            }
            else
            {
                System.Type type = dataitem.GetType();
                var propInfo = type.GetProperty(itemkey);
                if (propInfo != null)
                {
                    object itemvalue = propInfo.GetValue(dataitem, null);
                    return itemvalue;
                }
            }
            return null;
        }

        private string GetStringFromAnonymousType(object dataitem, string itemkey)
        {
            JObject jobj = dataitem as JObject;
            if (jobj != null)
            {
                if (jobj[itemkey] != null)
                {
                    return jobj[itemkey].ToString();
                }
            } else {
                System.Type type = dataitem.GetType();
                var propInfo = type.GetProperty(itemkey);
                if (propInfo != null)
                {
                    object itemvalue = propInfo.GetValue(dataitem, null);
                    if (itemvalue != null)
                    {
                        return itemvalue.ToString();
                    }
                }
            }

            return "";
        }

        private SmtpClient getSmtpClient(object props)
        {
            SmtpClient smtpClient = null;

            string host = GetStringFromAnonymousType(props, "host");
            string port = GetStringFromAnonymousType(props, "port");
            string username = GetStringFromAnonymousType(props, "username");
            string password = GetStringFromAnonymousType(props, "password");

            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(port)) {
                smtpClient = new SmtpClient(host, Convert.ToInt32(port));
            } else if (!string.IsNullOrEmpty(host)) {
                smtpClient = new SmtpClient(host);
            } else {
                smtpClient = new SmtpClient();
            }

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(username, password);
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

        private string getString(object o)
        {
            if (o != null) return "";
            return o.ToString();
        }
        private MailAddress getMailAddress(object o)
        {
            if (o != null)
            {
                return new MailAddress(o.ToString());
            }
            return null;
        }

        private void addInline(MailMessage mailMessage, object o, object mail)
        {

            string blobConnection = GetStringFromAnonymousType(this._settings, "blobConnection");
            string blobContainer = GetStringFromAnonymousType(this._settings, "blobContainer");

            IEnumerable array = o as IEnumerable;

            if (array != null)
            {
                foreach (object d in array)
                {
                    if (d != null)
                    {
                        string fileName = GetStringFromAnonymousType(d, "fileName");
                        string mediaType = GetStringFromAnonymousType(d, "mediaType");
                        string contentId = GetStringFromAnonymousType(d, "contentId");

                        byte[] imageBytes = LoadBlob(blobConnection, blobContainer, fileName, true) as byte[];
                        var inlineLogo = new Attachment(new System.IO.MemoryStream(imageBytes), mediaType);
                        inlineLogo.ContentId = contentId;
                        inlineLogo.ContentDisposition.Inline = true;
                        inlineLogo.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                        mailMessage.Attachments.Add(inlineLogo);
                    }
                }
            }
        }

        private void addAttachments(MailMessage mailMessage, object o, object mail)
        {

            string blobConnection = GetStringFromAnonymousType(this._settings, "blobConnection");
            string blobContainer = GetStringFromAnonymousType(this._settings, "blobContainer");

            IEnumerable array = o as IEnumerable;

            if (array != null)
            {
                foreach (object d in array)
                {
                    if (d != null)
                    {
                        string fileName = GetStringFromAnonymousType(d, "fileName");
                        string fileTitle = GetStringFromAnonymousType(d, "fileTitle");

                        System.Net.Mime.ContentType contentType = new System.Net.Mime.ContentType();
                        contentType.MediaType = System.Net.Mime.MediaTypeNames.Application.Octet;
                        contentType.Name = fileTitle;

                        byte[] imageBytes = LoadBlob(blobConnection, blobContainer, fileName, true) as byte[];
                        if (imageBytes != null)
                        {
                            var inlineLogo = new Attachment(new System.IO.MemoryStream(imageBytes), contentType);
                            mailMessage.Attachments.Add(inlineLogo);
                        }
                    }
                }
            }
        }

        private void addReceivers(MailAddressCollection collection, object o)
        {
            IEnumerable oenumerable = o as IEnumerable;
            if (oenumerable != null)
            {
                foreach (object oo in oenumerable)
                {
                    if (oo!=null)
                    {
                        collection.Add(oo.ToString());
                    }
                }
            }
        }

        private string parseTemplate(string template, object props)
        {
            JObject jobj = props as JObject;

            if (jobj != null)
            {
                foreach (var prop in jobj)
                {
                    string propName = prop.Key;
                    object value = prop.Value;
                    string stringValue = (value != null) ? value.ToString() : "";
                    template = template.Replace("{" + propName + "}", stringValue);
                }
            }
            else
            {
                foreach (var prop in props.GetType().GetProperties())
                {
                    string propName = prop.Name;
                    object value = prop.GetValue(props, null);
                    string stringValue = (value != null) ? value.ToString() : "";
                    template = template.Replace("{" + propName + "}", stringValue);
                }
            }
            return template;
        }
        private string loadMailTemplate(string mailTemplateName, object props)
        {
            string blobConnection = GetStringFromAnonymousType(this._settings, "blobConnection");
            string blobContainer = GetStringFromAnonymousType(this._settings, "blobContainer");

            object data = LoadBlob(blobConnection, blobContainer, mailTemplateName, false);
            return (data != null) ? data.ToString() : null;
        }
        private string getBody(object props, string body, string mailTemplateName)
        {
            if (!String.IsNullOrEmpty(body))
            {
                if (!String.IsNullOrEmpty(mailTemplateName))
                {
                    string template = loadMailTemplate(mailTemplateName, props);
                    if (!string.IsNullOrEmpty(template))
                    {
                        return parseTemplate(template, props);
                    }
                }
                return body.ToString();
            }
            return "";
        }

        public Client(object props)
        {
            this._settings = props;
        }
        public void Send(object mail)
        {

            // get SMTP client
            SmtpClient client = getSmtpClient(this._settings);


            string body = GetStringFromAnonymousType(mail, "body");
            string mailTemplateName = GetStringFromAnonymousType(mail, "mailTemplateName");
            string subject = GetStringFromAnonymousType(mail, "subject");
            MailAddress from = getMailAddress(GetStringFromAnonymousType(mail, "from"));
            string htmlBody = getBody(mail, body, mailTemplateName);

            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                From = from
            };

            // add inline attachments
            object inlineAttachments = GetValueFromAnonymousType(mail, "inlineAttachments");
            addInline(mailMessage, inlineAttachments, mail);

            // add attachments
            object attachments = GetValueFromAnonymousType(mail, "attachments");
            addAttachments(mailMessage, attachments, mail);

            // add receivers
            addReceivers(mailMessage.To, GetValueFromAnonymousType(mail, "to"));
            addReceivers(mailMessage.CC, GetValueFromAnonymousType(mail, "cc"));
            addReceivers(mailMessage.Bcc, GetValueFromAnonymousType(mail, "bcc"));

            client.Send(mailMessage);

        }
    }
}
