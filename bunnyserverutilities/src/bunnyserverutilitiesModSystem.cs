
using HarmonyLib;
using privileges.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace bunnyserverutilities
{
    public class bunnyserverutilitiesModSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server; //load on the server side
        }

        //=======================//
        //Variable Initialization//
        //=======================//



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
        public List<string> TempironManList = new List<string> { "default" }; //Holds the players names before they confirm
        int imx, imz = 0;
        bool imteleporting = false;
        IServerPlayer implayer;
        Dictionary<string, double> currentironmandict = new Dictionary<string, double>();
        Dictionary<string, int> ironmanhighscores = new Dictionary<string, int>();

        //Teleport Cost Initialization
        Dictionary<string, tptCostInfo> tptcostdictionary = new Dictionary<string, tptCostInfo>();

        Dictionary<string, string> replySave = new Dictionary<string, string>();

        public override void StartServerSide(ICoreServerAPI api)
        {


            //Start and assign APIs
            base.StartServerSide(api);
            sapi = api;
            ipm = api.Permissions;


            //===============//
            //Event Listeners//
            //===============//

            api.Event.PlayerDeath += OnPlayerDeath; // /back listens for the player's death
            api.Event.SaveGameLoaded += OnSaveGameLoading; // Load our data each game load
            api.Event.GameWorldSave += OnSaveGameSaving; // Save our data each game save
            api.Event.ChunkColumnLoaded += OnChunkColumnLoaded; // /grtp and /rtp use this to check for their chunk to be loaded
            api.Event.PlayerCreate += OnPlayerCreate; // Used by join announce and rising sun to track new players
            api.Event.PlayerNowPlaying += onNowPlaying; // Used by join announce and rising sun to tell when players are loaded into the game
            api.Event.PlayerChat += onPlayerChat; // Used by BunnyBell to read in player chat and check for names

            //////////End Event Listeners//////////



            //=================//
            //register commands//
            //=================//

            //Bunny Server Utilities Commands
            api.ChatCommands.Create("bsu")
                .WithDescription("Information regarding Bunny's Server Utility mod")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));
            api.ChatCommands.Create("bunnyserverutility")
                .WithDescription("Information regarding Bunny's Server Utility mod")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));
            api.ChatCommands.Create("bunnyserverutilities")
                .WithDescription("Information regarding Bunny's Server Utility mod")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));

            //Home Commands
            api.ChatCommands.Create("sethome")
                .WithDescription("Set your current position as home for teleports.")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(cmd_sethome));
            api.ChatCommands.Create("home")
                .WithDescription("Teleport to your set home.")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_home));

            //Back Commands
            api.ChatCommands.Create("back")
                .WithDescription("Return you to the point where you used your last teleport.")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_back));

            //Spawn Commands
            api.ChatCommands.Create("spawn")
                .WithDescription("Teleport to the spawn point of the world.")
                .RequiresPrivilege(BPrivilege.spawn)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_spawn));

            //tpcost Commands
            api.ChatCommands.Create("tpcost")
                .WithDescription("Enable or disable item costs for teleports.")
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_tpcost));

            //GRTP Commands
            api.ChatCommands.Create("grtp")
                .WithDescription("Randomly Teleports the player to a group location")
                .RequiresPrivilege(APrivilege.grtp)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_grtp));

            //////////End Register Commands//////////

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
            ipm.RegisterPrivilege("ironman", "Iron Man");

            //////////End Register Privileges//////////



            //======================//
            //Check config for nulls//
            //======================//

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

            //////////End Check config for NULL//////////



            //======================//
            //Register Permissions  //
            //======================//

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
                ipm.AddPrivilegeToGroup("admin", privileges.src.JPrivilege.ironman);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.JPrivilege.ironman);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.JPrivilege.ironman);
                //Add sethome permissions to all standard groups
                ipm.AddPrivilegeToGroup("admin", privileges.src.KPrivilege.sethome);
                ipm.AddPrivilegeToGroup("suplayer", privileges.src.KPrivilege.sethome);
                ipm.AddPrivilegeToGroup("doplayer", privileges.src.KPrivilege.sethome);
            }

            //////////End Register Permissions//////////



            //GRTP count and event listener set at server startup
            grtptimer = 0; //This puts the cooldown timer as expired and will force a new GRTP location
            count = (int)bsuconfig.Current.cooldownminutes;//grtp cooldown timer
            CID = api.Event.RegisterGameTickListener(CoolDown, 60000); //Check the cooldown timer every 1 minute
            int broadcastFrequency = (int)bsuconfig.Current.frequency; //SSM cooldown timer

        }

        //========//
        //COMMAND Functions//
        //========//
        private TextCommandResult Cmd_bsu(TextCommandCallingArgs args)
        {
            string cmd = args[0] as String;
            switch (cmd)
            {
                case "help":
                    //displayhelp(player, "all");
                    return TextCommandResult.Success("/bsu [Help|Version]");
                case "version":
                    var modinfo = Mod.Info;
                    return TextCommandResult.Success("Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version);
                default:
                    return TextCommandResult.Success("/bsu [Help|Version]");
            }
        }

        //Set Home command
        private TextCommandResult cmd_sethome(TextCommandCallingArgs args)
        {
            IServerPlayer[] playerlist = sapi.Server.Players;
            string playerUID = args.Caller.Player.PlayerUID;// the playerUID you have
            IServerPlayer player = null;

            foreach (IServerPlayer p in playerlist)
            {
                if (p.PlayerUID == playerUID)
                {
                    player = p;
                    break;
                }
            }
            string cmdname = "sethome";
            string cmd = args.ArgCount > 0 ? args[0] as String : null;
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
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-sethome"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user that they have set their home
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-sethome"));
                            }
                        }
                        else
                        {
                            //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-save"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-save"));
                        }
                    }
                    return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enabled", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enabled", cmdname));
                    }
                    else
                    {
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", cmdname));
                    }
                    else
                    {
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args[1] as String;
                        if (code == null)
                        {
                            //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.sethomecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        //Old Message// .SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.sethomecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    //displayhelp(player, cmdname); //ENABLE THIS ONCE DISPLAYHELP IS REBUILT
                    return TextCommandResult.Success("Placeholder for Displayhelp: Home. Fix this before release"); //THIS NEEDS REPLACED BEFORE RELEASE
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
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount + 1), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, homecount + 1));
                            }
                            else if (input > bsuconfig.Current.homelimit)
                            {
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit));
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
                                    //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-sethome"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user that they have set their home
                                    return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-sethome"));
                                }
                                else
                                {
                                    return TextCommandResult.Success("Placeholder Text. Please inform FunnybunnyofDOOM if you recieve this message. This should have been removed before release.");
                                }
                            }
                            else
                            {
                                //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(input);
                            //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //Old Message// player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                    }
            }

        }


        //home command
        private TextCommandResult Cmd_home(TextCommandCallingArgs args)
        {
            IServerPlayer[] playerlist = sapi.Server.Players;
            string playerUID = args.Caller.Player.PlayerUID;// the playerUID you have
            IServerPlayer player = null;

            foreach (IServerPlayer p in playerlist)
            {
                if (p.PlayerUID == playerUID)
                {
                    player = p;
                    break;
                }
            }

            string cmdname = "home";
            string cmd = args.ArgCount > 0 ? args[0] as String : null;
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
                            return TextCommandResult.Success(" "); //This may need to be changed to a confirmation message
                        }
                        else if (homecount2 == 0)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-home"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-home"));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-number"));
                        }
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable-home"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable-home"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable-home"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable-home"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.ArgCount > 1 ? args[1] as String : null;
                        if (code == null)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.homecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.homecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    //displayhelp(player, cmdname); //ENABLE THIS when display help is added
                    return TextCommandResult.Success(" ");
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numbr) && numbr >= 0)
                    {
                        int? cooldownnumber = numbr as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Success("Player Cooldown Set");
                    }
                    else
                    {
                        return TextCommandResult.Success("Player Cooldown could not be Set");
                    }
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
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, homecount));
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
                                return TextCommandResult.Success();
                            }
                            else if (homecount == 0)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-home"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-home"));
                            }
                            else if (input < 1)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, homecount), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, homecount));
                            }
                            else
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit), Vintagestory.API.Common.EnumChatType.Notification); //Tell player to use a number lower than the homelimit
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit));
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(input);
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-home"), Vintagestory.API.Common.EnumChatType.Notification); //Inform user home is disabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                    }
                case "limit":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? num = args.ArgCount > 1 ? args[1] as int? : null;
                        if (num != null && num > 0)
                        {
                            bsuconfig.Current.homelimit = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-limit-updated", num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-limit-updated", num));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }

                case "delete":
                    int? numb = args.ArgCount > 1 ? args[1] as int? : null; ;
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
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-homes", numb), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-homes", numb));
                    }
                    else if (numb == null)
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                    }
                    else
                    {
                        homelist3.RemoveAt((int)numb - 1);
                        homeSave.Remove(playerID3);
                        homeSave.Add(playerID3, homelist3);
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:home-removed", numb), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-removed", numb));
                    }

            }
        }

        //back command
        private TextCommandResult Cmd_back(TextCommandCallingArgs args)
        {
            IServerPlayer[] playerlist = sapi.Server.Players;
            string playerUID = args.Caller.Player.PlayerUID;
            IServerPlayer player = null;

            foreach (IServerPlayer p in playerlist)
            {
                if (p.PlayerUID == playerUID)
                {
                    player = p;
                    break;
                }
            }
            string cmdname = "back";
            string cmd = args.ArgCount > 0 ? args[0] as String : null;
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
                        return TextCommandResult.Success(" ");
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-back"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-back"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBack = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable-back"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable-back"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBack = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable-back"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable-back"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    //displayhelp(player, cmdname); //THIS NEEDS ENABLED ONCE DISPLAY HELP IS ENABLED
                    return TextCommandResult.Success("In Development");
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.ArgCount > 1 ? args[1] as String : null;
                        if (code == null)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.backcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.backcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Success("Player Cooldown Set");
                    }
                    else
                    {
                        return TextCommandResult.Success("Player Cooldown could not be Set");
                    }
            }
            return TextCommandResult.Success("In Development");
        }


        //spawn command
        private TextCommandResult Cmd_spawn(TextCommandCallingArgs args) //spawn command
        {
            IServerPlayer[] playerlist = sapi.Server.Players; //Get our list of players
            string playerUID = args.Caller.Player.PlayerUID;  //Get the playeruid of the caller of this command 
            IServerPlayer player = null; //A player object to hold our player

            foreach (IServerPlayer p in playerlist) //Search through the playerlist
            {
                if (p.PlayerUID == playerUID) //Check against the UID from the command call
                {
                    player = p; //Assign the player object to player if it's the matching PlayerUID
                    break; //End the foreach loop
                }
            }
            string cmdname = "spawn";
            string cmd = args.ArgCount > 0 ? args[0] as String : null;
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableSpawn == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {

                        string cooldownstate = checkCooldown(player, cmdname, bsuconfig.Current.spawnPlayerCooldown);
                        if (cooldownstate != "wait")
                        {
                            if (processPayment(bsuconfig.Current.spawncostitem, bsuconfig.Current.spawncostqty, player, null))
                            {
                                spawnTeleport(player);
                                addcooldown(cmdname, player, cooldownstate);
                            }
                        }
                        //return TextCommandResult.Success("In Development 1127");
                        return TextCommandResult.Deferred;
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", "spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", "spawn"));
                    }

                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "spawn"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "spawn"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "spawn"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    //displayhelp(player, cmdname); //This needs fixed when the
                    return TextCommandResult.Success("In Development"); //This needs fixed when the help command is complete
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Success("Player Cooldown Set");
                    }
                    else
                    {
                        return TextCommandResult.Success("Player Cooldown could not be Set");
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        
                        string code = args.ArgCount > 1 ? args[1] as String : null;
                        if (code == null)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.spawncostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                    //break;
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.spawncostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }                    
            }
            return TextCommandResult.Success("In Development");
        }


        //cost command
        private TextCommandResult Cmd_tpcost(TextCommandCallingArgs args)
        {
            IServerPlayer[] playerlist = sapi.Server.Players; //Get our list of players
            string playerUID = args.Caller.Player.PlayerUID;  //Get the playeruid of the caller of this command 
            IServerPlayer player = null; //A player object to hold our player

            foreach (IServerPlayer p in playerlist) //Search through the playerlist
            {
                if (p.PlayerUID == playerUID) //Check against the UID from the command call
                {
                    player = p; //Assign the player object to player if it's the matching PlayerUID
                    break; //End the foreach loop
                }
            }

            if (player.Role.Code == "admin" || player.HasPrivilege("tptadmin"))
            {
                string cmdname = "tpcost";
                string cmd = args.ArgCount > 0 ? args[0] as String : null;
                switch (cmd)
                {
                    case "help":
                        //displayhelp(player, cmdname);
                        return TextCommandResult.Success("In Development");
                    case "enable":
                        bsuconfig.Current.teleportcostenabled = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", cmdname));
                    case "disable":
                        bsuconfig.Current.teleportcostenabled = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", cmdname), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", cmdname));
                    case null:
                        return TextCommandResult.Success("[Help|Enable|Disable]");
                }
                return TextCommandResult.Success("[Help|Enable|Disable]");
            }
            else
            {
                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", "tptadmin"), Vintagestory.API.Common.EnumChatType.Notification);
                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", "tptadmin"));
            }
        }

        //grtp command
        private TextCommandResult Cmd_grtp(TextCommandCallingArgs args)
        {
            IServerPlayer[] playerlist = sapi.Server.Players; //Get our list of players
            string playerUID = args.Caller.Player.PlayerUID;  //Get the playeruid of the caller of this command 
            IServerPlayer player = null; //A player object to hold our player

            foreach (IServerPlayer p in playerlist) //Search through the playerlist
            {
                if (p.PlayerUID == playerUID) //Check against the UID from the command call
                {
                    player = p; //Assign the player object to player if it's the matching PlayerUID
                    break; //End the foreach loop
                }
            }

            string cmdname = "grtp";
            string cmd = args.ArgCount > 0 ? args[0] as String : null;
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
                                    if (processPayment(bsuconfig.Current.grtpcostitem, bsuconfig.Current.grtpcostqty, player, null))
                                    {
                                        grtpteleport(player);
                                        addcooldown(cmdname, player, cooldownstate);
                                        return TextCommandResult.Deferred;
                                    }
                                    return TextCommandResult.Deferred;
                                }
                                return TextCommandResult.Deferred;
                            }
                            else
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:chunkloading-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that the chunk is still loading                               
                                sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:chunkloading-grtp"));
                            }
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:notset-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that GRTP location is not yet set
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:notset-grtp"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-commands-disabled"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", "grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that GRTP is disabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", "grtp"));
                    }
                case "help":
                    //displayhelp(player, cmdname); //Fix this when help function is done
                    return TextCommandResult.Success("In Developmet: GRTP Help");
                case "cooldown":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int cdnum = 0; // Initialize cdnum to a default value
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum == 0)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:greater-than", 0));
                        }
                        else if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum < 0)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                        else
                        {
                            bsuconfig.Current.cooldownminutes = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cooldown-set-grtp", cdnum));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "radius":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int cdnum = 0; // Initialize cdnum to a default value
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum >= 10)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:greater-than", 10), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user for a number greater than 10
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:greater-than", 9));
                        }
                        else if (cdnum < 0)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user for a non-negative number
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                        else
                        {
                            bsuconfig.Current.teleportradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:set-radius-grtp", cdnum), Vintagestory.API.Common.EnumChatType.Notification); //Inform the user that the radius was set
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:set-radius-grtp", cdnum));
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "now":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        grtptimer = 0 - (int)bsuconfig.Current.cooldownminutes;
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wait-now-grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user to wait while the GRTP teleport location is updated
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:wait-now-grtp"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:enable", "grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user that GRTP has been enabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "grtp"));
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disable", "grtp"), Vintagestory.API.Common.EnumChatType.Notification); //Informs the user that GRTP has been disabled
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "grtp"));
                    }
                    else
                    {
                        // player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Success("Player Cooldown Set");
                    }
                    else
                    {
                        return TextCommandResult.Success("Player Cooldown could not be Set");
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {

                        string code = args.ArgCount > 1 ? args[1] as String : null;
                        if (code == null)
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-need-item"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.grtpcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.grtpcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:non-negative-number"), Vintagestory.API.Common.EnumChatType.Notification);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }

            }
            return TextCommandResult.Deferred;
        }

        //////////End Command Functions//////////



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

        private void homeTeleport(IServerPlayer player, int homenum)
        {
            setbackteleport(player);
            if (homeSave.ContainsKey(player.Entity.PlayerUID))
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:teleporting-home"), Vintagestory.API.Common.EnumChatType.Notification);
                List<BlockPos> homelist = new List<BlockPos>();
                homeSave.TryGetValue(player.Entity.PlayerUID, out homelist);
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
                if (cdnum == null)//|cdnum == 0)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-or-greater", 0), Vintagestory.API.Common.EnumChatType.Notification);
                }
                else if (cdnum < 0)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-or-greater", 0), Vintagestory.API.Common.EnumChatType.Notification);
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
                    else if (cmd == "tpt")
                    {
                        bsuconfig.Current.tptPlayerCooldown = cdnum;
                    }


                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cooldown-reusable", cmd, cdnum), Vintagestory.API.Common.EnumChatType.Notification);
                }

            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:no-permissions"), Vintagestory.API.Common.EnumChatType.Notification);
            }
        }

        //teleport to function
        private void teleportTo(IServerPlayer Splayer, string CMD)
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
                    tptInfo info = new tptInfo();
                    info.toplayer = pdata.PlayerUID;
                    info.haspermission = false;
                    info.waiting = true;
                    info.timer = count;
                    bsuconfig.Current.tptDict.Add(player.PlayerUID, info);
                    bsuconfig.Current.waitDict.Add(pdata.PlayerUID, player.PlayerUID);
                    sapi.StoreModConfig(bsuconfig.Current, "tptconfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:wait-for-tp"), Vintagestory.API.Common.EnumChatType.Notification);
                    setbackteleport(player);
                    sapi.SendMessage(sapi.World.PlayerByUid(pdata.PlayerUID), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:tp-to-you", player.PlayerName), Vintagestory.API.Common.EnumChatType.Notification);
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
        private void addcooldown(string modname, IServerPlayer player, string cdaction)
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
            if (itemslot == null)
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:cost-remove", blockstack.GetHeldItemName(itemslot.Itemstack), cost), Vintagestory.API.Common.EnumChatType.Notification);
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
            if (itemslot.Itemstack.Item == null || itemstack.Code == null)
            {
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
            if (itemslot == null)
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
                            tptCostInfo newtptcostinfo = new tptCostInfo();
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
            if (itemslot.Itemstack.Item == null || itemstack.Code == null)
            {
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
                tptCostInfo newtptcostinfo = new tptCostInfo();
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
            else { return false; }

        }


        /////////End Other Functions////////



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
                    tptInfo value = new tptInfo();
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

                    // Create a list to hold the keys to remove from waitDict
                    List<string> waitDictKeys = new List<string>();

                    // Iterate through waitDict and add any keys where the value matches the key to waitDictKeys
                    foreach (var pair in bsuconfig.Current.waitDict)
                    {
                        if (pair.Value == key)
                        {
                            waitDictKeys.Add(pair.Key);
                        }
                    }

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
            if (ironManPlayerList.Contains(player.PlayerUID))
            {
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-no-new-score", highscore), Vintagestory.API.Common.EnumChatType.Notification);
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
                        sapi.BroadcastMessageToAllGroups(Lang.Get("bunnyserverutilities:welcome-player", byPlayer.PlayerName), Vintagestory.API.Common.EnumChatType.AllGroups);
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
            if (bsuconfig.Current.enablejoinmessage == true)
            {
                IPlayer[] aoplayers = sapi.World.AllOnlinePlayers;
                byPlayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:players-online") + aoplayers.Length.ToString(), Vintagestory.API.Common.EnumChatType.Notification);
                string players = " ";
                for (int i = 0; i < aoplayers.Length; i++)
                {
                    players = players + aoplayers[i].PlayerName + " " + Lang.Get("bunnyserverutilities:player-divider") + " ";
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

        //////////End Event Listener Functions//////////



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
            public Dictionary<String, tptInfo> tptDict;
            public Dictionary<String, String> waitDict;
            public int? tptPlayerCooldown;

            //Random Teleport Properties
            //public int? rtpcooldownminutes; //How long the player must wait to teleport
            public int? rtpradius; //How far the player can teleport
            public int? cooldownduration; //how long between RTP teleports
            public Dictionary<String, int> cooldownDict { get; set; }

            //userwarning properties
            public Dictionary<String, userWarning> warningDict;


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
                Dictionary<String, tptInfo> tptdictionary = new Dictionary<string, tptInfo> //Dictionary to hold Teleport To info
                {
                    { "Default",new tptInfo() }
                };
                Dictionary<String, String> waitdictionary = new Dictionary<string, string> //Dictionary to hold cooldowns per player
                {
                    { "Default","Default"}
                };
                Dictionary<String, userWarning> warningdictionary = new Dictionary<string, userWarning>
                {
                    {"Default",new userWarning() }
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

        //////////End Config Functions//////////


        //=======//
        //Classes//
        //=======//

        public class tptInfo
        {

            public String toplayer;
            public Boolean haspermission;
            public Boolean waiting;
            public int timer;

        }

        public class userWarning
        {

            public String playeruid = "default";
            public String playername = "null";
            public int warnings = 0;
            public String ipaddress = "null";
            public List<String> reasons = new List<string>();

        }

        public class tptCostInfo
        {
            public ItemSlot slot = null;
            public string item = null;
            public int qty = 0;

        }

        //////////End Classes//////////

    }









}