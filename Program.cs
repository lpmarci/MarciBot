using System;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using TwitchLib;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Api.V5.Models.Streams;

namespace DiscordBot
{

    public class Program
    {
        public  DiscordClient Client { get; set; }
        public CommandsNextModule Commands { get; set; }
        public TwitchAPI api;

        public class twitchconfig
        {
            public string clientid { get; set; }
            public string accesstoken { get; set; }
        }

        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task MainAsync(string[] args)
        {
            
            //load the config file
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            //load the Discord related values from the config file
            var cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var cfg = new DiscordConfiguration
            {
                Token = cfgjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            //load the Twitch related values from the config file
            var tjson = "";
            using (var fs = File.OpenRead("twitchconfig.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                tjson = await sr.ReadToEndAsync();
            var tcfgjson = JsonConvert.DeserializeObject<twitchconfig>(tjson);

            //set the twitch related configs
            api = new TwitchAPI();
            api.Settings.ClientId = tcfgjson.clientid;
            api.Settings.AccessToken = tcfgjson.accesstoken;

            
            
            

            //instatiate our client 
            this.Client = new DiscordClient(cfg);

            //client event
            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;

            //set up commands
            //set the command configs
            var ccfg = new CommandsNextConfiguration
            {
                StringPrefix = cfgjson.CommandPrefix,
                EnableDms = false,
                EnableMentionPrefix = true
            };

            //load the config values
            this.Commands = this.Client.UseCommandsNext(ccfg);

            //command events
            this.Commands.CommandExecuted += this.Commands_CommandExecuted;
            this.Commands.CommandErrored += this.Commands_CommandErrored;

            //register the commands
            this.Commands.RegisterCommands<MyCommands>();


            //Connect the bot to the Discord server
            await Client.ConnectAsync();

            //prevent the quitting
            await Task.Delay(-1);
        }

        //Task when the client is ready
        private Task Client_Ready(ReadyEventArgs e)
        {
            //log this event
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "DiscordBot", "Client is ready.", DateTime.Now);

            return Task.CompletedTask;
        }

        //Task to log the Guild
        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            //log the name of the guild
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "DiscordBot", $"Guild available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        //Error Task
        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            //log the details of the error
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "DiscordBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        //Task to the commands
        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            //log the name of the command and the user
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "DiscordBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }
        
        //Taskto the failed commands
        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "DiscordBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);
            //check if the error is result of lack of permission
            if (e.Exception is ChecksFailedException ex)
            {
                //yes user lacks required permisssion
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry");

                //send the response to the user
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} Nincs jogod kiadni ezt a parancsot!",
                    Color = new DiscordColor(0xFF0000) //red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }

    }
    
    //this structure will hold data from config.json
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}
