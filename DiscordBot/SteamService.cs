using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DiscordBot
{
    public class SteamService
    {
        private SteamInfo steamInf;

        public SteamService()
        {

        }

        public async Task GetData()
        {
            WebRequest request = WebRequest.Create(@"https://api.steampowered.com/ISteamApps/GetAppList/v2/");

            WebResponse response = await request.GetResponseAsync();

            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = await reader.ReadToEndAsync();
            reader.Close();
            response.Close();

            steamInf = JsonConvert.DeserializeObject<SteamInfo>(responseFromServer);
        }

        public async Task<string> FindGame(string gameName)
        {
            if (steamInf == null)
                await GetData();//sanity check on data retrieval 
            var game = steamInf.applist.apps.Where(x => x.name.ToLower().Trim().Equals(gameName)).FirstOrDefault();

            if (game == null)
                return null;
            return await GetPlayerCount(gameName, game.appid);
        }

        private async Task<string> GetPlayerCount(string name, int appId)
        {
            WebRequest request = WebRequest.Create(@"http://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v0001/?appid=" + appId);

            WebResponse response = await request.GetResponseAsync();

            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = await reader.ReadToEndAsync();
            reader.Close();
            response.Close();
            var gameInf = JsonConvert.DeserializeObject<GameInfo>(responseFromServer);

            return "There are currently " + gameInf.response.player_count + " players on " + name;
        }

        public class App
        {
            public int appid { get; set; }
            public string name { get; set; }
        }

        public class Applist
        {
            public List<App> apps { get; set; }
        }

        public class SteamInfo
        {
            public Applist applist { get; set; }
        }

        public class Response
        {
            public int player_count { get; set; }
            public int result { get; set; }
        }

        public class GameInfo
        {
            public Response response { get; set; }
        }
    }
}
