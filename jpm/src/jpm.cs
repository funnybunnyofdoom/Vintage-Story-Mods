using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace jpm.src
{
    class jpm : ModSystem
    {
        ICoreServerAPI myAPI;
        bool jpmspy = false;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            myAPI = api; //Assign the api to myAPI to be used outside of server startup
            IPermissionManager ipm = api.Permissions; //Register a permission manager. 
            ipm.RegisterPrivilege("jpm", "Private Messages"); //Register the privilege for general teleports
            ipm.RegisterPrivilege("jpmadmin", "JPM management"); //Register the privilege for admin control
            api.RegisterCommand("jpm", "Simple Server Message Management", "[help | spy | version]", cmd_jpm, BPrivilege.jpmadmin); //Register the /jpm command for admins
            
            api.RegisterCommand("dm", "Private Message", " ", cmd_pm, BPrivilege.jpm);
            ipm.RemovePrivilegeFromGroup("suplayer", BPrivilege.jpmadmin);
            var modinfo = Mod.Info;
            System.Console.WriteLine("Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version + " LOADED");
            try
            {
                var Config = api.LoadModConfig<jpmConfig>("jpmconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    jpmConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    jpmConfig.Current = jpmConfig.getDefault();
                }
            }
            catch
            {
                jpmConfig.Current = jpmConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (jpmConfig.Current.spycfg == null)
                    jpmConfig.Current.spycfg = jpmConfig.getDefault().spycfg;

                api.StoreModConfig(jpmConfig.Current, "jpmconfig.json");
            }
        }

        private void cmd_pm(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            IServerPlayerData pdata = myAPI.PlayerData.GetPlayerDataByLastKnownName(cmd);
            if (cmd != "" & cmd != null & cmd != "help" & cmd != "version")
            {
                if (pdata == null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player could not be found. Please check your spelling and try again.", Vintagestory.API.Common.EnumChatType.Notification);
                    return;
                }
                else
                {
                    string message = args.PopAll();
                    System.Diagnostics.Debug.WriteLine(message);
                    if (message == null | message == "")
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please include a message.", Vintagestory.API.Common.EnumChatType.Notification);
                        return;
                    }
                    else
                    {
                        myAPI.SendMessage(myAPI.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#52307C\"><strong>" + player.PlayerName + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                        if (jpmConfig.Current.spycfg == true)
                        {


                            IPlayer[] playerlist = myAPI.World.AllOnlinePlayers;
                            for (int i = 0; i < playerlist.Count(); i++)
                            {
                                IPlayer Aplayer = playerlist[i];
                                IServerPlayerData adata = myAPI.PlayerData.GetPlayerDataByLastKnownName(Aplayer.PlayerName);
                                if (adata.RoleCode == "admin")
                                {
                                    myAPI.SendMessage(myAPI.World.PlayerByUid(Aplayer.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " to " + Aplayer.PlayerName + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                                }
                            }
                        }
                    }
                }
            }else if (cmd == "help")
            {
                displayhelp(player);
            }else if(cmd == "version")
            {
                var modinfo = Mod.Info;
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please include a player name.", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        private void cmd_jpm(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player);
                    break;
                case "spy":
                    if (jpmConfig.Current.spycfg == true)
                    {
                        jpmConfig.Current.spycfg = false;
                        myAPI.StoreModConfig(jpmConfig.Current, "jpmconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Private Message Spy has been disabled.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        jpmConfig.Current.spycfg = true;
                        myAPI.StoreModConfig(jpmConfig.Current, "jpmconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Private Message Spy has been enabled.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Just Private Message Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/dm <i>playername messageToPlayer</i> - sends a message to a player ", Vintagestory.API.Common.EnumChatType.Notification);
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/jpm spy - Toggles admin visibility of private messages", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        public class BPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use Simple Server Message commands
            /// </summary>
            public static string jpm = "jpm";
            public static string jpmadmin = "jpmadmin";
        }

        public class jpmConfig
        {
            public static jpmConfig Current { get; set; }
            public bool spycfg;
            public static jpmConfig getDefault()
            {
                var config = new jpmConfig();
                config.spycfg = false;
                return config;
            }
        }
    }
}
