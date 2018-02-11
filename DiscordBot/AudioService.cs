using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using Discord.Audio;

namespace DiscordBot
{
    public class AudioService
    {
        public ConfigHandler config;
        //public CommandService CommandService { get; set; }

        public AudioService(ConfigHandler conf)
        {
            config = conf;
        }

        public async Task<IAudioClient> ConnectAudio(SocketCommandContext context)
        {
            SocketGuildUser user = context.User as SocketGuildUser;
            IVoiceChannel channel = user.VoiceChannel;
            if (channel == null)
            {  
                await context.Message.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
                return null;
            }
            return await channel.ConnectAsync();
        }

        public async Task<IAudioClient> ConnectAudioRandom(IVoiceChannel channel)//Used for main guild only!
        {
            return await channel.ConnectAsync();
        }

        public async Task<IAudioClient> ConnectAudioDestination(SocketCommandContext context, SocketVoiceChannel chan)
        {
            if (chan == null)
            {
                await context.Message.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
                return null;
            }
            return await chan.ConnectAsync();
        }

        public async Task SendAsync(IAudioClient client, string path)
        {
            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed, 96000);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
            //if (DependencyMap.Get<VoiceService>().inUse()) BROKEN??????
            //    DependencyMap.Get<VoiceService>().stopContext();
        }

        public async Task SendTextAsync(SocketCommandContext context, string text)
        {
            await context.Channel.SendMessageAsync(text);
        }

        public async Task SendTextWithoutContextAsync(ISocketMessageChannel channel, string text)
        {
            await channel.SendMessageAsync(text);
        }

        public async Task SendFileAsync(SocketCommandContext context, string path)
        {
            await context.Channel.SendFileAsync(path);
        }

        public async Task Stream(IAudioClient client, string url)
        {
            var ffmpeg = CreateYoutubeStream(url);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed, 96000);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
            //if (DependencyMap.Get<VoiceService>().inUse()) BROKEN????????
            //    DependencyMap.Get<VoiceService>().stopContext();
        }

        public async Task StreamRadio(IAudioClient client, string url)
        {
            /*
            WebResponse res = await WebRequest.Create(@"http://uk5.internet-radio.com:8278/live").GetResponseAsync();
            Console.WriteLine(res.ContentLength);
            Stream web = res.GetResponseStream();
            var ffmpeg = CreateRadioStream();
            var input = ffmpeg.StandardInput.BaseStream;
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed, 1920);

            web.CopyTo(input);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
            if (DependencyMap.Get<VoiceService>().inUse())
                DependencyMap.Get<VoiceService>().stopContext();
            */
        }

        private Process CreateStream(string path)
        {
            ProcessStartInfo ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Process.Start(ffmpeg);
        }

        private Process CreateYoutubeStream(string url)
        {
            ProcessStartInfo ffmpeg = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $@"/C C:\YT\youtube-dl.exe --no-check-certificate -f bestaudio -o - {url} | ffmpeg -i pipe:0 -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Process.Start(ffmpeg);
        }
    }
}
