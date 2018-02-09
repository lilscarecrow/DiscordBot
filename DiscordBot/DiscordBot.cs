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
            client.Ready += readyCheck;
            client.MessageReceived += HandleCommand;
            client.Log += Log;
            commands = new CommandService();
            random = new Random();
            timer = new Timer(getRandomSeed());
            timer.Elapsed += timerTick;
            timer.Start();

            services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ConfigHandler>()
                .AddSingleton<AudioService>()
                .AddSingleton<VoiceService>()
                .AddSingleton<Smite>()
                .BuildServiceProvider();

            await services.GetService<ConfigHandler>().populateConfig();

            await generateCommands();

            await addCommands();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            await services.GetService<VoiceService>().init(songList);//Used to load custom commands into speech
            
            await services.GetService<Smite>().CreateSession();

            await client.LoginAsync(TokenType.Bot, services.GetService<ConfigHandler>().getToken());
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

        private async Task addCommands()
        {
            customModule = await commands.CreateModuleAsync("", module =>
            {
                foreach(string entry in songList.Keys)
                {
                    module.AddCommand(entry, sendAuto, cmd =>
                    {
                        cmd.RunMode = RunMode.Async;
                    });
                }

                module.AddCommand("add", addSong, cmd =>
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

        private async Task sendAuto(ICommandContext context, object[] parameters, IServiceProvider service, CommandInfo info)
        {
            SocketCommandContext Context = context as SocketCommandContext;
            var audio = await service.GetService<AudioService>().ConnectAudio(Context);
            await service.GetService<AudioService>().SendAsync(audio, songList[Context.Message.ToString().Substring(1)]);
        }

        private async Task readyCheck()
        {
            guilds = client.Guilds.ToList();
            foreach (SocketGuild serv in client.Guilds)
            {
                //await serv.DefaultChannel.SendMessageAsync("Found " + songList.Count + " custom songs!");
            }
        }

        private async void timerTick(object sender, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop();
                timer.Interval = getRandomSeed();
                List<SocketVoiceChannel> channels = guilds[0].VoiceChannels.ToList();
                IAudioClient audioClient = await services.GetService<AudioService>().ConnectAudioRandom(channels[random.Next(channels.Count)]);
                await services.GetService<AudioService>().SendAsync(audioClient, services.GetService<ConfigHandler>().getSongDir() + @"\john.mp3");
                timer.Start();
            }
            catch(TimeoutException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task sendRandom(SocketCommandContext context)
        {
            SocketCommandContext Context = context as SocketCommandContext;
            var audio = await services.GetService<AudioService>().ConnectAudio(Context);
            Random rand = new Random();
            await services.GetService<AudioService>().SendAsync(audio, songList.ElementAt(rand.Next(0, songList.Count)).Value);
        }

        public CommandService getCommands()
        {
            return commands;
        }

        public CharacterInfo getInfoObject(string rawResponse)
        {
            return JsonConvert.DeserializeObject<CharacterInfo>(rawResponse);
        }

        private async Task addSong(ICommandContext context, object[] parameters, IServiceProvider services, CommandInfo info)
        {
            if (context.Message.Attachments.Count != 1)
            {
                await context.Channel.SendMessageAsync("Number of files incorrect");
                return;
            }

            IAttachment att = context.Message.Attachments.First();
            if (att.Filename.EndsWith(".mp3"))
            {
                string filePath = Path.Combine(services.GetService<ConfigHandler>().getSongDir(), att.Filename).Replace(@"\", @"\\");
                webClient.DownloadFile(att.Url, filePath);
                MP3 mp3 = new MP3
                {
                    command = (string)parameters[0],
                    name = filePath
                };
                try
                {
                    songList.Add(mp3.command, mp3.name);
                    using (StreamWriter sw = File.AppendText(services.GetService<ConfigHandler>().getSongConf()))
                    {
                        sw.WriteLine(JsonConvert.SerializeObject(mp3));
                    }
                    await commands.RemoveModuleAsync(customModule);
                    await addCommands();
                }
                catch (ArgumentException ex)
                {
                    await context.Channel.SendMessageAsync(ex.Message);
                    return;
                }
            }
        }

        private async Task generateCommands()
        {
            MP3 song;

            if (!File.Exists(services.GetService<ConfigHandler>().getSongConf()))
            {
                using (var f = File.Create(services.GetService<ConfigHandler>().getSongConf()))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(services.GetService<ConfigHandler>().getSongConf());
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                }
            }

            using (StreamReader reader = new StreamReader(services.GetService<ConfigHandler>().getSongConf()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    song = JsonConvert.DeserializeObject<MP3>(line);
                    songList.Add(song.command, song.name);
                }
            }         
            await Task.CompletedTask;
        }

        private double getRandomSeed()
        {
            Console.WriteLine("RANDOM TIMER TICKING!");
            return 180000;
        }

        private async void mysqlConnect()
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
