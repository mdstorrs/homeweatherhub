<%@ Page Title="" Language="C#" MasterPageFile="~/ws.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="Home_Weather_Hub.Register" %>
<%@ MasterType VirtualPath="~/ws.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h2>Register</h2>

    <div id="account-details">

        <fieldset id="account-entry">

            <div>

                <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" data-val-required="The username field is required." placeholder="User Name" type="text" value=""></asp:TextBox>

            </div>

            <p></p>

            <div>

                <asp:TextBox ID="txtEmailAddress" runat="server" CssClass="form-control" data-val-required="The email address field is required." placeholder="Email Address" type="text" value=""></asp:TextBox>

            </div>

            <p></p>
                            
            <div>

                <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" data-val-required="The password field is required." placeholder="Password" type="password" value=""></asp:TextBox>

            </div>

            <p></p>

            <div>

                <asp:TextBox ID="txtConfirm" runat="server" CssClass="form-control" data-val-required="The confirm password field is required." placeholder="Confirm Password" type="password" value=""></asp:TextBox>

            </div>

            <p></p>                            

                           
            <div>
                                
                <asp:Button ID="btnSubmit" runat="server" Text="Register" CssClass="aspbutton" Height="40px" />

            </div>

            <p>
                <a href="Login.aspx" id="btnLogin">Already Registered?</a>
            </p>

        </fieldset>

    </div>

</asp:Content>
