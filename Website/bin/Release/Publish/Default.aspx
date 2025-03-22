<%@ Page Title="" Language="C#" MasterPageFile="~/ws.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Home_Weather_Hub.Default" %>
<%@ MasterType VirtualPath="~/ws.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h2>Recently Visited</h2>

    <div class="content">

        <p>
            <a href="Current.aspx?id=1">Casa di Storrs</a><br />
            <span class="fineprint">Dayboro QLD, Australia</span>
        </p>

        <p>
            <a href="Current.aspx?id=2">Bowerbird</a><br />
            <span class="fineprint">Cashmere QLD, Australia</span>
        </p>

    </div>

    <h2>Search All Stations</h2>

    <fieldset id="account-entry">

        <div>

            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" data-val-required="Enter a city or postcode." placeholder="City or Postcode or Station" type="text" value=""></asp:TextBox>

        </div>

        <p>
            <a href="Current.aspx?id=1">Casa di Storrs</a><br />
            <span class="fineprint">Dayboro QLD, Australia</span>
        </p>

    </fieldset>

</asp:Content>