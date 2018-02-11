using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System.IO;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Security.AccessControl;
using System.Reflection;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    public class DiscordBot
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;
        private WebClient webClient = new WebClient();
        private ModuleInfo customModule;
        private Dictionary<string, string> songList = new Dictionary<string, string>();
        private Timer timer;
        private string line;
        private Random random;
        private List<SocketGuild> guilds;

        struct MP3
        {
            public string command;
            public string name;
        }

        public struct Collected
        {
            public string name { get; set; }
            public int spellId { get; set; }
            public int creatureId { get; set; }
            public int itemId { get; set; }
            public int qualityId { get; set; }
            public string icon { get; set; }
            public bool isGround { get; set; }
            public bool isFlying { get; set; }
            public bool isAquatic { get; set; }
            public bool isJumping { get; set; }
        }

        public struct Mounts
        {
            public int numCollected { get; set; }
            public int numNotCollected { get; set; }
            public List<Collected> collected { get; set; }
        }

        public struct CharacterInfo
        {
            public long lastModified { get; set; }
            public string name { get; set; }
            public string realm { get; set; }
            public string battlegroup { get; set; }
            public int @class { get; set; }
            public int race { get; set; }
            public int gender { get; set; }
            public int level { get; set; }
            public int achievementPoints { get; set; }
            public string thumbnail { get; set; }
            public string calcClass { get; set; }
            public int faction { get; set; }
            public Mounts mounts { get; set; }
            public int totalHonorableKills { get; set; }
        }

        public static void Main(string[] args)
            => new DiscordBot().MainAsync().GetAwaiter().GetResult();

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            client.Ready += ReadyCheck;
            client.MessageReceived += HandleCommand;
            client.Log += Log;
            client.GuildMemberUpdated += HandleUpdate;
            commands = new CommandService();
            random = new Random();
            timer = new Timer(GetRandomSeed());
            timer.Elapsed += TimerTick;
            timer.Start();

            services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ConfigHandler>()
                .AddSingleton<AudioService>()
                .AddSingleton<VoiceService>()
                .AddSingleton<SmiteService>()
                .AddSingleton<SteamService>()
                .BuildServiceProvider();

            await services.GetService<ConfigHandler>().PopulateConfig();

            await GenerateCommands();

            await AddCommands();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            await services.GetService<VoiceService>().init(songList);//Used to load custom commands into speech
            
            await services.GetService<SmiteService>().CreateSession();

            await services.GetService<SteamService>().GetData();

            await client.LoginAsync(TokenType.Bot, services.GetService<ConfigHandler>().GetToken());
            await client.StartAsync();
            await Task.Delay(-1);
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            var context = new SocketCommandContext(client, message);
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }      
        }

        private async Task AddCommands()
        {
            customModule = await commands.CreateModuleAsync("", module =>
            {
                foreach(string entry in songList.Keys)
                {
                    module.AddCommand(entry, SendAuto, cmd =>
                    {
                        cmd.RunMode = RunMode.Async;
                    });
                }

                module.AddCommand("add", AddSong, cmd =>
                {
                    cmd.AddParameter<string>("command", para =>
                    {
                        para.IsOptional = false;
                    });
                });
                module.Name = "Custom Module";
            });
            Console.WriteLine("DONE LOADING CUSTOM COMMANDS");
        }

        private async Task SendAuto(ICommandContext context, object[] parameters, IServiceProvider service, CommandInfo info)
        {
            SocketCommandContext Context = context as SocketCommandContext;
            var audio = await service.GetService<AudioService>().ConnectAudio(Context);
            await service.GetService<AudioService>().SendAsync(audio, songList[Context.Message.ToString().Substring(1)]);
        }

        private async Task ReadyCheck()
        {
            guilds = client.Guilds.ToList();
            foreach (SocketGuild serv in client.Guilds)
            {
                //await serv.DefaultChannel.SendMessageAsync("Found " + songList.Count + " custom songs!");
            }
        }

        private async Task HandleUpdate(SocketGuildUser before, SocketGuildUser after)
        {
            Console.WriteLine("USER: " + after.Username + " ACTIVITY: " + after.Activity.Name);
            var result = await services.GetService<SteamService>().FindGame(after.Activity.Name.ToLower().Trim());
            if (result != null)
            {
                await services.GetService<AudioService>().SendTextWithoutContextAsync(after.Guild.DefaultChannel, result);
            }    
        }

        private async void TimerTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                timer.Interval = GetRandomSeed();
                List<SocketVoiceChannel> channels = guilds[0].VoiceChannels.ToList();
                IAudioClient audioClient = await services.GetService<AudioService>().ConnectAudioRandom(channels[random.Next(channels.Count)]);
                await services.GetService<AudioService>().SendAsync(audioClient, services.GetService<ConfigHandler>().GetSongDir() + @"\john.mp3");
                timer.Start();
            }
            catch(TimeoutException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task SendRandom(SocketCommandContext context)
        {
            SocketCommandContext Context = context as SocketCommandContext;
            var audio = await services.GetService<AudioService>().ConnectAudio(Context);
            Random rand = new Random();
            await services.GetService<AudioService>().SendAsync(audio, songList.ElementAt(rand.Next(0, songList.Count)).Value);
        }

        public CommandService GetCommands()
        {
            return commands;
        }

        public CharacterInfo GetInfoObject(string rawResponse)
        {
            return JsonConvert.DeserializeObject<CharacterInfo>(rawResponse);
        }

        private async Task AddSong(ICommandContext context, object[] parameters, IServiceProvider services, CommandInfo info)
        {
            if (context.Message.Attachments.Count != 1)
            {
                await context.Channel.SendMessageAsync("Number of files incorrect");
                return;
            }

            IAttachment att = context.Message.Attachments.First();
            if (att.Filename.EndsWith(".mp3"))
            {
                string filePath = Path.Combine(services.GetService<ConfigHandler>().GetSongDir(), att.Filename).Replace(@"\", @"\\");
                webClient.DownloadFile(att.Url, filePath);
                MP3 mp3 = new MP3
                {
                    command = (string)parameters[0],
                    name = filePath
                };
                try
                {
                    songList.Add(mp3.command, mp3.name);
                    using (StreamWriter sw = File.AppendText(services.GetService<ConfigHandler>().GetSongConf()))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(mp3));
                    }
                    await commands.RemoveModuleAsync(customModule);
                    await AddCommands();
                }
                catch (ArgumentException ex)
                {
                    await context.Channel.SendMessageAsync(ex.Message);
                    return;
                }
            }
        }

        private async Task GenerateCommands()
        {
            MP3 song;

            if (!File.Exists(services.GetService<ConfigHandler>().GetSongConf()))
            {
                using (var f = File.Create(services.GetService<ConfigHandler>().GetSongConf()))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(services.GetService<ConfigHandler>().GetSongConf());
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                }
            }

            using (StreamReader reader = new StreamReader(services.GetService<ConfigHandler>().GetSongConf()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    song = JsonConvert.DeserializeObject<MP3>(line);
                    songList.Add(song.command, song.name);
                }
            }         
            await Task.CompletedTask;
        }

        private double GetRandomSeed()
        {
            Console.WriteLine("RANDOM TIMER TICKING!");
            return 180000;
        }

        private async void MysqlConnect()
        {
            MySqlConnection conn = null;
            MySqlDataReader rdr = null;
            string myConnectionString = "server=localhost;uid=root;pwd=root;database=boys;";
            try
            {
                conn = new MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                string stm = "SELECT * FROM test";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    foreach (SocketGuild serv in client.Guilds)
                    {
                        await serv.DefaultChannel.SendMessageAsync("Boy Name = " + rdr.GetString(0) + "\nGame = " + rdr.GetString(1) + "\nPlayers = " + rdr.GetInt32(2));
                    }
                }
                foreach (SocketGuild serv in client.Guilds)
                {
                    await serv.DefaultChannel.SendMessageAsync("Connected to Boy Database version : " + conn.ServerVersion);
                }
            }
            catch (MySqlException ex)
            {
                foreach (SocketGuild serv in client.Guilds)
                {
                    await serv.DefaultChannel.SendMessageAsync(ex.Message);
                }
            }
            finally
            {
                if (rdr != null)
                {
                    rdr.Close();
                }
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }
    }
}
