<%@ Page Title="" Language="C#" MasterPageFile="~/ws.Master" AutoEventWireup="true" CodeBehind="Current.aspx.cs" Inherits="Home_Weather_Hub.Current" %>
<%@ MasterType VirtualPath="~/ws.master" %>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h2><asp:Label ID="lblStationName" runat="server" Text="Station Name"></asp:Label></h2>

    <div class="content">

        <h3><asp:Label ID="lblCurrent" runat="server" Text="Current Conditions"></asp:Label></h3>
        <p class="updated"><asp:Label ID="lblUpdated" runat="server" CSSClass="" Text="Offline"></asp:Label></p>

        <p>
            <asp:Label ID="lblTempValue" runat="server" CssClass="maintemp lightbluetext" Text="25.6"></asp:Label>
            <asp:Label ID="lblDegrees" runat="server" CssClass="maintempdegree" Text="&#176"></asp:Label>
            <asp:Label ID="lblTempUnit" runat="server" CssClass="maintempunit" Text="c"></asp:Label>
        </p>
        <p class="mainhumidty">
            <asp:Label ID="lblHumidity" runat="server" Text="Humidity 80%"></asp:Label>
        </p>

        <div class="table">

            <p>
                <span class="rowHeader lightbluetext">Rain</span>
                <asp:Label ID="lblRain" runat="server" CssClass="rowValue" Text="&nbsp;"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Rate</span>
                <asp:Label ID="lblPrecipRate" runat="server" CssClass="rowValue" Text="0.0 mm"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Accum.</span>
                <asp:Label ID="lblPrecipAccum" runat="server" CssClass="rowValue" Text="5.2 mm"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Wind</span>
                <asp:Label ID="lblWind" runat="server" CssClass="rowValue" Text="&nbsp;"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Direction</span>
                <asp:Label ID="lblWindDirection" runat="server" CssClass="rowValue" Text="NNE"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Speed</span>
                <asp:Label ID="lblWindSpeed" runat="server" CssClass="rowValue" Text="5 km/h"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Gusts</span>
                <asp:Label ID="lblWindGust" runat="server" CssClass="rowValue" Text="8 km/h"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Inside</span>
                <asp:Label ID="lblInside" runat="server" CssClass="rowValue" Text="&nbsp;"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Temp</span>
                <asp:Label ID="lblTempInside" runat="server" CssClass="rowValue" Text="20.2"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Humidty</span>
                <asp:Label ID="lblHumidtyInside" runat="server" CssClass="rowValue" Text="65%"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader">Pressure</span>
                <asp:Label ID="lblPressue" runat="server" CssClass="rowValue" Text="1022 hpa"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">UV Index</span>
                <asp:Label ID="lblUV" runat="server" CssClass="rowValue" Text="4"></asp:Label>
            </p>
            <br />
            <asp:Button ID="btnRefresh" CssClass="aspbutton" runat="server" Text="Refresh" Visible="True" Width="75%" OnClick="btnRefresh_Click" UseSubmitBehavior="False"/>
            <br />
            <br />
            <p><asp:HyperLink ID="linkHistory" runat="server" NavigateUrl="History.aspx">Historical Data</asp:HyperLink></p> 

        </div>

    </div>

</asp:Content>