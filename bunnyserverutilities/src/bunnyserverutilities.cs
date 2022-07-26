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
using Vintagestory.API.Common.Entities;

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
            api.RegisterCommand("spawn", "Teleports the player to spawn", "", cmd_spawn, Privilege.chat);


            try
            {
                var Config = api.LoadModConfig<bsuconfig>("BunnyServerUtilitiesConfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    bsuconfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    bsuconfig.Current = bsuconfig.getDefault();
                }
            }
            catch
            {
                bsuconfig.Current = bsuconfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (bsuconfig.Current.homeDict == null)//Must be preserved to pull old homes to the new save
                    bsuconfig.Current.homeDict = bsuconfig.getDefault().homeDict;//Must be preserved to pull old homes to the new save
                if (bsuconfig.Current.enablePermissions == null)
                    bsuconfig.Current.enablePermissions = bsuconfig.getDefault().enablePermissions;
                if (bsuconfig.Current.enableBack == null)
                    bsuconfig.Current.enableBack = bsuconfig.getDefault().enableBack;
                if (bsuconfig.Current.enableHome == null)
                    bsuconfig.Current.enableHome = bsuconfig.getDefault().enableHome;

                api.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
            }
            if (bsuconfig.Current.enablePermissions == false)
            {
                ipm.AddPrivilegeToGroup("admin", CPrivilege.home);
                ipm.AddPrivilegeToGroup("doplayer", CPrivilege.home);
                ipm.AddPrivilegeToGroup("admin", DPrivilege.back);
                ipm.AddPrivilegeToGroup("doplayer", DPrivilege.back);
                ipm.AddPrivilegeToGroup("suplayer", CPrivilege.home);
                ipm.AddPrivilegeToGroup("suplayer", DPrivilege.back);
            }

        }

        private void cmd_spawn(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.enableBack == true)
            {
                if (backSave.ContainsKey(player.PlayerUID))
                {
                    backSave.Remove(player.PlayerUID);
                }
                backSave.Add(player.PlayerUID, player.Entity.Pos.AsBlockPos);
            }
                EntityPlayer byEntity = player.Entity; //Get the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You are teleporting to spawn", Vintagestory.API.Common.EnumChatType.Notification);
            EntityPos spawnpoint = byEntity.World.DefaultSpawnPosition;
            byEntity.TeleportTo(spawnpoint);
        }

        private void cmd_importOldHomes(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (player.Role.Code == "admin")
            {
                if (bsuconfig.Current.homeDict != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Importing old homes", Vintagestory.API.Common.EnumChatType.Notification);
                    homeSave.Clear();
                    for (int i = 0; i < bsuconfig.Current.homeDict.Count(); i++)
                    {
                        KeyValuePair<string,BlockPos> kvp = bsuconfig.Current.homeDict.PopOne();
                        
                        homeSave.Add(kvp.Key,kvp.Value);
                    }
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "old homes imported", Vintagestory.API.Common.EnumChatType.Notification);
                    bsuconfig.Current.homeDict.Clear();
                    bsuconfig.Current.homeDict = null;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                }
            }
        }

        private void OnPlayerDeath(IServerPlayer player, DamageSource damageSource)
        {
            if (bsuconfig.Current.enableBack == true)
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
                    if (bsuconfig.Current.enableBack == true)
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
                        bsuconfig.Current.enableBack = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                        break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableBack = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
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
            if (bsuconfig.Current.enableHome == true)
            {
                if (homeSave.ContainsKey(player.Entity.PlayerUID))
                {
                    homeSave.Remove(player.Entity.PlayerUID);
                }
                homeSave.Add(player.Entity.PlayerUID, player.Entity.Pos.AsBlockPos);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "New home has been saved.", Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Home is disabled. an admin must use /home enable to enable", Vintagestory.API.Common.EnumChatType.Notification);
            }
            


        }
        private void cmd_home(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableHome == true)
                    {
                        if (bsuconfig.Current.enableBack == true)
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
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Home is disabled. an admin must use /home enable to enable", Vintagestory.API.Common.EnumChatType.Notification);
                    }     
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home and /sethome has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home and /sethome has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
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
            if (bsuconfig.Current.enableHome == true)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Home Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome - Sets your location as your home teleport", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home - teleports you to your set home location", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home version - Displays the version information of Just Home", Vintagestory.API.Common.EnumChatType.Notification);
            }
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home enable - enable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home disable - disable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
            }
            if (bsuconfig.Current.enableBack == true)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Back Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back - return to the last place you used /home, /back or died", Vintagestory.API.Common.EnumChatType.Notification);
            }
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
            sapi.WorldManager.SaveGame.StoreData("bsuBack", SerializerUtil.Serialize(backSave));
            sapi.WorldManager.SaveGame.StoreData("bsuHome", SerializerUtil.Serialize(homeSave));
        }

        private void OnSaveGameLoading()
        {
            byte[] backdata = sapi.WorldManager.SaveGame.GetData("bsuBack");
            byte[] homedata = sapi.WorldManager.SaveGame.GetData("bsuHome");

            backSave = backdata == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(backdata);
            homeSave = homedata == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(homedata);
        }


        public class bsuconfig
        {
            public static bsuconfig Current { get; set; }

            public Dictionary<String,BlockPos> homeDict { get; set; }//Must be preserved to pull old homes to the new save
            public bool? enablePermissions;
            public bool? enableBack;
            public bool? enableHome;



            public static bsuconfig getDefault()
            {
                var config = new bsuconfig();
                BlockPos defPos = new BlockPos(0,0,0);
                bool perms = false;
                bool backperms = true;

                Dictionary<String, BlockPos> homedictionary = null;
                
                config.homeDict = homedictionary;//Must be preserved to pull old homes to the new save
                config.enablePermissions = perms;
                config.enableBack = backperms;
                config.enableHome = true;
                return config;
            }


        }
            
    }
    
}
