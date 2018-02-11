using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class ConfigHandler
    {
        private Config conf;
        private string configPath, line;

        struct Config
        {
            public string token;
            public string songDir;
            public string songConf;
            public string wowKey;
            public string smiteDevKey;
            public string smiteAuthKey;
            public string[] speechArgs;
        }

        public ConfigHandler()
        {
            conf = new Config()
            {
                token = "",
                songDir = "",
                songConf = "",
                wowKey = "",
                smiteDevKey = "",
                smiteAuthKey = "",
                speechArgs = new string[1]
            };
        }

        public async Task PopulateConfig()
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json").Replace(@"\", @"\\");
            Console.WriteLine(configPath);

            if (!File.Exists(configPath))//Create the new config file to be filled out
            {
                using (var f = File.Create(configPath))
                {
                    DirectoryInfo dInfo = new DirectoryInfo(configPath);
                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                }
                using (StreamWriter sw = File.AppendText(configPath))
                {
                    sw.WriteLine(JsonConvert.SerializeObject(conf));
                }
                Console.WriteLine("WARNING! New Config initialized! Need to fill in values before running commands!");
                throw new Exception("NO CONFIG AVAILABLE! Go to executable path and fill out newly created file!");
            }

            using (StreamReader reader = new StreamReader(configPath))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    conf = JsonConvert.DeserializeObject<Config>(line);
                }
            }

            await Task.CompletedTask;
        }

        public string GetToken()
        {
            return conf.token;
        }

        public string GetSongDir()
        {
            return conf.songDir;
        }

        public string GetSongConf()
        {
            return conf.songConf;
        }

        public string GetWowKey()
        {
            return conf.wowKey;
        }

        public string GetSmiteDevKey()
        {
            return conf.smiteDevKey;
        }

        public string GetSmiteAuthKey()
        {
            return conf.smiteAuthKey;
        }

        public string[] GetSpeechArgs()
        {
            return conf.speechArgs;
        }
    }
}
