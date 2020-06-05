using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Events;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Api.V5.Models.Streams;
using Newtonsoft.Json;
using TwitchLib.PubSub.Models.Responses;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace DiscordBot
{
    public class MyCommands
    {

        public TwitchAPI api;

        public static class Global
        {
            public static bool token=false;
        }
     
        

        public struct twitchconfig
        {
            [JsonProperty("clientid")]
            public string clientid { get; set; }

            [JsonProperty("accesstoken")]
            public string accesstoken { get; set; }
        }

        [Command("csücsü"), Description("Köszön neked a bot."),Aliases("Csücsü","CsüCsü")]
        public async Task Greet(CommandContext ctx)
        {
            //trigger typing indicator
            await ctx.TriggerTypingAsync();

            //add emoji to the message
            var emoji = DiscordEmoji.FromName(ctx.Client, ":wave:");

            //respond the user
            await ctx.RespondAsync($"{emoji} Csücsü {ctx.User.Username}!");
        }

        [Command("random"), Description("Választ egy random számot a megadott két számon belül."), Aliases("Random")]
        public async Task Random(CommandContext ctx, [Description("A kisebbik szám.")] int min, [Description("A nagyobbik szám.")] int max)
        {
            var rnd = new Random();

            //trigger typing indicator
            await ctx.TriggerTypingAsync();

            //respond the number to the user
            await ctx.RespondAsync($"A random szám: {rnd.Next(min, max)}");
        }

        [Command("ping"), Description("Ping Pongozik veled a bot."), Aliases("Ping","pong","Pong")]
        public async Task Ping(CommandContext ctx)
        {
            //trigger typing indicator
            await ctx.TriggerTypingAsync();

            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");

            //respond to the user with ping
            await ctx.RespondAsync($"{emoji} Pong! Ping: {ctx.Client.Ping}ms");
        }

      

        [Command("stream"), Description("Megmondja, hogy a megadott streamer live-e."), Aliases("Stream")]
        public async Task Stream(CommandContext ctx, [Description("A streamer neve.")] string name)
        {
            //open the Twitch config file
            var tjson = "";
            using (var fs = File.OpenRead("twitchconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                tjson = await sr.ReadToEndAsync();
            var tcfgjson = JsonConvert.DeserializeObject<twitchconfig>(tjson);

            //set the twitch related configs
            api = new TwitchAPI();
            api.Settings.ClientId = tcfgjson.clientid;
            api.Settings.AccessToken = tcfgjson.accesstoken;

            var emoji1 = DiscordEmoji.FromName(ctx.Client, ":movie_camera:");
            var emoji2 = DiscordEmoji.FromName(ctx.Client, ":cry:");

            //check the given name is valid on twitch side or not
            User[] userList = api.V5.Users.GetUserByNameAsync(name).Result.Matches;
            string userid = null;
            try
            {
                userid = userList[0].Id;
            }
            catch (System.IndexOutOfRangeException)
            {
                var emoji3 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji3} Nincs ilyen Twitch user. Biztos jól adtad meg a nevét?");
            }

            //check the user is live or not
            var stream = await api.V5.Streams.GetStreamByUserAsync(userid);
            if (stream?.Stream?.Channel?.Status != null)
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji1} {userList[0].DisplayName} live! Ha nézni szeretnéd, kattints a linkre: {stream.Stream.Channel.Url}");
            }
            else
            {
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji2} Sajnos most {userList[0].DisplayName} nem live!");
            }


        }

        [Command("add_streamer"), Description("Felvesz a listára ami alapján küld értesítést ha live vagy."), Aliases("Add_streamer", "Add_Streamer")]
        public async Task AddStreamer(CommandContext ctx, [Description("Twitch felhasználónév")] string name)
        {

            //open the Twitch config file
            var tjson = "";
            using (var fs = File.OpenRead("twitchconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                tjson = await sr.ReadToEndAsync();
            var tcfgjson = JsonConvert.DeserializeObject<twitchconfig>(tjson);

            //set the twitch related configs
            api = new TwitchAPI();
            api.Settings.ClientId = tcfgjson.clientid;
            api.Settings.AccessToken = tcfgjson.accesstoken;

            //load the StreamerInformation to store the data in the filestream
            StreamerInformation si = StreamerInformation.Instance();
            si.Load();

            //check the user is valid on Twitch side or not
            User[] userList = api.V5.Users.GetUserByNameAsync(name).Result.Matches;
            string userid = null;
            try
            {
                userid = userList[0].Id;
            }
            catch (System.IndexOutOfRangeException)
            {
                var emoji1 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji1} Sajnos nincs ilyen Twitch user.");
            }

            //Check the user is streaming or not
            bool islive = false;
            var stream = await api.V5.Streams.GetStreamByUserAsync(userid);
            if (stream?.Stream?.Channel?.Status != null)
            {
                islive = true;
            }
            else
            {
                islive = false;
            }

            //Add the user to the Streamers file
            string DiscordName = ctx.User.Username;
            string TwitchName = name;
            string TwitchUserID = userid;
            bool IsLive = islive;

            await si.AddStreamer(ctx, DiscordName, TwitchName, TwitchUserID, IsLive);
            si.Save();
            si.Print();

            
        }

        [Command("delete_streamer"), Description("Töröl a listáról ami alapján küldi a stream értesítéseket."), Aliases("Delete_streamer", "Delete_Streamer")]
        public async Task DeleteStreamer(CommandContext ctx)
        {

            //load the StreamerInformation to store the data in the filestream
            StreamerInformation si = StreamerInformation.Instance();
            si.Load();

            //delete the user from the Streamers file
            string DiscordName = ctx.User.Username;

            await si.RemoveStreamer(ctx, DiscordName);
            si.Save();
            si.Print();

        }

        [Command("start_stream"), Description("Elindítja az értesítés küldést ha valaki live a mentett streamerek közül."), Aliases("Start_streamnotification", "Start_Streamnotification")]
        public async Task StratStreamNotif(CommandContext ctx)
        {
            //open the Twitch config file
            var tjson = "";
            using (var fs = File.OpenRead("twitchconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                tjson = await sr.ReadToEndAsync();
            var tcfgjson = JsonConvert.DeserializeObject<twitchconfig>(tjson);

            //set the twitch related configs
            api = new TwitchAPI();
            api.Settings.ClientId = tcfgjson.clientid;
            api.Settings.AccessToken = tcfgjson.accesstoken;

            //load the StreamerInformation
            StreamerInformation si = StreamerInformation.Instance();
            si.Load();

            if (ctx.Channel.Name.Equals("stream📡") && Global.token==false)
            {

                //set the token
                Global.token = true;
                var emoji2 = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji2} A stream értesítések elindítva.");

                while (Global.token==true)
                {
                    await si.SendNotification(ctx, api);
                    Console.WriteLine($"{DateTime.Now} The next cycle of the stream notification.");
                    System.Threading.Thread.Sleep(30000);
                }
                

            }
            else if (Global.token == true)
            {
                var emoji2 = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji2} A stream értesítések már el vannak indítva.");
            }
            else if (!ctx.Channel.Name.Equals("stream📡"))
            {
                var emoji1 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji1} Csak a stream-értesítés szobában adható ki a parancs.");
            }



        }

        [Command("stop_stream"), Description("Kikapcsolja az értesítés küldést a streamekről."), Aliases("Stop_streamnotification", "Stop_Streamnotification")]
        public async Task StopStreamNotif(CommandContext ctx)
        {
            //open the Twitch config file
            var tjson = "";
            using (var fs = File.OpenRead("twitchconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                tjson = await sr.ReadToEndAsync();
            var tcfgjson = JsonConvert.DeserializeObject<twitchconfig>(tjson);

            //set the twitch related configs
            api = new TwitchAPI();
            api.Settings.ClientId = tcfgjson.clientid;
            api.Settings.AccessToken = tcfgjson.accesstoken;

            StreamerInformation si = StreamerInformation.Instance();
            si.Load();

            if (ctx.Channel.Name.Equals("stream📡"))
            {
                Global.token = false;
                //await si.SendNotification(ctx, api);

                var emoji2 = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji2} A stream értesítések leállítva.");
            }
            else
            {
                var emoji1 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji1} Csak a stream-értesítés szobában adható ki a parancs.");
            }

        }

        

        




    }
}
