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
    public class jhome : ModSystem
    {
        public ICoreServerAPI sapi;
        public BlockPos homepos;
        //NOTE TO SELF: Remove admin only for release
        //our server will have HOME only available to donators

        

        

        

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


            api.RegisterCommand("sethome", "Set your current position as home", " ",
                cmd_sethome, BPrivilege.sethome);
            api.RegisterCommand("home", "Teleport to your /sethome location", " ",
                cmd_home, CPrivilege.home);
            api.RegisterCommand("back", "Go back to your last TP location", " ",
                cmd_back, DPrivilege.back);
            ipm.RegisterPrivilege("sethome", "Set your current position as home",false);
            ipm.RegisterPrivilege("home", "Set your current position as home",false);
            ipm.RegisterPrivilege("back", "Go back to your last TP location", false);



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
                if (HomeConfig.Current.homeDict == null)
                    HomeConfig.Current.homeDict = HomeConfig.getDefault().homeDict;
                if (HomeConfig.Current.backDict == null)
                    HomeConfig.Current.backDict = HomeConfig.getDefault().backDict;
                if (HomeConfig.Current.enablePermissions == null)
                    HomeConfig.Current.enablePermissions = HomeConfig.getDefault().enablePermissions;
                if (HomeConfig.Current.enableBack == null)
                    HomeConfig.Current.enableBack = HomeConfig.getDefault().enableBack;

                api.StoreModConfig(HomeConfig.Current, "homeconfig.json");
            }
            if (HomeConfig.Current.enablePermissions == false)
            {
                ipm.AddPrivilegeToGroup("admin", BPrivilege.sethome);
                ipm.AddPrivilegeToGroup("admin", CPrivilege.home);
                ipm.AddPrivilegeToGroup("suplayer", CPrivilege.home);
                ipm.AddPrivilegeToGroup("suplayer", BPrivilege.sethome);
                ipm.AddPrivilegeToGroup("admin", DPrivilege.back);
                ipm.AddPrivilegeToGroup("suplayer", DPrivilege.back);
            }

        }

        private void OnPlayerDeath(IServerPlayer player, DamageSource damageSource)
        {
            if (HomeConfig.Current.enableBack == true)
            {
                if (HomeConfig.Current.backDict.ContainsKey(player.Entity.PlayerUID))
                {
                    HomeConfig.Current.backDict.Remove(player.Entity.PlayerUID);
                }
                HomeConfig.Current.backDict.Add(player.Entity.PlayerUID, player.Entity.Pos.AsBlockPos);
                sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
            }
        }

        private void cmd_back(IServerPlayer player, int groupId, CmdArgs args)
        {
            
            if (HomeConfig.Current.enableBack == true)
            {
                BlockPos newPos = player.Entity.Pos.AsBlockPos;
                if (HomeConfig.Current.backDict.ContainsKey(player.Entity.PlayerUID))
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Returning to your last location", Vintagestory.API.Common.EnumChatType.Notification);
                    sapi.WorldManager.LoadChunkColumnPriority(HomeConfig.Current.backDict[player.Entity.PlayerUID].X / sapi.WorldManager.ChunkSize, HomeConfig.Current.backDict[player.Entity.PlayerUID].Z / sapi.WorldManager.ChunkSize);
                    player.Entity.TeleportTo(HomeConfig.Current.backDict[player.Entity.PlayerUID].X, HomeConfig.Current.backDict[player.Entity.PlayerUID].Y, HomeConfig.Current.backDict[player.Entity.PlayerUID].Z);
                    HomeConfig.Current.backDict.Remove(player.Entity.PlayerUID);
                    HomeConfig.Current.backDict.Add(player.Entity.PlayerUID, newPos);
                    sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No back location. Use /home to create a back location.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Back is disabled. Check modconfigs/homeconfig.json to enable.", Vintagestory.API.Common.EnumChatType.Notification);
            }
            
        }

        private void cmd_sethome(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (HomeConfig.Current.homeDict.ContainsKey(player.Entity.PlayerUID))
            {
                HomeConfig.Current.homeDict.Remove(player.Entity.PlayerUID);
            }
            HomeConfig.Current.homeDict.Add(player.Entity.PlayerUID,player.Entity.Pos.AsBlockPos);
            sapi.StoreModConfig(HomeConfig.Current,"homeconfig.json");
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "New home has been saved.", Vintagestory.API.Common.EnumChatType.Notification);


        }
        private void cmd_home(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (HomeConfig.Current.enableBack == true)
            {
                if (HomeConfig.Current.backDict.ContainsKey(player.Entity.PlayerUID))
                {
                    HomeConfig.Current.backDict.Remove(player.Entity.PlayerUID);
                }
                HomeConfig.Current.backDict.Add(player.Entity.PlayerUID, player.Entity.Pos.AsBlockPos);
                sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
            }
            if (HomeConfig.Current.homeDict.ContainsKey(player.Entity.PlayerUID))
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to your saved home", Vintagestory.API.Common.EnumChatType.Notification);
                sapi.WorldManager.LoadChunkColumnPriority(HomeConfig.Current.homeDict[player.Entity.PlayerUID].X / sapi.WorldManager.ChunkSize, HomeConfig.Current.homeDict[player.Entity.PlayerUID].Z / sapi.WorldManager.ChunkSize);
                player.Entity.TeleportTo(HomeConfig.Current.homeDict[player.Entity.PlayerUID].X, HomeConfig.Current.homeDict[player.Entity.PlayerUID].Y, HomeConfig.Current.homeDict[player.Entity.PlayerUID].Z);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No Home Saved. Teleporting to world center. Use /sethome to set a home.", Vintagestory.API.Common.EnumChatType.Notification);
                sapi.WorldManager.LoadChunkColumnPriority(sapi.World.DefaultSpawnPosition.XYZInt.X, sapi.World.DefaultSpawnPosition.XYZInt.Z);
                player.Entity.TeleportTo(sapi.World.DefaultSpawnPosition.XYZInt.X,sapi.World.DefaultSpawnPosition.XYZInt.Y, sapi.World.DefaultSpawnPosition.XYZInt.Z);
            }
        }

        public class BPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /sethome and /home
            /// </summary>
            public static string sethome = "sethome";
            

        }
        public class CPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /sethome and /home
            /// </summary>
            
            public static string home = "home";

        }
        public class DPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use /sethome and /home
            /// </summary>

            public static string back = "back";

        }


        public class HomeConfig
        {
            public static HomeConfig Current { get; set; }

            public Dictionary<String,BlockPos> homeDict { get; set; }
            public Dictionary<String,BlockPos> backDict { get; set; }
            public bool? enablePermissions;
            public bool? enableBack;



            public static HomeConfig getDefault()
            {
                var config = new HomeConfig();
                BlockPos defPos = new BlockPos(0,0,0);
                bool perms = false;
                bool backperms = true;
                Dictionary<String, BlockPos> homedictionary = new Dictionary<string, BlockPos> 
                {
                    { "Default", defPos }
                };
                Dictionary<String, BlockPos> backdictionary = new Dictionary<string, BlockPos>
                {
                    { "Default", defPos }
                };
                config.homeDict = homedictionary;
                config.backDict = backdictionary;
                config.enablePermissions = perms;
                config.enableBack = backperms;
                return config;
            }


        }
            
    }
    
}
