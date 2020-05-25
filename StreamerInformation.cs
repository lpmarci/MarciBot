using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Dynamic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Events;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Api.V5.Models.Streams;
using System.Threading.Channels;
using System.Threading;

namespace DiscordBot
{
    public class StreamerInformation
    {

        public TwitchAPI api;

        public struct twitchconfig
        {
            [JsonProperty("clientid")]
            public string clientid { get; set; }

            [JsonProperty("accesstoken")]
            public string accesstoken { get; set; }
        }

        private static StreamerInformation streamerInformation;

        public Dictionary<string, Streamers> streamersDictionary;
        private BinaryFormatter formatter;

        private const string DATA_FILENAME = "streamersinformation.dat";

        public static StreamerInformation Instance()
        {
            if (streamerInformation == null)
            {
                streamerInformation = new StreamerInformation();
            }
            return streamerInformation;
        }

        private StreamerInformation()
        {
            this.streamersDictionary = new Dictionary<string, Streamers>();
            this.formatter = new BinaryFormatter();
        }

        async public Task AddStreamer(CommandContext ctx, string discordname, string twitchname, string twitchuserid, bool islive)
        {
            if (this.streamersDictionary.ContainsKey(discordname))
            {
                var emoji0 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji0} A(z) {ctx.User.Username} felhasználóhoz már van elmentve Twitch user.");

            }
            else
            {
                this.streamersDictionary.Add(discordname, new Streamers(discordname, twitchname, twitchuserid, islive));
                var emoji2 = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji2} {discordname} felhasználóhoz elmentettem a(z) {twitchname} Twitch nevet és {twitchuserid} TwitchID-t.");
            }
        }

        async public Task RemoveStreamer(CommandContext ctx, string name)
        {
            if (!this.streamersDictionary.ContainsKey(name))
            {
                var emoji0 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                await ctx.TriggerTypingAsync();
                await ctx.RespondAsync($"{emoji0} A(z) {ctx.User.Username} felhasználóhoz még nem volt elmentve Twitch user.");
            }
            else
            {
                if (this.streamersDictionary.Remove(name))
                {
                    var emoji2 = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                    await ctx.TriggerTypingAsync();
                    await ctx.RespondAsync($"{emoji2} A(z) {ctx.User.Username} felhasználónak töröltem a tárolt adatait.");
                }
                else
                {
                    var emoji0 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                    await ctx.TriggerTypingAsync();
                    await ctx.RespondAsync($"{emoji0} Hiba történt a(z) {ctx.User.Username} felhasználó adatainak törlése közben.");
                }
            }
        }

        public void Save()
        {
            try
            {
                FileStream writeFileStream = new FileStream(DATA_FILENAME, FileMode.Create, FileAccess.Write);

                this.formatter.Serialize(writeFileStream, this.streamersDictionary);

                writeFileStream.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to save streamer' information");
            }
        }

        public void Load()
        {
            if (File.Exists(DATA_FILENAME))
            {
                try
                {
                    FileStream readerFileStream = new FileStream(DATA_FILENAME, FileMode.Open, FileAccess.Read);

                    this.streamersDictionary = (Dictionary<String, Streamers>)
                        this.formatter.Deserialize(readerFileStream);
                    readerFileStream.Close();

                }
                catch (Exception)
                {
                    Console.WriteLine("There seems to be a file that contains " +
                    "streamer information but somehow there is a problem " +
                    "with reading it.");
                }

            }
        }

        public void Print()
        {
            if (this.streamersDictionary.Count > 0)
            {
                Console.WriteLine("Discordname, Twitchname, Twitchuserid, Islive");
                foreach (Streamers streamers in this.streamersDictionary.Values)
                {
                    Console.WriteLine(streamers.DiscordName + ", " + streamers.TwitchName + ", " + streamers.TwitchUserID + ", " + streamers.IsLive);
                }
            }
            else
            {
                Console.WriteLine("There are no saved information");
            }
        }

        public async Task SendNotification(CommandContext ctx, TwitchAPI api)
        {
            //while (token==true) 
            //{ 
                if (this.streamersDictionary.Count > 0)
                {
                    foreach (Streamers streamers in this.streamersDictionary.Values)
                    {
                        var stream = await api.V5.Streams.GetStreamByUserAsync(streamers.TwitchUserID);
                        if (stream?.Stream?.Channel?.Status != null)
                        {

                            if (streamers.IsLive == false)
                            {
                                streamers.IsLive = true;
                                this.Save();
                                var emoji1 = DiscordEmoji.FromName(ctx.Client, ":movie_camera:");
                                await ctx.TriggerTypingAsync();
                                await ctx.RespondAsync($"{emoji1} {streamers.TwitchName} live! Ha nézni szeretnéd, kattints a linkre: {stream.Stream.Channel.Url}");
                            }
                        }
                        else
                        {

                            streamers.IsLive = false;
                            this.Save();
                        }
                      
                    
                    }
                }
                else
                {
                    var emoji1 = DiscordEmoji.FromName(ctx.Client, ":no_entry_sign:");
                    await ctx.TriggerTypingAsync();
                    await ctx.RespondAsync($"{emoji1} Nincs mentett streamer.");
                    Console.WriteLine("There are no saved information");
                }
              //  System.Threading.Thread.Sleep(120000);

            //}

        }
    }
}
