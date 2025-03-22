using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Home_Weather_Hub.App_Code;

namespace Home_Weather_Hub
{
    public partial class History : System.Web.UI.Page
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
        private int AddDate = 0;
        private DateTime FromDate = DateTime.Now.Date;
        private DateTime ToDate = DateTime.Now.Date.AddDays(1);

        protected void Page_Load(object sender, EventArgs e)
        {

            int id;
            string passkey = "";
            string stationName = "";

            try
            {

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

                this.GetDateRange();
                this.FillText(passkey, stationName);

            }
            catch (Exception ex)
            {
                Master.ErrorReport(ex);
            }

        }

        private void FillText(string passKey, string stationName)
        {

            decimal minTempOut, maxTempOut, minTempIn, maxTempIn, maxWindSpeed, maxWindGust, maxRainRate, totalRain;
            decimal minHumidityOut, maxHumidityOut, minHumidityIn, maxHumidityIn, minBarom, maxBarom, maxUV;
            int avgWindDir;

            try
            {

                this.lblStationName.Text = stationName;

                using (SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
                {

                    using (SqlCommand cmd1 = new System.Data.SqlClient.SqlCommand("SELECT MAX(TempOutF) AS MaxTempOut, MIN(TempOutF) AS MinTempOut, MAX(TempInF) AS MaxTempIn, MIN(TempInF) AS MinTempIn, " +
                                                                                        "MAX(WSReport.WindSpeedMPH) AS MaxWind, MAX(WSReport.WindGustMPH) AS MaxWindGust, " +
                                                                                        "MAX(WSReport.HumidityOut) AS MaxHumidityOut, MIN(WSReport.HumidityOut) AS MinHumidityOut, " +
                                                                                        "MAX(WSReport.HumidityIn) AS MaxHumidityIn, MIN(WSReport.HumidityIn) AS MinHumidityIn, " +
                                                                                        "MAX(WSReport.BaromRelIn) AS MaxBarom, MIN(WSReport.BaromRelIn) AS MinBarom, " +
                                                                                        "MAX(WSReport.RainRateInch) AS MaxRainRate, MAX(WSReport.UV) AS MaxUV, AVG(WSReport.WindDir) AS AvgWindDir, " +
                                                                                        "MAX(WSReport.DailyRainInch) AS MaxDailyRain, MAX(WSReport.WeeklyRainInch) AS MaxWeeklyRain, " +
                                                                                        "MAX(WSReport.MonthlyRainIn) AS MaxMonthlyRain, MAX(WSReport.TotalRainInch) AS TotalRain " +
                                                                                    "FROM WSReport " +
                                                                                    "WHERE(Passkey = @PassKey) AND(DateAdded BETWEEN @FromDate AND @ToDate) AND (TempOutF <> 0 )" +
                                                                                    "GROUP BY Passkey", cnn))
                    using (SqlCommand cmd2 = new System.Data.SqlClient.SqlCommand("SELECT (SUM(MaxDaily.TotalDailyRain)) AS TotalRain FROM (" +
                                                                                   "SELECT MAX(DailyRainInch) AS TotalDailyRain, DATEADD(DAY, 0, DATEDIFF(DAY, 0, DateAdded)) AS DateAdded " +
                                                                                   "FROM WSReport " +
                                                                                   "WHERE(WSReport.Passkey = @PassKey) AND(WSReport.DateAdded BETWEEN @FromDate AND @ToDate) " +
                                                                                   "GROUP BY DATEADD(DAY, 0, DATEDIFF(DAY, 0, DateAdded))) MaxDaily", cnn))
                    {

                        cmd1.Parameters.AddWithValue("@PassKey", passKey);
                        cmd1.Parameters.AddWithValue("@FromDate", this.FromDate);
                        cmd1.Parameters.AddWithValue("@ToDate", this.ToDate);

                        cmd2.Parameters.AddWithValue("@PassKey", passKey);
                        cmd2.Parameters.AddWithValue("@FromDate", this.FromDate);
                        cmd2.Parameters.AddWithValue("@ToDate", this.ToDate);

                        cnn.Open();

                        using (SqlDataReader rdr1 = cmd1.ExecuteReader())
                        {

                            if (rdr1.Read())
                            {

                                minTempOut = (decimal.Parse(rdr1["MinTempOut"].ToString()));
                                maxTempOut = (decimal.Parse(rdr1["MaxTempOut"].ToString()));
                                minTempIn = (decimal.Parse(rdr1["MinTempIn"].ToString()));
                                maxTempIn = (decimal.Parse(rdr1["MaxTempIn"].ToString()));
                                maxRainRate = (decimal.Parse(rdr1["MaxRainRate"].ToString()));
                                maxWindGust = (decimal.Parse(rdr1["MaxWindGust"].ToString()));
                                maxWindSpeed = (decimal.Parse(rdr1["MaxWind"].ToString()));

                                minHumidityOut = (decimal.Parse(rdr1["minHumidityOut"].ToString()));
                                maxHumidityOut = (decimal.Parse(rdr1["maxHumidityOut"].ToString()));
                                minHumidityIn = (decimal.Parse(rdr1["minHumidityIn"].ToString()));
                                maxHumidityIn = (decimal.Parse(rdr1["maxHumidityIn"].ToString()));
                                minBarom = (decimal.Parse(rdr1["minBarom"].ToString()));
                                maxBarom = (decimal.Parse(rdr1["maxBarom"].ToString()));
                                maxUV = (decimal.Parse(rdr1["maxUV"].ToString()));
                                avgWindDir = (int.Parse(rdr1["avgWindDir"].ToString()));

                            }
                            else
                            {
                                //No Data
                                Master.DisplayWarning("No data for the selected range");
                                return;
                            }

                        }

                        using (SqlDataReader rdr2 = cmd2.ExecuteReader())
                        {

                            if (rdr2.Read())
                            {
                                totalRain = (decimal.Parse(rdr2["TotalRain"].ToString()));
                            }
                            else
                            {
                                totalRain = -1;
                            }

                        }

                        if (WSSession.Current.PreferredUnit == WSSession.eUnit.Celsius)
                        {
                            minTempOut = Math.Round((minTempOut - 32) * 5 / 9, 1);
                            maxTempOut = Math.Round((maxTempOut - 32) * 5 / 9, 1);
                            minTempIn = Math.Round((minTempIn - 32) * 5 / 9, 1);
                            maxTempIn = Math.Round((maxTempIn - 32) * 5 / 9, 1);
                            maxRainRate = Math.Round((maxRainRate * (decimal)25.4), 0);
                            maxWindGust = Math.Round((maxWindGust * (decimal)1.609), 1);
                            maxWindSpeed = Math.Round((maxWindSpeed * (decimal)1.609), 1);
                            totalRain = Math.Round((totalRain * (decimal)25.4), 1);

                            minBarom = Math.Round(minBarom * (decimal)33.864, 1);
                            maxBarom = Math.Round(maxBarom * (decimal)33.8647, 1);

                            this.lblMaxRainRate.Text = maxRainRate.ToString("F0") + " mm/hr";
                            this.lblTotalRain.Text = totalRain.ToString("F1") + " mm";
                            this.lblMaxWindGust.Text = maxWindGust.ToString("F1") + " km/h";
                            this.lblMaxWindSpeed.Text = maxWindSpeed.ToString("F1") + " km/h";
                            this.lblMinPressure.Text = minBarom.ToString("F0");
                            this.lblMaxPressure.Text = maxBarom.ToString("F0");
                        }
                        else
                        {
                            this.lblMaxRainRate.Text = maxRainRate.ToString("F0") + " in/hr";
                            this.lblTotalRain.Text = totalRain.ToString("F1") + " in";
                            this.lblMaxWindGust.Text = maxWindGust.ToString("F1") + " mph";
                            this.lblMaxWindSpeed.Text = maxWindSpeed.ToString("F1") + " mph";
                            this.lblMinPressure.Text = minBarom.ToString("F1");
                            this.lblMaxPressure.Text = maxBarom.ToString("F1");
                        }

                        this.lblMinTempOut.Text = minTempOut.ToString("F1");
                        this.lblMaxTempOut.Text = maxTempOut.ToString("F1");
                        this.lblMinTempIn.Text = minTempIn.ToString("F1");
                        this.lblMaxTempIn.Text = maxTempIn.ToString("F1");
                        this.lblMaxHumidityIn.Text = maxHumidityIn.ToString("F0") + "%";
                        this.lblMinHumidityIn.Text = minHumidityIn.ToString("F0") + "%";
                        this.lblMaxHumidityOut.Text = maxHumidityOut.ToString("F0") + "%";
                        this.lblMinHumidityOut.Text = minHumidityOut.ToString("F0") + "%";
                        this.lblMaxUV.Text = maxUV.ToString("F0");
                        this.lblAvgWindDir.Text = WSGlobal.GetWindDirection(avgWindDir);

                    }

                }

                this.linkLive.NavigateUrl = $"Current.aspx?id={this.CurrentID}";

                Master.HideMessage();

            }
            catch (Exception ex)
            {
                Master.ErrorReport(ex);
            }
            finally
            {
                //Nothing to do here
            }

        }

        private void GetDateRange()
        {

            int addDate;
            int mode;

            //If the user has copied the url, get the date mode and set the drop down
            if (!IsPostBack)
            {
                if (int.TryParse(Master.Request.QueryString["mode"], out mode))
                {
                    if (mode != int.Parse(this.ddlRange.SelectedValue))
                    {
                        this.ddlRange.SelectedValue = mode.ToString();
                    }
                }
            }

            //mode = int.Parse(this.ddlRange.SelectedValue);
            if (int.TryParse(Master.Request.QueryString["add"], out addDate)) { this.AddDate = addDate; } else { this.AddDate = 0; }


            switch ((dateMode)int.Parse(this.ddlRange.SelectedValue))
            {
                case dateMode.week:
                    DateTime day = this.ToDate = DateTime.Now.Date.AddDays(-(this.AddDate * 7) + 1);
                    this.FromDate = WSGlobal.GetWSWeek(day);
                    this.ToDate = this.FromDate.AddDays(7);
                    this.lblDateRange.Text = $"{this.FromDate.ToShortDateString()} to {this.ToDate.AddDays(-1).ToShortDateString()}";
                    break;
                case dateMode.month:
                    DateTime thisMonthStart = DateTime.Now.Date.AddDays(-(DateTime.Now.Day - 1));
                    this.FromDate = thisMonthStart.AddMonths(-(this.AddDate));
                    this.ToDate = this.FromDate.AddMonths(1);
                    DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
                    this.lblDateRange.Text = $"{dtfi.GetMonthName(this.FromDate.Month)} {this.FromDate.Year}";
                    break;
                case dateMode.year:
                    this.ToDate = new DateTime(DateTime.Now.Date.AddYears(-(this.AddDate) + 1).Date.Year, 1, 1);
                    this.FromDate = this.ToDate.AddYears(-1);
                    this.lblDateRange.Text = $"{this.FromDate.Year}";
                    break;
                case dateMode.all:
                    this.FromDate = new DateTime(2000, 1, 1);
                    this.ToDate = DateTime.Now.Date.AddDays(1);
                    this.lblDateRange.Text = "All Time";
                    break;
                default:
                    this.ToDate = DateTime.Now.Date.AddDays(-(this.AddDate) + 1);
                    this.FromDate = this.ToDate.AddDays(-1);
                    //this.FromDate = DateTime.Now.Date.AddDays(-(this.AddDate));
                    //this.ToDate = DateTime.Now.Date.AddDays(-(this.AddDate) + 1);
                    if (this.AddDate == 0)
                    {
                        this.lblDateRange.Text = "Today";
                    }
                    else
                    {
                        this.lblDateRange.Text = $"{this.FromDate.ToShortDateString()}";
                    }
                    break;
            }

            //Unhide this to see the proper date range
            //this.lblDateRange.Text = $"{this.FromDate.ToShortDateString()} to {this.ToDate.AddDays(-1).ToShortDateString()}";

            //Set the post back urls for the next and previous buttons
            this.SetButtonPostBackUrls();

            //Onlye enable the next button when required
            if (this.AddDate == 0) { btnNext.Enabled = false; } else { btnNext.Enabled = true; }

        }

        private void SetButtonPostBackUrls()
        {

            btnPrevious.PostBackUrl = $"History.aspx?id={this.CurrentID}&add={this.AddDate + 1}&mode={int.Parse(this.ddlRange.SelectedValue)}";
            btnNext.PostBackUrl = $"History.aspx?id={this.CurrentID}&add={this.AddDate - 1}&mode={int.Parse(this.ddlRange.SelectedValue)}";

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

        protected void ddlRange_SelectedIndexChanged(object sender, EventArgs e)
        {

            //What we need to do here is get the mode and the date range and if the mode changes, calculae a new number of days or months to add based on the date range
            //For now it is easier to reset the add date value and make the user start from "now"
            this.AddDate = 0;

            Response.Redirect($"History.aspx?id={this.CurrentID}&add={this.AddDate}&mode={int.Parse(this.ddlRange.SelectedValue)}");

        }

    }
}