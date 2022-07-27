using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using privileges;

namespace bunnyserverutilities.src
{
    public class bunnyserverutilities : ModSystem
    {
        //BSU variable initilization
        public ICoreServerAPI sapi; //Variable to store our server API. We assign this in startServerSide
        
        //jHome variable initialization
        Dictionary<string, BlockPos> backSave; //Dictionary to hold our /back locations
        Dictionary<string, BlockPos> homeSave; //Dictionary to hold our /home locations
        
        //GRTP variable initialization
        int? count; //Variable to check against for timing and cooldowns
        long CID; //Variable to hold our event listener for the cooldown timer
        int randx, randz; //Variables to hold our random location
        public bool loaded = false; //Tracks whether or not the current GRTP chunk is loaded
        int height; //Stores the height of the GRTP location once GRTP loads the chunk

        //

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //Start and assign APIs
            base.StartServerSide(api);
            sapi = api;
            IPermissionManager ipm = api.Permissions;
            
            //Event listerners
            api.Event.PlayerDeath += OnPlayerDeath; // /back listens for the player's death
            api.Event.SaveGameLoaded += OnSaveGameLoading; // Load our data each game load
            api.Event.GameWorldSave += OnSaveGameSaving; // Save our data each game save
            api.Event.ChunkColumnLoaded += OnChunkColumnLoaded; // /grtp and /rtp use this to check for their chunk to be loaded

            //register commands

            //home commands
            api.RegisterCommand("sethome", "Set your current position as home", " ",
                cmd_sethome, privileges.src.CPrivilege.home);
            api.RegisterCommand("home", "Teleport to your /sethome location", " ",
                cmd_home, privileges.src.CPrivilege.home);
            api.RegisterCommand("importOldHomes", "Imports homes from version 1.0.5 and earlier", " ",
                cmd_importOldHomes, Privilege.ban);

            //back commands
            api.RegisterCommand("back", "Go back to your last TP location", " ",
                cmd_back, privileges.src.DPrivilege.back);

            //spawn commands
            api.RegisterCommand("spawn", "Teleports the player to spawn", "", cmd_spawn, privileges.src.BPrivilege.spawn);

            //grtp commands
            api.RegisterCommand("grtp", "Randomly Teleports the player to a group location", "",
            cmd_grtp, privileges.src.APrivilege.grtp);

            //Register Privileges

            //Home privileges
            ipm.RegisterPrivilege("sethome", "Set your current position as home");
            ipm.RegisterPrivilege("home", "Set your current position as home");
            ipm.RegisterPrivilege("back", "Go back to your last TP location");
            ipm.RegisterPrivilege("spawn","teleport to spawn");

            //GRTP privileges
            ipm.RegisterPrivilege("grtp", "Random Teleport");

            //Check config for nulls

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
                if (bsuconfig.Current.homesImported == null)
                    bsuconfig.Current.homesImported = bsuconfig.getDefault().homesImported;
                if (bsuconfig.Current.enableSpawn == null)
                    bsuconfig.Current.enableSpawn = bsuconfig.getDefault().enableSpawn;
                if (bsuconfig.Current.cooldownminutes == null)
                    bsuconfig.Current.cooldownminutes = bsuconfig.getDefault().cooldownminutes;
                if (bsuconfig.Current.teleportradius == null)
                    bsuconfig.Current.teleportradius = bsuconfig.getDefault().teleportradius;

                api.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
            }


            //If enable permissions is false, we will give the standard groups all low-level privileges
            if (bsuconfig.Current.enablePermissions == false)
            {

                //Add grtp privileges to all standard groups
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.APrivilege.grtp);
                ipm.AddPrivilegeToGroup("admin", privileges.src.APrivilege.grtp);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.APrivilege.grtp);
                //Add spawn privileges to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.BPrivilege.spawn);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.BPrivilege.spawn);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.BPrivilege.spawn);
                //Add home privileges to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.CPrivilege.home);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.CPrivilege.home);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.CPrivilege.home);
                //Add back privileges to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.DPrivilege.back);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.DPrivilege.back);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.DPrivilege.back);
                
                
            }

            //GRTP count and event listener set at server startup
            count = bsuconfig.Current.cooldownminutes + 1; //This puts the cooldown timer as expired and will force a new GRTP location
            CID = api.Event.RegisterGameTickListener(CoolDown, 60000); //Check the cooldown timer every 1 minute
        }

        private void cmd_spawn(IServerPlayer player, int groupId, CmdArgs args) //spawn command
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if(bsuconfig.Current.enableSpawn == true)
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
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/Spawn is currently disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableSpawn = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableSpawn = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }
            
        }

        private void cmd_importOldHomes(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.homesImported == false)
            {
                if (player.Role.Code == "admin")
                {
                    if (bsuconfig.Current.homeDict != null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Importing old homes", Vintagestory.API.Common.EnumChatType.Notification);
                        homeSave.Clear();
                        for (int i = 0; i < bsuconfig.Current.homeDict.Count(); i++)
                        {
                            KeyValuePair<string, BlockPos> kvp = bsuconfig.Current.homeDict.PopOne();

                            homeSave.Add(kvp.Key, kvp.Value);
                        }
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "old homes imported", Vintagestory.API.Common.EnumChatType.Notification);
                        bsuconfig.Current.homeDict.Clear();
                        bsuconfig.Current.homeDict = null;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No valid home configs to import.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    bsuconfig.Current.homesImported = true;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You do not have permission to run this command.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }           
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Old homes have already been imported. This command should only be used once.", Vintagestory.API.Common.EnumChatType.Notification);
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
        //========//
        //COMMANDS//
        //========//

        //back command
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

        //Set Home command
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

        //home command
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

        //grtp command
        private void cmd_grtp(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (loaded == true)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location. Others can join you for " + (bsuconfig.Current.cooldownminutes - count) + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);

                        player.Entity.TeleportTo(randx, height + 2, randz);

                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Chunk is loading. Please try again in a few seconds", Vintagestory.API.Common.EnumChatType.Notification);
                        sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                    }
                    break;
                case "help":
                    displayhelp(player);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "cooldown":
                    if (player.Role.Code == "admin")
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null | cdnum == 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number greater than 0 in minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a non-negative number.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.cooldownminutes = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP cooldown has been updated to " + cdnum + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    break;
                case "radius":
                    if (player.Role.Code == "admin")
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null | cdnum < 10)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number 10 or greater.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a non-negative number.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.teleportradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP radius has been updated to " + cdnum + " blocks.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    break;
                case "now":
                    if (player.Role.Code == "admin")
                    {
                        count = bsuconfig.Current.cooldownminutes + 1;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP now issued. Please wait up to a couple minutes to take effect.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }



        }
        //============//
        //End Commands//
        //============//

        private void displayhelp(IServerPlayer player)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny's Server Utility Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            //home help
            if (bsuconfig.Current.enableHome == true)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Home Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome - Sets your location as your home teleport", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home - teleports you to your set home location", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home version - Displays the version information of Just Home", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //admin home help
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home enable - enable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home disable - disable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/importOldHomes - moves saved homes from jhome 1.05 and earlier to the new save type. Run this only once if you are updating to this mod from 1.0.5 or earlier.", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //back help
            if (bsuconfig.Current.enableBack == true)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Back Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back - return to the last place you used /home, /back or died", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //admin back help
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back enable - enable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back disable - disable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //spawn help
            if (bsuconfig.Current.enableSpawn == true)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Spawn Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn - return to the server set spawn point", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //admin spawn help
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn enable - enable the /spawn command", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn disable - disable the /spawn command", Vintagestory.API.Common.EnumChatType.Notification);
            }
            //grtp help / needs enable/disable added
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Group Random Teleport Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP location updates every " + bsuconfig.Current.cooldownminutes + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport Radius: " + bsuconfig.Current.teleportradius + " Blocks.", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp - teleports the player to the group teleport point", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp version - displays the current version of GRTP installed", Vintagestory.API.Common.EnumChatType.Notification);
            if (player.Role.Code == "admin")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp cooldown <i>number</i> - sets the cooldown timer", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp radius <i>number</i> - sets the teleport radius", Vintagestory.API.Common.EnumChatType.Notification);
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp now - Sets the GRTP to update on the next 1 minute tick", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }
        //========================//
        //Event Listener Functions//
        //========================//

        private void CoolDown(float ct)
        {
            if (count >= bsuconfig.Current.cooldownminutes)
            {
                count = 0;

                int radius = bsuconfig.Current.teleportradius ?? default(int);
                int worldx = sapi.WorldManager.MapSizeX;
                int worldz = sapi.WorldManager.MapSizeZ;
                int rawxmin = (worldx / 2) - radius;
                int rawxmax = (worldx / 2) + radius;
                int rawzmin = (worldz / 2) - radius;
                int rawzmax = (worldz / 2) + radius;
                randx = sapi.World.Rand.Next(rawxmin, rawxmax);
                randz = sapi.World.Rand.Next(rawzmin, rawzmax);
                loaded = false;
                sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);

            }
            else
            {
                count = count + 1;
            }
        }

        private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if ((randx / sapi.WorldManager.ChunkSize == chunkCoord.X) & (randz / sapi.WorldManager.ChunkSize == chunkCoord.Y))
            {
                BlockPos checkheight = new BlockPos();
                checkheight.X = randx;
                checkheight.Y = 1;
                checkheight.Z = randz;
                height = sapi.World.BlockAccessor.GetTerrainMapheightAt(checkheight);
                if (loaded == false)
                {
                    sapi.BroadcastMessageToAllGroups("New /grtp coordinates generated. New location will be available in " + bsuconfig.Current.cooldownminutes + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);

                }

                loaded = true;
            }

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
        //===========//
        //Config file//
        //===========//

        public class bsuconfig
        {
            public static bsuconfig Current { get; set; }

            //enable/disable properties
            public bool? enableBack;
            public bool? enableHome;
            public bool? enableSpawn;

            //jhome properties
            public Dictionary<String,BlockPos> homeDict { get; set; }//Must be preserved to pull old homes to the new save
            public bool? enablePermissions;
            public bool? homesImported;


            //grtp properties
            public int? teleportradius;
            public int? cooldownminutes;



            public static bsuconfig getDefault()
            {
                var config = new bsuconfig();
                BlockPos defPos = new BlockPos(0,0,0);
                bool perms = false;


                //jHome default assignments
                Dictionary<String, BlockPos> homedictionary = null;
                config.homeDict = homedictionary;//Must be preserved to pull old homes to the new save
                config.enablePermissions = perms;
                config.homesImported = false;

                //enable/disable module defaults
                config.enableBack = true;
                config.enableHome = true;
                config.enableSpawn = true;

                //grtp module defaults
                config.cooldownminutes = 60;
                config.teleportradius = 100000;

                return config;
            }


        }
            
    }
    
}
