using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using System.Collections.Generic;
//FunnyBunnyofDOOM@gmail.com
//https://www.doomlandgaming.com

namespace tpt.src
{
    class tpt : ModSystem
    {
        ICoreServerAPI myAPI;
        int timevalue;
        
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api); //Register the server api to "api"
            myAPI = api;
            IPermissionManager ipm = api.Permissions;
            ipm.RegisterPrivilege("tpt", "Teleport To");
            ipm.AddPrivilegeToGroup("suplayer", BPrivilege.tpt);
            api.RegisterCommand("tpt", "Teleports the player to another player", "",
                cmd_tpt, BPrivilege.tpt);
            api.RegisterCommand("tpaccept", "Teleports the player to another player", "",
                cmd_tpaccept, BPrivilege.tpt);
            timevalue = 0;
            myAPI.Event.RegisterGameTickListener(CoolDown, 60000);

            try
            {
                var Config = api.LoadModConfig<tptConfig>("jrtpconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    tptConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    tptConfig.Current = tptConfig.getDefault();
                }
            }
            catch
            {
                tptConfig.Current = tptConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (tptConfig.Current.tptDict == null)
                    tptConfig.Current.tptDict = tptConfig.getDefault().tptDict;
                if (tptConfig.Current.waitDict == null)
                    tptConfig.Current.waitDict = tptConfig.getDefault().waitDict;

                api.StoreModConfig(tptConfig.Current, "tptconfig.json");
            }
        }

        private void cmd_tpaccept(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (tptConfig.Current.waitDict.ContainsKey(player.PlayerUID))
            {
                String value;
                tptConfig.Current.waitDict.TryGetValue(player.PlayerUID, out value);
                String tpPlayer = value;
                myAPI.SendMessage(myAPI.World.PlayerByUid(tpPlayer), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your teleport request has been accepted. Stand by while you are teleported.", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport To accepted.", Vintagestory.API.Common.EnumChatType.Notification);
                EntityPlayer tpserverplayer = myAPI.World.PlayerByUid(tpPlayer).WorldData.EntityPlayer;
                tpserverplayer.TeleportTo(player.Entity.Pos.AsBlockPos);
                tptConfig.Current.waitDict.Remove(player.PlayerUID);
                tptConfig.Current.tptDict.Remove(tpPlayer);
                myAPI.StoreModConfig(tptConfig.Current, "tptconfig.json");
            }
        }

        private void cmd_tpt(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (args.PeekWord() != null)
            {
                IServerPlayerData pdata = myAPI.PlayerData.GetPlayerDataByLastKnownName(args.PeekWord());
                if (pdata == null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player could not be found. Please check your spelling and try again.", Vintagestory.API.Common.EnumChatType.Notification);
                    return;
                }
                
                
                if (tptConfig.Current.tptDict.ContainsKey(player.PlayerUID) == false)
                {
                    
                    if (tptConfig.Current.waitDict.ContainsKey(pdata.PlayerUID) == false)
                    {
                        tptinfo info = new tptinfo();
                        info.toplayer = args.PeekWord();
                        info.haspermission = false;
                        info.waiting = true;
                        info.timer = timevalue;
                        tptConfig.Current.tptDict.Add(player.PlayerUID, info);
                        tptConfig.Current.waitDict.Add(pdata.PlayerUID,player.PlayerUID);
                        myAPI.StoreModConfig(tptConfig.Current, "tptconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Stand by. You will be teleported when the other player accepts the teleport", Vintagestory.API.Common.EnumChatType.Notification);
                        myAPI.SendMessage(myAPI.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, pdata.LastKnownPlayername + " would like to teleport to you. Please type /tpaccept to accept.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Destination player already has another active TP request. Try again shortly.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You already have a pending teleport request.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter the name of the player you would like to teleport to.", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        private void CoolDown(float obj)
        {
            timevalue++;
            foreach (var keyvalue in tptConfig.Current.tptDict.Keys)
            {
                tptinfo value = new tptinfo();
                var dic = tptConfig.Current.tptDict.Values;
                tptConfig.Current.tptDict.TryGetValue(keyvalue, out value);
                if ((timevalue - value.timer)>=2 )
                {
                    myAPI.SendMessage(myAPI.World.PlayerByUid(keyvalue), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup,"Your TP to player has expired", Vintagestory.API.Common.EnumChatType.Notification);
                    tptConfig.Current.tptDict.Remove(keyvalue);
                    myAPI.StoreModConfig(tptConfig.Current, "tptconfig.json");
                    return;
                }
            }
        }

        public class tptConfig
        {
            public static tptConfig Current { get; set; }

            public Dictionary<String, tptinfo> tptDict { get; set; }
            public Dictionary<String, String> waitDict { get; set; }



            public static tptConfig getDefault()
            {
                var config = new tptConfig();
                //BlockPos defPos = new BlockPos(0, 0, 0);
                Dictionary<String, tptinfo> tptdictionary = new Dictionary<string, tptinfo>
                {
                    { "Default",new tptinfo() }
                };
                Dictionary<String, String> waitdictionary = new Dictionary<string, string>
                {
                    { "Default","Default"}
                };
                config.tptDict = tptdictionary;
                config.waitDict = waitdictionary;
                return config;
            }
        }
        public class tptinfo
        {
            
            public String toplayer;
            public Boolean haspermission;
            public Boolean waiting;
            public int timer;

        }

            public class BPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /tpt
            /// </summary>
            public static string tpt = "tpt";
        }
    }
}
