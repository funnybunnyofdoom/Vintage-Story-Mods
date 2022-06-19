using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
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

            api.StoreModConfig(JrtpConfig.Current, "jrtpconfig.json");
        }
    }

  

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        if (randx / myAPI.WorldManager.ChunkSize == chunkCoord.X & (randz / myAPI.WorldManager.ChunkSize == chunkCoord.Y) & (teleporting == true))
        {
            Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location.", Vintagestory.API.Common.EnumChatType.Notification);
            int height = myAPI.World.BlockAccessor.GetRainMapHeightAt(randx,randz);
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

                if (count == 0 & teleporting == false)
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
                   
            {

            }
                    CID = api.Event.RegisterGameTickListener(CoolDown, 1000); // register the cooldown tick listener
                    count = 1;
                    teleporting = true;
                }
                else if(teleporting == true & count != 0){
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Your chunks are still being generated, please be patient.", Vintagestory.API.Common.EnumChatType.Notification);
                }
        else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You can teleport again in " + (JrtpConfig.Current.cooldownseconds-count) + " seconds", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }

    private void CoolDown(float ct)
    {
        if (count >= JrtpConfig.Current.cooldownseconds)
        {
            count = 0;
            myAPI.Event.UnregisterGameTickListener(CID);
        }
        else
        {
            count = count + 1;
        }
    }

    public class JrtpConfig
    {
        public static JrtpConfig Current { get; set; }

        public int? cooldownseconds { get; set; }
        public int? teleportradius { get; set; }

        public static JrtpConfig getDefault()
        {
            var config = new JrtpConfig();

            config.cooldownseconds = 120;
            config.teleportradius = 100000;

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
