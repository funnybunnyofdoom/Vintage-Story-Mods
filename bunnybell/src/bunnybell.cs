using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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
        List<AssetLocation> soundList = new List<AssetLocation>() //Create a list of sounds for the user to choose from. You can just append to this list to add more sounds. 
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
            new AssetLocation("game", "sounds/creature/raccoon/hurt"),
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
            api.Event.OnEntityDeath += onPlayerDeath;
            api.RegisterCommand("bb", "Bunny Bell configuration", "[volume|mute|help|version]", cmd_bb);

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
                //if (bbConfig.Current.globalmention == null)
                //    bbConfig.Current.globalmention = bbConfig.getDefault().globalmention;
                if (bbConfig.Current.globallogin == null)
                    bbConfig.Current.globallogin = bbConfig.getDefault().globallogin;
                if (bbConfig.Current.globallogout == null)
                    bbConfig.Current.globallogout = bbConfig.getDefault().globallogout;
                //if (bbConfig.Current.globaldeath == null)
                    //bbConfig.Current.globaldeath = bbConfig.getDefault().globaldeath;
                //if (bbConfig.Current.globalPVPdeath == null)
                    //bbConfig.Current.globalPVPdeath = bbConfig.getDefault().globalPVPdeath;
                if (bbConfig.Current.personalSoundList == null)
                    bbConfig.Current.personalSoundList = bbConfig.getDefault().personalSoundList;

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
                case "sound":
                    var num = args.PopWord();
                    if (num == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:need-number",soundList.Count), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        int result;
                        if (int.TryParse(num, out result))
                        {
                            if (result > -1 && result <= soundList.Count()-1)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:play-sound",result), Vintagestory.API.Common.EnumChatType.Notification);
                                player.Entity.World.PlaySoundFor(soundList[result], player);
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:need-number", soundList.Count-1), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:need-number", soundList.Count-1), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }                   
                    break;
                case "set":
                    string scope = args.PopWord();
                    string action = args.PopWord();
                    int? sound = args.PopWord().ToInt();
                    if (scope == null || action == null || sound == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:set-sound-help-1", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                    }else if (sound < 0 || sound > soundList.Count - 1)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:set-sound-help-2", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        if (scope == "global")
                        {
                            if (!player.HasPrivilege(APrivilege.bbadmin) && player.Role.Code != "admin")
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:admin", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                                return;
                            }
                            soundSettings tempSettings = bbConfig.Current.soundsettings;
                            if (action == "mention")
                            {
                                tempSettings.mention = soundList[(int)sound];
                                bbConfig.Current.soundsettings = tempSettings;
                                sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "login")
                            {
                                tempSettings.login = soundList[(int)sound];
                                bbConfig.Current.soundsettings = tempSettings;
                                sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "logout")
                            {
                                tempSettings.logout = soundList[(int)sound];
                                bbConfig.Current.soundsettings = tempSettings;
                                sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "death")
                            {
                                tempSettings.death = soundList[(int)sound];
                                bbConfig.Current.soundsettings = tempSettings;
                                sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "PVPdeath")
                            {
                                tempSettings.PVPdeath = soundList[(int)sound];
                                bbConfig.Current.soundsettings = tempSettings;
                                sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }else if(scope == "personal")
                        {
                            soundSettings tempSettings;
                            if (bbConfig.Current.personalSoundList.ContainsKey(player.PlayerUID) == false) //Check if the player has an entry in the config
                            {
                                tempSettings = bbConfig.Current.soundsettings; //Clone the server's sound settings         
                                
                            }else
                            {
                                bbConfig.Current.personalSoundList.TryGetValue(player.PlayerUID, out tempSettings);//Get the personal settings for this player from the config
                            }
                            
                             
                            if (action == "mention")
                            {
                                tempSettings.mention = soundList[(int)sound];
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:personal-sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "login")
                            {
                                tempSettings.login = soundList[(int)sound];
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:personal-sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "logout")
                            {
                                tempSettings.logout = soundList[(int)sound];
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:personal-sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "death")
                            {
                                tempSettings.death = soundList[(int)sound];
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:personal-sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (action == "PVPdeath")
                            {
                                tempSettings.PVPdeath = soundList[(int)sound];
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:personal-sound-set", scope, action, sound), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            bbConfig.Current.personalSoundList.Remove(player.PlayerUID);
                            bbConfig.Current.personalSoundList.Add(player.PlayerUID, tempSettings);
                            sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        }
                    }
                    break;
                case "enable": //Enables the sounds 
                    string gloper = args.PopWord();
                    string gaction = args.PopWord();
                    if (gloper == null || gaction == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-enable"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    if (gloper == "global")
                    {
                        if (!player.HasPrivilege(APrivilege.bbadmin) && player.Role.Code != "admin")
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:admin", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                            return;
                        }
                        if (gaction == "mention")
                        {
                            bbConfig.Current.soundsettings.enablemention = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "login")
                        {
                            bbConfig.Current.soundsettings.enablelogin = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "logout")
                        {
                            bbConfig.Current.soundsettings.enablelogout = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "death")
                        {
                            bbConfig.Current.soundsettings.enabledeath = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "PVPdeath")
                        {
                            bbConfig.Current.soundsettings.enablePVPdeath = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable-mention-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }else if (gloper == "personal")
                    {
                        soundSettings tempSettings;
                        if (bbConfig.Current.personalSoundList.ContainsKey(player.PlayerUID) == false) //Check if the player has an entry in the config
                        {
                            tempSettings = bbConfig.Current.soundsettings; //Clone the server's sound settings
                        }
                        else
                        {
                            bbConfig.Current.personalSoundList.TryGetValue(player.PlayerUID, out tempSettings);//Get the personal settings for this player from the config
                        }


                        if (gaction == "mention")
                        {
                            tempSettings.enablemention = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "login")
                        {
                            tempSettings.enablelogin = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "logout")
                        {
                            tempSettings.enablelogout = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "death")
                        {
                            tempSettings.enabledeath = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (gaction == "PVPdeath")
                        {
                            tempSettings.enablePVPdeath = true;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable", gloper, gaction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        bbConfig.Current.personalSoundList.Remove(player.PlayerUID);
                        bbConfig.Current.personalSoundList.Add(player.PlayerUID, tempSettings);
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");

                    }
                    break;
                case "disable":
                    string dloper = args.PopWord();
                    string daction = args.PopWord();
                    if (dloper == null || daction == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-disable"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    if (dloper == "global")
                    {
                        if (!player.HasPrivilege(APrivilege.bbadmin) && player.Role.Code != "admin")
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:admin", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                            return;
                        }
                        if (daction == "mention")
                        {
                            bbConfig.Current.soundsettings.enablemention = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "login")
                        {
                            bbConfig.Current.soundsettings.enablelogin = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "logout")
                        {
                            bbConfig.Current.soundsettings.enablelogout = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "death")
                        {
                            bbConfig.Current.soundsettings.enabledeath = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "PVPdeath")
                        {
                            bbConfig.Current.soundsettings.enablePVPdeath = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:enable-mention-global"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else if (dloper == "personal")
                    {
                        soundSettings tempSettings;
                        if (bbConfig.Current.personalSoundList.ContainsKey(player.PlayerUID) == false) //Check if the player has an entry in the config
                        {
                            tempSettings = bbConfig.Current.soundsettings; //Clone the server's sound settings
                        }
                        else
                        {
                            bbConfig.Current.personalSoundList.TryGetValue(player.PlayerUID, out tempSettings);//Get the personal settings for this player from the config
                        }


                        if (daction == "mention")
                        {
                            tempSettings.enablemention = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "login")
                        {
                            tempSettings.enablelogin = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "logout")
                        {
                            tempSettings.enablelogout = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "death")
                        {
                            tempSettings.enabledeath = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (daction == "PVPdeath")
                        {
                            tempSettings.enablePVPdeath = false;
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:disable", dloper, daction), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        bbConfig.Current.personalSoundList.Remove(player.PlayerUID);
                        bbConfig.Current.personalSoundList.Add(player.PlayerUID, tempSettings);
                        sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");

                    }
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /bb [help|version|set|enable|disable|sound]", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        public class bbConfig
        {
            public static bbConfig Current { get; set; }

            public soundSettings soundsettings = new soundSettings();
            //public bool? globalmention;
            public bool? globallogin;
            public bool? globallogout;
            //public bool? globaldeath;
            //public bool? globalPVPdeath;
            public Dictionary<string,soundSettings> personalSoundList; //This dict holds our personal sound selections for each player

            public static bbConfig getDefault()
            {
                var config = new bbConfig();

                //config.globalmention = true;
                config.globallogin = true;
                config.globallogout = true;
                //config.globaldeath = true;
                //config.globalPVPdeath = true;
                config.personalSoundList = new Dictionary<string, soundSettings>();

                soundSettings tempsettings = new soundSettings();
                //tempsettings.user = "server";
                tempsettings.mention = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.login = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.logout = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.death = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.PVPdeath = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.enabledeath = true;
                tempsettings.enablelogin = true;
                tempsettings.enablelogout = true;
                tempsettings.enablemention = true;
                tempsettings.enablePVPdeath = true;

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
                    if (bbConfig.Current.personalSoundList.ContainsKey(templayer.PlayerUID))
                    {
                        soundSettings outputValue;
                        bbConfig.Current.personalSoundList.TryGetValue(templayer.PlayerUID, out outputValue);
                        if(outputValue.enablemention == true)
                        {
                            templayer.Entity.World.PlaySoundFor(outputValue.mention, templayer, true, 32, volume);
                        }
                        return;
                    }
                    if (bbConfig.Current.soundsettings.enablemention == true) //Check to see if mention is enabled. This is where we will check if a player has overriding settings
                    {
                        templayer.Entity.World.PlaySoundFor(bbConfig.Current.soundsettings.mention, templayer, true, 32, volume);
                    }
                    
                }
            }
        }

        private void onPlayerJoined(IServerPlayer byPlayer)
        {

            IPlayer[] allPlayers =  sapi.World.AllOnlinePlayers; // Get a list of all the online players
            AssetLocation joinSound = bbConfig.Current.soundsettings.login;
            soundSettings outputValue;
            

            for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
            {
                if (allPlayers[i].PlayerUID != byPlayer.PlayerUID)
                { //Don't make a sound for the player joining
                    if(bbConfig.Current.personalSoundList.ContainsKey(allPlayers[i].PlayerUID))
                    {
                        bbConfig.Current.personalSoundList.TryGetValue(allPlayers[i].PlayerUID, out outputValue);
                        if (outputValue.enablelogin == true)
                        {
                            joinSound = outputValue.login; //Get's the player's personal login sound
                            byPlayer.Entity.World.PlaySoundFor(joinSound, allPlayers[i]);
                        }
                    }
                    else {
                        if(bbConfig.Current.globallogin == true)
                        {
                            byPlayer.Entity.World.PlaySoundFor(joinSound, allPlayers[i]);
                        }
                    }
                }//Make a sound for all the online players
            }
            
        }

        private void onPlayerLogout(IServerPlayer byPlayer)
        {

            IPlayer[] allPlayers = sapi.World.AllOnlinePlayers; // Get a list of all the online players
            AssetLocation logoutSound = bbConfig.Current.soundsettings.logout;
            soundSettings outputValue;

            for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
            {
                if (allPlayers[i].PlayerUID != byPlayer.PlayerUID)
                { //Don't make a sound for the player joining
                    if (bbConfig.Current.personalSoundList.ContainsKey(allPlayers[i].PlayerUID))
                    {
                        bbConfig.Current.personalSoundList.TryGetValue(allPlayers[i].PlayerUID, out outputValue);
                        if (outputValue.enablelogout == true)
                        {
                            logoutSound = outputValue.logout; //Get's the player's personal login sound
                            byPlayer.Entity.World.PlaySoundFor(logoutSound, allPlayers[i]);
                        }
                    }
                    else
                    {
                        if (bbConfig.Current.globallogout == true)
                        {
                            byPlayer.Entity.World.PlaySoundFor(logoutSound, allPlayers[i]);
                        }
                    }
                }//Make a sound for all the online players
            }
        }

        private void onPlayerDeath(Entity entity, DamageSource damageSource)
        {

            IPlayer[] allPlayers = sapi.World.AllOnlinePlayers; // Get a list of all the online players
            if (entity.Code.FirstCodePart() == "player")//Check for player death
            {
                string fcp;
                if (damageSource.SourceEntity == null)
                {
                    fcp = "notPlayer";
                }
                else
                {
                    fcp = damageSource.SourceEntity.FirstCodePart();
                }
                if (fcp == "player")//Check for PVP
                {   
                    for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
                    {
                        if (bbConfig.Current.personalSoundList.ContainsKey(allPlayers[i].PlayerUID))
                        {
                            soundSettings outputValue;
                            bbConfig.Current.personalSoundList.TryGetValue(allPlayers[i].PlayerUID, out outputValue);
                            if (outputValue.enablePVPdeath == true)
                            {
                                entity.World.PlaySoundFor(outputValue.PVPdeath, allPlayers[i]); //Make a sound for all the online players
                            }

                        }else if (bbConfig.Current.soundsettings.enablePVPdeath == true)
                        {
                            
                            entity.World.PlaySoundFor(bbConfig.Current.soundsettings.PVPdeath, allPlayers[i]); //Make a sound for all the online players
                        }
                            
                    }
                }
                else
                {
                    //not PVP
                    for (int i = 0; i < allPlayers.Length; i++) //Iterate through the players online
                    {
                        if (bbConfig.Current.personalSoundList.ContainsKey(allPlayers[i].PlayerUID))
                        {
                            soundSettings outputValue;
                            bbConfig.Current.personalSoundList.TryGetValue(allPlayers[i].PlayerUID, out outputValue);
                            if (outputValue.enabledeath == true)
                            {
                                entity.World.PlaySoundFor(outputValue.death, allPlayers[i]); //Make a sound for all the online players
                            }

                        }
                        else if (bbConfig.Current.soundsettings.enabledeath == true)
                        {
                            entity.World.PlaySoundFor(bbConfig.Current.soundsettings.death, allPlayers[i]); //Make a sound for all the online players
                        }

                    }
                }
            }
        }

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-title"), Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-sound",soundList.Count-1), Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-set", soundList.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-enable"), Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-disable"), Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnybell:help-version"), Vintagestory.API.Common.EnumChatType.Notification);
        }

        public class soundSettings
        {
            //public string user;
            public AssetLocation mention;
            public AssetLocation login;
            public AssetLocation logout;
            public AssetLocation death;
            public AssetLocation PVPdeath;

            public bool enablemention;
            public bool enablelogin;
            public bool enablelogout;
            public bool enabledeath;
            public bool enablePVPdeath;
        }

        public class APrivilege : Privilege
        {
            /// <summary>
            /// Ability to use global commands for BunnyBell
            /// </summary>
            public static string bbadmin = "bbadmin";
        }
    }
}
