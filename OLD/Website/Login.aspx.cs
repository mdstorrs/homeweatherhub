using System;
using Home_Weather_Hub.App_Code;
using System.Data.SqlClient;

namespace Home_Weather_Hub
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            if (IsPostBack == true)
            {
                this.LoginUser();
            }
            else
            {
                if (WSSession.Current.LoggedIn) {
                    Master.Response.Redirect("~/Account.aspx", false);
                }
            }

        }

        private void LoginUser()
        {

            string password;
            string hashedPassword;

            try
            {

                Master.HideMessage();

                using (SqlConnection cnn = new SqlConnection(WSSession.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT  UserID, Username, EmailAddress, AccessLevel FROM WSUsers WHERE (Username = @Username) AND (Password = @Password)", cnn))
                {

                    if (txtLogin.Text.Trim(' ').Length == 0)
                    {
                        Master.DisplayWarning("Invalid Screen Name or Email. This field cannot be blank.");
                        return;
                    }

                    if (txtPassword.Text.Trim(' ').Length == 0)
                    {
                        Master.DisplayWarning("Invalid Password. Password cannot be blank.");
                        return;
                    }

                    password = txtPassword.Text.Trim(' ');
                    hashedPassword = Encryption.MD5Hash(password);

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@Username", txtLogin.Text.Trim(' '));
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);


                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {

                        if (rdr.Read()) //Success
                        {
                            //Customer Details
                            WSSession.Current.LoggedIn = true;
                            WSSession.Current.UserID = (int)rdr[0];
                            WSSession.Current.Username = (string)rdr[1];
                            WSSession.Current.EmailAddress = (string)rdr[2];
                            int accessLevel;
                            int.TryParse(rdr[3].ToString(), out accessLevel);
                            WSSession.Current.AccessLevel = (WSSession.eAccessLevel)(accessLevel);
                            WSSession.Current.IPAddress = Request.UserHostAddress.ToString();
                        }
                        else //Failed
                        {

                            //Reset details
                            WSSession.Current.LoggedIn = false;
                            WSSession.Current.UserID = 0;
                            WSSession.Current.Username = "";
                            WSSession.Current.EmailAddress = "";
                            WSSession.Current.AccessLevel = WSSession.eAccessLevel.None;

                            Master.DisplayWarning("Incorrect Logon or Password.");

                            return;

                        }

                    }

                }

                if (WSSession.Current.ReturnToPage)
                {
                    Response.Redirect(WSSession.Current.ReturnToPageURL, false);
                }
                else
                {
                    Response.Redirect("~/Account.aspx", false);
                }

            }
            catch (Exception ex)
            {
                Master.ErrorReport(ex);
            }

        }

    }

}