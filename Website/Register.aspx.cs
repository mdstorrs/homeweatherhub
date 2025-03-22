using System;
using System.Data.SqlClient;
using Home_Weather_Hub.App_Code;

namespace Home_Weather_Hub
{
    public partial class Register : System.Web.UI.Page
    {

        private string redirect;

        protected void Page_Load(object sender, EventArgs e)
        {

            redirect = Master.Request.QueryString["Redirect"];

            if (IsPostBack == true)
            {
                this.RegisterUser();
            }

        }

        private void RegisterUser()
        {

            string hashedPassword;
            int userID;

            try
            {

                Master.HideMessage();

                if (this.ValidateData() == false)
                {
                    return;
                }

                hashedPassword = Encryption.MD5Hash(txtPassword.Text.Trim(' '));

                using (SqlConnection cnn = new SqlConnection(WSSession.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("INSERT INTO WSUsers(Username, Password, EmailAddress, AccessLevel) VALUES (@Username, @Password, @EmailAddress, @AccessLevel); SELECT @@IDENTITY", cnn))
                {

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim(' '));
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@EmailAddress", txtEmailAddress.Text.Trim(' '));
                    cmd.Parameters.AddWithValue("@AccessLevel", WSSession.eAccessLevel.User);

                    object newuser = cmd.ExecuteScalar();

                    int.TryParse(newuser.ToString(), out userID);

                }

                if (userID > 0)
                {

                    //Success
                    WSSession.Current.UserID = userID;
                    WSSession.Current.Username = txtUsername.Text.Trim(' ');
                    WSSession.Current.EmailAddress = txtEmailAddress.Text.Trim(' ');
                    WSSession.Current.LoggedIn = true;
                    WSSession.Current.AccessLevel = WSSession.eAccessLevel.User;

                    if (String.IsNullOrEmpty(redirect))
                    {
                        Response.Redirect("Account.aspx", false);
                    }
                    else
                    {
                        Response.Redirect(redirect, false);
                    }

                }
                else
                {
                    //Failed
                    WSSession.Current.UserID = 0;
                    WSSession.Current.Username = "Not Logged In";
                    WSSession.Current.EmailAddress = "";
                    WSSession.Current.AccessLevel = WSSession.eAccessLevel.None;
                    WSSession.Current.LoggedIn = false;

                }


            }
            catch (Exception ex)
            {
                Master.ErrorReport(ex);
            }

        }

        private bool ValidateData()
        {
            try
            {
                if (this.txtUsername.Text.Trim(' ').Length == 0)
                {
                    Master.DisplayWarning("Invalid User Name.");
                    return false;
                }

                if (this.txtEmailAddress.Text.Trim(' ').Length == 0)
                {
                    Master.DisplayWarning("Invalid Email Address.");
                    return false;
                }

                if (!this.txtEmailAddress.Text.Contains("@") || !this.txtEmailAddress.Text.Contains("."))
                {
                    Master.DisplayWarning("Invalid Email Address.");
                    return false;
                }

                if (this.txtPassword.Text.Trim(' ').Length == 0)
                {
                    Master.DisplayWarning("Invalid Password.");
                    return false;
                }

                if (this.txtConfirm.Text.Trim(' ').Length == 0)
                {
                    Master.DisplayWarning("Invalid Confirmation Password. ");
                    return false;
                }

                if (this.txtConfirm.Text.Trim(' ') != this.txtPassword.Text.Trim(' '))
                {
                    Master.DisplayWarning("Passwords don't match.");
                    this.txtConfirm.Text = "";
                    this.txtPassword.Text = "";
                    return false;
                }

                using (System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM WSUsers WHERE (Username = @Username) OR (EmailAddress = @EmailAddress)", cnn)
        )
                {
                    cmd.Parameters.AddWithValue("@Username", this.txtUsername.Text.Trim(' '));
                    cmd.Parameters.AddWithValue("@EmailAddress", this.txtEmailAddress.Text.Trim(' '));

                    cnn.Open();

                    int count = (int)cmd.ExecuteScalar();

                    if (count > 0)
                    {
                        Master.DisplayWarning("User Name and/or Email Address already exists.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Master.ErrorReport(ex);
            }

            return false;
        }


    }

}