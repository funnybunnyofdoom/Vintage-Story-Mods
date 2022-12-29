using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace bunnybell.src
{
    class bunnybell : ModSystem
    {
        ICoreServerAPI sapi;
        List<AssetLocation> soundList = new List<AssetLocation>()
        {
            new AssetLocation("game", "sounds/effect/receptionbell"),
            new AssetLocation("game", "sounds/effect/anvilhit1"),
            new AssetLocation("game", "sounds/effect/cashregister"),
            new AssetLocation("game", "sounds/effect/clothrip"),
            new AssetLocation("game", "sounds/effect/crusher-impact1"),
            new AssetLocation("game", "sounds/effect/deepbell"),
            new AssetLocation("game", "sounds/effect/latch"),
            new AssetLocation("game", "sounds/effect/squish2"),
            new AssetLocation("game", "sounds/effect/stonecrush"),
            new AssetLocation("game", "sounds/effect/woodswitch"),
            new AssetLocation("game", "sounds/creature/beesting"),
            new AssetLocation("game", "sounds/creature/wolf/pup-bark"),
            new AssetLocation("game", "sounds/creature/racoon/hurt"),
            new AssetLocation("game", "sounds/creature/pig/hurt"),
            new AssetLocation("game", "sounds/creature/chicken/rooster-call")

        };
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell"); //replace this sound variable. Instead, we will pull the sound to use from the configs

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            api.Event.PlayerChat += onPlayerChat; //Event listener for when a player chats
            api.Event.PlayerNowPlaying += onPlayerJoined; //Event Listener for when a player logs in
            api.Event.PlayerDisconnect += onPlayerLogout; //Event Listener for when a players logs out
            api.RegisterCommand("bb", "Bunny Bell configuration", "[volume|mute|help|version]", cmd_bb, Privilege.controlserver);

            try
            {
                var Config = api.LoadModConfig<bbConfig>("bbconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    bbConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    bbConfig.Current = bbConfig.getDefault();
                }
            }
            catch
            {
                bbConfig.Current = bbConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {

                if (bbConfig.Current.soundsettings == null)
                    bbConfig.Current.soundsettings = bbConfig.getDefault().soundsettings;
                if (bbConfig.Current.globalmention == null)
                    bbConfig.Current.globalmention = bbConfig.getDefault().globalmention;
                if (bbConfig.Current.globallogin == null)
                    bbConfig.Current.globallogin = bbConfig.getDefault().globallogin;
                if (bbConfig.Current.globallogout == null)
                    bbConfig.Current.globallogout = bbConfig.getDefault().globallogout;

                api.StoreModConfig(bbConfig.Current, "bbconfig.json");
            }
        }

       

        private void cmd_bb(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "volume":
                    //Add configuration logic| not larger than dusk
                    break;
                case "mute":
                    break;
                case "enablemention": //Enables the global sound 
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globalmention = true;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable-mention-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disablemention":
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globalmention = false;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable-mention-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enablelogin": //Enables the global sound 
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globallogin = true;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable-login-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disablelogin":
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globallogin = false;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable-login-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enablelogout": //Enables the global sound 
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globallogout = true;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable-logout-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disablelogout":
                    if (args.PopWord() == "global")
                    {
                        bbConfig.Current.globallogout = false;
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable-logout-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /bb volume|mute|help|version", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        public class bbConfig
        {
            public static bbConfig Current { get; set; }

            public soundSettings soundsettings = new soundSettings();
            public bool? globalmention;
            public bool? globallogin;
            public bool? globallogout;

            public static bbConfig getDefault()
            {
                var config = new bbConfig();

                config.globalmention = true;
                config.globallogin = true;
                config.globallogout = true;

                soundSettings tempsettings = new soundSettings();
                tempsettings.user = "server";
                tempsettings.mention = new AssetLocation("game", "sounds/effect/crusher-impact1");
                tempsettings.login = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.logout = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.death = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.PVPdeath = new AssetLocation("game", "sounds/effect/receptionbell");

                config.soundsettings = tempsettings;

                return config;
            }
        }
        private void onPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            string checklist = data;
            IPlayer[] playerList = sapi.World.AllOnlinePlayers;
            int volume = 1;
            for (var i = 0; i < playerList.Count(); i++)
            {
                string templist = checklist;
                IPlayer templayer = playerList[i];
                if (templist.CaseInsensitiveContains(templayer.PlayerName))
                {
                    if (bbConfig.Current.globalmention == true) //Check to see if mention is enabled. This is where we will check if a player has overriding settings
                    {
                        templayer.Entity.World.PlaySoundFor(bbConfig.Current.soundsettings.mention, templayer, true, 32, volume);
                    }
                    
                }
            }
        }

        private void onPlayerJoined(IServerPlayer byPlayer)
        {
            if (bbConfig.Current.globallogin == false) return;//exit function if sounds are disabled
            
            IPlayer[] allPlayers =  sapi.World.AllOnlinePlayers; // Get a list of all the online players
            for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
            {
                if (allPlayers[i].PlayerUID == byPlayer.PlayerUID) return; //Don't make a sound for the player joining
                byPlayer.Entity.World.PlaySoundFor(bbConfig.Current.soundsettings.login,allPlayers[i]); //Make a sound for all the online players
            }
            
        }

        private void onPlayerLogout(IServerPlayer byPlayer)
        {
            if (bbConfig.Current.globallogout == false) return;//exit function if sounds are disabled

            IPlayer[] allPlayers = sapi.World.AllOnlinePlayers; // Get a list of all the online players
            for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
            {
                if (allPlayers[i].PlayerUID == byPlayer.PlayerUID) return; //Don't make a sound for the player logging out
                byPlayer.Entity.World.PlaySoundFor(bbConfig.Current.soundsettings.logout, allPlayers[i]); //Make a sound for all the online players
            }
        }

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny Bell Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb volume <i>number</i> - Sets your notification volume", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb mute - turns off your notification sound", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home version - Displays the version information of Bunny Bell", Vintagestory.API.Common.EnumChatType.Notification);
        }

        public class soundSettings
        {
            public string user;
            public AssetLocation mention;
            public AssetLocation login;
            public AssetLocation logout;
            public AssetLocation death;
            public AssetLocation PVPdeath;
        }
    }
}
