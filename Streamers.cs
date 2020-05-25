using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace DiscordBot
{
    [Serializable]
    public class Streamers
    {

        private string discordname;
        private string twitchname;
        private string twitchuserid;
        private bool islive;

        public Streamers (string discordname, string twitchname, string twitchuserid, bool islive)
        {
            this.discordname = discordname;
            this.twitchname = twitchname;
            this.twitchuserid = twitchuserid;
            this.islive = islive;
        }
        
        public string DiscordName
        {
            get
            {
                return this.discordname;
            }
            set
            {
                this.discordname = value;
            }
        }//end public string DiscordName

        public string TwitchName
        {
            get
            {
                return this.twitchname;
            }
            set
            {
                this.twitchname = value;
            }
        }//end public string TwitchName

        public string TwitchUserID
        {
            get
            {
                return this.twitchuserid;
            }
            set
            {
                this.twitchuserid = value;
            }
        }//end public string TwitchUserID

        public bool IsLive
        {
            get
            {
                return this.islive;
            }
            set
            {
                this.islive = value;
            }
        }//end public bool IsLive

    }// end public class Streamers


}
