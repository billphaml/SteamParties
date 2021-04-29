<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SteamParties._Default" Async="true" %>

<%@ Register Src="~/Account/OpenAuthProviders.ascx" TagPrefix="uc" TagName="OpenAuthProviders" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>Steam Parties</h1>
        <p class="lead">Log in to save members.</p>
    </div>

    <div class="row">
        <br />
        <asp:TextBox ID="TextBox1" runat="server" placeholder="SteamID or VanityID" Width="540px"></asp:TextBox>
        &nbsp;&nbsp;&nbsp;
        <asp:Button ID="Button1" runat="server" Text="Add" OnClick="Button1_Click" />
        <br />
        <h1>Members</h1>
        <p>
        <asp:Button ID="Button2" runat="server" OnClick="Button2_Click" Text="Clear" />
        </p>
        <asp:TextBox ID="TextBox2" runat="server" Height="200px" Width="600px" TextMode="MultiLine" ReadOnly="true"></asp:TextBox>
        <br />
        <h1>Games</h1>
        <p>
            <asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="Show Games" />
        </p>
        <asp:TextBox ID="TextBox3" runat="server" Height="200px" Width="600px" TextMode="MultiLine" ReadOnly="true"></asp:TextBox>
        <br />
        <br />
    </div>

</asp:Content>
