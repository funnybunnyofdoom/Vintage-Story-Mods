﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;

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
        Dictionary<string, List<BlockPos>> homeSave; //Dictionary to hold our /home locations
        Dictionary<string, BlockPos> oldHomeSave; //Dictionary to hold our old home locations
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

        //Iron Man Initialization
        public List<string> ironManPlayerList; //List to hold our players in ironman mode
        public List<string> TempironManList = new List<string>{"default"}; //Holds the players names before they confirm
        int imx, imz = 0;
        bool imteleporting = false;
        IServerPlayer implayer;
        Dictionary<string,double> currentironmandict = new Dictionary<string,double>();
        Dictionary<string, int> ironmanhighscores = new Dictionary<string, int>();

        //Teleport Cost Initialization
        Dictionary<string, tptcostinfo> tptcostdictionary = new Dictionary<string, tptcostinfo>();

        Dictionary<string, string> replySave = new Dictionary<string, string>();

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
                cmd_bsu,Privilege.chat);
            api.RegisterCommand("bunnyServerUtilities", "Bunny Server utilities", "[help | Version]",
                cmd_bsu, Privilege.chat);
            api.RegisterCommand("bunnyServerUtility", "Bunny Server utilities", "[help | Version]",
                cmd_bsu, Privilege.chat);
            api.RegisterCommand("removedeny", "removes a privilege denial", "/removedeny <i>playername privilege</i>",
                cmd_removedeny, Privilege.controlserver);
            api.RegisterCommand("warn", "Issues a warning for a player", "/warn <i>playername reason</i>",
                cmd_warn, privileges.src.IPrivilege.warn);

            //home commands
            api.RegisterCommand("sethome", "Set your current position as home", " ",
                cmd_sethome, privileges.src.KPrivilege.sethome);
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
            api.RegisterCommand("reply", "Private Message", " ", cmd_reply, privileges.src.EPrivilege.jpm);
            api.RegisterCommand("r", "Private Message", " ", cmd_reply, privileges.src.EPrivilege.jpm);

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
            api.RegisterCommand("rtp", "Randomly Teleports the player", "[rtp|help|cooldown|enable|disable|costitm|costqty]",
            cmd_rtp, privileges.src.HPrivilege.rtp);

            //Ironman Commands
            api.RegisterCommand("ironman", "Sets the player to ironman mode", "",cmd_ironman,privileges.src.JPrivilege.ironman);

            //telport cost commands
            api.RegisterCommand("tpcost", "enables/disables teleport costs", "[help|enable|disable]", cmd_tpcost, Privilege.controlserver);

            //Cooldowns command
            api.RegisterCommand("cooldown", "List of the user's current cooldowns", "[help | Version]", cmd_cooldown);
            api.RegisterCommand("cooldowns", "List of the user's current cooldowns", "[help | Version]", cmd_cooldown);

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

            //Ironman privileges
            ipm.RegisterPrivilege("ironman","Iron Man");


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
                if (bsuconfig.Current.enableSetHome == null)
                    bsuconfig.Current.enableSetHome = bsuconfig.getDefault().enableHome;
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
                if (bsuconfig.Current.tptPlayerCooldown == null)
                    bsuconfig.Current.tptPlayerCooldown = bsuconfig.getDefault().tptPlayerCooldown;
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
                if (bsuconfig.Current.enableironman == null)
                    bsuconfig.Current.enableironman = bsuconfig.getDefault().enableironman;
                if (bsuconfig.Current.backcostitem == null)
                    bsuconfig.Current.backcostitem = bsuconfig.getDefault().backcostitem;
                if (bsuconfig.Current.backcostqty == null)
                    bsuconfig.Current.backcostqty = bsuconfig.getDefault().backcostqty;
                if (bsuconfig.Current.homecostitem == null)
                    bsuconfig.Current.homecostitem = bsuconfig.getDefault().homecostitem;
                if (bsuconfig.Current.homecostqty == null)
                    bsuconfig.Current.homecostqty = bsuconfig.getDefault().homecostqty;
                if (bsuconfig.Current.spawncostitem == null)
                    bsuconfig.Current.spawncostitem = bsuconfig.getDefault().spawncostitem;
                if (bsuconfig.Current.spawncostqty == null)
                    bsuconfig.Current.spawncostqty = bsuconfig.getDefault().spawncostqty;
                if (bsuconfig.Current.rtpcostitem == null)
                    bsuconfig.Current.rtpcostitem = bsuconfig.getDefault().rtpcostitem;
                if (bsuconfig.Current.rtpcostqty == null)
                    bsuconfig.Current.rtpcostqty = bsuconfig.getDefault().rtpcostqty;
                if (bsuconfig.Current.grtpcostitem == null)
                    bsuconfig.Current.grtpcostitem = bsuconfig.getDefault().grtpcostitem;
                if (bsuconfig.Current.grtpcostqty == null)
                    bsuconfig.Current.grtpcostqty = bsuconfig.getDefault().grtpcostqty;
                if (bsuconfig.Current.tptcostitem == null)
                    bsuconfig.Current.tptcostitem = bsuconfig.getDefault().tptcostitem;
                if (bsuconfig.Current.tptcostqty == null)
                    bsuconfig.Current.tptcostqty = bsuconfig.getDefault().tptcostqty;
                if (bsuconfig.Current.homelimit == null)
                    bsuconfig.Current.homelimit = bsuconfig.getDefault().homelimit;
                if (bsuconfig.Current.multihomemigration == null)
                    bsuconfig.Current.multihomemigration = bsuconfig.getDefault().multihomemigration;
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
                //Add ironman permissions to all standard groups
                ipm.AddPrivilegeToGroup("admin",privileges.src.JPrivilege.ironman);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.JPrivilege.ironman);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.JPrivilege.ironman);
                //Add sethome permissions to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.KPrivilege.sethome);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.KPrivilege.sethome);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.KPrivilege.sethome);
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
                    if (bsuconfig.Current.enableBack == true && !ironManPlayerList.Contains(player.PlayerUID))//Check to see if back is enabled, or if the user is in ironman mode
                    {
                        string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.backPlayerCooldown);
                        if (cooldownstate != "wait")
                        {
                            if (processPayment(bsuconfig.Current.backcostitem, bsuconfig.Current.backcostqty, player, null))
                            {
                                backteleport(player);
                                addcooldown(cmdname, player, cooldownstate);
                            }
                        }
                    } else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-back"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname+"admin"))
                    {
                        bsuconfig.Current.enableBack = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable-back"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }   
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBack = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable-back"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                           bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.backcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated",cmdname,code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.backcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
            }
        }

        //Set Home command
        private void cmd_sethome(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "sethome";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableHome == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        string playerID = player.Entity.PlayerUID; //get the using player's player ID
                        int homecount;
                        List<BlockPos> homelist = new List<BlockPos>();
                        if (homeSave.ContainsKey(playerID))
                        {
                            homeSave.TryGetValue(playerID, out homelist);
                            homecount = homelist.Count;
                        }
                        else
                        {
                            homecount = 0;
                        }

                        if ((bsuconfig.Current.homelimit == 1 && homecount < 2) || homecount == 0)
                        {
                            if (processPayment(bsuconfig.Current.sethomecostitem, bsuconfig.Current.sethomecostqty, player, null))
                            {
                                if (homeSave.ContainsKey(player.Entity.PlayerUID))
                                {
                                    homeSave.Remove(player.Entity.PlayerUID);
                                }
                                homelist.Add(player.Entity.Pos.AsBlockPos);
                                homeSave.Add(player.Entity.PlayerUID, homelist);
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-sethome"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user that they have set their home
                            }
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-save"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        }                     
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enabled",cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled",cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.sethomecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.sethomecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                default:
                    if (bsuconfig.Current.enableHome == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {


                        string inputString = cmd;
                        int input;
                        System.Diagnostics.Debug.WriteLine(inputString);
                        if (int.TryParse(inputString, out input))
                        {
                            System.Diagnostics.Debug.WriteLine(input);

                            string playerID = player.Entity.PlayerUID; //get the using player's player ID
                            int homecount;
                            List<BlockPos> homelist = new List<BlockPos>();
                            if (homeSave.ContainsKey(playerID))
                            {
                                homeSave.TryGetValue(playerID, out homelist);
                                homecount = homelist.Count;
                            }
                            else
                            {
                                homecount = 0;
                            }

                            if (input > homecount + 1)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount + 1), Vintagestory.API.Common.EnumChatType.Notification);
                            }else if (input > bsuconfig.Current.homelimit)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (input > 0)
                            {
                                if (processPayment(bsuconfig.Current.sethomecostitem, bsuconfig.Current.sethomecostqty, player, null))
                                {
                                    if (homeSave.ContainsKey(player.Entity.PlayerUID))
                                    {
                                        homeSave.Remove(player.Entity.PlayerUID);
                                    }
                                    if (input - 1 < homelist.Count)
                                    {
                                        homelist[input - 1] = player.Entity.Pos.AsBlockPos;
                                    }
                                    else
                                    {
                                        homelist.Add(player.Entity.Pos.AsBlockPos);
                                    }
                                    homeSave.Add(player.Entity.PlayerUID, homelist);
                                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-sethome"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user that they have set their home
                                }
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(input);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                    }
                    break;
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
                    if (bsuconfig.Current.enableHome == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        string playerID2 = player.Entity.PlayerUID; //get the using player's player ID
                        int homecount2;
                        List<BlockPos> homelist2 = new List<BlockPos>();
                        if (homeSave.ContainsKey(playerID2))
                        {
                            homeSave.TryGetValue(playerID2, out homelist2);
                            homecount2 = homelist2.Count;
                        }
                        else
                        {
                            homecount2 = 0;
                        }

                        if ((bsuconfig.Current.homelimit == 1 && homecount2 > 0) || homecount2 == 1)
                        {
                            string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.homePlayerCooldown);
                            if (cooldownstate != "wait")
                            {
                                if (processPayment(bsuconfig.Current.homecostitem, bsuconfig.Current.homecostqty, player, null))
                                {

                                    homeTeleport(player, 1 - 1);
                                    addcooldown(cmdname, player, cooldownstate);
                                }
                            }
                        }
                        else if (homecount2 == 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-home"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable-home"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable-home"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.homecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.homecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
                default:
                    if (bsuconfig.Current.enableHome == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        string inputString = cmd;
                        int input;
                        if (int.TryParse(inputString, out input))
                        {
                            string playerID = player.Entity.PlayerUID; //get the using player's player ID
                            int homecount;
                            List<BlockPos> homelist = new List<BlockPos>();
                            if (homeSave.ContainsKey(playerID))
                            {
                                homeSave.TryGetValue(playerID, out homelist);
                                homecount = homelist.Count;
                            }
                            else
                            {
                                homecount = 0;
                            }

                            if (input > homecount && homecount > 0)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else if (input > 0 && input <= bsuconfig.Current.homelimit && homecount != 0)
                            {
                                string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.homePlayerCooldown);
                                if (cooldownstate != "wait")
                                {
                                    if (processPayment(bsuconfig.Current.homecostitem, bsuconfig.Current.homecostqty, player, null))
                                    {

                                        homeTeleport(player, input - 1);
                                        addcooldown(cmdname, player, cooldownstate);
                                    }
                                }
                            }
                            else if (homecount == 0)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-home"), Vintagestory.API.Common.EnumChatType.Notification);
                            }else if (input < 1)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit), Vintagestory.API.Common.EnumChatType.Notification); //Tell player to use a number lower than the homelimit
                            }       
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(input);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                    }
                    break;
                case "limit":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num > 0)
                        {
                            bsuconfig.Current.homelimit = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-limit-updated", num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "delete":
                    int? numb = args.PopInt();
                    string playerID3 = player.Entity.PlayerUID; //get the using player's player ID
                    int homecount3;
                    List<BlockPos> homelist3 = new List<BlockPos>();
                    if (homeSave.ContainsKey(playerID3))
                    {
                        homeSave.TryGetValue(playerID3, out homelist3);
                        homecount3 = homelist3.Count;
                    }
                    else
                    {
                        homecount3 = 0;
                    }

                    if (homecount3 == 0 || homecount3 < numb || numb <= 0)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-homes", numb), Vintagestory.API.Common.EnumChatType.Notification);
                    }else if(numb == null){
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        homelist3.RemoveAt((int)numb - 1);
                        homeSave.Remove(playerID3);
                        homeSave.Add(playerID3, homelist3);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-removed", numb), Vintagestory.API.Common.EnumChatType.Notification);
                    }
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
                    if (bsuconfig.Current.enableGrtp == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        if (randx != 0 & randz != 0)
                        {
                            if (loaded == true)
                            {
                                
                                string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.grtpPlayerCooldown);
                                if (cooldownstate != "wait")
                                {
                                    if (processPayment(bsuconfig.Current.grtpcostitem, bsuconfig.Current.grtpcostqty, player,null))
                                    {
                                        grtpteleport(player);
                                        addcooldown(cmdname, player, cooldownstate);
                                    }
                                }
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:chunkloading-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that the chunk is still loading
                                sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                            }
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:notset-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that GRTP location is not yet set
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled","grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that GRTP is disabled
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "cooldown":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null | cdnum == 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:greater-than",0), Vintagestory.API.Common.EnumChatType.Notification); //Enter a number greater than 0
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user for a non-negative number
                        }
                        else
                        {
                            bsuconfig.Current.cooldownminutes = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldown-set-grtp",cdnum), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that the cooldown for GRTP locations have been set
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "radius":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null | cdnum < 10)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:greater-than", 10), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user for a number greater than 10
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user for a non-negative number
                        }
                        else
                        {
                            bsuconfig.Current.teleportradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:set-radius-grtp",cdnum), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that the radius was set
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "now":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        grtptimer = 0 - (int)bsuconfig.Current.cooldownminutes;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wait-now-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user to wait while the GRTP teleport location is updated
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable","grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user that GRTP has been enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable","grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user that GRTP has been disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.grtpcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.grtpcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
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
                    if (bsuconfig.Current.enableSpawn == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        
                        string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.spawnPlayerCooldown);
                        if (cooldownstate != "wait")
                        {
                            if (processPayment(bsuconfig.Current.spawncostitem, bsuconfig.Current.spawncostqty, player,null))
                            {
                                spawnTeleport(player);
                                addcooldown(cmdname, player, cooldownstate);
                            }
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled","spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable","spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable","spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "playercooldown":
                    setplayercooldown(player, args.PopInt(), cmdname);
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.spawncostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.spawncostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }

        }
        //Import Old Homes Command
        private void cmd_importOldHomes(IServerPlayer player, int groupId, CmdArgs args)
        {
            /*if (bsuconfig.Current.homesImported == false)
            {
                if (player.Role.Code == "admin")
                {
                    if (HomeConfig.Current.homeDict != null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:importing"), Vintagestory.API.Common.EnumChatType.Notification);
                        homeSave.Clear();
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:old-home-config-size") + HomeConfig.Current.homeDict.Count(), Vintagestory.API.Common.EnumChatType.Notification);
                        int configsize = HomeConfig.Current.homeDict.Count();
                        for (int i = 0; i < configsize; i++)
                        {
                            KeyValuePair<string, BlockPos> kvp = HomeConfig.Current.homeDict.PopOne();

                            homeSave.Add(kvp.Key, kvp.Value);

                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (i + 1) + "/" + configsize + ":"+ Lang.Get("bunnyserverutilities:player") + ": " + kvp.Key + " " + kvp.Value, Vintagestory.API.Common.EnumChatType.Notification);

                        }
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:old-home-imported"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that their homes have been imported
                        //HomeConfig.Current.homeDict.Clear();
                        //HomeConfig.Current.homeDict = null;
                        //sapi.StoreModConfig(HomeConfig.Current, "homeconfig.json");
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-old-homes"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user there are no old homes found
                    }
                    bsuconfig.Current.homesImported = true;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-permission"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user they don't have permission for this command
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-already-imported"), Vintagestory.API.Common.EnumChatType.Notification);//inform the user that their homes have already been imported
            }
*/
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "No Longer Supported", Vintagestory.API.Common.EnumChatType.Notification);
        }

        private void cmd_joinannounce(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "joinannounce";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "[help | enable | disable]", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableJoinAnnounce = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "Join Announce"), Vintagestory.API.Common.EnumChatType.Notification); //inform the player that join announce is now disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableJoinAnnounce = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "Join Announce"), Vintagestory.API.Common.EnumChatType.Notification); //inform the user that joina nnounce is now enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum < 1 | cdnum > 23)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:between-hours"), Vintagestory.API.Common.EnumChatType.Notification); //Asks user for hour between 1-23
                        }
                        else if (cdnum > bsuconfig.Current.dusk)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:smaller-dusk") + bsuconfig.Current.dusk, Vintagestory.API.Common.EnumChatType.Notification); //informs user to use a smaller number
                        }
                        else if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:between-hours"), Vintagestory.API.Common.EnumChatType.Notification); //Asks user for hour between 1-23
                        }
                        else
                        {
                            bsuconfig.Current.dawn = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:dawn-updated") + cdnum + ":00", Vintagestory.API.Common.EnumChatType.Notification); //informs user that the dawn time is updated
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "dusk":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum < 1 | cdnum > 23)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:between-hours"), Vintagestory.API.Common.EnumChatType.Notification); //Asks user for hour between 1-23
                        }
                        else if (cdnum < bsuconfig.Current.dawn)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:larger-dawn") + bsuconfig.Current.dawn, Vintagestory.API.Common.EnumChatType.Notification); //informs user to use a larger number
                        }
                        else if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:between-hours"), Vintagestory.API.Common.EnumChatType.Notification); //Asks user for hour between 1-23
                        }
                        else
                        {
                            bsuconfig.Current.dusk = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:dusk-updated") + cdnum + ":00", Vintagestory.API.Common.EnumChatType.Notification); //informs the user that the dusk time is updated
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableRisingSun = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification); // Inform the user that Rising Sun has been enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableRisingSun = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification); // Inform the user that Rising Sun has been disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
                        return;
                    }
                    else
                    {
                        string message = args.PopAll();
                        System.Diagnostics.Debug.WriteLine(message);
                        if (message == null | message == "")
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:include-message"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user to include a message with the command
                            return;
                        }
                        else
                        {
                            sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                            sapi.SendMessage(sapi.World.PlayerByUid(player.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " to " + pdata.LastKnownPlayername + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                            if (replySave.ContainsKey(pdata.PlayerUID))
                            {
                                replySave[pdata.PlayerUID] = player.PlayerUID; // set the player being messaged to reply to the player that messaged them last
                            }
                            else
                            {
                                replySave.Add(pdata.PlayerUID, player.PlayerUID); // set the player being messaged to reply to the player that messaged them last
                            }
                        
                        }
                    }
                }
                else if (cmd == "help")
                {
                    displayhelp(player, cmdname);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:include-player"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }

        //Reply command
        private void cmd_reply(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (bsuconfig.Current.enablejpm == true)
            {
                string cmdname = "jpm";
                string message = args.PopAll();
                
                
                if (message != "help")
                {
                    
                        if (message == null | message == "")
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:include-message"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user to include a message with the command
                            return;
                        }
                        else
                        {
                        if (replySave.ContainsKey(player.PlayerUID))
                        {
                            IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByUid(replySave[player.PlayerUID]);
                            if (pdata == null)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
                                return;
                            }
                            sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                            sapi.SendMessage(sapi.World.PlayerByUid(player.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "<font color=\"#B491C8\"><strong>" + player.PlayerName + " to " + pdata.LastKnownPlayername + " : </strong><i>" + message + "</i></font>", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-reply"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                }
                else if (message == "help")
                {
                    displayhelp(player, cmdname);
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
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enablejpm = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "dm"), Vintagestory.API.Common.EnumChatType.Notification); // inform the user that /dm has been enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enablejpm = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "dm"), Vintagestory.API.Common.EnumChatType.Notification); // inform the user that /dm has been disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:add-message"), Vintagestory.API.Common.EnumChatType.Notification);
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
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:remove-message"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:remove-message-help"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:remove-message-help-2"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "list":
                    List<string> listofmessages = bsuconfig.Current.messages;
                    int lastindex = listofmessages.Count;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:list-of-messages"), Vintagestory.API.Common.EnumChatType.Notification);
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:message-frequency",frqnum), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:greater-than", 0), Vintagestory.API.Common.EnumChatType.Notification);
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
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSimpleServerMessages = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "Server Messages"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that server messagess are enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSimpleServerMessages = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "Server Messages"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that server messages are disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    sapi.SendMessage(sapi.World.PlayerByUid(tpPlayer), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-deny-player"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-deny-target"), Vintagestory.API.Common.EnumChatType.Notification);
                    bsuconfig.Current.waitDict.Remove(player.PlayerUID);
                    bsuconfig.Current.tptDict.Remove(tpPlayer);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-tp-deny"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    EntityPlayer tpserverplayer = sapi.World.PlayerByUid(tpPlayer).WorldData.EntityPlayer;
                    if (bsuconfig.Current.teleportcostenabled)
                    {
                        if (processPayment(tptcostdictionary[sapi.World.PlayerByUid(tpPlayer).PlayerName].item, tptcostdictionary[sapi.World.PlayerByUid(tpPlayer).PlayerName].qty, (IServerPlayer)sapi.World.PlayerByUid(tpPlayer), tptcostdictionary[sapi.World.PlayerByUid(tpPlayer).PlayerName].slot) == false)
                        {
                            return;
                        }
                    }
                     
                    sapi.SendMessage(sapi.World.PlayerByUid(tpPlayer), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleport-accepted"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleport-accepted-user"), Vintagestory.API.Common.EnumChatType.Notification);
                        //Add BACK here    
                    string actions = checkCooldown((IServerPlayer)sapi.World.PlayerByUid(tpPlayer), "tpt", bsuconfig.Current.tptPlayerCooldown);
                    addcooldown("tpt", (IServerPlayer)sapi.World.PlayerByUid(tpPlayer), actions);
                    tpserverplayer.TeleportTo(player.Entity.Pos.AsBlockPos);
                    bsuconfig.Current.waitDict.Remove(player.PlayerUID);
                    bsuconfig.Current.tptDict.Remove(tpPlayer);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-tp-accept"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
        }
        //teleport to
        private void cmd_tpt(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "tpt";
            string cmd = args.PopWord();
            if (cmd != null & cmd != "help" & cmd != "enable" & cmd != "disable" & cmd != "playercooldown" & cmd != "costqty" & cmd != "costitem" & cmd != "wipe")
            {
                if (bsuconfig.Current.enabletpt == true && !ironManPlayerList.Contains(player.PlayerUID))
                {

                    string actions = checkCooldown(player, cmdname, bsuconfig.Current.tptPlayerCooldown);
                    if (actions != "wait")
                    {
                        if (bsuconfig.Current.teleportcostenabled == true)
                        {
                            bool outcome = prepay(bsuconfig.Current.tptcostitem, bsuconfig.Current.tptcostqty, player);
                            if (outcome == true) { teleportTo(player, cmd); }
                        }
                        else
                        {
                            teleportTo(player, cmd);
                        }

                    }

                }
                else if (ironManPlayerList.Contains(player.PlayerUID))
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", "tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                }

            }
            else if (cmd == "help")
            {
                displayhelp(player, cmdname);
            }
            else if (cmd == "enable")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    bsuconfig.Current.enabletpt = true;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "Teleport To"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "disable")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    bsuconfig.Current.enabletpt = false;
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "Teleport To"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "playercooldown")
            {
                setplayercooldown(player, args.PopInt(), cmdname);
            }
            else if (cmd == "costqty")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    int? num = args.PopInt();
                    if (num != null && num >= 0)
                    {
                        bsuconfig.Current.tptcostqty = (int)num;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "costitem")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    string code = args.PopWord();
                    if (code == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        bool outcome = checkcostitem(code);
                        if (outcome == false)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.tptcostitem = code;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        }
                    }
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "wipe")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    bsuconfig.Current.tptDict = new Dictionary<string, tptinfo>{
                    { "Default",new tptinfo() }
                };
                    bsuconfig.Current.waitDict = new Dictionary<string, string>
                    {
                        { "Default", "Default" }
                    };
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wiped"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else
            {
                if (bsuconfig.Current.enabletpt == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:need-playername-tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", "tpt"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBunnyBell = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "Bunny Bell"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user that bunny bell is enabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBunnyBell = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "Bunny Bell"), Vintagestory.API.Common.EnumChatType.Notification); //Inforn user that bunny bell is disabled
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.cooldownduration = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-cooldown-set") + bsuconfig.Current.cooldownduration + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-permission"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "help":
                    displayhelp(player, cmdname);
                    break;
                case null:
                    if (bsuconfig.Current.enablejrtp == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {
                        ICoreServerAPI api = sapi; //get the server api
                        Splayer = player;
                        GEntity = player.Entity; //assign the entity to global variable
                        IWorldManagerAPI world = api.WorldManager;
                        System.Diagnostics.Debug.Write(count);
                        string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.cooldownduration);
                        if (cooldownstate != "wait" & teleporting == false)
                        {
                            if (processPayment(bsuconfig.Current.rtpcostitem, bsuconfig.Current.rtpcostqty, player, null))
                            {
                                setbackteleport(player);
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-wait"), Vintagestory.API.Common.EnumChatType.Notification);
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
                                addcooldown(cmdname,player,cooldownstate);

                            }
                        }
                        else if (teleporting == true)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-wait-2"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (bsuconfig.Current.cooldownDict.ContainsKey(player.PlayerUID) == true & teleporting == false)
                        {
                            int values;
                            bsuconfig.Current.cooldownDict.TryGetValue(player.PlayerUID, out values);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-cooldown-timer", ((values + bsuconfig.Current.cooldownduration) - cooldowntimer)), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled","rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enablejrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enablejrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "radius":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.PopInt();
                        if (cdnum == null | cdnum < 10)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-or-greater", 10), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (cdnum < 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bsuconfig.Current.rtpradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:set-radius-rtp",cdnum), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.PopWord();
                        if (code == null)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                bsuconfig.Current.rtpcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.PopInt();
                        if (num != null && num >= 0)
                        {
                            bsuconfig.Current.rtpcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
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
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:remove-denial",cmd2,cmd), Vintagestory.API.Common.EnumChatType.Notification);
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-player") + targetplayer.LastKnownPlayername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + uwd.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-warnings") + uwd.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-reasons"), Vintagestory.API.Common.EnumChatType.Notification);
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-player") + targetplayer.LastKnownPlayername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + uwd.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-warnings") + uwd.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-reasons"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else if (cmd == "list")
            {
                int? listnum = args.PopInt();
                Dictionary<String, userwarning> uswd = new Dictionary<String, userwarning>();
                uswd = bsuconfig.Current.warningDict;
                if (listnum == null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    for (var i = 1; i < uswd.Count; i++)
                    {
                        userwarning UW = uswd.ElementAt(i).Value;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i+") "+ Lang.Get("bunnyserverutilities:warn-player") + UW.playername+ " | "+ Lang.Get("bunnyserverutilities:warn-warning") + ": "+UW.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                else
                {
                    if (listnum != null & listnum >0 & listnum < uswd.Count)
                    {
                        userwarning UW = uswd.ElementAt((int)listnum).Value;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-player") + UW.playername, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "IP: " + UW.ipaddress, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-warnings") + UW.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-reasons"), Vintagestory.API.Common.EnumChatType.Notification);
                        for (int j = 0; j < UW.reasons.Count; j++)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, j + 1 + ": " + UW.reasons[j], Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between",1, (uswd.Count - 1)), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/warn playername reason | /warn list", Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        private void cmd_ironman(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmdname = "ironman";
            string cmd = args.PopWord();
            switch (cmd)
            {
                case null:
                    if(bsuconfig.Current.enableironman == true)
                    {
                        if (!ironManPlayerList.Contains(player.PlayerUID) && !TempironManList.Contains(player.PlayerUID))
                        {
                            TempironManList.Add(player.PlayerUID);
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-use-confirm"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else if (TempironManList.Contains(player.PlayerUID))
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-use-confirm"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-already-joined"), Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                    }

                    break;
                case "confirm":
                    if (TempironManList.Contains(player.PlayerUID))
                    {
                        TempironManList.Remove(player.PlayerUID);
                        ironManPlayerList.Add(player.PlayerUID);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-confirmed"), Vintagestory.API.Common.EnumChatType.Notification);
                        player.InventoryManager.DiscardAll();
                        IInventory gearslot = player.InventoryManager.GetInventory(player.Entity.GearInventory.InventoryID);
                        player.InventoryManager.DropAllInventoryItems(gearslot);
                        PlayerSpawnPos oldspawn = new PlayerSpawnPos();
                        oldspawn.x = (((int)sapi.World.DefaultSpawnPosition.X));
                        oldspawn.y = (((int)sapi.World.DefaultSpawnPosition.Y));
                        oldspawn.z = (((int)sapi.World.DefaultSpawnPosition.X));
                        player.SetSpawnPosition(oldspawn);
                        implayer = player;
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-teleport-wait"), Vintagestory.API.Common.EnumChatType.Notification);
                        int startradius = 190000;
                        int radius = 20000;
                        int worldx = sapi.WorldManager.MapSizeX;
                        int worldz = sapi.WorldManager.MapSizeZ;
                        int rawxmin = (worldx / 2) + startradius + radius;
                        int rawxmax = (worldx / 2) + startradius + (radius*2);
                        int rawzmin = (worldz / 2) + startradius + radius;
                        int rawzmax = (worldz / 2) + startradius + (radius*2);
                        imx = sapi.World.Rand.Next(rawxmin, rawxmax);
                        imz = sapi.World.Rand.Next(rawzmin, rawzmax);
                        sapi.WorldManager.LoadChunkColumnPriority(imx / sapi.WorldManager.ChunkSize, imz / sapi.WorldManager.ChunkSize);

                        //add player to current ironman score list
                        currentironmandict.Add(player.PlayerUID, sapi.World.Calendar.TotalDays);
                        imteleporting = true;



                    }
                    break;
                case "highscores":
                    System.Diagnostics.Debug.WriteLine("SORTING LIST");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-highscores-title"), Vintagestory.API.Common.EnumChatType.Notification);
                    if (ironmanhighscores.Count == 0)
                    {
                        ironmanhighscores.Add("Placeholder", -1);
                    }
                    Dictionary<string, int> tempscores = new Dictionary<string, int>();
                    for (int i = 0; i < currentironmandict.Count; i++)
                    {
                        if (ironmanhighscores.ContainsKey(currentironmandict.ElementAt(i).Key))
                        {
                            int value;
                            ironmanhighscores.TryGetValue(currentironmandict.ElementAt(i).Key, out value);
                            if (currentironmandict.ElementAt(i).Value > value)
                            {
                                tempscores.Add(currentironmandict.ElementAt(i).Key, (int)(sapi.World.Calendar.TotalDays - currentironmandict.ElementAt(i).Value));
                            }
                        }
                        
                    }
                    for (int i = 0; i < ironmanhighscores.Count; i++)
                    {
                        if (!tempscores.ContainsKey(ironmanhighscores.ElementAt(i).Key))
                        {
                            tempscores.Add(ironmanhighscores.ElementAt(i).Key, ironmanhighscores.ElementAt(i).Value);
                        }
                    }
                    //var sortedDict = from entry in ironmanhighscores orderby entry.Value ascending select entry;
                    tempscores = tempscores.OrderByDescending(i => i.Value).ToDictionary(i => i.Key, i => i.Value);
                    System.Diagnostics.Debug.WriteLine(tempscores.Count);
                    int skip = 0; //skip this many
                    for (int i = 0; i < tempscores.Count; i++) {
                        IServerPlayerData playername = sapi.PlayerData.GetPlayerDataByUid(tempscores.ElementAt(i).Key);
                        if (playername != null)
                        {
                            if (currentironmandict.ContainsKey(tempscores.ElementAt(i).Key))
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (i-skip) + 1 + ") " + playername.LastKnownPlayername + " (In Progress): " + tempscores.ElementAt(i).Value, Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (i-skip) + 1 + ") " + playername.LastKnownPlayername + ": " + tempscores.ElementAt(i).Value, Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                        else
                        {
                            skip++;
                        }
                        
                    }
                    break;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableironman = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableironman = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
            }
        }

        //cost command
        private void cmd_tpcost(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (player.Role.Code == "admin" || player.HasPrivilege("tptadmin"))
            {
                string cmdname = "tpcost";
                string cmd = args.PopWord();
                switch (cmd)
                {
                    case "help":
                        displayhelp(player, cmdname);
                        break;
                    case "enable":
                        bsuconfig.Current.teleportcostenabled = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        break;
                    case "disable":
                        bsuconfig.Current.teleportcostenabled = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        break;
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", "tptadmin"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        private void cmd_cooldown(IServerPlayer player, int groupId, CmdArgs args)
        {
            string[] modlist = { "rtp", "home", "spawn", "back", "tpt", "grtp" };
            int[] cooldownlist = { (int)bsuconfig.Current.cooldownduration, (int)bsuconfig.Current.homePlayerCooldown, (int)bsuconfig.Current.spawnPlayerCooldown, (int)bsuconfig.Current.backPlayerCooldown, (int)bsuconfig.Current.tptPlayerCooldown, (int)bsuconfig.Current.grtpPlayerCooldown };
            int cooldowncount = 0;
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldowns"), Vintagestory.API.Common.EnumChatType.Notification);
            for (int i = 0; i < modlist.Length; i++)
            {
                string modname = modlist[i];
                if (cooldownDict.ContainsKey(modname)) //look for the mods cooldown dictionary
                {
                    Dictionary<string, int> dicdata = cooldownDict[modname]; //Assign our cooldown dictionary to dicdata
                    if (dicdata.ContainsKey(player.PlayerUID)) //Check dictionary for player's uid
                    {
                        int playersactivecooldowntime;
                        dicdata.TryGetValue(player.PlayerUID, out playersactivecooldowntime);
                        if (cooldownlist[i] - (count - playersactivecooldowntime) > 0)
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldown-command", modlist[i], cooldownlist[i] - (count - playersactivecooldowntime)), Vintagestory.API.Common.EnumChatType.Notification);
                            cooldowncount++;
                        }
                        
                        
                    }
                }
                
            }
            if (cooldowncount == 0)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-cooldowns"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        //=============//
        //Help Function//
        //=============//
        private void displayhelp(IServerPlayer player, string helpType = "all")
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:bsu-commands"), Vintagestory.API.Common.EnumChatType.Notification);
            if (helpType != "all")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:bsu-help"), Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:bsu-version", "/bsu version"), Vintagestory.API.Common.EnumChatType.Notification);
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/removedeny <i>playername privilege</i> "+Lang.Get("bunnyserverutilities:help-removedeny)"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            //BSU help

            //home help
            if (helpType == "home" || helpType == "all")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands","Home"), Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableHome == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome " + Lang.Get("bunnyserverutilities:help-sethome"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home " + Lang.Get("bunnyserverutilities:help-home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home delete " + Lang.Get("bunnyserverutilities:help-home-delete"), Vintagestory.API.Common.EnumChatType.Notification);

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-2","/home"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin home help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable","/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable","/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown","/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:old-home-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-limit-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //back help
            if (helpType == "back" || helpType == "all")
            {
                //back help
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "Back"), Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableBack == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:back-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-2", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin back help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands","Back"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/back"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //spawn help
            if (helpType == "spawn" || helpType == "all")
            {
                //spawn help
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "Spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableSpawn == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:spawn-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-2", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin spawn help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands","Spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //grtp help
            if (helpType == "grtp" || helpType == "all")
            {
                //grtp help

                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "GRTP"), Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableGrtp == true)
                {
                    if (helpType == "grtp")
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-update", bsuconfig.Current.cooldownminutes), Vintagestory.API.Common.EnumChatType.Notification);
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-radius", bsuconfig.Current.teleportradius), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-cmd-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-2", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                //grtp admin help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "GRTP"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-cooldown"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-help-radius"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:grtp-help-now"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/grtp"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Join Announce help
            if (helpType == "joinannounce" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "Join Announce"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ja-enable"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ja-disable"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Rising Sun help
            if (helpType == "risingsun" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:dawn-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:dusk-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/rs","Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/rs","Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Just Private Message help
            if (helpType == "jpm" || helpType == "all")
            {
                if (bsuconfig.Current.enablejpm == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "Private Message"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:dm-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:reply-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/jpm","private message"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/jpm", "private message"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Simple Server Message help
            if (helpType == "ssm" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "Simple Server Messages"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ssm-add-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ssm-list-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ssm-remove-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ssm-freq-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ssm-now-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/ssm","Simple Server Messages"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/ssm", "Simple Server Messages"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Simple Teleport To help
            if (helpType == "tpt" || helpType == "all")
            {
                if (bsuconfig.Current.enabletpt == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tpt-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tpaccept-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tpdeny-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown", "/tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/tpt"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tpt-help-wipe"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //BunnyBell help
            if (helpType == "bb" || helpType == "all")
            {
                if (bsuconfig.Current.enableBunnyBell == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:bunny-bell-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/bb","Bunny Bell"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/bb", "Bunny Bell"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //Random Teleport Help
            if (helpType == "rtp" || helpType == "all")
            {
                if (bsuconfig.Current.enablejrtp == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "RTP"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-help"), Vintagestory.API.Common.EnumChatType.Notification);
                }

                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:rtp-cooldown"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costqty", "/rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-costitm", "/rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

            //TPCost Help
            if (helpType == "tpcost" || helpType == "all")
            {
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2","/tpcost","TP Cost"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/tpcost", "TP Cost"), Vintagestory.API.Common.EnumChatType.Notification);
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
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-grtp"), Vintagestory.API.Common.EnumChatType.Notification);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-grtp-2", (bsuconfig.Current.cooldownminutes - (count - grtptimer))), Vintagestory.API.Common.EnumChatType.Notification);
            }
            setbackteleport(player);
            player.Entity.TeleportTo(randx, height + 2, randz);
        }

        private void homeTeleport(IServerPlayer player,int homenum)
        {
            setbackteleport(player);
            if (homeSave.ContainsKey(player.Entity.PlayerUID))
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-home"), Vintagestory.API.Common.EnumChatType.Notification);
                List<BlockPos> homelist = new List<BlockPos>();
                homeSave.TryGetValue(player.Entity.PlayerUID,out homelist);
                int X = homelist[homenum].X;
                int Y = homelist[homenum].Y;
                int Z = homelist[homenum].Z;
                sapi.WorldManager.LoadChunkColumnPriority(X / sapi.WorldManager.ChunkSize, Z / sapi.WorldManager.ChunkSize);
                player.Entity.TeleportTo(X, Y, Z);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-home-saved"), Vintagestory.API.Common.EnumChatType.Notification);
                sapi.WorldManager.LoadChunkColumnPriority(sapi.World.DefaultSpawnPosition.XYZInt.X, sapi.World.DefaultSpawnPosition.XYZInt.Z);
                player.Entity.TeleportTo(sapi.World.DefaultSpawnPosition.XYZInt.X, sapi.World.DefaultSpawnPosition.XYZInt.Y, sapi.World.DefaultSpawnPosition.XYZInt.Z);
            }
        }

        private void spawnTeleport(IServerPlayer player)
        {
            setbackteleport(player);
            EntityPlayer byEntity = player.Entity; //Get the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-spawn"), Vintagestory.API.Common.EnumChatType.Notification);
            EntityPos spawnpoint = byEntity.World.DefaultSpawnPosition;
            byEntity.TeleportTo(spawnpoint);
        }

        private void backteleport(IServerPlayer player)
        {
            BlockPos newPos = player.Entity.Pos.AsBlockPos;
            if (backSave.ContainsKey(player.Entity.PlayerUID))
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-back"), Vintagestory.API.Common.EnumChatType.Notification);
                sapi.WorldManager.LoadChunkColumnPriority(backSave[player.Entity.PlayerUID].X / sapi.WorldManager.ChunkSize, backSave[player.Entity.PlayerUID].Z / sapi.WorldManager.ChunkSize);
                player.Entity.TeleportTo(backSave[player.Entity.PlayerUID].X, backSave[player.Entity.PlayerUID].Y, backSave[player.Entity.PlayerUID].Z);
                backSave.Remove(player.Entity.PlayerUID);
                backSave.Add(player.Entity.PlayerUID, newPos);
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-back-saved"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        //Set player's cooldown for (player, cooldown time, mod cmd name)
        //You must add the mod name to the if cmd == statement to update cooldowns
        private void setplayercooldown(IServerPlayer player, int? cdnum, string cmd)
        {
            if (player.Role.Code == "admin")
            {
                //int? cdnum = args.PopInt();
                if (cdnum == null )//|cdnum == 0)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-or-greater",0), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else if (cdnum < 0)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-or-greater",0), Vintagestory.API.Common.EnumChatType.Notification);
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
                    }else if (cmd == "tpt")
                    {
                        bsuconfig.Current.tptPlayerCooldown = cdnum;
                    }


                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldown-reusable",cmd,cdnum), Vintagestory.API.Common.EnumChatType.Notification);
                }

            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-permissions"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        //teleport to function
        private void teleportTo(IServerPlayer Splayer,string CMD)
        {
            IServerPlayer player = Splayer;
            string cmd = CMD;
            IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
            if (pdata == null)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wait-for-tp"), Vintagestory.API.Common.EnumChatType.Notification);
                    setbackteleport(player);
                    sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup,Lang.Get("bunnyserverutilities:tp-to-you", player.PlayerName), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:active-tp"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:pending-tp"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }
        private string checkCooldown(IServerPlayer player, string cmdname, int? modPlayerCooldown)
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
                        return "cooldownover";
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldown-player-reusable", ((playersactivecooldowntime + modPlayerCooldown) - count)), Vintagestory.API.Common.EnumChatType.Notification);
                        return "wait";
                    }
                }
                else
                {
                    return "newplayer";
                }
            }
            else
            {
                return "newdictionary";
            }
        }

    //add user to cooldown list
    private void addcooldown(string modname,IServerPlayer player,string cdaction)
        {
            switch (cdaction)
            {
                case "newdictionary":
                    cooldownDict.Add(modname, new Dictionary<string, int>());
                    cooldownDict[modname].Add(player.PlayerUID, count);
                    break;
                case "newplayer":
                    cooldownDict[modname].Add(player.PlayerUID, count);
                    break;
                case "wait":
                    break;
                case "cooldownover":
                    cooldownDict[modname].Remove(player.PlayerUID);
                    cooldownDict[modname].Add(player.PlayerUID, count);
                    break;

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
                        messageplace++;
                    }
                }
            }

        }

        //Check for cost and take payment function
        private bool processPayment(string item, int cost, IServerPlayer player, ItemSlot itemslot)
        {
            if (itemslot == null)
            {
                itemslot = player.InventoryManager.ActiveHotbarSlot;
            }
            if (bsuconfig.Current.teleportcostenabled == false) { return true; }
            if (cost <= 0) { return true; } //skip check if there is no cost
            Item itemstack = player.Entity.World.GetItem(new AssetLocation(item)); //Get our Item from the item name provided to the function
            Block blockstack = player.Entity.World.GetBlock(new AssetLocation(item));
            if (itemslot == null){
                if (itemstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else if(blockstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else { return false; }
                
                } //Check if the hotbar slot is null
            if (itemslot.Empty) //Check for an empty hotbar slot
            {
                if (itemstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else if (blockstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else { return false; }
            }
            if (itemstack == null)
            {

                if (blockstack == null)
                {
                    return false;
                }
                else
                {
                    if (itemslot.Itemstack.Block != null)
                    {
                        if (itemslot.Itemstack.Block.Code != blockstack.Code) //Check if the held item matches
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                            return false;
                        }
                        if (itemslot.Itemstack.StackSize < cost)//Check if the player has enough items
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough", player.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize, cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                            return false;
                        }
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-remove",blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification);
                        itemslot.TakeOut(cost); //Remove the items from the inventory
                        itemslot.MarkDirty(); //Update the client
                        return true; //Tell the calling function that the payment was sucessful
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong,
                        return false;
                    }
                }
            }
            if (itemslot.Itemstack.Item == null || itemstack.Code == null) {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
                 }
            if (itemslot.Itemstack.Item.Code == null)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            if (itemslot.Itemstack.Item.Code != itemstack.Code) //Check if the held item matches
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            if (itemslot.Itemstack.StackSize < cost)//Check if the player has enough items
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough", player.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize, cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-remove", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification);
            itemslot.TakeOut(cost); //Remove the items from the inventory
            itemslot.MarkDirty(); //Update the client
            return true; //Tell the calling function that the payment was sucessful
        }

        //Pre-payment function
        private bool prepay(string item, int cost, IServerPlayer player)
        {
            ItemSlot itemslot = player.InventoryManager.ActiveHotbarSlot;
            if (bsuconfig.Current.teleportcostenabled == false) { return true; }
            if (cost <= 0) { return true; } //skip check if there is no cost
            Item itemstack = player.Entity.World.GetItem(new AssetLocation(item)); //Get our Item from the item name provided to the function
            Block blockstack = player.Entity.World.GetBlock(new AssetLocation(item));
            if (itemslot == null){
                if (itemstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else if(blockstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else { return false; }
                
                } //Check if the hotbar slot is null
            if (itemslot.Empty) //Check for an empty hotbar slot
            {
                if (itemstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else if (blockstack != null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:empty-slot", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player their hand is empty, and what the cost is
                    return false;
                }
                else { return false; }
            }
            if (itemstack == null)
            {

                if (blockstack == null)
                {
                    return false;
                }
                else
                {
                    if (itemslot.Itemstack.Block != null)
                    {
                        if (itemslot.Itemstack.Block.Code != blockstack.Code) //Check if the held item matches
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                            return false;
                        }
                        if (itemslot.Itemstack.StackSize < cost)//Check if the player has enough items
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough", player.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize, cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                            return false;
                        }
                        if (tptcostdictionary.ContainsKey(player.PlayerName))
                        {
                            tptcostdictionary[player.PlayerName].item = item;
                            tptcostdictionary[player.PlayerName].qty = cost;
                            tptcostdictionary[player.PlayerName].slot = player.InventoryManager.ActiveHotbarSlot;

                        }
                        else
                        {
                            tptcostinfo newtptcostinfo = new tptcostinfo();
                            newtptcostinfo.item = item;
                            newtptcostinfo.qty = cost;
                            newtptcostinfo.slot = player.InventoryManager.ActiveHotbarSlot;
                            tptcostdictionary.Add(player.PlayerName, newtptcostinfo);
                        }
                        return true; //Tell the calling function that the payment was sucessful
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong,
                        return false;
                    }
                }
            }
            if (itemslot.Itemstack.Item == null || itemstack.Code == null) {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
                 }
            if (itemslot.Itemstack.Item.Code == null)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            if (itemslot.Itemstack.Item.Code != itemstack.Code) //Check if the held item matches
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wrong-item", itemstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            if (itemslot.Itemstack.StackSize < cost)//Check if the player has enough items
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough", player.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize, cost), Vintagestory.API.Common.EnumChatType.Notification); //Inform player the item is wrong, and what the cost is
                return false;
            }
            if (tptcostdictionary.ContainsKey(player.PlayerName))
            {
                tptcostdictionary[player.PlayerName].item = item;
                tptcostdictionary[player.PlayerName].qty = cost;
                tptcostdictionary[player.PlayerName].slot = player.InventoryManager.ActiveHotbarSlot;

            }
            else
            {
                tptcostinfo newtptcostinfo = new tptcostinfo();
                newtptcostinfo.item = item;
                newtptcostinfo.qty = cost;
                newtptcostinfo.slot = player.InventoryManager.ActiveHotbarSlot;
                tptcostdictionary.Add(player.PlayerName, newtptcostinfo);
            }



            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-dont-move"), Vintagestory.API.Common.EnumChatType.Notification);

            return true;
        }
        //Check cost item
        private bool checkcostitem(string itemCode)
        {
            Item itemstack = sapi.World.GetItem(new AssetLocation(itemCode)); //Get our Item from the item name provided to the function
            Block blockstack = sapi.World.GetBlock(new AssetLocation(itemCode));
            if (itemstack != null)
            {
                return true;
            }
            else if (blockstack != null) { return true; }
            else{ return false; }

        }

        private void MigrateHomeSave()
        {
            // Check if migration is needed
            if (!bsuconfig.Current.multihomemigration.HasValue || !bsuconfig.Current.multihomemigration.Value)
            {
                // Get the old data
                byte[] oldHomedata = sapi.WorldManager.SaveGame.GetData("bsuHome");
                if (oldHomedata != null)
                {
                    // Deserialize the old data
                    var oldHomeSave = SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(oldHomedata);

                    // Prepare a new dictionary
                    Dictionary<string, List<BlockPos>> newHomeSave = new Dictionary<string, List<BlockPos>>();

                    // Convert old data to the new format
                    foreach (var pair in oldHomeSave)
                    {
                        // If for some reason there's an existing entry in the new dictionary, append to it, else create new list
                        if (newHomeSave.ContainsKey(pair.Key))
                        {
                            newHomeSave[pair.Key].Add(pair.Value);
                        }
                        else
                        {
                            newHomeSave[pair.Key] = new List<BlockPos> { pair.Value };
                        }
                    }

                    // Update the homeSave variable
                    homeSave = newHomeSave;

                    // Serialize and save the new data
                    byte[] newHomedata = SerializerUtil.Serialize(newHomeSave);
                    sapi.WorldManager.SaveGame.StoreData("bsuHome", newHomedata);

                    // Set multihomemigration to true after migration
                    bsuconfig.Current.multihomemigration = true;
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
                List<string> keysToRemove = new List<string>();

                foreach (var keyvalue in bsuconfig.Current.tptDict.Keys)
                {
                    tptinfo value = new tptinfo();
                    var dic = bsuconfig.Current.tptDict.Values;
                    bsuconfig.Current.tptDict.TryGetValue(keyvalue, out value);

                    if ((count - value.timer) >= 2)
                    {
                        keysToRemove.Add(keyvalue);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (bsuconfig.Current.tptDict.ContainsKey(key))
                    {
                        bsuconfig.Current.tptDict.Remove(key);
                    }

                    // Iterate through waitDict and remove any entries where the value matches the key
                    var waitDictKeys = bsuconfig.Current.waitDict.Where(pair => pair.Value == key).Select(pair => pair.Key).ToList();

                    foreach (var waitKey in waitDictKeys)
                    {
                        if (bsuconfig.Current.waitDict.ContainsKey(waitKey))
                        {
                            bsuconfig.Current.waitDict.Remove(waitKey);
                        }
                    }
                }

                if (keysToRemove.Count > 0)
                {
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
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
                        if (bsuconfig.Current.cooldownDict.ContainsKey(keyvalue))
                        {
                            bsuconfig.Current.cooldownDict.Remove(keyvalue);
                        }
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
                    sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:new-grtp", bsuconfig.Current.cooldownminutes), Vintagestory.API.Common.EnumChatType.Notification);

                }

                loaded = true;
            }
            //rtp check chunk
            if (rtprandx / sapi.WorldManager.ChunkSize == chunkCoord.X & (rtprandz / sapi.WorldManager.ChunkSize == chunkCoord.Y) & (teleporting == true))
            {
                Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:new-rtp"), Vintagestory.API.Common.EnumChatType.Notification);
                BlockPos checkheight = new BlockPos();
                checkheight.X = rtprandx;
                checkheight.Y = 1;
                checkheight.Z = rtprandz;
                int height = sapi.World.BlockAccessor.GetTerrainMapheightAt(checkheight);
                GEntity.TeleportTo(rtprandx, height + 1, rtprandz);
                teleporting = false;
            }

            if (imx / sapi.WorldManager.ChunkSize == chunkCoord.X & (imz / sapi.WorldManager.ChunkSize == chunkCoord.Y) & (imteleporting == true))
            {
                implayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-teleporting"), Vintagestory.API.Common.EnumChatType.Notification);
                BlockPos checkheight = new BlockPos();
                checkheight.X = imx;
                checkheight.Y = 1;
                checkheight.Z = imz;
                int imheight = sapi.World.BlockAccessor.GetTerrainMapheightAt(checkheight);
                implayer.Entity.TeleportTo(imx, imheight + 1, imz);
                imteleporting = false;
                implayer.InventoryManager.DiscardAll();
            }

        }
        private void OnSaveGameSaving()
        {
            sapi.WorldManager.SaveGame.StoreData("bsuBack", SerializerUtil.Serialize(backSave));
            sapi.WorldManager.SaveGame.StoreData("bsuHome", SerializerUtil.Serialize(homeSave));
            sapi.WorldManager.SaveGame.StoreData("ironman", SerializerUtil.Serialize(ironManPlayerList));
            sapi.WorldManager.SaveGame.StoreData("ironmancurrent", SerializerUtil.Serialize(currentironmandict));
            sapi.WorldManager.SaveGame.StoreData("ironmanhighscores", SerializerUtil.Serialize(ironmanhighscores));
            sapi.WorldManager.SaveGame.StoreData("bsuReply", SerializerUtil.Serialize(replySave));
            
        }

        private void OnSaveGameLoading()
        {

            byte[] backdata = sapi.WorldManager.SaveGame.GetData("bsuBack");
            byte[] homedata = sapi.WorldManager.SaveGame.GetData("bsuHome");
            byte[] ironmandata = sapi.WorldManager.SaveGame.GetData("ironman");
            byte[] currentironmandata = sapi.WorldManager.SaveGame.GetData("ironmancurrent");
            byte[] ironmanhighscoredata = sapi.WorldManager.SaveGame.GetData("ironmanhighscores");
            byte[] replydata = sapi.WorldManager.SaveGame.GetData("bsuReply");

            backSave = backdata == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(backdata);
            homeSave = homedata == null ? new Dictionary<string, List<BlockPos>>() : SerializerUtil.Deserialize<Dictionary<string, List<BlockPos>>>(homedata);
            ironManPlayerList = ironmandata == null ? new List<string>() : SerializerUtil.Deserialize<List<string>>(ironmandata);
            currentironmandict = currentironmandata == null ? new Dictionary<string, double>() : SerializerUtil.Deserialize<Dictionary<string, double>>(currentironmandata);
            ironmanhighscores = ironmanhighscoredata == null ? new Dictionary<string, int>() : SerializerUtil.Deserialize<Dictionary<string, int>>(ironmanhighscoredata);
            replySave = replydata == null ? new Dictionary<string, string>() : SerializerUtil.Deserialize<Dictionary<string, string>>(replydata);
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
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:on-death-back"), Vintagestory.API.Common.EnumChatType.Notification);
            }
            if(ironManPlayerList.Contains(player.PlayerUID)){
                ironManPlayerList.Remove(player.PlayerUID);
                sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:ironman-ended", player.PlayerName), Vintagestory.API.Common.EnumChatType.AllGroups);
                PlayerSpawnPos oldspawn = new PlayerSpawnPos();
                oldspawn.x = (((int)sapi.World.DefaultSpawnPosition.X));
                oldspawn.y = (((int)sapi.World.DefaultSpawnPosition.Y));
                oldspawn.z = (((int)sapi.World.DefaultSpawnPosition.Z));
                player.SetSpawnPosition(oldspawn);

                double value;
                currentironmandict.TryGetValue(player.PlayerUID, out value);
                Double elapsedDays = sapi.World.Calendar.TotalDays - value;
                if (ironmanhighscores.ContainsKey(player.PlayerUID))
                {
                    int highscore;
                    ironmanhighscores.TryGetValue(player.PlayerUID, out highscore);
                    if ((int)elapsedDays > highscore)
                    {
                        sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:ironman-new-personal-highscore", player.PlayerName), Vintagestory.API.Common.EnumChatType.AllGroups);
                        ironmanhighscores.Remove(player.PlayerUID);
                        currentironmandict.Remove(player.PlayerUID);
                        ironmanhighscores.Add(player.PlayerUID, (int)elapsedDays);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-no-new-score",highscore), Vintagestory.API.Common.EnumChatType.Notification);
                        currentironmandict.Remove(player.PlayerUID);
                    }
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-first"), Vintagestory.API.Common.EnumChatType.Notification);
                    currentironmandict.Remove(player.PlayerUID);
                    ironmanhighscores.Add(player.PlayerUID, (int)elapsedDays);
                }
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
                        sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:welcome-player",byPlayer.PlayerName), Vintagestory.API.Common.EnumChatType.AllGroups);
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
                        sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:welcome-rs", byPlayer.PlayerName), Vintagestory.API.Common.EnumChatType.AllGroups);
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
                IPlayer[] aoplayers = sapi.World.AllOnlinePlayers;
                byPlayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:players-online") + aoplayers.Length.ToString(), Vintagestory.API.Common.EnumChatType.Notification);
                string players = " ";
                for (int i = 0;i < aoplayers.Length; i++)
                {
                    players = players + aoplayers[i].PlayerName + " "+Lang.Get("bunnyserverutilities:player-divider")+" ";
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
                for (var i = 0; i < playerList.Length; i++)
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

        public class tptcostinfo
        {
            public ItemSlot slot = null;
            public string item = null;
            public int qty = 0;

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
            public bool? enableSetHome;
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
            public int? homelimit; //How many homes a player can set
            public bool? enablePermissions;
            public bool? homesImported;
            public bool? multihomemigration;


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
            public int? tptPlayerCooldown;

            //Random Teleport Properties
            //public int? rtpcooldownminutes; //How long the player must wait to teleport
            public int? rtpradius; //How far the player can teleport
            public int? cooldownduration; //how long between RTP teleports
            public Dictionary<String, int> cooldownDict { get; set; }

            //userwarning properties
            public Dictionary<String, userwarning> warningDict;
            

            //ironman properties
            public bool? enableironman;

            //Teleport cost configs
            public bool teleportcostenabled;
            public string homecostitem;
            public int homecostqty;
            public string backcostitem;
            public int backcostqty;
            public string spawncostitem;
            public int spawncostqty;
            public string rtpcostitem;
            public int rtpcostqty;
            public string grtpcostitem;
            public int grtpcostqty;
            public string tptcostitem;
            public int tptcostqty;
            public string sethomecostitem;
            public int sethomecostqty;

            public static bsuconfig getDefault()
            {
                var config = new bsuconfig();
                BlockPos defPos = new BlockPos(0, 0, 0);
                bool perms = false;
                List<String> dmessages = new List<string> //SSM default dmessages
                {
                    Lang.Get("bunnyserverutilities:default-welcome")
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
                config.homelimit = 1;
                config.multihomemigration = false;

                //enable/disable module defaults
                config.enableBack = true;
                config.enableHome = true;
                config.enableSetHome = true;
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
                config.enableironman = true;


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
                config.tptPlayerCooldown = 1;

                //Random Teleport defaults
                config.rtpradius = 100000;
                config.cooldownduration = 15;
                config.cooldownDict = new Dictionary<string, int> //Dictionary to hold JRTP cooldown
                {
                    { "Default",1}
                };

                //user warning defaults
                config.warningDict = warningdictionary;

                //Teleport Costs
                config.teleportcostenabled = false;
                config.homecostitem = "game:gear-rusty";
                config.homecostqty = 0;
                config.backcostitem = "game:gear-rusty";
                config.backcostqty = 0;
                config.spawncostitem = "game:gear-rusty";
                config.spawncostqty = 0;
                config.rtpcostitem = "game:gear-rusty";
                config.rtpcostqty = 0;
                config.grtpcostitem = "game:gear-rusty";
                config.grtpcostqty = 0;
                config.tptcostitem = "game:gear-rusty";
                config.tptcostqty = 0;


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
