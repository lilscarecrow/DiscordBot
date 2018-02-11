using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.IO;
using RedditSharp;
using RedditSharp.Things;

namespace DiscordBot
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public AudioService audio { get; set; }
        public ConfigHandler config { get; set; }
        public VoiceService voice { get; set; }
        public DiscordBot client { get; set; }
        public SmiteService smite { get; set; }
        public CommandService CommandService { get; set; }
        public SteamService steam { get; set; }

        private Reddit reddit = new Reddit();
        //private Dictionary<ulong, Dictionary<Stopwatch, long>> timerDict = new Dictionary<ulong, Dictionary<Stopwatch, long>>();

        [Command("numb", RunMode = RunMode.Async)]
        [Summary("By Lincoln Bark")]
        [RequireContext(ContextType.Guild)]
        public async Task Numb()
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            await audio.SendFileAsync(Context, config.GetSongDir() + @"\Pictures\doggo.jpg");
            await audio.SendAsync(audioClient, config.GetSongDir() + @"\numb.mp3");
        }

        [Command("betteroffdoggo", RunMode = RunMode.Async)]
        [Summary("Bark")]
        [RequireContext(ContextType.Guild)]
        public async Task DogAlone()
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            await audio.SendFileAsync(Context, config.GetSongDir() + @"\Pictures\doggo.jpg");
            await audio.SendAsync(audioClient, config.GetSongDir() + @"\dogalone.mp3");
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Get 'em out")]
        [RequireContext(ContextType.Guild)]
        public async Task Stop()
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            audio.SendAsync(audioClient, config.GetSongDir() + @"\stop.mp3");
            await Task.Delay(1000);
            await Context.Guild.AFKChannel.ConnectAsync();
        }

        [Command("roulette", RunMode = RunMode.Async)]
        [Summary("boys will be boys")]
        [RequireContext(ContextType.Guild)]
        public async Task Roulette()
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            SocketGuildUser user = Context.User as SocketGuildUser;
            IVoiceChannel chan = user.VoiceChannel;
            IEnumerable<IGuildUser> boysArr = await chan.GetUsersAsync().FlattenAsync();
            IGuildUser[] boys = boysArr.ToArray();
            Random random = new Random();
            int randomNumber = random.Next(0, boys.Length);
            audio.SendAsync(audioClient, config.GetSongDir() + @"\jaws.mp3");
            await Task.Delay(10000);
            await Context.Channel.SendMessageAsync(boys[randomNumber].Username);
            await boys[randomNumber].ModifyAsync(change =>
            {
                change.Channel = Context.Guild.AFKChannel;
            });
            await Context.Guild.AFKChannel.ConnectAsync();
        }

        [Command("mlg", RunMode = RunMode.Async)]
        [Summary("no scope champion")]
        [RequireContext(ContextType.Guild)]
        public async Task Mlg()
        {
            string[] mlg = Directory.GetFiles(config.GetSongDir() + @"\mlg");
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;          
            Random random = new Random();
            int randomNumber = random.Next(0, mlg.Length);
            await audio.SendAsync(audioClient, mlg[randomNumber]);
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Youtube Player")]
        [RequireContext(ContextType.Guild)]
        public async Task Play(string url)
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            await audio.Stream(audioClient, url.Split('&')[0]);
        }

        [Command("andy", RunMode = RunMode.Async)]
        [Summary("Andy's Status")]
        [RequireContext(ContextType.Guild)]
        public async Task Andy()
        {
            // Create a request for the URL.   
            WebRequest request = WebRequest.Create(config.GetWowKey());
            // Get the response.  
            WebResponse response = await request.GetResponseAsync();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.  
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.  
            string responseFromServer = await reader.ReadToEndAsync();
            DiscordBot.CharacterInfo info = client.GetInfoObject(responseFromServer);
            // Clean up the streams and the response.  
            reader.Close();
            response.Close();
            int total = info.mounts.numNotCollected + info.mounts.numCollected;
            await audio.SendTextAsync(Context, "Andy has collected " + info.mounts.numCollected + "/" + total + " mounts in Azeroth");
        }

        [Command("listen", RunMode = RunMode.Async)]
        [Summary("He's watching")]
        [RequireContext(ContextType.Guild)]
        public async Task Listen()
        {
            var audioClient = await audio.ConnectAudio(Context);
            if (audioClient == null)
                return;
            voice.setContext(Context);
        }

        [Command("ignore", RunMode = RunMode.Async)]
        [Summary("He's watching")]
        [RequireContext(ContextType.Guild)]
        public async Task Ignore()
        {
            voice.stopContext();
        }

        [Command("reddit", RunMode = RunMode.Async)]
        [Summary("Get a cool pic")]
        [RequireContext(ContextType.Guild)]
        public async Task RedditDownload(string sub)
        {
            try
            {
                Subreddit subr = await reddit.GetSubredditAsync(sub);
                Post[] posts = await subr.GetTop(FromTime.Month).Take(10).ToArray<Post>();
                StringBuilder builder = new StringBuilder();
                foreach (Post post in posts)
                {
                    if (!post.NSFW)
                        builder.Append(Convert.ToString(post.Url) + "\n");
                }
                await audio.SendTextAsync(Context, builder.Length == 0 ? "N/A" : builder.ToString());
            }
            catch(Exception ex)
            {
                await audio.SendTextAsync(Context, ex.Message);
            }
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("All the info for our lovely boy")]
        [RequireContext(ContextType.Guild)]
        public async Task Help()
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                List<ModuleInfo> mods = client.GetCommands().Modules.ToList<ModuleInfo>();
                foreach (ModuleInfo mod in mods)
                {
                    foreach (CommandInfo command in mod.Commands)
                    {
                        builder.Append("```!" + command.Name + "\n\tSummary:\n\t\t" + command.Summary + " \n\tAliases: ");
                        foreach (string alias in command.Aliases)
                        {
                            builder.Append("\n\t\t" + alias);
                        }
                        builder.Append("\n\tParameters:");
                        foreach (ParameterInfo param in command.Parameters)
                        {
                            builder.Append("\n\t\t" + param.Name + " : " + param.Type);
                        }
                        builder.Append("```\n");
                        if (builder.Length >= 1800)
                        {
                            await audio.SendTextAsync(Context, builder.Length == 0 ? "N/A" : builder.ToString());
                            builder.Clear();
                        }
                    }
                }
                await audio.SendTextAsync(Context, builder.Length == 0 ? "N/A" : builder.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        [Command("smite", RunMode = RunMode.Async)]
        [Summary("Random Smite God/Build")]
        [RequireContext(ContextType.Guild)]
        public async Task SmiteCommand(string param = "")
        { 
            if (param.ToLower().Equals("random"))
            {
                Gods god = smite.getRandomGod();
                await audio.SendTextAsync(Context, god.Name);
            }
            else if(param.ToLower().Equals("refresh"))
            {
                await smite.CreateSession();
            }
            else
            {
                await audio.SendTextAsync(Context, "Invalid parameter!");
            }
        }

        [Command("stats", RunMode = RunMode.Async)]
        [Summary("Steam Stats")]
        [RequireContext(ContextType.Guild)]
        public async Task SteamStats([Remainder] string gameName)
        {
            var result = await steam.FindGame(gameName);
            if (result != null)
                await audio.SendTextAsync(Context, result);
            else
                await audio.SendTextAsync(Context, "Game Not Found!");
    }

    /*

            [Command("timer", RunMode = RunMode.Async)]
            [Summary("A friendly reminder")]
            [RequireContext(ContextType.Guild)]
            public async Task Timer([Summary("Time in minutes")] int time, [Summary("The message to be displayed")] [Remainder] string message)
            {
                Dictionary<Stopwatch, long> nestDict;
                Stopwatch temp = new Stopwatch();

                if (timerDict.ContainsKey(Context.User.Id))
                {
                    nestDict = timerDict[Context.User.Id];
                    nestDict.Add(temp, time * 60000);
                }
                else
                {
                    nestDict = new Dictionary<Stopwatch, long>();
                    nestDict.Add(temp, time * 60000);
                    timerDict.Add(Context.User.Id, nestDict);
                }

                temp.Start();
                await Task.Delay(time * 60000);
                nestDict.Remove(temp);
                await DependencyMap.Get<AudioService>().SendTextAsync(Context, message);
            }

            [Command("remaining", RunMode = RunMode.Async)]
            [Summary("A friendly remainder")]
            [RequireContext(ContextType.Guild)]
            public async Task Remaining()
            {
                foreach (Stopwatch watch in timerDict[Context.User.Id].Keys)
                {
                    await DependencyMap.Get<AudioService>().SendTextAsync(Context, ("Time remaining: " + (timerDict[Context.User.Id][watch] - watch.ElapsedMilliseconds) / 1000));
                }
            }
    */
}
}
