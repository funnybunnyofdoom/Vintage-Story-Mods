using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace bunnybell.src
{
    class bunnybell : ModSystem
    {
        ICoreClientAPI sapi;
        Dictionary<string, BlockPos> location;
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell");

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            sapi = api;
            api.Event.ChatMessage += onPlayerChat;
            //api.RegisterCommand("bb", "Bunny Bell configuration", "[volume|mute|help|version]", cmd_bb, Privilege.controlserver);

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

                if (bbConfig.Current.settings == null)
                    bbConfig.Current.settings = bbConfig.getDefault().settings;

                api.StoreModConfig(bbConfig.Current, "bbconfig.json");
            }
        }

       

        /*private void cmd_bb(IClientPlayer player, int groupId, CmdArgs args)
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
                    List<soundSettings> stlist = bbConfig.Current.settings;
                    List<soundSettings> newList = new List<soundSettings>();
                    for (var j = 0; j < stlist.Count; j++)
                    {
                        soundSettings usersound = stlist[j];
                        System.Diagnostics.Debug.WriteLine(usersound.user);
                        System.Diagnostics.Debug.WriteLine(usersound.volume);
                        
                        if (usersound.user != player.PlayerUID)
                        {
                            
                            newList.Add(usersound);
                        }                                                      
                            
                    }
                    soundSettings sounds = new soundSettings();
                    sounds.user = player.PlayerUID;
                    sounds.volume = 0;
                    newList.Add(sounds);
                    bbConfig.Current.settings = newList;
                    sapi.StoreModConfig(bbConfig.Current, "bbconfig.json");
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /bb volume|mute|help|version", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }*/

        public class bbConfig
        {
            public static bbConfig Current { get; set; }

            public List<soundSettings> settings;

            public static bbConfig getDefault()
            {
                var config = new bbConfig();

                soundSettings tempsettings = new soundSettings();
                tempsettings.user = "server";
                tempsettings.volume = 7;
                List<soundSettings> tempsetlist = new List<soundSettings>();
                tempsetlist.Add(tempsettings);
                config.settings = tempsetlist;

                return config;
            }
        }
        private void onPlayerChat(int groupId, string message, EnumChatType chattype, string data)
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
                    List<soundSettings> stlist = bbConfig.Current.settings;
                    for (var j = 0;j < stlist.Count; j++)
                    {
                        soundSettings usersound = stlist[j];
                        if (usersound.user == templayer.PlayerUID)
                        {
                            volume = usersound.volume;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine(volume);
                    if (volume > 0)
                    {
                        templayer.Entity.World.PlaySoundFor(sound, templayer, true, 32, volume);//volume);
                    }
                    
                }
            }
        }

        /*private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny Bell Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb volume <i>number</i> - Sets your notification volume", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb mute - turns off your notification sound", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb version - Displays the version information of Bunny Bell", Vintagestory.API.Common.EnumChatType.Notification);
        }*/

        public class soundSettings
        {
            public string user;
            public int volume;
        }
    }
}
