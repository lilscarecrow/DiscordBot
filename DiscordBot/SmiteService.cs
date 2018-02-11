using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Discord.Commands;

namespace DiscordBot
{
    public class SmiteService
    {
        private ConfigHandler config;
        private string devKey, authKey, timestamp, urlPrefix, signature, session;
        private List<Gods> gods;
        private List<Items> items;
        private Random random;

        public SmiteService(ConfigHandler conf)
        {
            timestamp = "";
            urlPrefix = "http://api.smitegame.com/smiteapi.svc/";
            signature = "";
            session = "";
            gods = null;
            items = null;
            random = new Random();
            config = conf;
        }

        public Gods getRandomGod()
        {
            int r = random.Next(gods.Count);
            return gods[r];
        }

        public async Task<List<Items>> getRandomItem(Gods god)
        {
            //List<Items> temp1 = null;
            List<Items> temp2 = null;
            Console.WriteLine(god.Type + ":" + items[0].RootItemId);
            var item = items.Where(x => x.Type == god.Type).ToList();
            foreach(Items i in item)
            {
                Console.WriteLine(i.DeviceName);
            }
            int r = 0;
            //foreach (Items i in items)
            //{
            //    if (i.Type == god.Type)
            //        temp1.Add(i);
            //}
            Console.WriteLine(item.ToString());
            for (int i = 0; i < 6; i++)
            {
                /*r = random.Next(await item.Count());
                if(temp2.Contains(await item.ElementAt(r)))
                {
                    i--;
                    continue;
                }
                Console.WriteLine("Adding: " + item.ElementAt(r));
                temp2.Add(await item.ElementAt(r));*/
            }

            return temp2;
        }

        public async Task CreateSession()
        {
            devKey = config.GetSmiteDevKey();
            authKey = config.GetSmiteAuthKey();
            timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            signature = GetMD5Hash(devKey + "createsession" + authKey + timestamp);
            WebRequest request = WebRequest.Create(urlPrefix + "createsessionjson/" + devKey + "/" + signature + "/" + timestamp);

            WebResponse response = await request.GetResponseAsync();

            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = await reader.ReadToEndAsync();
            reader.Close();
            response.Close();
            using (var web = new WebClient())
            {
                web.Encoding = Encoding.UTF8;
                string jsonString = responseFromServer;
                JavaScriptSerializer jss = new JavaScriptSerializer();
                SessionInfo g = jss.Deserialize<SessionInfo>(jsonString);
                session = g.session_id;
            }

            gods = await GetGods();
            items = await GetItems();

            Console.WriteLine("Smite Data Retrieved");
        }

        private static string GetMD5Hash(string input)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            bytes = md5.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2").ToLower());
            }
            return sb.ToString();
        }

        private async Task<List<Gods>> GetGods()
        {
            signature = GetMD5Hash(devKey + "getgods" + authKey + timestamp);
            string languageCode = "1";
            WebRequest request = WebRequest.Create(urlPrefix + "getgodsjson/" + devKey + "/" + signature + "/" + session + "/" + timestamp + "/"
            + languageCode);
            WebResponse response = await request.GetResponseAsync();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = await reader.ReadToEndAsync();
            reader.Close();
            response.Close();
            using (var web = new WebClient())
            {
                web.Encoding = Encoding.UTF8;
                string jsonString = responseFromServer;
                JavaScriptSerializer jss = new JavaScriptSerializer();
                List<Gods> godsList = jss.Deserialize<List<Gods>>(jsonString);
                return godsList;
            }
        }

        private async Task<List<Items>> GetItems()
        {
            signature = GetMD5Hash(devKey + "getitems" + authKey + timestamp);
            string languageCode = "1";
            WebRequest request = WebRequest.Create(urlPrefix + "getitemsjson/" + devKey + "/" + signature + "/" + session + "/" + timestamp + "/"
            + languageCode);
            WebResponse response = await request.GetResponseAsync();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = await reader.ReadToEndAsync();
            reader.Close();
            response.Close();
            using (var web = new WebClient())
            {
                web.Encoding = Encoding.UTF8;
                string jsonString = responseFromServer;
                JavaScriptSerializer jss = new JavaScriptSerializer();
                List<Items> itemList = jss.Deserialize<List<Items>>(jsonString);
                return itemList;
            }
        }
    }

    public class SessionInfo
    {
        public string ret_msg {get; set;}
        public string session_id {get; set;}
        public string timestamp {get; set;}
    }

    public class Menuitem
    {
        public string description {get; set;}
        public string value {get; set;}
    }

    public class Rankitem
    {
        public string description {get; set;}
        public string value {get; set;}
    }

    public class AbilityDescription
    {
        public string description {get; set;}
        public string secondaryDescription
        {
            get; set;
        }
        public List<Menuitem> menuitems
        {
            get; set;
        }
        public List<Rankitem> rankitems
        {
            get; set;
        }
        public string cooldown
        {
            get; set;
        }
        public string cost
        {
            get; set;
        }
    }

    public class AbilityRoot
    {
        public AbilityDescription itemDescription{get; set;}
    }

    public class Gods
    {
        public int abilityId1
        {
            get; set;
        }
        public int abilityId2
        {
            get; set;
        }
        public int abilityId3
        {
            get; set;
        }
        public int abilityId4
        {
            get; set;
        }
        public int abilityId5
        {
            get; set;
        }
        public AbilityRoot abilityDescription1
        {
            get; set;
        }
        public AbilityRoot abilityDescription2
        {
            get; set;
        }
        public AbilityRoot abilityDescription3
        {
            get; set;
        }
        public AbilityRoot abilityDescription4
        {
            get; set;
        }
        public AbilityRoot abilityDescription5
        {
            get; set;
        }
        public int id
        {
            get; set;
        }
        public string Pros
        {
            get; set;
        }
        public string Type
        {
            get; set;
        }
        public string Roles
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public string Title
        {
            get; set;
        }
        public string OnFreeRotation
        {
            get; set;
        }
        public string Lore
        {
            get; set;
        }
        public int Health
        {
            get; set;
        }
        public Double HealthPerLevel
        {
            get; set;
        }
        public Double Speed
        {
            get; set;
        }
        public Double HealthPerFive
        {
            get; set;
        }
        public Double HP5PerLevel
        {
            get; set;
        }
        public Double Mana
        {
            get; set;
        }
        public Double ManaPerLevel
        {
            get; set;
        }
        public Double ManaPerFive
        {
            get; set;
        }
        public Double MP5PerLevel
        {
            get; set;
        }
        public Double PhysicalProtection
        {
            get; set;
        }
        public Double PhysicalProtectionPerLevel
        {
            get; set;
        }
        public Double MagicProtection
        {
            get; set;
        }
        public Double MagicProtectionPerLevel
        {
            get; set;
        }
        public Double PhysicalPower
        {
            get; set;
        }
        public Double PhysicalPowerPerLevel
        {
            get; set;
        }
        public Double AttackSpeed
        {
            get; set;
        }
        public Double AttackSpeedPerLevel
        {
            get; set;
        }
        public string Pantheon
        {
            get; set;
        }
        public string Ability1
        {
            get; set;
        }
        public string Ability2
        {
            get; set;
        }
        public string Ability3
        {
            get; set;
        }
        public string Ability4
        {
            get; set;
        }
        public string Ability5
        {
            get; set;
        }
        public string Item1
        {
            get; set;
        }
        public string Item2
        {
            get; set;
        }
        public string Item3
        {
            get; set;
        }
        public string Item4
        {
            get; set;
        }
        public string Item5
        {
            get; set;
        }
        public string Item6
        {
            get; set;
        }
        public string Item7
        {
            get; set;
        }
        public string Item8
        {
            get; set;
        }
        public string Item9
        {
            get; set;
        }
        public int ItemId1
        {
            get; set;
        }
        public int ItemId2
        {
            get; set;
        }
        public int ItemId3
        {
            get; set;
        }
        public int ItemId4
        {
            get; set;
        }
        public int ItemId5 { get; set; }
        public int ItemId6 { get; set; }
        public int ItemId7 { get; set; }
        public int ItemId8 { get; set; }
        public int ItemId9 { get; set; }
        public string ret_msg { get; set; }
    }

    public class ItemDescription
    {
        public string Description { get; set; }
        public List<object> Menuitems { get; set; }
        public string SecondaryDescription { get; set; }
    }

    public class Items
    {
        public int ChildItemId { get; set; }
        public string DeviceName { get; set; }
        public int IconId { get; set; }
        public ItemDescription ItemDescription { get; set; }
        public int ItemId { get; set; }
        public int ItemTier { get; set; }
        public int Price { get; set; }
        public int RootItemId { get; set; }
        public string ShortDesc { get; set; }
        public bool StartingItem { get; set; }
        public string Type { get; set; }
        public string itemIcon_URL { get; set; }
        public object ret_msg { get; set; }
    }
}
