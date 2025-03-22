using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Home_Weather_Hub
{
    public partial class ws : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public void DisplayMessage(string message)
        {
            this.lblError.BackColor = System.Drawing.Color.LightGreen;
            this.lblError.ForeColor = System.Drawing.Color.Black;
            this.lblError.Text = message;
            this.lblError.Visible = true;
        }

        public void DisplayWarning(string message)
        {
            this.lblError.BackColor = System.Drawing.Color.LightCoral;
            this.lblError.ForeColor = System.Drawing.Color.DarkRed;
            this.lblError.Text = message;
            this.lblError.Visible = true;
        }

        public void ErrorReport(Exception ex)
        {
            this.lblError.BackColor = System.Drawing.Color.LightPink;
            this.lblError.ForeColor = System.Drawing.Color.DarkRed;
            this.lblError.Text = "An Error Has Occurred";
            this.lblError.ToolTip = ex.Message;
            this.lblError.Visible = true;
        }

        public void HideMessage()
        {
            this.lblError.Text = string.Empty;
            this.lblError.Visible = false;
        }
    }
}