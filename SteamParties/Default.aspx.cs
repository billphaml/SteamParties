/******************************************************************************
 * Code behind file for default page. Implements the ability to find common
 * games between steam users.
 * 
 * Author: Bill Pham
 *****************************************************************************/

using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Web.UI;
using SteamParties.Models;

namespace SteamParties
{
    public partial class _Default : Page
    {
        /// <summary>
        /// Local copy of each party member's steam id.
        /// </summary>
        public string partyMemberIDs = "";

        /// <summary>
        /// Blob connection string.
        /// </summary>
        const string BLOB_KEY = "";
        
        /// <summary>
        /// Blob container name.
        /// </summary>
        const string BLOB_USERS_NAME = "users";

        /// <summary>
        /// Gets called when the page loads. Attempts to retrieve saved party
        /// members from azure storage if user is signed in with a google
        /// account. Will then load common games afterwards.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void Page_Load(object sender, EventArgs e)
        {
            // Check if user is signed in
            if (Context.User.Identity.GetUserId() != null)
            {
                try
                {
                    // Grab party member steamIDs from azure storage
                    string steamIDs =
                        await BlobStorage.GetUserData(Context.User.Identity.GetUserId());
                    // Save browser copy
                    Session["_IDs"] = steamIDs;
                    // Save object copy
                    partyMemberIDs = steamIDs;
                    // If no members were loaded return
                    if (partyMemberIDs == "") return;

                    // For each steam id display usernames in textbox 2 using
                    // steam ids
                    TextBox2.Text = "";
                    SteamWeb sw = new SteamWeb();
                    string[] ids = partyMemberIDs.Split('\n');
                    foreach (string steamid in ids)
                    {
                        sw.verifyUserSteamId(steamid);
                        if (!TextBox2.Text.Contains(steamid)) {
                            TextBox2.Text += sw.profile.response.players[0].personaname + "\n";
                        }   
                    }

                    // Display games in textbox 3 after users are loaded
                    System.EventArgs emptyEvent = new System.EventArgs();
                    Button3_Click(this, emptyEvent);
                }
                catch (Exception exception)
                {
                }
            }
        }

        /// <summary>
        /// Gets called when user presses add button. Grabs the input from
        /// textbox one and verifies it. If valid, adds it to azure storage
        /// and displays the steam user's name in textbox two.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Button1_Click(object sender, EventArgs e)
        {
            // Grab input from textbox
            string inputID = TextBox1.Text;

            // Updating object copy of steam ids
            partyMemberIDs = Convert.ToString(Session["_IDs"]);

            // Verify id with steam and convert to steam id if vanity id
            // Return if id is invalid
            SteamWeb sw = new SteamWeb();
            if (sw.verifyUserSteamId(inputID))
            {
                // Duplicate check
                if (partyMemberIDs.Contains(inputID)) return;
            }
            else if (sw.verifyUserVanity(inputID))
            {
                // Convert to steam id
                sw.verifyUserSteamId(sw.profile.response.steamid);
                inputID = sw.profile.response.players[0].steamid;
                // Duplicate check
                if (partyMemberIDs.Contains(inputID)) return;
            } 
            else
            {
                return;
            }

            // Display username in textbox two
            TextBox2.Text += sw.profile.response.players[0].personaname + "\n";

            // Save steam id to storage, object, and browser
            partyMemberIDs += inputID + "\n";
            Session["_IDs"] = partyMemberIDs;
            if (Context.User.Identity.GetUserId() != null)
            {
                BlobStorage.SetUserData(Context.User.Identity.GetUserId(), partyMemberIDs);
            }
        }

        /// <summary>
        /// Gets called when user press clear button. Clears storage, object,
        /// browser, and textboxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected async void Button2_Click(object sender, EventArgs e)
        {
            // Deletes steam ids in storage
            if (Context.User.Identity.GetUserId() != null)
            {
                await BlobStorage.ClearUserBlobAsync(Context.User.Identity.GetUserId());
            }
            // Clear local copies
            partyMemberIDs = "";
            Session["_IDs"] = "";
            // Clear display
            TextBox2.Text = "";
            TextBox3.Text = "";
        }

        // Show games
        /// <summary>
        /// Gets called when user press Show Games button. Grabs all the steam
        /// ids stored in locally and determines what games the users have in
        /// common, displaying the games in textbox three.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Button3_Click(object sender, EventArgs e)
        {
            // Clear textbox
            TextBox3.Text = "";
            // Restore ids
            partyMemberIDs = Convert.ToString(Session["_IDs"]);
            // Split each id by new line
            List<string> ids = new List<string>(partyMemberIDs.Split('\n'));
            ids.RemoveAt(ids.Count - 1);
            // Determine games
            SteamWeb sw = new SteamWeb();
            List<string> games = sw.AddCommonGames(ids);
            if (games.Count == 0)
            {
                TextBox3.Text = "Steam api limit reached";
            }
            // Print games
            foreach (string game in games)
            {
                TextBox3.Text += game + "\n";
            }
        }
    }
}
