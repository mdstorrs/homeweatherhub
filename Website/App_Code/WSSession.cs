using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Home_Weather_Hub.App_Code
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.IO;
    using System.Text;
    using System.Web.UI;
    using System.Configuration;

    public class WSSession
    {

        public const int ONLINE_MIN = 12;
        public bool LoggedIn { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public eAccessLevel AccessLevel { get; set; }
        public eUnit PreferredUnit { get; set; }
        public string PassKey { get; set; }
        public string StationName { get; set; } = "Casa di Storrs";
        public string IPAddress { get; set; }
        public string ReturnToPageURL { get; set; }
        public bool ReturnToPage
        {
            get
            {
                return !string.IsNullOrEmpty(this.ReturnToPageURL);
            }
        }

        public enum eAccessLevel
        {
            None = 0,
            User = 1,
            Host = 2,
            PowerHost = 5,
            Admin = 10,
            Blocked = 99
        }

        public enum eUnit
        {
            Celsius,
            Fahrenheit
        }

        public static string ConnectionString
        {
            get
            {
                // Return ConfigurationManager.ConnectionStrings("ApplicationServices").ConnectionString
                System.Data.SqlClient.SqlConnectionStringBuilder csb = new System.Data.SqlClient.SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString);
                //csb.Password = Encryption.Decrypt(csb.Password);
                return csb.ToString();
            }
        }

        public bool CheckLogin(Page page, string returnURL = null, bool register = false)
        {
            if (!Current.LoggedIn)
            {
                if (returnURL != null)
                    Current.ReturnToPageURL = returnURL;
                else
                    Current.ReturnToPageURL = page.Request.Url.AbsoluteUri;

                if (register)
                    page.Response.Redirect("~/ws/Register.aspx", false);
                else
                    page.Response.Redirect("~/ws/Login.aspx", false);

                return false;
            }
            else
                return true;
        }

        public void LogOut()
        {
            {
                var withBlock = Current;
                withBlock.AccessLevel = 0;
                withBlock.LoggedIn = false;
                withBlock.EmailAddress = "";
                withBlock.UserID = 0;
            }
        }



        // Set Default Values here if you need.
        private WSSession()
        {
        }

        public static WSSession Current
        {
            get
            {
                WSSession session = (WSSession)HttpContext.Current.Session["__WSSession__"];

                if (session == null)
                {
                    session = new WSSession();
                    HttpContext.Current.Session["__WSSession__"] = session;
                }

                return session;
            }
        }
    }
}