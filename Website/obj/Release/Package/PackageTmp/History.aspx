<%@ Page Title="" Language="C#" MasterPageFile="~/ws.Master" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="Home_Weather_Hub.History" %>
<%@ MasterType VirtualPath="~/ws.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h2><asp:Label ID="lblStationName" runat="server" Text="Station Name"></asp:Label></h2>

    <div class="content">

        <h3><asp:Label ID="lblDateRange" runat="server" Text="Summary 01/01/2000"></asp:Label></h3>

        <div class="table">

            <p>
                <span class="rowHeader lightbluetext">Temps</span>
                <asp:Label ID="lblMin1" runat="server" CssClass="rowValueHalfHeader" Text="Min"></asp:Label>
                <asp:Label ID="lblMax1" runat="server" CssClass="rowValueHalfHeader" Text="Max"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Outside</span>
                <asp:Label ID="lblMinTempOut" runat="server" CssClass="rowValueHalf" Text="10.2"></asp:Label>
                <asp:Label ID="lblMaxTempOut" runat="server" CssClass="rowValueHalf" Text="20.5"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Inside</span>
                <asp:Label ID="lblMinTempIn" runat="server" CssClass="rowValueHalf" Text="10.2"></asp:Label>
                <asp:Label ID="lblMaxTempIn" runat="server" CssClass="rowValueHalf" Text="20.5"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Rain</span>
                <asp:Label ID="lblMin3" runat="server" CssClass="rowValueHalfHeader" Text="&nbsp;"></asp:Label>
                <asp:Label ID="lblMax3" runat="server" CssClass="rowValueHalfHeader" Text="Max"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Total</span>
                <asp:Label ID="lblTotalRain" runat="server" CssClass="rowValue" Text="35 mm"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Rate</span>
                <asp:Label ID="lblMaxRainRate" runat="server" CssClass="rowValue" Text="35 mm/h"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Wind</span>
                <asp:Label ID="lblMin2" runat="server" CssClass="rowValueHalfHeader" Text="&nbsp;"></asp:Label>
                <asp:Label ID="lblMax2" runat="server" CssClass="rowValueHalfHeader" Text="&nbsp"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Max. Speed</span>
                <asp:Label ID="lblMaxWindSpeed" runat="server" CssClass="rowValue" Text="15 km"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Max. Gusts</span>
                <asp:Label ID="lblMaxWindGust" runat="server" CssClass="rowValue" Text="20 km"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Avg. Direction</span>
                <asp:Label ID="lblAvgWindDir" runat="server" CssClass="rowValue" Text="SW"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Humidty</span>
                <asp:Label ID="lblMinHumidity" runat="server" CssClass="rowValueHalfHeader" Text="Min"></asp:Label>
                <asp:Label ID="lblMaxHumidity" runat="server" CssClass="rowValueHalfHeader" Text="Max"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Outside</span>
                <asp:Label ID="lblMinHumidityOut" runat="server" CssClass="rowValueHalf" Text="0 %"></asp:Label>
                <asp:Label ID="lblMaxHumidityOut" runat="server" CssClass="rowValueHalf" Text="0 %"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Inside</span>
                <asp:Label ID="lblMinHumidityIn" runat="server" CssClass="rowValueHalf" Text="0 %"></asp:Label>
                <asp:Label ID="lblMaxHumidityIn" runat="server" CssClass="rowValueHalf" Text="0 %"></asp:Label>
            </p>
            <br />
            <p>
                <span class="rowHeader lightbluetext">Misc</span>
                <asp:Label ID="lblMinMisc" runat="server" CssClass="rowValueHalfHeader" Text="Min"></asp:Label>
                <asp:Label ID="lblMaxMisc" runat="server" CssClass="rowValueHalfHeader" Text="Max"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">Pressure</span>
                <asp:Label ID="lblMinPressure" runat="server" CssClass="rowValueHalf" Text="980"></asp:Label>
                <asp:Label ID="lblMaxPressure" runat="server" CssClass="rowValueHalf" Text="1010"></asp:Label>
            </p>
            <p>
                <span class="rowHeader">UV Index</span>
                <asp:Label ID="lblMinUV" runat="server" CssClass="rowValueHalf" Text="-"></asp:Label>
                <asp:Label ID="lblMaxUV" runat="server" CssClass="rowValueHalf" Text="8"></asp:Label>
            </p>
            <br />
            <div>
                <asp:Button ID="btnPrevious" CssClass="aspbutton" runat="server" Text="<" Visible="True" Width="20%" PostBackUrl="History.aspx?id=1&amp;mode=0&amp;add=1" />
                <asp:DropDownList ID="ddlRange" CssClass="ddlstyle" runat="server" Width="30%" AutoPostBack="True" OnSelectedIndexChanged="ddlRange_SelectedIndexChanged">
                    <asp:ListItem Value="0">Day</asp:ListItem>
                    <asp:ListItem Value="1">Week</asp:ListItem>
                    <asp:ListItem Value="2">Month</asp:ListItem>
                    <asp:ListItem Value="3">Year</asp:ListItem>
                    <asp:ListItem Value="4">All</asp:ListItem>
                </asp:DropDownList>
                <asp:Button ID="btnNext" CssClass="aspbutton" runat="server" Text=">" Visible="True" Width="20%" Enabled="False" PostBackUrl="History.aspx?id=1&amp;mode=0&amp;add=0"/>
            </div>
            <br />
            <p><asp:HyperLink ID="linkLive" runat="server" NavigateUrl="Current.aspx">Live Data</asp:HyperLink></p> 

        </div>

    </div>

</asp:Content>
