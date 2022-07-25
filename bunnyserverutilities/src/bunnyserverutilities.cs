using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace jhome.src
{
    public class bunnyserverutilities : ModSystem
    {
        public ICoreServerAPI sapi;
        public BlockPos homepos;
        Dictionary<string, BlockPos> backSave;
        Dictionary<string, BlockPos> homeSave;
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            IPermissionManager ipm = api.Permissions;
            api.Event.PlayerDeath += OnPlayerDeath;
            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.GameWorldSave += OnSaveGameSaving;


            api.RegisterCommand("sethome", "Set your current position as home", " ",
                cmd_sethome, CPrivilege.home);
            api.RegisterCommand("home", "Teleport to your /sethome location", " ",
                cmd_home, CPrivilege.home);
            api.RegisterCommand("back", "Go back to your last TP location", " ",
                cmd_back, DPrivilege.back);
            ipm.RegisterPrivilege("sethome", "Set your current position as home",false);
            ipm.RegisterPrivilege("home", "Set your current position as home",false);
            ipm.RegisterPrivilege("back", "Go back to your last TP location", false);
            api.RegisterCommand("importOldHomes", "Imports homes from version 1.0.5 and earlier", " ",
                cmd_importOldHomes);


            try
            {
                var Config = api.LoadModConfig<HomeConfig>("homeconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    HomeConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    HomeConfig.Current = HomeConfig.getDefault();
                }
            }
            catch
            {
                HomeConfig.Current = HomeConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (HomeConfig.Current.homeDict == null)//Must be preserved to pull old homes to the new save
                    HomeConfig.Current.homeDict = HomeConfig.getDefault().homeDict;//Must be preserved to pull old homes to the new save
                if (HomeConfig.Current.enablePermissions == null)
                    HomeConfig.Current.enablePermissions = HomeConfig.getDefault().enablePermissions;
                if (HomeConfig.Current.enableBack == null)
                    HomeConfig.Current.enableBack = HomeConfig.getDefault().enableBack;

                api.StoreModConfig(HomeConfig.Current, "homeconfig.json");
            }
            if (HomeConfig.Current.enablePermissions == false)
            {
                ipm.AddPrivilegeToGroup("admin", CPrivilege.home);
                ipm.AddPrivilegeToGroup("doplayer", CPrivilege.home);
                ipm.AddPrivilegeToGroup("admin", DPrivilege.back);
                ipm.AddPrivilegeToGroup("doplayer", DPrivilege.back);
                ipm.AddPrivilegeToGroup("suplayer", CPrivilege.home);
                ipm.AddPrivilegeToGroup("suplayer", DPrivilege.back);
            }

        }

        private void cmd_importOldHomes(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (player.Role.Code == "admin")
            {
                if (HomeConfig.Current.homeDict != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Importing old homes", Vintagestory.API.Common.EnumChatType.Notification);
                    homeSave.Clear();
                    for (int i = 0; i < HomeConfig.Current.homeDict.Count(); i++)
                    {
                        KeyValuePair<string,BlockPos> kvp = HomeConfig.Current.homeDict.PopOne();
                        
                        homeSave.Add(kvp.Key,kvp.Value);
                    }
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "old homes imported", Vintagestory.API.Common.EnumChatType.Notification);
                    HomeConfig.Current.homeDict.Clear();
                    HomeConfig.Current.homeDict = null;
                    sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
                }

            }
        }

        private void OnPlayerDeath(IServerPlayer player, DamageSource damageSource)
        {
            if (HomeConfig.Current.enableBack == true)
            {
                if (backSave.ContainsKey(player.PlayerUID))
                {
                    backSave.Remove(player.PlayerUID);
                }
                backSave.Add(player.PlayerUID, player.Entity.Pos.AsBlockPos);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Use /back to return to your death point", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        private void cmd_back(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (HomeConfig.Current.enableBack == true)
                    {
                        BlockPos newPos = player.Entity.Pos.AsBlockPos;
                        if (backSave.ContainsKey(player.Entity.PlayerUID))
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Returning to your last location", Vintagestory.API.Common.EnumChatType.Notification);
                            sapi.WorldManager.LoadChunkColumnPriority(backSave[player.Entity.PlayerUID].X / sapi.WorldManager.ChunkSize, backSave[player.Entity.PlayerUID].Z / sapi.WorldManager.ChunkSize);
                            player.Entity.TeleportTo(backSave[player.Entity.PlayerUID].X, backSave[player.Entity.PlayerUID].Y, backSave[player.Entity.PlayerUID].Z);
                            backSave.Remove(player.Entity.PlayerUID);
                            backSave.Add(player.Entity.PlayerUID, newPos);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No back location. Use /home to create a back location.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Back is disabled. an admin must use /back enable to enable", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        HomeConfig.Current.enableBack = true;
                        sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                        break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        HomeConfig.Current.enableBack = false;
                        sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                        break;
                case "help":
                    displayhelp(player);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        private void cmd_sethome(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (homeSave.ContainsKey(player.Entity.PlayerUID))
            {
                homeSave.Remove(player.Entity.PlayerUID);
            }
            homeSave.Add(player.Entity.PlayerUID,player.Entity.Pos.AsBlockPos);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "New home has been saved.", Vintagestory.API.Common.EnumChatType.Notification);


        }
        private void cmd_home(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (HomeConfig.Current.enableBack == true)
                    {
                        if (backSave.ContainsKey(player.PlayerUID))
                        {
                            backSave.Remove(player.PlayerUID);
                        }

                        backSave.Add(player.PlayerUID, player.Entity.Pos.AsBlockPos);
                    }
                    if (homeSave.ContainsKey(player.Entity.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to your saved home", Vintagestory.API.Common.EnumChatType.Notification);
                        sapi.WorldManager.LoadChunkColumnPriority(homeSave[player.Entity.PlayerUID].X / sapi.WorldManager.ChunkSize, homeSave[player.Entity.PlayerUID].Z / sapi.WorldManager.ChunkSize);
                        player.Entity.TeleportTo(homeSave[player.Entity.PlayerUID].X, homeSave[player.Entity.PlayerUID].Y, homeSave[player.Entity.PlayerUID].Z);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No Home Saved. Teleporting to world center. Use /sethome to set a home.", Vintagestory.API.Common.EnumChatType.Notification);
                        sapi.WorldManager.LoadChunkColumnPriority(sapi.World.DefaultSpawnPosition.XYZInt.X, sapi.World.DefaultSpawnPosition.XYZInt.Z);
                        player.Entity.TeleportTo(sapi.World.DefaultSpawnPosition.XYZInt.X, sapi.World.DefaultSpawnPosition.XYZInt.Y, sapi.World.DefaultSpawnPosition.XYZInt.Z);
                    }
                    break;
                case "help":
                    displayhelp(player);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny's Server Utility Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Home Commands---", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome - Sets your location as your home teleport", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home - teleports you to your set home location", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home version - Displays the version information of Just Home", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Back Commands---", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back - return to the last place you used /home, /back or died", Vintagestory.API.Common.EnumChatType.Notification);
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back enable - enable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back disable - disable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
            }
            
        }

        public class CPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /home
            /// </summary>
            
            public static string home = "home";

        }
        public class DPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /back
            /// </summary>

            public static string back = "back";

        }

        private void OnSaveGameSaving()
        {
            sapi.WorldManager.SaveGame.StoreData("back", SerializerUtil.Serialize(backSave));
        }

        private void OnSaveGameLoading()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData("back");

            backSave = data == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(data);
            homeSave = data == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(data);
        }


        public class HomeConfig
        {
            public static HomeConfig Current { get; set; }

            public Dictionary<String,BlockPos> homeDict { get; set; }//Must be preserved to pull old homes to the new save
            public bool? enablePermissions;
            public bool? enableBack;



            public static HomeConfig getDefault()
            {
                var config = new HomeConfig();
                BlockPos defPos = new BlockPos(0,0,0);
                bool perms = false;
                bool backperms = true;

                Dictionary<String, BlockPos> homedictionary = null;
                
                config.homeDict = homedictionary;//Must be preserved to pull old homes to the new save
                config.enablePermissions = perms;
                config.enableBack = backperms;
                return config;
            }


        }
            
    }
    
}
