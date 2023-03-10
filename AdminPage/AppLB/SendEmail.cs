namespace AdminPage.AppLB
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;
    using AdminPage.Models;

    public class SendEmail
    {
        #region send email function

        private static string smtp = ConfigurationManager.AppSettings["SMTP"];
        static string webmail = ConfigurationManager.AppSettings["Email"];
        static string webmailpass = ConfigurationManager.AppSettings["EmailPass"];
        static int webmailport = int.Parse(ConfigurationManager.AppSettings["EmailPort"] ?? "587");
        static bool ssl = bool.Parse(ConfigurationManager.AppSettings["EmailSsl"] ?? "false");
        static string _domain = ConfigurationManager.AppSettings["Domain"];

        private static void init()
        {
            var info = UserContent.GetWebInfomation(true);
            if (string.IsNullOrEmpty(info.MailServer) || string.IsNullOrEmpty(info.AuthEmail) || string.IsNullOrEmpty(info.AuthPassword) || info.Port == 0)
            {
                throw new Exception("Hãy cập nhật cấu hình Email");
            }
            smtp = info?.MailServer;
            webmail = info?.AuthEmail;
            webmailpass = info?.AuthPassword;
            webmailport = info.Port;
            ssl = info.SSL;
        }

        /// <summary>
        /// Send email.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="bcc">chuoi cac email can bbc ngan cach boi dau ;.</param>
        /// <param name="cc"></param>
        /// <param name="fileAttach">duong dan tuong doi file dinh kem tren server.</param>
        /// <returns></returns>
        public static string Send(string to, string subject, string body, string bcc = "", string cc = "", string fileAttach = null)
        {
            try
            {
                init();
                var client = new SmtpClient(smtp);
                var nc = new NetworkCredential(webmail, webmailpass);
                client.Credentials = nc;
                client.Port = webmailport;
                client.EnableSsl = ssl;

                var mail = new MailMessage();
                var info = UserContent.GetWebInfomation(true);
                mail.From = new MailAddress(webmail, info.CompanyName);
                string[] arrTo = to.Split(new char[] { ';' });
                for (int i = 0; i < arrTo.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(arrTo[i]) == false)
                    {
                        mail.To.Add(new MailAddress(arrTo[i]));
                    }
                }

                if (string.IsNullOrWhiteSpace(bcc) == false)
                {
                    // bcc
                    if (string.IsNullOrWhiteSpace(bcc) == false)
                    {
                        string[] ademail = bcc.Split(new char[] { ';' });
                        foreach (var item in ademail)
                        {
                            if (string.IsNullOrWhiteSpace(item) == false && to.Contains(item) == false)
                            {
                                mail.Bcc.Add(new MailAddress(item));
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(cc) == false)
                {
                    // cc
                    string[] ccArr = cc.Split(new char[] { ';' });
                    foreach (var item in ccArr)
                    {
                        if (string.IsNullOrWhiteSpace(item) == false && to.Contains(item) == false)
                        {
                            mail.CC.Add(new MailAddress(item));
                        }
                    }
                }

                mail.Subject = subject;
                mail.Body = "<html><header></header><body>" + body + "</body></html>";
                mail.IsBodyHtml = true;
                mail.BodyEncoding = System.Text.Encoding.UTF8;

                if (string.IsNullOrWhiteSpace(fileAttach) == false)
                {
                    string[] attachFileArr = fileAttach.Split(new char[] { ';' });
                    foreach (var item in attachFileArr)
                    {
                        if (string.IsNullOrWhiteSpace(item) == false)
                        {
                            var attack = new Attachment(item, MediaTypeNames.Application.Octet);
                            mail.Attachments.Add(attack);
                        }
                    }
                }

                client.Send(mail);

                client.Dispose();

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        /// <summary>
        /// gui email thong bao.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="to"></param>
        /// <param name="firstname"></param>
        /// <param name="subject"></param>
        /// <param name="cc">email can cc.</param>
        /// <param name="signature">false: khong dung default footer.</param>
        /// <returns></returns>
        public static string SendEmailNotice_Vi(string body, string to, string firstname, string subject, string cc = "", bool signature = true)
        {
            string b = "Xin chào " + firstname + ",<br/>";
            b += body;
            if (signature)
            {
                b += "<br/><br/>Phòng CSKH<br/>" + _domain + "<br/>";
            }
            return Send(to, subject, b, string.Empty, cc);
        }

        public static string SendEmailNotice(string body, string to, string firstname, string subject, string cc = "", bool signature = true)
        {
            string b = "Dear " + firstname + ",<br/>";
            b += body;
            if (signature)
            {
                b += "<br/><br/>Support Team <br/>" + _domain + "<br/><hr/>" +
                    "<p  style='color:darkgrey' >Please do not reply to this email.This mailbox is not monitored and you will not receive a response.</p>";
            }
            return Send(to, subject, b, string.Empty, cc);
        }

        /// <summary>
        /// Email notice when password has changed.
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="to"></param>
        public static void SendEmailAfterChangedPass(string memberName, string username, string password, string to)
        {
            string body = "<p>Dear " + memberName + ", </p> " +
                        "<p>You are receiving this notification because you have changed password  in <a href='" + _domain + "'>" + _domain + "</a>.</p>" +
                        "<p>Your account information is as follows:</p>" +
                        "---------------------------- " +
                        "<p>Username: " + username + " </p>" +
                        "<p>password : " + password + " </p> " +
                        "----------------------------<br/><br/>" +

                       "Best Regards,<br/> Support Team  <br/>" +
                       "<a href='" + _domain + "'>" + _domain + "</a> <br/> " +
                       "<hr>" +
                        "<p style='color:darkgrey'> " +
                        "Please do not reply to this email.This mailbox is not monitored and you will not receive a response.";

            Send(to, "Your account password has changed", body);
        }

        /// <summary>
        /// Gui email sau khi reset pass.
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="to"></param>
        public static void SendEmailAfterResetPass(string memberName, string username, string password, string to)
        {
            string body = "<p>Dear " + memberName + ", </p> " +
                        "<p>You are receiving this notification because your password has been resetted by admin in <a href='" + _domain + "'>" + _domain + "</a>.</p>" +
                        "<p>Your account information is as follows:</p>" +
                        "---------------------------- " +
                        "<p>Username: " + username + " </p>" +
                        "<p>password : " + password + " </p> " +
                        "----------------------------<br/><br/>" +
                        "<p>Please note that this password is being generated automatically. To ensure the accuracy of your information and privacy," +
                        "please login to update and change your password at your earliest convenience.<br/><br/>" +

                       "Best Regards,<br/> Support Team  <br/>" +
                       "<a href='" + _domain + "'>" + _domain + "</a> <br/> " +
                       "<hr>" +
                        "<p style='color:darkgrey'> " +
                        "Please do not reply to this email.This mailbox is not monitored and you will not receive a response.";

            Send(to, "Your password has been resetted", body);
        }

        /// <summary>
        /// Email notice when password has changed.
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="to"></param>
        public static void SendEmailAfterFortgotPass(string memberName, string username, string password, string to)
        {
            string body = "<p>Dear " + memberName + ", </p> " +
                        "<p>You are receiving this notification because you forgot password  in <a href='" + _domain + "'>" + _domain + "</a>.</p>" +
                        "<p>Your account information is as follows:</p>" +
                        "---------------------------- " +
                        "<p>Username: " + username + " </p>" +
                        "<p>password : " + password + " </p> " +
                        "----------------------------<br/><br/>" +

                       "Best Regards,<br/> Support Team  <br/>" +
                       "<a href='" + _domain + "'>" + _domain + "</a> <br/> " +
                       "<hr>" +
                        "<p style='color:darkgrey'> " +
                        "Please do not reply to this email.This mailbox is not monitored and you will not receive a response.";

            Send(to, "Your account", body);
        }
    }
}