using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using System.Speech.Recognition;
using System.Collections;
using System.Speech.AudioFormat;
using Discord.Audio;
using System.IO;

namespace DiscordBot
{
    public class VoiceService
    {
        public ConfigHandler config;
        public AudioService audio;
        public DiscordBot client;

        private SpeechRecognitionEngine recEngine = new SpeechRecognitionEngine();
        private SocketCommandContext Context = null;
        private Dictionary<string, string> songList = null;
        private ArrayList arrList;
        private bool listening = false;

        public VoiceService(ConfigHandler conf, AudioService aud, DiscordBot dis)
        {
            config = conf;
            audio = aud;
            client = dis;
        }

        public async Task init(Dictionary<string, string> songList)
        {
            arrList = new ArrayList();
            arrList.AddRange(config.GetSpeechArgs());

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

            await Task.CompletedTask;
        }

        public void setContext(SocketCommandContext c)
        {
            Context = c;
            SocketGuildUser s = c.User as SocketGuildUser;
            try
            {
                var format = new SpeechAudioFormatInfo(1920, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);
                var stream = s.AudioStream;
                //recEngine.SetInputToAudioStream(stream, format);//OH BOY
            }
            catch (Exception e)
            {
                Console.WriteLine(s.Username + " AND THE MESSAGE IS: " + e.Message);
            }
            
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
                    await audio.SendTextAsync(Context, "That's Me!");
                }     
                else
                    return;
            }
            if (e.Result.Text.Contains("play the song"))
            {
                var audioClient = await audio.ConnectAudio(Context);
                if (audioClient == null)
                    return;
                audio.SendAsync(audioClient, songList[e.Result.Text.Split(' ').Last()]);
            }
            else if (e.Result.Text.Equals(arrList[1]))
            {
                await audio.SendTextAsync(Context, "Yeah!");
            }
            else if (e.Result.Text.Equals(arrList[2]))
            {
                await audio.SendFileAsync(Context, config.GetSongDir() + @"\Pictures\doggo.jpg");
            }
            else if (e.Result.Text.Equals(arrList[3]))
            {
                await audio.SendFileAsync(Context, config.GetSongDir() + @"\Pictures\comm_abby.PNG");
            }
            else if (e.Result.Text.Equals(arrList[4]))
            {
                await audio.SendFileAsync(Context, config.GetSongDir() + @"\Pictures\eman.png");
            }
            else if (e.Result.Text.Equals(arrList[5]))
            {
                client.SendRandom(Context);
            }
            else if (e.Result.Text.Equals(arrList[6]))
            {
                var audioClient = await audio.ConnectAudio(Context);
                if (audioClient == null)
                    return;
                audio.SendAsync(audioClient, config.GetSongDir() + @"\Andy_Ext.ogg");
            }
        }
    }
}
