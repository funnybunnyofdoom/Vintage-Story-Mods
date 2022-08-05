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
        int count; //Variable to check against for timing and cooldowns
        public Dictionary<string, Dictionary<string, int>> cooldownDict = new Dictionary<string, Dictionary<string, int>>(); //dictionary to hold mod cooldown lists
        IPermissionManager ipm;


        //jHome variable initialization
        Dictionary<string, BlockPos> backSave; //Dictionary to hold our /back locations
        Dictionary<string, BlockPos> homeSave; //Dictionary to hold our /home locations

        //GRTP variable initialization

        int? grtptimer;
        long CID; //Variable to hold our event listener for the cooldown timer
        int randx, randz; //Variables to hold our random location
        public bool loaded = false; //Tracks whether or not the current GRTP chunk is loaded
        int height; //Stores the height of the GRTP location once GRTP loads the chunk

        //Join Announce Initialization
        List<IServerPlayer> joinedPlayers = new List<IServerPlayer>(); //Holds players names between joining for the first time and being loaded into the game

        //Rising Sun Initialization
        List<IServerPlayer> rsjoinedPlayers = new List<IServerPlayer>(); //Holds players names between joining for the first time and being loaded into the game

        //Simple Server Message initialization
        int messageplace = 0;
        int ssmtimer = 0; //sets the SSM cooldown timer at 0

        //Bunny Bell Initilization
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell");
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server; //load on the server side
        }

        //Random Teleport Initialization
        public EntityPlayer GEntity;
        public IServerPlayer Splayer;
        public IServerChunk SChunk;
        public BlockPos cblockpos;
        int rtprandx, rtprandz = 0;
        bool teleporting = false;
        int cooldowntimer;

        public override void StartServerSide(ICoreServerAPI api)
        {
            //Start and assign APIs
            base.StartServerSide(api);
            sapi = api;
            ipm = api.Permissions;
            //Event listerners
            api.Event.PlayerDeath += OnPlayerDeath; // /back listens for the player's death
            api.Event.SaveGameLoaded += OnSaveGameLoading; // Load our data each game load
            api.Event.GameWorldSave += OnSaveGameSaving; // Save our data each game save
            api.Event.ChunkColumnLoaded += OnChunkColumnLoaded; // /grtp and /rtp use this to check for their chunk to be loaded
            api.Event.PlayerCreate += OnPlayerCreate; // Used by join announce and rising sun to track new players
            api.Event.PlayerNowPlaying += onNowPlaying; // Used by join announce and rising sun to tell when players are loaded into the game
            api.Event.PlayerChat += onPlayerChat; // Used by BunnyBell to read in player chat and check for names

            //=================//
            //register commands//
            //=================//

            //Bunny Server Utilities Commands
            api.RegisterCommand("bsu", "Bunny Server utilities", "[help | Version]",
                cmd_bsu);
            api.RegisterCommand("bunnyServerUtilities", "Bunny Server utilities", "[help | Version]",
                cmd_bsu);
            api.RegisterCommand("bunnyServerUtility", "Bunny Server utilities", "[help | Version]",
                cmd_bsu);
            api.RegisterCommand("removedeny", "removes a privilege denial", "/removedeny <i>playername privilege</i>",
                cmd_removedeny, Privilege.controlserver);
            api.RegisterCommand("warn", "Issues a warning for a player", "/warn <i>playername reason</i>",
                cmd_warn, privileges.src.IPrivilege.warn);

            //home commands
            api.RegisterCommand("sethome", "Set your current position as home", " ",
                cmd_sethome, privileges.src.CPrivilege.home);
            api.RegisterCommand("home", "Teleport to your /sethome location", " ",
                cmd_home, privileges.src.CPrivilege.home);
            api.RegisterCommand("importOldHomes", "Imports homes from version 1.0.5 and earlier", " ",
                cmd_importOldHomes, Privilege.controlserver);

            //back commands
            api.RegisterCommand("back", "Go back to your last TP location", " ",
                cmd_back, privileges.src.DPrivilege.back);

            //spawn commands
            api.RegisterCommand("spawn", "Teleports the player to spawn", "", cmd_spawn, privileges.src.BPrivilege.spawn);

            //grtp commands
            api.RegisterCommand("grtp", "Randomly Teleports the player to a group location", "",
            cmd_grtp, privileges.src.APrivilege.grtp);

            //Join Announce Commands
            api.RegisterCommand("joinannounce", "Announces a new player to the server when they join", "[help | enable | disable]", cmd_joinannounce, Privilege.controlserver);

            //Rising Sun Commands
            api.RegisterCommand("rs", "Rising Sun configuration", "[dawn|dusk|help|version]", cmd_rs, Privilege.controlserver);

            //Just Private Message commands
            api.RegisterCommand("jpm", "Simple Server Message Management", "[help | enable | disable]", cmd_jpm, privileges.src.EPrivilege.jpmadmin); //Register the /jpm command for admins
            api.RegisterCommand("dm", "Private Message", " ", cmd_pm, privileges.src.EPrivilege.jpm);

            //Simple Server Message commands
            api.RegisterCommand("ssm", "Simple Server Message Management", "[add|remove|list|frequency|now|help|version]", cmd_ssm, privileges.src.FPrivilege.ssm);

            //Teleport To Commands
            api.RegisterCommand("tpt", "Teleports the player to another player", "",
                cmd_tpt, privileges.src.GPrivilege.tpt);
            api.RegisterCommand("tpaccept", "Teleports the player to another player", "",
                cmd_tpaccept, privileges.src.GPrivilege.tpt);
            api.RegisterCommand("tpdeny", "Teleports the player to another player", "",
                cmd_tpdeny, privileges.src.GPrivilege.tpt);

            //Bunny Bell Commands
            api.RegisterCommand("bb", "Bunny Bell configuration", "[help|enable|disable]", cmd_bb, Privilege.controlserver);

            //Random Teleport Commands
            api.RegisterCommand("rtp", "Randomly Teleports the player", "[rtp|help|cooldown|enable|disable]",
            cmd_rtp, privileges.src.HPrivilege.rtp);

            //===================//
            //Register Privileges//
            //===================//

            //Home privileges
            ipm.RegisterPrivilege("sethome", "Set your current position as home");
            ipm.RegisterPrivilege("home", "Set your current position as home");
            ipm.RegisterPrivilege("back", "Go back to your last TP location");
            ipm.RegisterPrivilege("spawn", "teleport to spawn");

            //Group Random Teleport privileges
            ipm.RegisterPrivilege("grtp", "Random Teleport");

            //Just Random Teleport privileges
            ipm.RegisterPrivilege("jpm", "Private Messages");//Register the privilege for general private messages
            ipm.RegisterPrivilege("jpmadmin", "JPM management"); //Register the privilege for admin control

            //Simple Server message privileges
            ipm.RegisterPrivilege("ssm", "Simple Server Messages");

            //Teleport To privileges
            ipm.RegisterPrivilege("tpt", "Teleport To");

            //Random Teleport Privileges
            ipm.RegisterPrivilege("rtp", "Random Teleport");

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
                if (bsuconfig.Current.enableGrtp == null)
                    bsuconfig.Current.enableGrtp = bsuconfig.getDefault().enableGrtp;
                if (bsuconfig.Current.grtpPlayerCooldown == null)
                    bsuconfig.Current.grtpPlayerCooldown = bsuconfig.getDefault().grtpPlayerCooldown;
                if (bsuconfig.Current.homePlayerCooldown == null)
                    bsuconfig.Current.homePlayerCooldown = bsuconfig.getDefault().homePlayerCooldown;
                if (bsuconfig.Current.backPlayerCooldown == null)
                    bsuconfig.Current.backPlayerCooldown = bsuconfig.getDefault().backPlayerCooldown;
                if (bsuconfig.Current.spawnPlayerCooldown == null)
                    bsuconfig.Current.spawnPlayerCooldown = bsuconfig.getDefault().spawnPlayerCooldown;
                if (bsuconfig.Current.enableJoinAnnounce == null)
                    bsuconfig.Current.enableJoinAnnounce = bsuconfig.getDefault().enableJoinAnnounce;
                if (bsuconfig.Current.enableBunnyBell == null)
                    bsuconfig.Current.enableBunnyBell = bsuconfig.getDefault().enableBunnyBell;
                if (bsuconfig.Current.enablejpm == null)
                    bsuconfig.Current.enablejpm = bsuconfig.getDefault().enablejpm;
                if (bsuconfig.Current.enablejrtp == null)
                    bsuconfig.Current.enablejrtp = bsuconfig.getDefault().enablejrtp;
                if (bsuconfig.Current.enableRisingSun == null)
                    bsuconfig.Current.enableRisingSun = bsuconfig.getDefault().enableRisingSun;
                if (bsuconfig.Current.enableSimpleServerMessages == null)
                    bsuconfig.Current.enableSimpleServerMessages = bsuconfig.getDefault().enableSimpleServerMessages;
                if (bsuconfig.Current.enabletpt == null)
                    bsuconfig.Current.enabletpt = bsuconfig.getDefault().enabletpt;
                if (bsuconfig.Current.dawn == null)
                    bsuconfig.Current.dawn = bsuconfig.getDefault().dawn;
                if (bsuconfig.Current.dusk == null)
                    bsuconfig.Current.dusk = bsuconfig.getDefault().dusk;
                if (bsuconfig.Current.messages == null)
                    bsuconfig.Current.messages = bsuconfig.getDefault().messages;
                if (bsuconfig.Current.frequency == null)
                    bsuconfig.Current.frequency = bsuconfig.getDefault().frequency;
                if (bsuconfig.Current.tptDict == null)
                    bsuconfig.Current.tptDict = bsuconfig.getDefault().tptDict;
                if (bsuconfig.Current.waitDict == null)
                    bsuconfig.Current.waitDict = bsuconfig.getDefault().waitDict;
                if (bsuconfig.Current.rtpradius == null)
                    bsuconfig.Current.rtpradius = bsuconfig.getDefault().rtpradius;
                if (bsuconfig.Current.cooldownDict == null)
                    bsuconfig.Current.cooldownDict = bsuconfig.getDefault().cooldownDict;
                if (bsuconfig.Current.cooldownduration == null)
                    bsuconfig.Current.cooldownduration = bsuconfig.getDefault().cooldownduration;
                if (bsuconfig.Current.warningDict == null)
                    bsuconfig.Current.warningDict = bsuconfig.getDefault().warningDict;
                if (bsuconfig.Current.enablejoinmessage == null)
                    bsuconfig.Current.enablejoinmessage = bsuconfig.getDefault().enablejoinmessage;
                api.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
            }

            //Old Home config used to pull homes for new config
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
                //add back privileges to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.EPrivilege.jpm);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.EPrivilege.jpm);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.EPrivilege.jpm);
                //add Simple Server Message permissions to ADMIN ONLY:
                ipm.AddPrivilegeToGroup("admin", privileges.src.FPrivilege.ssm);
                //add Teleport To permissions to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.GPrivilege.tpt);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.GPrivilege.tpt);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.GPrivilege.tpt);
                //Add Random teleport permissions to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.HPrivilege.rtp);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.HPrivilege.rtp);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.HPrivilege.rtp);

                //Verify admin permissions are not avaialble for default groups
                ipm.RemovePrivilegeFromGroup("suplayer", privileges.src.EPrivilege.jpmadmin);
                ipm.RemovePrivilegeFromGroup("doplayer", privileges.src.EPrivilege.jpmadmin);
                ipm.RemovePrivilegeFromGroup("suplayer", privileges.src.FPrivilege.ssm);
                ipm.RemovePrivilegeFromGroup("doplayer", privileges.src.FPrivilege.ssm);
                //add /warn permissions to ADMIN ONLY:
                ipm.AddPrivilegeToGroup("admin", privileges.src.IPrivilege.warn);
            }

            //GRTP count and event listener set at server startup
            grtptimer = 0; //This puts the cooldown timer as expired and will force a new GRTP location
            count = (int)bsuconfig.Current.cooldownminutes;//grtp cooldown timer
            CID = api.Event.RegisterGameTickListener(CoolDown, 60000); //Check the cooldown timer every 1 minute
            int broadcastFrequency = (int)bsuconfig.Current.frequency; //SSM cooldown timer

        }



        //========//
        //COMMANDS//
        //========//

        //back command
        private void cmd_back(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "back";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableBack == true)
                    {
                        Action<IServerPlayer> a = (IServerPlayer) => backteleport(player);
                        checkCooldown(player, cmdname, a, bsuconfig.Current.backPlayerCooldown);

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
                    displayhelp(player, cmdname);
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
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
            string cmdname = "home";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableHome == true)
                    {
                        Action<IServerPlayer> a = (IServerPlayer) => homeTeleport(player);
                        checkCooldown(player, cmdname, a, bsuconfig.Current.homePlayerCooldown);

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
                    displayhelp(player, cmdname);
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
            }
        }

        //grtp command
        private void cmd_grtp(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "grtp";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableGrtp == true)
                    {
                        if (randx != 0 & randz != 0)
                        {
                            if (loaded == true)
                            {
                                Action<IServerPlayer> a = (IServerPlayer) => grtpteleport(player);
                                checkCooldown(player, cmdname, a, bsuconfig.Current.grtpPlayerCooldown);
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Chunk is loading. Please try again in a few seconds", Vintagestory.API.Common.EnumChatType.Notification);
                                sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                            }
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP has not been set yet. Did the server just start?", Vintagestory.API.Common.EnumChatType.Notification);
                        }

                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Grtp is disabled. an admin must use /grtp enable to enable this command.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
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
                        grtptimer = 0 - (int)bsuconfig.Current.cooldownminutes;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP now issued. Please wait up to a couple minutes to take effect.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableGrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableGrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
            }
        }

        //Bunnys Server Utility command
        private void cmd_bsu(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player, "all");
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
            }
        }
        //spawn command
        private void cmd_spawn(IServerPlayer player, int groupId, CmdArgs args) //spawn command
        {
            string cmdname = "spawn";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableSpawn == true)
                    {
                        Action<IServerPlayer> a = (IServerPlayer) => spawnTeleport(player);
                        checkCooldown(player, cmdname, a, bsuconfig.Current.spawnPlayerCooldown);

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
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
            }

        }
        //Import Old Homes Command
        private void cmd_importOldHomes(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.homesImported == false)
            {
                if (player.Role.Code == "admin")
                {
                    if (HomeConfig.Current.homeDict != null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Importing old homes", Vintagestory.API.Common.EnumChatType.Notification);
                        homeSave.Clear();
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Size of Home Config: " + HomeConfig.Current.homeDict.Count(), Vintagestory.API.Common.EnumChatType.Notification);
                        int configsize = HomeConfig.Current.homeDict.Count();
                        for (int i = 0; i < configsize; i++)
                        {
                            KeyValuePair<string, BlockPos> kvp = HomeConfig.Current.homeDict.PopOne();

                            homeSave.Add(kvp.Key, kvp.Value);

                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (i + 1) + "/" + configsize + ":Player: " + kvp.Key + " " + kvp.Value, Vintagestory.API.Common.EnumChatType.Notification);

                        }
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "old homes imported", Vintagestory.API.Common.EnumChatType.Notification);
                        //HomeConfig.Current.homeDict.Clear();
                        //HomeConfig.Current.homeDict = null;
                        //sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
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

        private void cmd_joinannounce(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "joinannounce";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "[help | Enable | disable]", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableJoinAnnounce = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Join Announce has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableJoinAnnounce = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Join Announce has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }
        }

        //rising sun command
        private void cmd_rs(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "risingsun";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "dawn":
                    if (player.Role.Code == "admin")
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum < 1 | cdnum > 23)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter an hour between 1 and 23.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum > bsuconfig.Current.dusk)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number smaller than dusk: " + bsuconfig.Current.dusk, Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter an hour between 1 and 23.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.dawn = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "dawn time has been updated to " + cdnum + ":00", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    break;
                case "dusk":
                    if (player.Role.Code == "admin")
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum < 1 | cdnum > 23)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter an hour between 1 and 23.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < bsuconfig.Current.dawn)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number larger than dawn: " + bsuconfig.Current.dawn, Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter an hour between 1 and 23.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.dusk = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "dusk time has been updated to " + cdnum + ":00", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableRisingSun = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Rising Sun has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableRisingSun = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Rising Sun has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /rs dawn|dusk|help|enable|disable", Vintagestory.API.Common.EnumChatType.Notification);

                    break;
            }
        }

        //private message player command
        private void cmd_pm(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.enablejpm == true)
            {
                string cmdname = "jpm";
                string cmd = args.PopWord();
                IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
                if (cmd != "" & cmd != null & cmd != "help")
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
                            sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                            sapi.SendMessage(sapi.World.PlayerByUid(player.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " to " + pdata.LastKnownPlayername + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                }
                else if (cmd == "help")
                {
                    displayhelp(player, cmdname);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please include a player name.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }


        }

        //Private Message admin commands
        private void cmd_jpm(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "jpm";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enablejpm = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/dm has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enablejpm = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/dm has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }
        }

        private void cmd_ssm(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "ssm";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "add":
                    string text = args.PopAll();
                    bsuconfig.Current.messages.Add(text);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Added message.", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "remove":
                    int? listindex = args.PopInt();
                    if (listindex != null)
                    {
                        int lindex = (int)listindex;

                        List<string> msglist = bsuconfig.Current.messages;
                        if (msglist.Count >= lindex)
                        {
                            string removemsg = msglist.ElementAt(lindex);
                            bsuconfig.Current.messages.Remove(removemsg);
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Removed message", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please use /ssm list and then use /ssm remove number to delete the message", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please use /ssm remove aNumber to remove a message", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "list":
                    List<string> listofmessages = bsuconfig.Current.messages;
                    int lastindex = listofmessages.Count;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "List of current server messages:", Vintagestory.API.Common.EnumChatType.Notification);
                    for (int i = 0; i < lastindex; i++)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i + " : " + listofmessages[i], Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "frequency":
                    int? frqnum = args.PopInt();
                    if (frqnum != null & frqnum >= 1)
                    {
                        bsuconfig.Current.frequency = frqnum;
                        sapi.StoreModConfig(bsuconfig.Current, "ssmconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Broadcast Message Frequency set to " + frqnum + " Minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number in minutes", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "now":
                    broadcast();
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /ssm help|add|remove|list|frequency|now|enable|disable", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableSimpleServerMessages = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Server Messages have been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableSimpleServerMessages = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Server Messages have been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;

            }
        }

        //Teleport to deny
        private void cmd_tpdeny(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.enabletpt == true)
            {
                if (bsuconfig.Current.waitDict.ContainsKey(player.PlayerUID))
                {
                    String value;
                    bsuconfig.Current.waitDict.TryGetValue(player.PlayerUID, out value);
                    string tpPlayer = value;
                    sapi.SendMessage(sapi.World.PlayerByUid(tpPlayer), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your teleport request has been denied.", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport To denied.", Vintagestory.API.Common.EnumChatType.Notification);
                    bsuconfig.Current.waitDict.Remove(player.PlayerUID);
                    bsuconfig.Current.tptDict.Remove(tpPlayer);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You have no active requests to deny.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }
        //teleport to accept
        private void cmd_tpaccept(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.enabletpt == true)
            {
                if (bsuconfig.Current.waitDict.ContainsKey(player.PlayerUID))
                {

                    String value;
                    bsuconfig.Current.waitDict.TryGetValue(player.PlayerUID, out value);
                    String tpPlayer = value;
                    sapi.SendMessage(sapi.World.PlayerByUid(tpPlayer), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your teleport request has been accepted. Stand by while you are teleported.", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport To accepted.", Vintagestory.API.Common.EnumChatType.Notification);
                    //Add BACK here
                    EntityPlayer tpserverplayer = sapi.World.PlayerByUid(tpPlayer).WorldData.EntityPlayer;
                    tpserverplayer.TeleportTo(player.Entity.Pos.AsBlockPos);
                    bsuconfig.Current.waitDict.Remove(player.PlayerUID);
                    bsuconfig.Current.tptDict.Remove(tpPlayer);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You have no active requests to accept.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }
        //teleport to
        private void cmd_tpt(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "tpt";
            string cmd = args.PopWord();
            if (cmd != null & cmd != "help" & cmd != "enable" & cmd != "disable")
            {
                if (bsuconfig.Current.enabletpt == true)
                {
                    IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
                    if (pdata == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player could not be found. Please check your spelling and try again.", Vintagestory.API.Common.EnumChatType.Notification);
                        return;
                    }


                    if (bsuconfig.Current.tptDict.ContainsKey(player.PlayerUID) == false)
                    {

                        if (bsuconfig.Current.waitDict.ContainsKey(pdata.PlayerUID) == false)
                        {
                            tptinfo info = new tptinfo();
                            info.toplayer = pdata.PlayerUID;
                            info.haspermission = false;
                            info.waiting = true;
                            info.timer = count;
                            bsuconfig.Current.tptDict.Add(player.PlayerUID, info);
                            bsuconfig.Current.waitDict.Add(pdata.PlayerUID, player.PlayerUID);
                            sapi.StoreModConfig(bsuconfig.Current, "tptconfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Stand by. You will be teleported when the other player accepts the teleport", Vintagestory.API.Common.EnumChatType.Notification);
                            setbackteleport(player);
                            sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, player.PlayerName + " would like to teleport to you. Please type /tpaccept to accept.", Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "TPT is disabled by admin", Vintagestory.API.Common.EnumChatType.Notification);
                }

            }
            else if (cmd == "help")
            {
                displayhelp(player, cmdname);
            }
            else if (cmd == "enable")
            {
                if (player.Role.Code == "admin")
                {
                    bsuconfig.Current.enabletpt = true;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport To Player has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "disable")
            {
                bsuconfig.Current.enabletpt = false;
                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport To Player has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                if (bsuconfig.Current.enabletpt == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter the name of the player you would like to teleport to.", Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "TPT is disabled by admin.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }

        private void cmd_bb(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "bb";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /bb [help|enable|disable]", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableBunnyBell = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny Bell has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enableBunnyBell = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny Bell has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }
        }

        private void cmd_rtp(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "rtp";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "cooldown":
                    if (player.Role.Code == "admin")
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number 0 or higher in minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a non-negative number.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.cooldownduration = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "RTP cooldown timer set to " + bsuconfig.Current.cooldownduration + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You do not have permission to use this command.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case null:
                    if (bsuconfig.Current.enablejrtp == true)
                    {
                        ICoreServerAPI api = sapi; //get the server api
                        Splayer = player;
                        GEntity = player.Entity; //assign the entity to global variable
                        IWorldManagerAPI world = api.WorldManager;
                        System.Diagnostics.Debug.Write(count);
                        if (bsuconfig.Current.cooldownDict.ContainsKey(player.PlayerUID) == false & teleporting == false)
                        {
                            setbackteleport(player);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait while destination chunks are loaded.", Vintagestory.API.Common.EnumChatType.Notification);
                            int radius = bsuconfig.Current.rtpradius ?? default(int);
                            int worldx = world.MapSizeX;
                            int worldz = world.MapSizeZ;
                            int rawxmin = (worldx / 2) - radius;
                            int rawxmax = (worldx / 2) + radius;
                            int rawzmin = (worldz / 2) - radius;
                            int rawzmax = (worldz / 2) + radius;
                            rtprandx = GEntity.World.Rand.Next(rawxmin, rawxmax);
                            rtprandz = GEntity.World.Rand.Next(rawzmin, rawzmax);
                            world.LoadChunkColumnPriority(rtprandx / sapi.WorldManager.ChunkSize, rtprandz / sapi.WorldManager.ChunkSize);

                            teleporting = true;
                            bsuconfig.Current.cooldownDict.Add(player.PlayerUID, cooldowntimer);
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        }
                        else if (teleporting == true)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your chunks are still being generated, please be patient.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (bsuconfig.Current.cooldownDict.ContainsKey(player.PlayerUID) == true & teleporting == false)
                        {
                            int values;
                            bsuconfig.Current.cooldownDict.TryGetValue(player.PlayerUID, out values);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You can teleport again in " + ((values + bsuconfig.Current.cooldownduration) - cooldowntimer) + " minutes", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "JRTP is disabled by admin.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enablejrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Random Teleport has been enabled", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin")
                    {
                        bsuconfig.Current.enablejrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Random Teleport has been disabled", Vintagestory.API.Common.EnumChatType.Notification);
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
                            bsuconfig.Current.rtpradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "RTP radius has been updated to " + cdnum + " blocks.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    break;
            }
        }

        private void cmd_removedeny(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            string cmd2 = args.PopWord();
            IServerPlayerData targetplayer = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
            if (targetplayer != null)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Removing denial of " + cmd2 + " from " + cmd + ".", Vintagestory.API.Common.EnumChatType.Notification);
                ipm.RemovePrivilegeDenial(targetplayer.PlayerUID, cmd2);

            }

        }

        //warning command
        private void cmd_warn(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "warn";
            string cmd = args.PopWord();
            if (cmd != "list" & cmd != null)
            {
                IServerPlayerData targetplayer = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
                if (targetplayer != null)
                {
                    string warnReason = args.PopAll();
                    if (bsuconfig.Current.warningDict.ContainsKey(targetplayer.PlayerUID))
                    {
                        userwarning uwd;
                        bsuconfig.Current.warningDict.TryGetValue(targetplayer.PlayerUID, out uwd);
                        uwd.warnings++;
                        uwd.reasons.Add(warnReason);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player: " + targetplayer.LastKnownPlayername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + uwd.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Warnings: " + uwd.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Reasons: ", Vintagestory.API.Common.EnumChatType.Notification);
                        for (int i = 0; i < uwd.reasons.Count; i++)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i + 1 + ": " + uwd.reasons[i], Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        bsuconfig.Current.warningDict.Remove(targetplayer.PlayerUID);
                        bsuconfig.Current.warningDict.Add(targetplayer.PlayerUID, uwd);
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    }
                    else
                    {
                        userwarning uwd = new userwarning();
                        uwd.playeruid = targetplayer.PlayerUID;
                        uwd.playername = targetplayer.LastKnownPlayername;
                        uwd.warnings = 1;
                        uwd.reasons.Add(warnReason);
                        IServerPlayer[] pdata = sapi.Server.Players;
                        for (int i = 0; i < pdata.Length; i++)
                        {
                            IServerPlayer splayer = pdata[i];
                            if (splayer.PlayerUID == targetplayer.PlayerUID)
                            {
                                uwd.ipaddress = splayer.IpAddress;
                            }
                        }
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player: " + targetplayer.LastKnownPlayername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + uwd.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Warnings: " + uwd.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Reasons: ", Vintagestory.API.Common.EnumChatType.Notification);
                        for (int i = 0; i < uwd.reasons.Count; i++)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i + 1 + ": " + uwd.reasons[i], Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        bsuconfig.Current.warningDict.Add(targetplayer.PlayerUID, uwd);
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    }
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player not found", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "list")
            {
                int? listnum = args.PopInt();
                Dictionary<String, userwarning> uswd = new Dictionary<String, userwarning>();
                uswd = bsuconfig.Current.warningDict;
                if (listnum == null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Use /warn list num to see the warnings for that player", Vintagestory.API.Common.EnumChatType.Notification);
                    for (var i = 1; i < uswd.Count; i++)
                    {
                        userwarning UW = uswd.ElementAt(i).Value;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i+") Player: " + UW.playername+ " | Warnings: "+UW.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                else
                {
                    if (listnum != null & listnum >0 & listnum < uswd.Count)
                    {
                        userwarning UW = uswd.ElementAt((int)listnum).Value;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Player: " + UW.playername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + UW.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Warnings: " + UW.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Reasons: ", Vintagestory.API.Common.EnumChatType.Notification);
                        for (int j = 0; j < UW.reasons.Count; j++)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, j + 1 + ": " + UW.reasons[j], Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number between 1 and "+ (uswd.Count - 1), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/warn playername reason | /warn list", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        //=============//
        //Help Function//
        //=============//

        private void displayhelp(IServerPlayer player, string helpType = "all")
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny's Server Utility Commands:", Vintagestory.API.Common.EnumChatType.Notification);
            if (helpType != "all")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "-Use /bsu help to see help for all Bunny Server utility commands", Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "-Use /bsu version for mod version", Vintagestory.API.Common.EnumChatType.Notification);
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/removedeny <i>playername privilege</i> - remove a privilege denial from a player", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            //BSU help
            //home help
            if (helpType == "home" || helpType == "all")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Home Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableHome == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome - Sets your location as your home teleport", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home - teleports you to your set home location", Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home is disabled by admin", Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin home help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home enable - enable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home disable - disable the /home command", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/importOldHomes - moves saved homes from jhome 1.05 and earlier to the new save type. Run this only once if you are updating to this mod from 1.0.5 or earlier.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //back help
            if (helpType == "back" || helpType == "all")
            {
                //back help
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Back Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableBack == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back - return to the last place you used /home, /back or died", Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back is disabled by admin", Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin back help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back enable - enable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/back disable - disable the /back command", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //spawn help
            if (helpType == "spawn" || helpType == "all")
            {
                //spawn help
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "---Spawn Commands---", Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableSpawn == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn - return to the server set spawn point", Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn is disabled by admin", Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin spawn help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn enable - enable the /spawn command", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/spawn disable - disable the /spawn command", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //grtp help
            if (helpType == "grtp" || helpType == "all")
            {
                //grtp help

                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Group Random Teleport Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableGrtp == true)
                {
                    if (helpType == "grtp")
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP location updates every " + bsuconfig.Current.cooldownminutes + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport Radius: " + bsuconfig.Current.teleportradius + " Blocks.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp - teleports the player to the group teleport point", Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp is disabled by admin", Vintagestory.API.Common.EnumChatType.Notification);
                }
                //grtp admin help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP Admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp cooldown <i>number</i> - sets the cooldown timer", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp radius <i>number</i> - sets the teleport radius", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp now - Sets the GRTP to update on the next 1 minute tick", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp enable - enables the GRTP module", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp disable - disables the GRTP module", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Join Announce help
            if (helpType == "joinannounce" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Join Announce Admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/joinannounce enable - enables sending a message to players the first time they join", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/joinannounce disable - disables sending a message to players the first time they join", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Rising Sun help
            if (helpType == "risingsun" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "rising sun admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs dawn <i>number</i> - Sets the hour Rising Sun will advance the night to", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs dusk <i>number</i> - Sets the hour that Rising Sun considers Night", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs enable - Turns on Rising Sun", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rs disable - Turns off Rising Sun", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Just Private Message help
            if (helpType == "jpm" || helpType == "all")
            {
                if (bsuconfig.Current.enablejpm == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Just Private Message Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/dm <i>playername messageToPlayer</i> - sends a message to a player ", Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/jpm enable - enables private messages", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/jpm disable - disables private messages", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Simple Server Message help
            if (helpType == "ssm" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Simple Server messages Admin Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm add <i>Server Message</i> - adds a message to the list of server messages", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm list - lists the existing server messages ", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm remove <i>number from /ssm list</i> - remove the message from the number in the list", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm frequency <i>number in minutes</i> - changes the duration of minutes between messages", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm now - broadcasts the next server message in line", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm enable - enables SSM", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm disable - disables SSM", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Simple Teleport To help
            if (helpType == "tpt" || helpType == "all")
            {
                if (bsuconfig.Current.enabletpt == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/tpt playername - Asks a player if you can teleport to them", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/tpaccept - accepts a tpt request and teleports player to you", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/tpdeny - denies a tpt request", Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/tpt enable - enables tpt", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/tpt disable - disables tpt", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //BunnyBell help
            if (helpType == "bb" || helpType == "all")
            {
                if (bsuconfig.Current.enableBunnyBell == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Bunny bell creates a -ding- when your name is typed", Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb enable - enables Bunny Bell", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/bb disable - disables Bunny Bell", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Random Teleport Help
            if (helpType == "rtp" || helpType == "all")
            {
                if (bsuconfig.Current.enableBunnyBell == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "RTP Commands:", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rtp - Teleports the player to a random location", Vintagestory.API.Common.EnumChatType.Notification);
                }

                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rtp cooldown <i>number</i> - Sets the cooldown timer in minutes", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rtp enable - enables Random Teleport", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/rtp disable - disables Random Teleport", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

        }


        //===============//
        //other functions//
        //===============//
        private void setbackteleport(IServerPlayer player)
        {
            if (bsuconfig.Current.enableBack == true)
            {
                if (backSave.ContainsKey(player.PlayerUID))
                {
                    backSave.Remove(player.PlayerUID);
                }

                backSave.Add(player.PlayerUID, player.Entity.Pos.AsBlockPos);
            }
        }
        
        private void grtpteleport(IServerPlayer player)
        {
            if (bsuconfig.Current.cooldownminutes - (count - grtptimer) <= 0)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location. A new location will be generated within 1 minute", Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location. Others can join you for " + (bsuconfig.Current.cooldownminutes - (count - grtptimer)) + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
            }
            setbackteleport(player);
            player.Entity.TeleportTo(randx, height + 2, randz);
        }

        private void homeTeleport(IServerPlayer player)
        {
            setbackteleport(player);
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

        private void spawnTeleport(IServerPlayer player)
        {
            setbackteleport(player);
            EntityPlayer byEntity = player.Entity; //Get the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You are teleporting to spawn", Vintagestory.API.Common.EnumChatType.Notification);
            EntityPos spawnpoint = byEntity.World.DefaultSpawnPosition;
            byEntity.TeleportTo(spawnpoint);
        }

        private void backteleport(IServerPlayer player)
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

        //Set player's cooldown for (player, cooldown time, mod cmd name)
        //You must add the mod name to the if cmd == statement to update cooldowns
        private void setplayercooldown(IServerPlayer player, int? cdnum, string cmd)
        {
            if (player.Role.Code == "admin")
            {
                //int? cdnum = args.PopInt();
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
                    if (cmd == "spawn")
                    {
                        bsuconfig.Current.spawnPlayerCooldown = cdnum;
                    }
                    else if (cmd == "home")
                    {
                        bsuconfig.Current.homePlayerCooldown = cdnum;
                    }
                    else if (cmd == "back")
                    {
                        bsuconfig.Current.backPlayerCooldown = cdnum;
                    }
                    else if (cmd == "grtp")
                    {
                        bsuconfig.Current.grtpPlayerCooldown = cdnum;
                    }


                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, cmd + " cooldown has been updated to " + cdnum + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }

        private void checkCooldown(IServerPlayer player, string cmdname, Action<IServerPlayer> function, int? modPlayerCooldown)
        {
            int playersactivecooldowntime;
            string modname = cmdname;
            if (cooldownDict.ContainsKey(modname)) //look for the mods cooldown dictionary
            {
                Dictionary<string, int> dicdata = cooldownDict[modname]; //Assign our cooldown dictionary to dicdata
                if (dicdata.ContainsKey(player.PlayerUID)) //Check dictionary for player's uid
                {
                    dicdata.TryGetValue(player.PlayerUID, out playersactivecooldowntime);
                    if (count >= playersactivecooldowntime + modPlayerCooldown)
                    {
                        function(player);

                        cooldownDict[modname].Remove(player.PlayerUID);
                        cooldownDict[modname].Add(player.PlayerUID, count);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait for " + ((playersactivecooldowntime + modPlayerCooldown) - count) + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        return;
                    }
                }
                else
                {
                    function(player);
                    cooldownDict[modname].Add(player.PlayerUID, count);
                }
            }
            else
            {
                function(player);
                cooldownDict.Add(modname, new Dictionary<string, int>());
                cooldownDict[modname].Add(player.PlayerUID, count);
            }
        }

        //Simple Server Messages broadcast messages
        private void broadcast()
        {
            if (bsuconfig.Current.enableSimpleServerMessages == true)
            {
                if (bsuconfig.Current.messages.Count > 0)
                {
                    List<String> messagelist = bsuconfig.Current.messages;


                    if (messageplace < messagelist.Count)
                    {
                        sapi.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                        messageplace++;
                    }
                    else
                    {
                        messageplace = 0;
                        sapi.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                    }
                }
            }

        }

        //========================//
        //Event Listener Functions//
        //========================//

        private void CoolDown(float ct)
        {
            if (bsuconfig.Current.enableGrtp == true)
            {
                if (count >= bsuconfig.Current.cooldownminutes + grtptimer)
                {
                    grtptimer = count;

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

            }
            if (bsuconfig.Current.enableSimpleServerMessages == true)
            {
                if (count >= bsuconfig.Current.frequency + ssmtimer)
                {
                    ssmtimer = count; //set the timer to the current time
                    broadcast(); //broadcast our messages
                }
            }
            if (bsuconfig.Current.enabletpt == true)
            {
                foreach (var keyvalue in bsuconfig.Current.tptDict.Keys)
                {
                    tptinfo value = new tptinfo();
                    var dic = bsuconfig.Current.tptDict.Values;
                    bsuconfig.Current.tptDict.TryGetValue(keyvalue, out value);
                    if ((count - value.timer) >= 2)
                    {
                        sapi.SendMessage(sapi.World.PlayerByUid(keyvalue), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your TP to player has expired", Vintagestory.API.Common.EnumChatType.Notification);
                        bsuconfig.Current.tptDict.Remove(keyvalue); //player that is teleporting
                        bsuconfig.Current.waitDict.Remove(value.toplayer); //player that is being teleported to

                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return;
                    }
                }
            }
            if (bsuconfig.Current.enablejrtp == true)
            {
                Dictionary<string, int>.KeyCollection tempdict = bsuconfig.Current.cooldownDict.Keys;
                foreach (var keyvalue in tempdict)
                {
                    int value;
                    int cooldowntimer = count;
                    bsuconfig.Current.cooldownDict.TryGetValue(keyvalue, out value);
                    if (cooldowntimer >= value + bsuconfig.Current.cooldownduration)
                    {
                        bsuconfig.Current.cooldownDict.Remove(keyvalue);
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return;
                    }
                }
            }


            count = count + 1; //add a minute to our timer
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
            //rtp check chunk
            if (rtprandx / sapi.WorldManager.ChunkSize == chunkCoord.X & (rtprandz / sapi.WorldManager.ChunkSize == chunkCoord.Y) & (teleporting == true))
            {
                Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location.", Vintagestory.API.Common.EnumChatType.Notification);
                BlockPos checkheight = new BlockPos();
                checkheight.X = rtprandx;
                checkheight.Y = 1;
                checkheight.Z = rtprandz;
                int height = sapi.World.BlockAccessor.GetTerrainMapheightAt(checkheight);
                GEntity.TeleportTo(rtprandx, height + 1, rtprandz);
                teleporting = false;
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

        private void onNowPlaying(IServerPlayer byPlayer)
        {
            if (bsuconfig.Current.enableJoinAnnounce == true)
            {
                if (joinedPlayers != null)
                {
                    if (joinedPlayers.Contains(byPlayer))
                    {
                        sapi.BroadcastMessageToAllGroups("Please welcome <font color=\"white\"><strong>" + byPlayer.PlayerName + "</strong></font> to the server!", Vintagestory.API.Common.EnumChatType.AllGroups);
                        joinedPlayers.Remove(byPlayer);
                    }
                }
            }
            if (bsuconfig.Current.enableRisingSun == true)
            {
                if (rsjoinedPlayers != null)
                {
                    if (rsjoinedPlayers.Contains(byPlayer))
                    {
                        sapi.BroadcastMessageToAllGroups("The sun has risen on a new player! Welcome " + byPlayer.PlayerName, Vintagestory.API.Common.EnumChatType.AllGroups);
                        int hour = byPlayer.Entity.World.Calendar.FullHourOfDay;
                        if (hour < bsuconfig.Current.dawn)
                        {

                            byPlayer.Entity.World.Calendar.Add((int)bsuconfig.Current.dawn - hour);
                        }
                        else if (hour > (int)bsuconfig.Current.dusk)
                        {
                            byPlayer.Entity.World.Calendar.Add(24 - hour + (int)bsuconfig.Current.dawn);
                        }
                        rsjoinedPlayers.Remove(byPlayer);
                    }
                }
            }
            if(bsuconfig.Current.enablejoinmessage == true)
            {
                byPlayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Players Online: " + sapi.Server.Players.Length.ToString(), Vintagestory.API.Common.EnumChatType.Notification);
                string players = " ";
                for (int i = 0;i < sapi.Server.Players.Length; i++)
                {
                    players = players + sapi.Server.Players[i].PlayerName + " | ";
                }
                byPlayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, players, Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        public void OnPlayerCreate(IServerPlayer byPlayer)
        {
            if (bsuconfig.Current.enableJoinAnnounce == true)
            {
                joinedPlayers.Add(byPlayer);
            }
            if (bsuconfig.Current.enableRisingSun == true)
            {
                rsjoinedPlayers.Add(byPlayer);
            }

        }

        private void onPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            if (bsuconfig.Current.enableBunnyBell == true)
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
                        templayer.Entity.World.PlaySoundFor(sound, templayer, true, 32, volume);//volume);
                    }
                }
            }

        }
        //=======//
        //Classes//
        //=======//

        public class tptinfo
        {

            public String toplayer;
            public Boolean haspermission;
            public Boolean waiting;
            public int timer;

        }

        public class userwarning
        {

            public String playeruid = "default";
            public String playername = "null";
            public int warnings = 0;
            public String ipaddress = "null";
            public List<String> reasons = new List<string>();

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
            public bool? enableGrtp;
            public bool? enableBunnyBell;
            public bool? enablejpm;
            public bool? enablejrtp;
            public bool? enableRisingSun;
            public bool? enableSimpleServerMessages;
            public bool? enabletpt;
            public bool? enablejoinmessage;

            //jhome properties
            public Dictionary<String, BlockPos> homeDict { get; set; }//Must be preserved to pull old homes to the new save
            public int? homePlayerCooldown; //How often the player can use /home
            public int? backPlayerCooldown;//How often the player can use /back
            public bool? enablePermissions;
            public bool? homesImported;


            //grtp properties
            public int? teleportradius; //radius for GRTP to choose new locations
            public int? cooldownminutes; //How often GRTP changes locations
            public int? grtpPlayerCooldown; //How often the player can use GRTP

            //spawn properties
            public int? spawnPlayerCooldown;

            //Join announce Properties
            public bool? enableJoinAnnounce;

            //Rising Sun Properties
            public int? dawn;
            public int? dusk;

            //Simple Server Message Properties
            public List<String> messages { get; set; }
            public int? frequency;

            //Teleport To Properties
            public Dictionary<String, tptinfo> tptDict;
            public Dictionary<String, String> waitDict;

            //Random Teleport Properties
            //public int? rtpcooldownminutes; //How long the player must wait to teleport
            public int? rtpradius; //How far the player can teleport
            public int? cooldownduration; //how long between RTP teleports
            public Dictionary<String, int> cooldownDict { get; set; }

            //userwarning properties
            public Dictionary<String, userwarning> warningDict;


            public static bsuconfig getDefault()
            {
                var config = new bsuconfig();
                BlockPos defPos = new BlockPos(0, 0, 0);
                bool perms = false;
                List<String> dmessages = new List<string> //SSM default dmessages
                {
                    "Welcome to the server!"
                };
                Dictionary<String, tptinfo> tptdictionary = new Dictionary<string, tptinfo> //Dictionary to hold Teleport To info
                {
                    { "Default",new tptinfo() }
                };
                Dictionary<String, String> waitdictionary = new Dictionary<string, string> //Dictionary to hold cooldowns per player
                {
                    { "Default","Default"}
                };
                Dictionary<String, userwarning> warningdictionary = new Dictionary<string, userwarning>
                {
                    {"Default",new userwarning() }
                };



                //jHome default assignments
                Dictionary<String, BlockPos> homedictionary = null;
                config.homeDict = homedictionary;//Must be preserved to pull old homes to the new save
                config.enablePermissions = perms;
                config.homesImported = false;
                config.homePlayerCooldown = 1;
                config.backPlayerCooldown = 1;

                //enable/disable module defaults
                config.enableBack = true;
                config.enableHome = true;
                config.enableSpawn = true;
                config.enableGrtp = true;
                config.enableJoinAnnounce = true;
                config.enableBunnyBell = true;
                config.enablejpm = true;
                config.enablejrtp = false;
                config.enableRisingSun = false;
                config.enableSimpleServerMessages = false;
                config.enabletpt = true;
                config.enablejoinmessage = true;


                //grtp module defaults
                config.cooldownminutes = 60;
                config.teleportradius = 100000;
                config.grtpPlayerCooldown = 1;

                //spawn module defaults
                config.spawnPlayerCooldown = 1;

                //Rising Sun module defaults
                config.dawn = 8;
                config.dusk = 21;

                //Simple Server Message defaults
                config.messages = dmessages;
                config.frequency = 10;

                //Teleport To player defaults
                config.tptDict = tptdictionary;
                config.waitDict = waitdictionary;

                //Random Teleport defaults
                config.rtpradius = 100000;
                config.cooldownduration = 15;
                config.cooldownDict = new Dictionary<string, int> //Dictionary to hold JRTP cooldown
                {
                    { "Default",1}
                };

                //user warning defaults
                config.warningDict = warningdictionary;

                return config;
            }


        }

        //Old home config to allow us to get the old home file
        public class HomeConfig
        {
            public static HomeConfig Current { get; set; }

            public Dictionary<String, BlockPos> homeDict { get; set; }//Must be preserved to pull old homes to the new save
            public bool? enablePermissions;
            public bool? enableBack;



            public static HomeConfig getDefault()
            {
                var config = new HomeConfig();
                BlockPos defPos = new BlockPos(0, 0, 0);
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
