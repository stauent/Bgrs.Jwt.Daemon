using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Bgrs.Configuration.Secure;

namespace Bgrs.Jwt.Daemon
{
	public class Email
	{
		public MailMessage Message { get; set; }
		public SmtpClient Smtp { get; set; }
		public string Body { get; set; }
		public List<LinkedResource> LinkedResources { get; set; } = new List<LinkedResource>();

		public Email()
		{
			Message = new MailMessage();
			Smtp = new SmtpClient();
		}

		public Email(string From, string To, string Subject, string Body, string Host = "smtp.gmail.com", int Port = 587)
		{
			this.Body = Body;
			//Message = new MailMessage(From, To, Subject, Body);
			Message = new MailMessage();
			Message.From = new MailAddress(From);
			Message.Subject = Subject;
			Message.Body = Body;

			foreach (string ToAddress in To.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
			{
				Message.To.Add(ToAddress);
			}


			Message.IsBodyHtml = true;
			Message.SubjectEncoding = Encoding.UTF8;
			Message.BodyEncoding = Encoding.GetEncoding("utf-8");


			Smtp = new SmtpClient();
			Smtp.UseDefaultCredentials = true;
			Smtp.Credentials = new NetworkCredential("stauent@gmail.com", "11Philly59");
			Smtp.Host = Host;
			Smtp.Port = Port;
			Smtp.EnableSsl = true;
		}

		public void AddImageDiv(string DivId, string FilePath, bool IncludeBreak = true, string ImageText = "")
		{
			try
			{
				string ImagePath = $"{DivId}ImgPath";

				string divString = IncludeBreak ? "</br>" : "";
				divString += $"<div id='{DivId}Div' name='{DivId}Div'><img id='{DivId}Image' name='{DivId}Image' src='cid:{ImagePath}' style='width:100%'></div>";
				Body += divString;

				AddLinkedResource(FilePath, ImagePath);
			}
			catch (Exception Err)
			{
			}
		}

		public void AddLinkedResource(string FilePath, string cid)
		{
			try
			{
				string ext = Path.GetExtension(FilePath);
				if (string.IsNullOrEmpty(ext))
				{
					ext = ext.Replace(".", "");
				}
				LinkedResource lr = new LinkedResource(FilePath, $"image/{ext}");
				lr.ContentId = cid;
				lr.ContentType.Name = cid.Replace("Vin", "").Replace("ImgPath", "") + $".{ext}";
				LinkedResources.Add(lr);

				//			AddAttachment(FilePath);
			}
			catch (Exception Err)
			{
			}
		}

		public void AddAttachment(string FileName)
		{
			// Create  the file attachment for this email message.
			System.Net.Mail.Attachment data = new System.Net.Mail.Attachment(FileName);

			// Add time stamp information for the file.
			ContentDisposition disposition = data.ContentDisposition;
			disposition.CreationDate = System.IO.File.GetCreationTime(FileName);
			disposition.ModificationDate = System.IO.File.GetLastWriteTime(FileName);
			disposition.ReadDate = System.IO.File.GetLastAccessTime(FileName);
		}

		public void Send()
		{
			AlternateView imgView = AlternateView.CreateAlternateViewFromString(Body, null, MediaTypeNames.Text.Html);

			foreach (LinkedResource lr in LinkedResources)
			{
				imgView.LinkedResources.Add(lr);
			}

			// Keep in mind when you add Alternate View to MailMessage that view will be the body of your email and you DONT need to fill the Body property
			Message.AlternateViews.Add(imgView);

			// Gmail won't send email using your account details from an arbitrary application.
			// Someone just used your password to try to sign in to your account from a non-Google app. Google blocked them, but you should check what happened. Review your account activity to make sure no one else has access.
			// 1) Sign in to your Gmail
			// 2) Navigate to this page https://www.google.com/settings/security/lesssecureapps & set to "Turn On"
			Smtp.Send(Message);
		}



        public static void CreateEmail(string JWT, string From, String To, string Subject)
        {
            try
            {
                Email email = new Email(From, To, Subject, $"<p>{JWT}</p>");

                // We have to tell GMAIL to allow a "less secure" app to have access to email
                // https://myaccount.google.com/lesssecureapps
                email.Send();
            }
            catch (Exception e)
            {
                $"CreateEmail exception: {e.Message}".TraceError();
            }
        }
	}
}
