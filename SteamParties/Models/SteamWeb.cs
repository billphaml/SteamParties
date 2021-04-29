using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SteamParties.Models
{
    public class SteamWeb
    {
        // Steam Api Key
        private string key = "";
        public Rootobject profile;

       /*
        * This method verifies that a user exists using their vanity URL ender
        */
        public bool verifyUserVanity(string VanityUrl)
        {
            //set base address to base url and then the get query
            string[] baseAddress = new string[2];
            baseAddress[0] = "http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/";
            baseAddress[1] = "?key=" + key + "&" + "vanityurl=" + VanityUrl;

            var info = ExponentialBackOffAsync(baseAddress);
            Rootobject result = JsonConvert.DeserializeObject<Rootobject>(info);

            //if the result returns null return false
            if (result == null)
            {
                return false;
            }
            // if the response is successful return true
            if (result.response.success == 1)
            {
                profile = result;
                return true;
            }
            //Otherwise return false
            return false;
        }
       /*
        * Verifies that a user exists using their base 64 steam id, works like the method above
        */
        public bool verifyUserSteamId(string SteamID)
        {
            string[] baseAddress = new string[2];
            baseAddress[0] = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/";
            baseAddress[1] = "?key=" + key + "&" + "steamids=" + SteamID;

            var info = ExponentialBackOffAsync(baseAddress);
            Rootobject result = JsonConvert.DeserializeObject<Rootobject>(info);

            if (result == null)
            {
                return false;
            }

            if (result.response.players.Length != 0)
            {
                profile = result;
                return true;
            }

            return false;
        }

       /*
        * This method compares the party leaders games to the players steam libraries. It then returns all the multiplayer games that everyone is able to play with one another
        */
        public List<string> AddCommonGames(List<string> steamIds)
        {
            if (steamIds.Count < 1) return new List<string>();

            //sets base Address to the query we want to execute
            string[] baseAddress = new string[2];
            baseAddress[0] = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/";
            baseAddress[1] = "?key=" + key + "&" + "steamid=" + steamIds[0];

            var info = ExponentialBackOffAsync(baseAddress);
            Rootobject result = JsonConvert.DeserializeObject<Rootobject>(info);
            if (result.response.game_count < 1) return new List<string>();
            // calls the helper method to filter the list to only multiplayer games
            List<int> multiPlayergames = GetMultiPlayerGames(result.response.games);
            // calls the helper method to filter the list to only games that everyone has
            List<string> commonGames = CompareCommonGames(multiPlayergames, steamIds);

            // returns the list of games and their names
            return commonGames;
        }
        
        /*
         * This method is a helper method that essentially takes the party leaders games and filters it to only multiplayer games.
         * This utilizes the steamstore web api which unfortunately has a 200 calls per 5 minutes rate limit.
         */
        private List<int> GetMultiPlayerGames(Game[] games) 
        {
            //sets the base address to the query we want to execute
            string[] baseAddress = new string[2];
            baseAddress[0] = "https://store.steampowered.com/api/appdetails/";
            
            List<int> multiplayerGames = new List<int>();
            // for each game in the list of games we recieved itterate through its catagories...
            foreach (Game game in games) 
            {
                int gameId = game.appid;
                baseAddress[1] = "?appids=" + gameId + "&filters=categories";

                var info = ExponentialBackOffAsync(baseAddress);
                //the regex is used to replace the appId of the game and change it to result, This allows us to use the json classes.
                Regex rgx = new Regex(@"[0-9]+");
                info = rgx.Replace(info, "Result", 1);
                if (info.Contains("false")) continue;

                Rootobject result = default;
                //  This try catch is to avoid games that a user may have but steam doesnt sell anymore
                try
                {
                    result = JsonConvert.DeserializeObject<Rootobject>(info);
                } 
                catch
                {
                    continue;
                }

                if (result == null) continue;
              
                foreach (Category tag in result.result.data.categories) 
                {
                    if (tag.id == 1) // if the catagory matches the id of Multi-Player add it to the list
                    {
                        multiplayerGames.Add(gameId);
                    }
                }

                baseAddress[1] = "";
            }
            // return the list of the appids
            return multiplayerGames;
        }
       /*
        * This method compares the common games between the party leader and the party members.
        * It updates the list until every game on the list is a game that everyone owns and can play.
        */
        private List<string> CompareCommonGames(List<int> multiPlayerGames, List<string> steamIds) 
        {
            List<int> games = new List<int>();
            // sets the base address to the query we want to execute
            string[] baseAddress = new string[2];
            baseAddress[0] = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v1/";
            // translate the list of appids from that we got from GetMultiplayerGames method to the format for the query to work
            string filter = IdsToFilter(multiPlayerGames);
            for(int i = 1; i < steamIds.Count; i++) // for the party members...
            {
                baseAddress[1] = "?key=" + key + "&" + "steamid=" + steamIds[i] + filter;

                var info = ExponentialBackOffAsync(baseAddress);
                Rootobject result = JsonConvert.DeserializeObject<Rootobject>(info);
                
                games = gamesToList(result); // convert the filtered results into a list of games
                filter = "";
                filter = IdsToFilter(games); // turn that list into a filter
                if (i == steamIds.Count - 1) // if this is the last user break out of the for loop
                {
                    break;
                }
                games.Clear(); // we clear games at the end of each loop
            }

            filter = IdsToFilter(games); // convert the last games list we got into a filter again
            baseAddress[1] = "?key=" + key + "&" + "steamid=" + steamIds[steamIds.Count - 1] + "&include_appinfo=1&include_played_free_games=0" + filter;

            var info2 = ExponentialBackOffAsync(baseAddress);
            Rootobject result2 = JsonConvert.DeserializeObject<Rootobject>(info2); // query the last users games

            List<string> gameNames = new List<string>();
            foreach (Game game in result2.response.games) // for each game in games we get the name of the game and add it to a list
            {
                gameNames.Add(game.name);
            }
            return gameNames; // return the list of game names
        }
       /*
        * This method converts a list of games into the filter format for the CompareCommonGames method
        */
        private string IdsToFilter(List<int> Games) 
        {
            string filter = "";
            int i = 0;
            foreach (int game in Games) 
            {
                filter += "&appids_filter[" + i + "]" + "=" + game;
                i++;
            }
            return filter;
        }
       /*
        * This method converts the response of a games list to a list that we can itterate through. This is yet another helper method for CompareCommonGames.
        */
        private List<int> gamesToList(Rootobject results) 
        {
            List<int> games = new List<int>();

            // Steam API limit reached
            if (results.response.game_count == 0) return games;

            foreach (Game game in results.response.games)
            {
                games.Add(game.appid);
            }

            return games;
        }
        /*
         * Reuse of my ExponentialBackoffAsync method 
         */
        static string ExponentialBackOffAsync(string[] baseAddress)
        {
            //initilizing the variables needed for this loop
            string result = "";
            bool retry = false;
            int retries = 0;
            int MAX_RETRIES = 5;

            using (var client = new HttpClient())
            {
                // set base address outside of loop to avoid errors
                client.BaseAddress = new Uri(baseAddress[0]);

                do
                {
                    int waitTime = Math.Min(getWaitTimeExp(retries), 3000); //calculate the exponential wait time based on the number of retries

                    try
                    {

                        //wait for previosly calculated time
                        Task.Delay(TimeSpan.FromMilliseconds(waitTime)).Wait();

                        //make and await the response
                        HttpResponseMessage response = client.GetAsync(baseAddress[1]).Result;
                        HttpStatusCode code = response.StatusCode;
                        //code = HttpStatusCode.BadGateway;

                        //Based on the status code I will either retry or set the result to the json that has been passed. 
                        //If its not a 5xx error or valid I dont retry and just exit
                        if ((int)code >= 200 && (int)code < 300)
                        {
                            retry = false;
                            result = response.Content.ReadAsStringAsync().Result;
                        }
                        else if ((int)code >= 500 && (int)code < 600)
                        {
                            Console.WriteLine("Retrying");
                            Console.WriteLine(System.DateTime.Now);
                            retry = true;
                        }
                        else
                        {
                            retry = false;
                            Console.WriteLine("API unable to be called");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error " + e.Message);
                    }
                } while (retry && (retries++ < MAX_RETRIES)); //the conditions for my while loop to loop
            }
            return result;
        }
       /*
        * helper method for ExponentialBackoffAsync. 
        * This calculates the wait time before going through with another call.
        */
        public static int getWaitTimeExp(int retryCount)
        {
            if (0 == retryCount)
            {
                return 0;
            }

            int waitTime = ((int)Math.Pow(2, retryCount) * 100);

            return waitTime;
        }

    }

    #region SteamWebApi
    public class Rootobject
    {
        public Response response { get; set; }

        public Result result { get; set; }
    }

    public class Response
    {
        public Player[] players { get; set; }
        public string steamid { get; set; }
        public int success { get; set; }
        public int game_count { get; set; }
        public Game[] games { get; set; }
    }

    public class Game
    {
        public int appid { get; set; }
        public string name { get; set; }
        public int playtime_forever { get; set; }
        public string img_icon_url { get; set; }
        public string img_logo_url { get; set; }
        public int playtime_windows_forever { get; set; }
        public int playtime_mac_forever { get; set; }
        public int playtime_linux_forever { get; set; }
        public bool has_community_visible_stats { get; set; }
        public int playtime_2weeks { get; set; }
    }

    public class Player
    {
        public string steamid { get; set; }
        public int communityvisibilitystate { get; set; }
        public int profilestate { get; set; }
        public string personaname { get; set; }
        public string profileurl { get; set; }
        public string avatar { get; set; }
        public string avatarmedium { get; set; }
        public string avatarfull { get; set; }
        public string avatarhash { get; set; }
        public int personastate { get; set; }
        public string realname { get; set; }
        public string primaryclanid { get; set; }
        public int timecreated { get; set; }
        public int personastateflags { get; set; }
        public string loccountrycode { get; set; }
        public string locstatecode { get; set; }
        public int loccityid { get; set; }
    }
    #endregion

    #region SteamStoreApi
    //public class Rootobject2
    //{
    //    public Result result { get; set; }
    //}

    public class Result
    {
        public bool success { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public Category[] categories { get; set; }
    }

    public class Category
    {
        public int id { get; set; }
        public string description { get; set; }
    }

    #endregion
}