using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace risingsun.src
{
    class risingsun : ModSystem
    {
        List<IServerPlayer> joinedPlayers = new List<IServerPlayer>();
        ICoreServerAPI sapi;
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            api.Event.PlayerCreate += OnPlayerCreate;
            api.Event.PlayerNowPlaying += onNowPlaying;

            api.RegisterCommand("rs", "Rising Sun configuration", "[dawn|dusk|help|version]", cmd_rs, Privilege.controlserver);

            try
            {
                var Config = api.LoadModConfig<rsConfig>("rsconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    rsConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    rsConfig.Current = rsConfig.getDefault();
                }
            }
            catch
            {
                rsConfig.Current = rsConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {

                if (rsConfig.Current.dawn == null)
                    rsConfig.Current.dawn = rsConfig.getDefault().dawn;
                if (rsConfig.Current.dusk == null)
                    rsConfig.Current.dusk = rsConfig.getDefault().dusk;

                api.StoreModConfig(rsConfig.Current, "rsconfig.json");
            }
        }

        private void cmd_rs(IServerPlayer player, int groupId, CmdArgs args)
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
                case "dawn":
                    //Add configuration logic| not larger than dusk
                    break;
                case "dusk":
                    //Add configuration logic
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /rs dawn|dusk|help|version", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "rising sun Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs dawn <i>number</i> - Sets the hour Rising Sun will advance the night to", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs dusk <i>number</i> - Sets the hour that Rising Sun considers Night", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home version - Displays the version information of Rising Sun", Vintagestory.API.Common.EnumChatType.Notification);
        }

        private void onNowPlaying(IServerPlayer byPlayer)
        {
            if (joinedPlayers != null)
            {
                if (joinedPlayers.Contains(byPlayer))
                {
                    sapi.BroadcastMessageToAllGroups("The sun has risen on a new player! Welcome " + byPlayer.PlayerName, Vintagestory.API.Common.EnumChatType.AllGroups);
                    int hour = byPlayer.Entity.World.Calendar.FullHourOfDay;
                    if (hour < rsConfig.Current.dawn)
                    {

                        byPlayer.Entity.World.Calendar.Add((int)rsConfig.Current.dawn - hour);
                    }
                    else if (hour > (int)rsConfig.Current.dusk)
                    {
                        byPlayer.Entity.World.Calendar.Add(24 - hour + (int)rsConfig.Current.dawn);
                    }
                    joinedPlayers.Remove(byPlayer);
                }
            }

            
            
        }
        public void OnPlayerCreate(IServerPlayer byPlayer)
        {
            joinedPlayers.Add(byPlayer);
        }

        public class rsConfig
        {
            public static rsConfig Current { get; set; }

            public int? dawn { get; set; }
            public int? dusk { get; set; }

            public static rsConfig getDefault()
            {
                var config = new rsConfig();

                config.dawn = 8;
                config.dusk = 21;

                return config;
            }
        }
    }
}
