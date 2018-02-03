using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using System.Speech.Recognition;
using System.Collections;

namespace DiscordBot
{
    public class VoiceService
    {
        public IDependencyMap DependencyMap { get; set; }
        public CommandService CommandService { get; set; }

        private SpeechRecognitionEngine recEngine = new SpeechRecognitionEngine();
        private SocketCommandContext Context = null;
        private Dictionary<string, string> songList = null;
        private ArrayList arrList;
        private bool listening = false;

        public VoiceService()
        {
             
        }

        public void init(Dictionary<string, string> songList)
        {
            arrList = new ArrayList();
            arrList.AddRange(DependencyMap.Get<ConfigHandler>().getSpeechArgs());

            foreach (string song in songList.Keys)
            {
                arrList.Add("play the song " + song);
            }

            this.songList = songList;
            Choices commands = new Choices();
            commands.Add((string[]) arrList.ToArray(typeof(string)));

            GrammarBuilder gBuilder = new GrammarBuilder();
            gBuilder.Append(commands);
            Grammar grammar = new Grammar(gBuilder);

            recEngine.LoadGrammarAsync(grammar);

            recEngine.SpeechRecognized += RecEngine_SpeechRecognized;
        }

        public void setContext(SocketCommandContext c)
        {
            Context = c;
            SocketGuildUser s = c.User as SocketGuildUser;
            //recEngine.SetInputToAudioStream(s.AudioStream, new SpeechAudioFormatInfo(1920, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));//OH BOY
            recEngine.SetInputToDefaultAudioDevice();
            recEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void stopContext()
        {
            recEngine.RecognizeAsyncStop();
            Context = null;
            Console.WriteLine("Stopped Listening");
        }
        /*
        public bool inUse()
        {
            if (Context == null)
                return false;
            return true;
        }
        */

        private async Task setActive()
        {
            listening = true;
            await Task.Delay(6000);
            listening = false;
        }

        public async void RecEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if(!listening)
            {
                if (e.Result.Text.Equals(arrList[0]))
                {
                    setActive();
                    await DependencyMap.Get<AudioService>().SendTextAsync(Context, "That's Me!");
                }     
                else
                    return;
            }
            if (e.Result.Text.Contains("play the song"))
            {
                var audioClient = await DependencyMap.Get<AudioService>().ConnectAudio(Context);
                if (audioClient == null)
                    return;
                DependencyMap.Get<AudioService>().SendAsync(audioClient, songList[e.Result.Text.Split(' ').Last()]);
            }
            else if (e.Result.Text.Equals(arrList[1]))
            {
                await DependencyMap.Get<AudioService>().SendTextAsync(Context, "Yeah!");
            }
            else if (e.Result.Text.Equals(arrList[2]))
            {
                await DependencyMap.Get<AudioService>().SendFileAsync(Context, DependencyMap.Get<ConfigHandler>().getSongDir() + @"\Pictures\doggo.jpg");
            }
            else if (e.Result.Text.Equals(arrList[3]))
            {
                await DependencyMap.Get<AudioService>().SendFileAsync(Context, DependencyMap.Get<ConfigHandler>().getSongDir() + @"\Pictures\comm_abby.PNG");
            }
            else if (e.Result.Text.Equals(arrList[4]))
            {
                await DependencyMap.Get<AudioService>().SendFileAsync(Context, DependencyMap.Get<ConfigHandler>().getSongDir() + @"\Pictures\eman.png");
            }
            else if (e.Result.Text.Equals(arrList[5]))
            {
                DependencyMap.Get<DiscordBot>().sendRandom(Context);
            }
            else if (e.Result.Text.Equals(arrList[6]))
            {
                var audioClient = await DependencyMap.Get<AudioService>().ConnectAudio(Context);
                if (audioClient == null)
                    return;
                DependencyMap.Get<AudioService>().SendAsync(audioClient, DependencyMap.Get<ConfigHandler>().getSongDir() + @"\Andy_Ext.ogg");
            }
        }
    }
}
