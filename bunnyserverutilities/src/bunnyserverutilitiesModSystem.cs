
using privileges.src;
using ProperVersion;
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
using static bunnyserverutilities.bunnyserverutilitiesModSystem;

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
                .WithDescription(Lang.Get("desc-bsu"))
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));
            api.ChatCommands.Create("bunnyserverutility")
                .WithDescription(Lang.Get("desc-bsu"))
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));
            api.ChatCommands.Create("bunnyserverutilities")
                .WithDescription(Lang.Get("desc-bsu"))
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));

            //Home Commands
            api.ChatCommands.Create("sethome")
                .WithDescription(Lang.Get("desc-sethome"))
                .RequiresPrivilege(CPrivilege.home)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(cmd_sethome));
            api.ChatCommands.Create("home")
                .WithDescription(Lang.Get("desc-home"))
                .RequiresPrivilege(CPrivilege.home)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_home));

            //Back Commands
            api.ChatCommands.Create("back")
                .WithDescription(Lang.Get("desc-back"))
                .RequiresPrivilege(DPrivilege.back)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_back));

            //Spawn Commands
            api.ChatCommands.Create("spawn")
                .WithDescription(Lang.Get("desc-spawn"))
                .RequiresPrivilege(BPrivilege.spawn)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_spawn));

            //tpcost Commands
            api.ChatCommands.Create("tpcost")
                .WithDescription(Lang.Get("desc-tpcost"))
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_tpcost));

            //GRTP Commands
            api.ChatCommands.Create("grtp")
                .WithDescription(Lang.Get("desc-grtp"))
                .RequiresPrivilege(APrivilege.grtp)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_grtp));

            //Join Announce Commands
            api.ChatCommands.Create("joinannounce")
                .WithDescription(Lang.Get("desc-joinannounce"))
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_joinannounce));

            //Rising Sun Commands
            api.ChatCommands.Create("rs")
                .WithDescription(Lang.Get("desc-rs"))
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("secondArg"))
                .HandleWith(new OnCommandDelegate(Cmd_rs));

            //Bunnybell Commands
            api.ChatCommands.Create("bb")
                .WithDescription(Lang.Get("desc-bb"))
                .RequiresPrivilege(Privilege.controlserver) //This will need changed if we allow non-admins to change their own sound settings
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_bb));

            //Remove Deny Commands
            api.ChatCommands.Create("removedeny")
                .WithDescription(Lang.Get("desc-removedeny"))
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"), api.ChatCommands.Parsers.OptionalWord("cmd2"))
                .HandleWith(new OnCommandDelegate(Cmd_removedeny));
            
            //Remove Deny Commands
            api.ChatCommands.Create("cooldowns")
                .WithDescription(Lang.Get("desc-cooldowns"))
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_cooldown));
            
            //Simple Server Messages Commands
            api.ChatCommands.Create("ssm")
                .WithDescription(Lang.Get("desc-ssm"))
                .RequiresPrivilege(FPrivilege.ssm) //Consider changing this to FPrivilege.admin and giving normal players access to /ssm list
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_ssm));

            //Warn Commands
            api.ChatCommands.Create("warn")
                .WithDescription(Lang.Get("desc-warn"))
                .RequiresPrivilege(IPrivilege.warn)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_warn));

            //TPT Commands
            api.ChatCommands.Create("tpt")
                .WithDescription(Lang.Get("desc-tpt"))
                .RequiresPrivilege(GPrivilege.tpt)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_tpt));
            api.ChatCommands.Create("tpaccept")
                .WithDescription(Lang.Get("desc-tpaccept"))
                .RequiresPrivilege(GPrivilege.tpt)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_tpaccept));
            api.ChatCommands.Create("tpdeny")
                .WithDescription(Lang.Get("desc-tpdeny"))
                .RequiresPrivilege(GPrivilege.tpt)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_tpdeny));

            //RTP Commands
            api.ChatCommands.Create("rtp")
                .WithDescription(Lang.Get("desc-rtp"))
                .RequiresPrivilege(HPrivilege.rtp)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_rtp));

            //Ironman Commands
            api.ChatCommands.Create("ironman")
                .WithDescription(Lang.Get("desc-ironman"))
                .RequiresPrivilege(JPrivilege.ironman)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_ironman));

            //Just Private Message Commands
            api.ChatCommands.Create("jpm")
                .WithDescription("[help | enable | disable]")
                .RequiresPrivilege(EPrivilege.jpmadmin)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_jpm));
            api.ChatCommands.Create("dm")
                .WithDescription(Lang.Get("desc-dm"))
                .RequiresPrivilege(EPrivilege.jpm)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_pm));
            api.ChatCommands.Create("reply")
                .WithDescription(Lang.Get("desc-dm"))
                .RequiresPrivilege(EPrivilege.jpm)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_reply));
            api.ChatCommands.Create("r")
                .WithDescription(Lang.Get("desc-dm"))
                .RequiresPrivilege(EPrivilege.jpm)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Unparsed("cmd"))
                .HandleWith(new OnCommandDelegate(Cmd_reply));

            //////////End Register Commands//////////

            //===================//
            //Register Privileges//
            //===================//

            //Home privileges
            ipm.RegisterPrivilege("sethome", Lang.Get("desc-sethome"));
            ipm.RegisterPrivilege("home", Lang.Get("desc-home"));
            ipm.RegisterPrivilege("back", Lang.Get("desc-back"));
            ipm.RegisterPrivilege("spawn", Lang.Get("desc-spawn"));

            //Group Random Teleport privileges
            ipm.RegisterPrivilege("grtp", Lang.Get("desc-grtp"));

            //Just Random Teleport privileges
            ipm.RegisterPrivilege("jpm", Lang.Get("desc-dm"));//Register the privilege for general private messages
            ipm.RegisterPrivilege("jpmadmin", Lang.Get("desc-jpm")); //Register the privilege for admin control

            //Simple Server message privileges
            ipm.RegisterPrivilege("ssm", Lang.Get("desc-ssm"));

            //Teleport To privileges 
            ipm.RegisterPrivilege("tpt", Lang.Get("desc-tpt"));

            //Random Teleport Privileges
            ipm.RegisterPrivilege("rtp", Lang.Get("desc-rtp"));

            //Ironman privileges
            ipm.RegisterPrivilege("ironman", Lang.Get("desc-ironman"));

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

            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
                
            }

                string cmd = lowerArgs[0]; // Use the lowercase version of the command
            switch (cmd)
            {
                case "help":
                    displayhelp(player, "all");
                    return TextCommandResult.Deferred;
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


            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
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
                                
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-sethome"));
                            }
                        }
                        else
                        {
                           
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-save"));
                        }
                    }
                    return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enabled", cmdname));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSetHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", cmdname));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = lowerArgs[1]; // Use the lowercase version of the command
                        if (code == null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.sethomecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.sethomecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                default:
                    if (bsuconfig.Current.enableHome == true && !ironManPlayerList.Contains(player.PlayerUID))
                    {


                        string inputString = cmd;
                        int input;
                        //System.Diagnostics.Debug.WriteLine(inputString);
                        if (int.TryParse(inputString, out input))
                        {
                            //System.Diagnostics.Debug.WriteLine(input);

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
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, homecount + 1));
                            }
                            else if (input > bsuconfig.Current.homelimit)
                            {
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
                                    return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-sethome"));
                                }
                                else
                                {
                                    return TextCommandResult.Deferred;
                                }
                            }
                            else
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                            }
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine(input);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
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

            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
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
                            return TextCommandResult.Deferred;
                        }
                        else if (homecount2 == 0)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-home"));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-number"));
                        }
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable-home"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableHome = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable-home"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.ArgCount > 1 ? lowerArgs[1] as String : null;
                        if (code == null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.homecostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.homecostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numbr) && numbr >= 0)
                    {
                        int? cooldownnumber = numbr as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("cooldown-fail"));
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
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-home"));
                            }
                            else if (input < 1)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, homecount));
                            }
                            else
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:number-between", 1, bsuconfig.Current.homelimit));
                            }
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine(input);
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-home"));
                    }
                case "limit":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int num;
                        if (args.ArgCount > 1 && int.TryParse(args[1].ToString(), out num) && num > 0)
                        {
                            bsuconfig.Current.homelimit = num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:home-limit-updated", num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
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
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:no-homes", numb));
                    }
                    else if (numb == null)
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                    }
                    else
                    {
                        homelist3.RemoveAt((int)numb - 1);
                        homeSave.Remove(playerID3);
                        homeSave.Add(playerID3, homelist3);
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

            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
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
                        return TextCommandResult.Deferred;
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled-back"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBack = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable-back"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBack = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable-back"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        string code = args.ArgCount > 1 ? lowerArgs[1] as String : null;
                        if (code == null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.backcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.backcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("cooldown-fail"));
                    }
            }
            return TextCommandResult.Deferred;
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
            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
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
                        return TextCommandResult.Deferred;
                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", "spawn"));
                    }

                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "spawn"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableSpawn = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "spawn"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("cooldown-fail"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        
                        string code = args.ArgCount > 1 ? lowerArgs[1] as String : null;
                        if (code == null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.spawncostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.spawncostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }                    
            }
            return TextCommandResult.Deferred;
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
                // Create a new array to store the lowercase arguments
                string[] lowerArgs = new string[args.ArgCount];
                for (int i = 0; i < args.ArgCount; i++)
                {
                    if (args[i] as string != null)
                    {
                        lowerArgs[i] = (args[i] as string).ToLower();
                    }
                    else
                    {
                        lowerArgs[i] = null;
                    }
                }

                string cmd = lowerArgs[0]; // Use the lowercase version of the command
                switch (cmd)
                {
                    case "help":
                        displayhelp(player, cmdname);
                        return TextCommandResult.Deferred;
                    case "enable":
                        bsuconfig.Current.teleportcostenabled = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", cmdname));
                    case "disable":
                        bsuconfig.Current.teleportcostenabled = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", cmdname));
                    case null:
                        return TextCommandResult.Success("[Help|Enable|Disable]");
                }
                return TextCommandResult.Deferred;
            }
            else
            {
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
            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
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
                                sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:chunkloading-grtp"));
                            }
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:notset-grtp"));
                        }

                    }
                    else if (ironManPlayerList.Contains(player.PlayerUID))
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:ironman-commands-disabled"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disabled", "grtp"));
                    }
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
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
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:greater-than", 9));
                        }
                        else if (cdnum < 0)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                        else
                        {
                            bsuconfig.Current.teleportradius = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:set-radius-grtp", cdnum));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "now":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        grtptimer = 0 - (int)bsuconfig.Current.cooldownminutes;
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:wait-now-grtp"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "grtp"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableGrtp = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "grtp"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "playercooldown":
                    if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int numb) && numb >= 0)
                    {
                        int? cooldownnumber = numb as int?;
                        setplayercooldown(player, cooldownnumber, cmdname);
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("cooldown-fail"));
                    }
                case "costitem":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {

                        string code = args.ArgCount > 1 ? lowerArgs[1] as String : null;
                        if (code == null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                        }
                        else
                        {
                            bool outcome = checkcostitem(code);
                            if (outcome == false)
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:tp-need-item"));
                            }
                            else
                            {
                                bsuconfig.Current.grtpcostitem = code;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-itm-updated", cmdname, code));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "costqty":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out int num) && num >= 0)
                        {
                            bsuconfig.Current.grtpcostqty = (int)num;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:cost-qty-updated", cmdname, num));
                        }
                        else
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:non-negative-number"));
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }

            }
            return TextCommandResult.Deferred;
        }

        //Join Announce Command
        private TextCommandResult Cmd_joinannounce(TextCommandCallingArgs args)
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

            string cmdname = "joinannounce";

            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
            switch (cmd)
            {
                case null:
                    return TextCommandResult.Success("[help | enable | disable]");
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableJoinAnnounce = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "Join Announce"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableJoinAnnounce = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "Join Announce"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
            }
            return TextCommandResult.Deferred;
        }


        //rising sun command
        private TextCommandResult Cmd_rs(TextCommandCallingArgs args)
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

            string cmdname = "risingsun";
            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
            switch (cmd)
            {
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case "dawn":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int cdnum = 0; // Initialize cdnum to a default value
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && (cdnum < 1 | cdnum > 23))
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                        }
                        else if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum > bsuconfig.Current.dusk)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:smaller-dusk") + bsuconfig.Current.dusk);
                        }
                        else if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum == 0)//null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                        }
                        else
                        {
                            if (cdnum != 0)
                            {
                                bsuconfig.Current.dawn = cdnum;
                                sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:dawn-updated") + cdnum + ":00");
                            }
                            else
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "dusk":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int cdnum = 0; // Initialize cdnum to a default value
                        if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && (cdnum < 1 | cdnum > 23))
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                        }
                        else if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum < bsuconfig.Current.dawn)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:larger-dawn") + bsuconfig.Current.dawn);
                        }
                        else if (args.ArgCount > 1 && int.TryParse(args[1] as string, out cdnum) && cdnum == 0)//null)
                        {
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                        }
                        else
                        {
                            if (cdnum != 0) {
                            bsuconfig.Current.dusk = cdnum;
                            sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                            return TextCommandResult.Success(Lang.Get("bunnyserverutilities:dusk-updated") + cdnum + ":00");
                            }
                            else
                            {
                                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:between-hours"));
                            }
                        }
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableRisingSun = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "Rising Sun"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableRisingSun = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "Rising Sun"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case null:
                    return TextCommandResult.Success("use /rs dawn|dusk|help|enable|disable");
            }
            return TextCommandResult.Deferred;
        }

        private TextCommandResult Cmd_bb(TextCommandCallingArgs args)
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

            string cmdname = "bb";

            // Create a new array to store the lowercase arguments
            string[] lowerArgs = new string[args.ArgCount];
            for (int i = 0; i < args.ArgCount; i++)
            {
                if (args[i] as string != null)
                {
                    lowerArgs[i] = (args[i] as string).ToLower();
                }
                else
                {
                    lowerArgs[i] = null;
                }
            }

            string cmd = lowerArgs[0]; // Use the lowercase version of the command
            switch (cmd)
            {
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case null:
                    return TextCommandResult.Success("use /bb [help|enable|disable]");
                case "enable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBunnyBell = true;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:enable", "Bunny Bell"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
                case "disable":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        bsuconfig.Current.enableBunnyBell = false;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:disable", "Bunny Bell"));
                    }
                    else
                    {
                        return TextCommandResult.Success(Lang.Get("bunnyserverutilities:not-enough-permissions", cmdname + "admin"));
                    }
            }
            return TextCommandResult.Deferred;
        }

        private TextCommandResult Cmd_removedeny(TextCommandCallingArgs args)
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

            string cmd = args.ArgCount > 0 ? args[0] as String : null;
            string cmd2 = args.ArgCount > 1 ? args[1] as String : null;
            IServerPlayerData targetplayer = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
            if (targetplayer != null)
            {
                ipm.RemovePrivilegeDenial(targetplayer.PlayerUID, cmd2);
                return TextCommandResult.Success(Lang.Get("bunnyserverutilities:remove-denial", cmd2, cmd));

            }
            return TextCommandResult.Deferred;
        }

        private TextCommandResult Cmd_cooldown(TextCommandCallingArgs args)
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
            return TextCommandResult.Deferred;
        }

        //Simple Server Messages Commands

        private TextCommandResult Cmd_ssm(TextCommandCallingArgs args)
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

            string cmdname = "ssm";
            string cmd;
            if (args.RawArgs.PeekWord() != null)
            {
                cmd = args.RawArgs.PopWord().ToLower();
            }
            else
            {
                cmd = null;
            }

            switch (cmd)
            {
                case "add":
                    string text = args.RawArgs.PopAll();
                    bsuconfig.Current.messages.Add(text);
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:add-message"), Vintagestory.API.Common.EnumChatType.Notification);
                    return TextCommandResult.Deferred;
                case "remove":
                    int? listindex = args.RawArgs.PopInt();
                    if (listindex != null)
                    {
                        int lindex = (int)listindex;

                        List<string> msglist = bsuconfig.Current.messages;
                        if (msglist.Count >= lindex)
                        {
                            string removemsg = msglist[lindex];
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
                    return TextCommandResult.Deferred;
                case "list":
                    List<string> listofmessages = bsuconfig.Current.messages;
                    int lastindex = listofmessages.Count;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:list-of-messages"), Vintagestory.API.Common.EnumChatType.Notification);
                    for (int i = 0; i < lastindex; i++)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i + " : " + listofmessages[i], Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    return TextCommandResult.Deferred;
                case "frequency":
                    int? frqnum = args.RawArgs.PopInt();
                    if (frqnum != null & frqnum >= 1)
                    {
                        bsuconfig.Current.frequency = frqnum;
                        sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:message-frequency", frqnum), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:greater-than", 0), Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    return TextCommandResult.Deferred;
                case "now":
                    broadcast();
                    return TextCommandResult.Deferred;
                case "help":
                    displayhelp(player, cmdname);
                    return TextCommandResult.Deferred;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /ssm help|add|remove|list|frequency|now|enable|disable", Vintagestory.API.Common.EnumChatType.Notification);
                    return TextCommandResult.Deferred;
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
                    return TextCommandResult.Deferred;
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
                    return TextCommandResult.Deferred;

            }
            return TextCommandResult.Deferred;
        }


        //warning command
        private TextCommandResult Cmd_warn(TextCommandCallingArgs args)
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

            string cmdname = "warn";
            string cmd;
            if (args.RawArgs.PeekWord() != null)
            {
                cmd = args.RawArgs.PopWord().ToLower();
            }
            else
            {
                cmd = null;
            }
            if (cmd != "list" & cmd != null)
            {
                IServerPlayerData targetplayer = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
                if (targetplayer != null)
                {
                    string warnReason = args.RawArgs.PopAll();
                    if (bsuconfig.Current.warningDict.ContainsKey(targetplayer.PlayerUID))
                    {
                        userWarning uwd;
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
                        userWarning uwd = new userWarning();
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
                int? listnum = args.RawArgs.PopInt();
                Dictionary<String, userWarning> uswd = new Dictionary<String, userWarning>();
                uswd = bsuconfig.Current.warningDict;
                List<userWarning> warningsList = new List<userWarning>();
                foreach (userWarning warning in uswd.Values)
                {
                    warningsList.Add(warning);
                }
                if (listnum == null)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:warn-help"), Vintagestory.API.Common.EnumChatType.Notification);
                    for (var i = 1; i < uswd.Count; i++)
                    {
                        userWarning UW = warningsList[i];
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i + ") " + Lang.Get("bunnyserverutilities:warn-player") + UW.playername + " | " + Lang.Get("bunnyserverutilities:warn-warning") + ": " + UW.warnings, Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                else
                {
                    if (listnum != null & listnum > 0 & listnum < uswd.Count)
                    {
                        userWarning UW = warningsList[(int)listnum];// ElementAt((int)listnum).Value;
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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:number-between", 1, (uswd.Count - 1)), Vintagestory.API.Common.EnumChatType.Notification);
                    }

                }
            }
            else
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/warn playername reason | /warn list", Vintagestory.API.Common.EnumChatType.Notification);
            }
            return TextCommandResult.Deferred;
        }

        //teleport to
        private TextCommandResult Cmd_tpt(TextCommandCallingArgs args)
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

            string cmdname = "tpt";
            string cmd;
            if (args.RawArgs.PeekWord() != null)
            {
                cmd = args.RawArgs.PopWord().ToLower();
            }
            else
            {
                cmd = null;
            }
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
                setplayercooldown(player, args.RawArgs.PopInt(), cmdname);
            }
            else if (cmd == "costqty")
            {
                if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                {
                    int? num = args.RawArgs.PopInt();
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
                    string code;
                    if (args.RawArgs.PeekWord() != null)
                    {
                        code = args.RawArgs.PopWord().ToLower();
                    }
                    else
                    {
                        code = null;
                    }
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
                    bsuconfig.Current.tptDict = new Dictionary<string, tptInfo>{
                    { "Default",new tptInfo() }
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
            return TextCommandResult.Deferred;
        }

        //Teleport to deny
        private TextCommandResult Cmd_tpdeny(TextCommandCallingArgs args)
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
            return TextCommandResult.Deferred;
        }

        //teleport to accept
        private TextCommandResult Cmd_tpaccept(TextCommandCallingArgs args)
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
                            return TextCommandResult.Deferred;
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
            return TextCommandResult.Deferred;
        }

        //RTP Commands
        private TextCommandResult Cmd_rtp(TextCommandCallingArgs args)
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

            string cmdname = "rtp";
            string cmd = args.RawArgs.PeekWord();
            if (cmd != null)
            {
                cmd = cmd.ToLower();
                args.RawArgs.PopWord(); // Remove the word after checking
            }
            else
            {
                cmd = null;
            }
            switch (cmd)
            {
                case "cooldown":
                    if (player.Role.Code == "admin" || player.HasPrivilege(cmdname + "admin"))
                    {
                        int? cdnum = args.RawArgs.PopInt();
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
                        //System.Diagnostics.Debug.Write(count);
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
                                addcooldown(cmdname, player, cooldownstate);

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
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled", "rtp"), Vintagestory.API.Common.EnumChatType.Notification);
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
                        int? cdnum = args.RawArgs.PopInt();
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
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:set-radius-rtp", cdnum), Vintagestory.API.Common.EnumChatType.Notification);
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
                        string code = args.RawArgs.PeekWord();
                        if (code != null)
                        {
                            code = code.ToLower();
                            args.RawArgs.PopWord(); // Remove the word after checking
                        }
                        else
                        {
                            code = null;
                        }

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
                        int? num = args.RawArgs.PopInt();
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
            return TextCommandResult.Deferred;
        }

        //Ironman COmmands
        private TextCommandResult Cmd_ironman(TextCommandCallingArgs args)
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

            string cmdname = "ironman";
            string cmd = args.RawArgs.PeekWord();
            if (cmd != null)
            {
                cmd = cmd.ToLower();
                args.RawArgs.PopWord(); // Remove the word after checking
            }
            else
            {
                cmd = null;
            }
            switch (cmd)
            {
                case null:
                    if (bsuconfig.Current.enableironman == true)
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
                        int rawxmax = (worldx / 2) + startradius + (radius * 2);
                        int rawzmin = (worldz / 2) + startradius + radius;
                        int rawzmax = (worldz / 2) + startradius + (radius * 2);
                        imx = sapi.World.Rand.Next(rawxmin, rawxmax);
                        imz = sapi.World.Rand.Next(rawzmin, rawzmax);
                        sapi.WorldManager.LoadChunkColumnPriority(imx / sapi.WorldManager.ChunkSize, imz / sapi.WorldManager.ChunkSize);

                        //add player to current ironman score list
                        currentironmandict.Add(player.PlayerUID, sapi.World.Calendar.TotalDays);
                        imteleporting = true;
                    }
                    break;
                case "highscores":
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:ironman-highscores-title"), Vintagestory.API.Common.EnumChatType.Notification);
                    if (ironmanhighscores.Count == 0)
                    {
                        ironmanhighscores.Add("Placeholder", -1);
                    }
                    Dictionary<string, int> tempscores = new Dictionary<string, int>();
                    foreach (var entry in currentironmandict)
                    {
                        if (ironmanhighscores.ContainsKey(entry.Key))
                        {
                            int value;
                            ironmanhighscores.TryGetValue(entry.Key, out value);
                            if (entry.Value > value)
                            {
                                tempscores.Add(entry.Key, (int)(sapi.World.Calendar.TotalDays - entry.Value));
                            }
                        }
                    }
                    foreach (var entry in ironmanhighscores)
                    {
                        if (!tempscores.ContainsKey(entry.Key))
                        {
                            tempscores.Add(entry.Key, entry.Value);
                        }
                    }
                    var sortedTempscores = new List<KeyValuePair<string, int>>(tempscores);
                    sortedTempscores.Sort((firstPair, nextPair) => nextPair.Value.CompareTo(firstPair.Value));

                    Dictionary<string, int> sortedTempscoresDict = new Dictionary<string, int>();
                    foreach (var pair in sortedTempscores)
                    {
                        sortedTempscoresDict.Add(pair.Key, pair.Value);
                    }
                    tempscores = sortedTempscoresDict;

                    int skip = 0; //skip this many
                    int index = 0;
                    foreach (var entry in tempscores)
                    {
                        IServerPlayerData playername = sapi.PlayerData.GetPlayerDataByUid(entry.Key);
                        if (playername != null)
                        {
                            if (currentironmandict.ContainsKey(entry.Key))
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (index - skip + 1) + ") " + playername.LastKnownPlayername + " (In Progress): " + entry.Value, Vintagestory.API.Common.EnumChatType.Notification);
                            }
                            else
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, (index - skip + 1) + ") " + playername.LastKnownPlayername + ": " + entry.Value, Vintagestory.API.Common.EnumChatType.Notification);
                            }
                        }
                        else
                        {
                            skip++;
                        }
                        index++;
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
            return TextCommandResult.Deferred;
        }

        //Private Message admin commands
        private TextCommandResult Cmd_jpm(TextCommandCallingArgs args)
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

            string cmdname = "jpm";

            string cmd;
            if (args.RawArgs.PeekWord() != null)
            {
                cmd = args.RawArgs.PopWord().ToLower();
            }
            else
            {
                cmd = null;
            }

            switch (cmd)
            {
                case null:

                    displayhelp(player, cmdname);
                    break;
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
            return TextCommandResult.Deferred;
        }

        //Reply command
        private TextCommandResult Cmd_reply(TextCommandCallingArgs args)
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

            if (bsuconfig.Current.enablejpm == true)
            {
                string cmdname = "jpm";
                string message = args.RawArgs.PopAll();


                if (message != "help")
                {

                    if (message == null | message == "")
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:include-message"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user to include a message with the command
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        if (replySave.ContainsKey(player.PlayerUID))
                        {
                            IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByUid(replySave[player.PlayerUID]);
                            if (pdata == null)
                            {
                                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
                                return TextCommandResult.Deferred;
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
            return TextCommandResult.Deferred;
        }

        //private message player command
        private TextCommandResult Cmd_pm(TextCommandCallingArgs args)
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

            if (bsuconfig.Current.enablejpm == true)
            {
                string cmdname = "jpm";
                string cmd = args.RawArgs.PopWord();
                IServerPlayerData pdata = sapi.PlayerData.GetPlayerDataByLastKnownName(cmd);
                if (cmd != "" & cmd != null & cmd != "help")
                {
                    if (pdata == null)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:player-not-found"), Vintagestory.API.Common.EnumChatType.Notification);
                        return TextCommandResult.Deferred;
                    }
                    else
                    {
                        string message = args.RawArgs.PopAll();
                        //System.Diagnostics.Debug.WriteLine(message);
                        if (message == null | message == "")
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:include-message"), Vintagestory.API.Common.EnumChatType.Notification); //Ask the user to include a message with the command
                            return TextCommandResult.Deferred;
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
            return TextCommandResult.Deferred;
        }

        //////////End Command Functions//////////


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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/removedeny <i>playername privilege</i> " + Lang.Get("bunnyserverutilities:help-removedeny)"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }
            //BSU help

            //home help
            if (helpType == "home" || helpType == "all")
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-commands", "Home"), Vintagestory.API.Common.EnumChatType.Notification);
                if (bsuconfig.Current.enableHome == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/sethome " + Lang.Get("bunnyserverutilities:help-sethome"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home " + Lang.Get("bunnyserverutilities:help-home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/home delete " + Lang.Get("bunnyserverutilities:help-home-delete"), Vintagestory.API.Common.EnumChatType.Notification);

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:disabled-2", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
                }
                //admin home help
                if (player.Role.Code == "admin")
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-cooldown", "/home"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "Back"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:admin-commands", "Spawn"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/rs", "Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/rs", "Rising Sun"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/jpm", "private message"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/ssm", "Simple Server Messages"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/bb", "Bunny Bell"), Vintagestory.API.Common.EnumChatType.Notification);
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
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-enable-2", "/tpcost", "TP Cost"), Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("bunnyserverutilities:module-disable-2", "/tpcost", "TP Cost"), Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

        }


        /////////////////////End Help Function////////


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
                    sapi.StoreModConfig(bsuconfig.Current, "BunnyServerUtilitiesConfig.json");
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