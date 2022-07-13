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

public class JustRandomTeleport : ModSystem
{
    public EntityPlayer GEntity;
    public IServerPlayer Splayer;
    public ICoreServerAPI myAPI;
    public IServerChunk SChunk;
    public BlockPos cblockpos;
    int randx, randz = 0;
    bool teleporting = false;
    int count = 0;
    int cooldowntimer;
    long CID;
    

    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api); //Register the server api to "api"
        myAPI = api;
        IPermissionManager ipm = api.Permissions;
        ipm.RegisterPrivilege("rtp", "Random Teleport");
        ipm.AddPrivilegeToGroup("suplayer", BPrivilege.rtp);
        api.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        api.RegisterCommand("rtp", "Randomly Teleports the player", "",
            cmd_rtp, BPrivilege.rtp);
        cooldowntimer = 0;
        CID = api.Event.RegisterGameTickListener(CoolDown, 60000);

        try
        {
            var Config = api.LoadModConfig<JrtpConfig>("jrtpconfig.json");
            if (Config != null)
            {
                api.Logger.Notification("Mod Config successfully loaded.");
                JrtpConfig.Current = Config;
            }
            else
            {
                api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                JrtpConfig.Current = JrtpConfig.getDefault();
            }
        }
        catch
        {
            JrtpConfig.Current = JrtpConfig.getDefault();
            api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
        }
        finally
        {
            if (JrtpConfig.Current.cooldownseconds == null)
                JrtpConfig.Current.cooldownseconds = JrtpConfig.getDefault().cooldownseconds;
            if (JrtpConfig.Current.teleportradius == null)
                JrtpConfig.Current.teleportradius = JrtpConfig.getDefault().teleportradius;                
            if (JrtpConfig.Current.cooldownduration == null)
                JrtpConfig.Current.cooldownduration = JrtpConfig.getDefault().cooldownduration;
            JrtpConfig.Current.cooldownDict = JrtpConfig.getDefault().cooldownDict;

            api.StoreModConfig(JrtpConfig.Current, "jrtpconfig.json");
        }
    }

  

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        if (randx / myAPI.WorldManager.ChunkSize == chunkCoord.X & (randz / myAPI.WorldManager.ChunkSize == chunkCoord.Y) & (teleporting == true))
        {
            Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location.", Vintagestory.API.Common.EnumChatType.Notification);
            //int height = myAPI.World.BlockAccessor.GetRainMapHeightAt(randx,randz);
            BlockPos checkheight = new BlockPos();
            checkheight.X = randx;
            checkheight.Y = 1;
            checkheight.Z = randz;
            int height = myAPI.World.BlockAccessor.GetTerrainMapheightAt(checkheight);
            GEntity.TeleportTo(randx, height+1, randz);
            teleporting = false;
        }
        //if (teleporting == true)
        //{
         //   Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "loading chunk " + chunkCoord.X.ToString() + " " + chunkCoord.Y.ToString(), Vintagestory.API.Common.EnumChatType.Notification);
        //}

    }

    private void cmd_rtp(IServerPlayer player, int groupId, CmdArgs args)
            {
                ICoreServerAPI api = myAPI; //get the server api
                Splayer = player;
                GEntity = player.Entity; //assign the entity to global variable
                IWorldManagerAPI world = api.WorldManager;
                System.Diagnostics.Debug.Write(count);

                if (JrtpConfig.Current.cooldownDict.ContainsKey(player.PlayerUID) == false & teleporting == false)
                {                   
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait while destination chunks are loaded.", Vintagestory.API.Common.EnumChatType.Notification);
                    int radius = JrtpConfig.Current.teleportradius ?? default(int);
                    int worldx = world.MapSizeX;
                    int worldz = world.MapSizeZ;
                    int rawxmin = (worldx/2) - radius;
                    int rawxmax = (worldx / 2) + radius;
                    int rawzmin = (worldz / 2) - radius;
                    int rawzmax = (worldz / 2) + radius;
                    randx = GEntity.World.Rand.Next(rawxmin, rawxmax);
                    randz = GEntity.World.Rand.Next(rawzmin, rawzmax);
                    world.LoadChunkColumnPriority(randx / myAPI.WorldManager.ChunkSize, randz / myAPI.WorldManager.ChunkSize);     
                    count = 1;  
                    teleporting = true;
                    JrtpConfig.Current.cooldownDict.Add(player.PlayerUID,cooldowntimer);
                    myAPI.StoreModConfig(JrtpConfig.Current, "jrtpconfig.json");
        }
                else if(teleporting == true){
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your chunks are still being generated, please be patient.", Vintagestory.API.Common.EnumChatType.Notification);
                }else if(JrtpConfig.Current.cooldownDict.ContainsKey(player.PlayerUID) == true & teleporting == false)
                {
                    int values;
                    JrtpConfig.Current.cooldownDict.TryGetValue(player.PlayerUID,out values);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You can teleport again in " + ((values+JrtpConfig.Current.cooldownduration)-cooldowntimer) + " minutes", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

    private void CoolDown(float ct)
    {
        cooldowntimer++;
       
            Dictionary<string,int>.KeyCollection tempdict = JrtpConfig.Current.cooldownDict.Keys;
            foreach (var keyvalue in tempdict)
            {
                int value;
                //var dic = JrtpConfig.Current.cooldownDict.Values;
                JrtpConfig.Current.cooldownDict.TryGetValue(keyvalue, out value);
                if ((cooldowntimer - value) >= JrtpConfig.Current.cooldownduration)
                {
                    //myAPI.SendMessage(myAPI.World.PlayerByUid(keyvalue), Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait until the cooldown timer has elapsed", Vintagestory.API.Common.EnumChatType.Notification);
                    JrtpConfig.Current.cooldownDict.Remove(keyvalue);
                    myAPI.StoreModConfig(JrtpConfig.Current, "jrtpconfig.json");
                    return;
                }
            }
    }

    public class JrtpConfig
    {
        public static JrtpConfig Current { get; set; }

        public int? cooldownseconds { get; set; }
        public int? teleportradius { get; set; }
        public int? cooldownduration { get; set; }
        public Dictionary<String, int> cooldownDict { get; set; }


        public static JrtpConfig getDefault()
        {
            var config = new JrtpConfig();

            config.cooldownseconds = 120;
            config.teleportradius = 100000;
            config.cooldownduration = 15;
            config.cooldownDict = new Dictionary<string, int>
                {
                    { "Default",1}
                };

            return config;
        }
    }

    public class BPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /rtp
        /// </summary>
        public static string rtp = "rtp";
    }
}