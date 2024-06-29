using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static HarmonyLib.Code;

namespace Bunny_Server_Utility
{
    public class Bunny_Server_UtilityModSystem : ModSystem
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


            //=================//
            //register commands//
            //=================//

            //Bunny Server Utilities Command
            api.ChatCommands.Create("bsu")
                .WithDescription("List or join the lfg list")
                .RequiresPrivilege(Privilege.chat)
                .RequiresPlayer()
                .WithArgs(api.ChatCommands.Parsers.Word("cmd", new string[] { "help", "version", "leave" }))
                .HandleWith(new OnCommandDelegate(Cmd_bsu));



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




        }

        //========//
        //COMMANDS//
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

    public class tptCostInfo
    {
        public ItemSlot slot = null;
        public string item = null;
        public int qty = 0;

    }
}
