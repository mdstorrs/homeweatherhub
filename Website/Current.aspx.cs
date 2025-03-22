using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Home_Weather_Hub.App_Code;

namespace Home_Weather_Hub
{
    public partial class Current : System.Web.UI.Page
    {

        private enum dateMode : int
        {
            day,
            week,
            month,
            year,
            all
        }

        private int CurrentID = 1;

        protected void Page_Load(object sender, EventArgs e)
        {

            int id;
            string passkey = "";
            string stationName = "";

            if (int.TryParse(Master.Request.QueryString["id"], out id))
            {
                if (WSGlobal.GetPassKey(id, ref passkey, ref stationName))
                {
                    CurrentID = id;
                }
            }

            if (passkey == String.Empty)
            {
                passkey = "64342F32A039AFA8CACC2061B1A77938";
                stationName = "Casa di Storrs";
            }

            this.FillText(passkey, stationName);

        }

        private void FillText(string passKey, string stationName)
        {

            DateTime lastUpdated = DateTime.Now;
            bool isOnline = false;

            decimal tempIn, tempOut, baromRel, windSpeed, windGust, maxGust, rainRate, rainDay, minTempOut = 0, maxTempOut = 0, minTempIn = 0, maxTempIn = 0;
            int humidityIn, humidityOut, uvIndex, windDir;

            try
            {

                this.lblStationName.Text = stationName;

                using (SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
                {

                    using (SqlCommand cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP (1) * FROM WSReport WHERE (Passkey = @PassKey) ORDER BY DateAdded DESC;", cnn))
                    {

                        cnn.Open();
                        cmd.Parameters.AddWithValue("@PassKey", passKey);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {

                            while (rdr.Read())
                            {

                                tempOut = (decimal.Parse(rdr["TempOutF"].ToString()));
                                tempIn = (decimal.Parse(rdr["TempInF"].ToString()));
                                humidityIn = (int.Parse(rdr["HumidityIn"].ToString()));
                                humidityOut = (int.Parse(rdr["HumidityOut"].ToString()));
                                lastUpdated = (DateTime.Parse(rdr["DateAdded"].ToString()));
                                baromRel = (decimal.Parse(rdr["BaromRelIn"].ToString()));
                                windDir = (int.Parse(rdr["WindDir"].ToString()));
                                windSpeed = (decimal.Parse(rdr["WindSpeedMPH"].ToString()));
                                windGust = (decimal.Parse(rdr["WindGustMPH"].ToString()));
                                maxGust = (decimal.Parse(rdr["MaxDailyGust"].ToString()));
                                rainRate = (decimal.Parse(rdr["RainRateInch"].ToString()));
                                rainDay = (decimal.Parse(rdr["DailyRainInch"].ToString()));
                                uvIndex = (int.Parse(rdr["UV"].ToString()));

                                if (WSSession.Current.PreferredUnit == WSSession.eUnit.Celsius)
                                {
                                    tempOut = Math.Round((tempOut - 32) * 5 / 9, 1);
                                    tempIn = Math.Round((tempIn - 32) * 5 / 9, 1);
                                    baromRel = Math.Round(baromRel * (decimal)33.863886666667, 1);
                                    this.lblTempUnit.Text = "C";
                                    this.lblPressue.Text = baromRel.ToString() + " hPa";
                                    windSpeed = Math.Round(windSpeed * (decimal)1.609344, 1);
                                    windGust = Math.Round(windGust * (decimal)1.609344, 1);
                                    maxGust = Math.Round(maxGust * (decimal)1.609344, 1);
                                    this.lblWindSpeed.Text = windSpeed.ToString() + " km/h";
                                    this.lblWindGust.Text = windGust.ToString() + " km/h";
                                    rainRate = Math.Round(rainRate * (decimal)25.4, 1);
                                    rainDay = Math.Round(rainDay * (decimal)25.4, 1);
                                    this.lblPrecipRate.Text = rainRate.ToString() + " mm";
                                    this.lblPrecipAccum.Text = rainDay.ToString() + " mm";
                                }
                                else
                                {
                                    this.lblTempUnit.Text = "F";
                                    this.lblPressue.Text = baromRel.ToString() + " in";
                                    this.lblWindSpeed.Text = windSpeed.ToString() + " mph";
                                    this.lblWindGust.Text = windGust.ToString() + " mph";
                                    this.lblPrecipRate.Text = rainRate.ToString() + " in";
                                    this.lblPrecipAccum.Text = rainDay.ToString() + " in";
                                }

                                this.lblWindDirection.Text = WSGlobal.GetWindDirection(windDir);
                                this.lblTempValue.Text = tempOut.ToString("F1");
                                this.lblTempInside.Text = tempIn.ToString("F1");
                                this.lblHumidity.Text = "Humidity " + humidityOut.ToString() + "%";
                                this.lblHumidtyInside.Text = humidityIn.ToString() + "%";
                                this.lblUV.Text = uvIndex.ToString();

                                if (tempOut <= 18)
                                {
                                    lblTempValue.CssClass = "maintemp lightbluetext";
                                }
                                else if (tempOut > 32)
                                {
                                    lblTempValue.CssClass = "maintemp darkredtext";
                                }
                                else if (tempOut > 26)
                                {
                                    lblTempValue.CssClass = "maintemp darkorangetext";
                                }
                                else
                                {
                                    lblTempValue.CssClass = "maintemp midgreentext";
                                }

                            }

                        }

                    }

                }

                string lastUpdatedString = GetLastUpdated(lastUpdated, ref isOnline);

                if (isOnline == true)
                {
                    this.lblUpdated.CssClass = "online";
                    this.lblUpdated.Text = "Online " + lastUpdatedString;
                }
                else
                {
                    this.lblUpdated.CssClass = "offline";
                    this.lblUpdated.Text = "Offline " + lastUpdatedString;
                }

                linkHistory.NavigateUrl = $"History.aspx?id={this.CurrentID}";

            }
            catch (System.IO.IOException ex)
            {
                this.lblUpdated.Text = "ERROR " + ex.Message;
            }
            finally
            {
                //Nothing to do here
            }

        }

        private string GetLastUpdated(DateTime lastUpdated, ref bool online)
        {

            int seconds = (int)Math.Floor((DateTime.Now - lastUpdated).TotalSeconds);
            int minutes = (int)Math.Floor((DateTime.Now - lastUpdated).TotalMinutes);
            int hours = (int)Math.Floor((DateTime.Now - lastUpdated).TotalHours);

            if (minutes > WSSession.ONLINE_MIN)
            {
                online = false;
            }
            else
            {
                online = true;
            }

            if (hours > 1)
            {
                return "(updated " + hours.ToString() + " hours ago)";
            }
            else if (minutes > 1)
            {
                return "(updated " + minutes.ToString() + " minutes ago)";
            }
            else
            {
                return "(updated " + seconds.ToString() + " seconds ago)";
            }

        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                Response.Redirect($"Current.aspx?id={this.CurrentID}");
            }
            catch
            {

            }

        }

    }

}