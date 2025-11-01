<%@ Page Title="" Language="C#" MasterPageFile="~/ws.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="Home_Weather_Hub.Login" %>
<%@ MasterType VirtualPath="~/ws.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

        <h2>Login</h2>

        <div id="account-details">

            <fieldset id="account-entry">

                <div>

                    <asp:TextBox ID="txtLogin" runat="server" CssClass="form-control" data-val-required="The screen name / email field is required." placeholder="Screen name / Email Address" type="text" value=""></asp:TextBox>

                </div>

                <p></p>

                <div>

                    <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" data-val-required="The password field is required." placeholder="Password" type="password"></asp:TextBox>

                </div>

                <p></p>

                <div>

                    <asp:Button ID="btnLogin" runat="server" Text="Log In" CssClass="aspbutton" Height="40px" />
                                
                </div>

                <p>
                    <a href="Register.aspx" id="btn_RegisterAccount">Register</a>
                </p>
                            
            </fieldset>

        </div>

</asp:Content>
