using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
public class grtp : ModSystem
{
    ICoreServerAPI sapi;
    int? count = 99999;
    long CID;
    int randx, randz;
    public bool loaded = false;
    int height = 100;
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        IPermissionManager ipm = api.Permissions;
        ipm.RegisterPrivilege("grtp", "Random Teleport");
        ipm.AddPrivilegeToGroup("suplayer", BPrivilege.grtp);
        ipm.AddPrivilegeToGroup("admin", BPrivilege.grtp);
        ipm.AddPrivilegeToGroup("doplayer", BPrivilege.grtp);
        api.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        api.RegisterCommand("grtp", "Randomly Teleports the player to a group location", "",
            cmd_grtp, BPrivilege.grtp);

        try
        {
            var Config = api.LoadModConfig<grtpConfig>("grtpconfig.json");
            if (Config != null)
            {
                api.Logger.Notification("Mod Config successfully loaded.");
                grtpConfig.Current = Config;
            }
            else
            {
                api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                grtpConfig.Current = grtpConfig.getDefault();
            }
        }
        catch
        {
            grtpConfig.Current = grtpConfig.getDefault();
            api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
        }
        finally
        {
            
            if (grtpConfig.Current.teleportradius == null)
                grtpConfig.Current.teleportradius = grtpConfig.getDefault().teleportradius;
            if (grtpConfig.Current.cooldownseconds == null)
                grtpConfig.Current.cooldownseconds = grtpConfig.getDefault().cooldownseconds;

            api.StoreModConfig(grtpConfig.Current, "grtpconfig.json");
        }
        count = grtpConfig.Current.cooldownseconds + 1;
        CID = api.Event.RegisterGameTickListener(CoolDown, 1000);
    }

    public class BPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /grtp
        /// </summary>
        public static string grtp = "grtp";
    }

    private void cmd_grtp(IServerPlayer player, int groupId, CmdArgs args)
    {
        if (loaded == true)
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location. Others can join you for " + (grtpConfig.Current.cooldownseconds - count) + " Seconds.", Vintagestory.API.Common.EnumChatType.Notification);          
            
            player.Entity.TeleportTo(randx, height + 2, randz);
            
        }
        else
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Chunk is loading. Please try again in a few seconds", Vintagestory.API.Common.EnumChatType.Notification);
            sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
        }
    }
    private void CoolDown(float ct)
    {
        if (count >= grtpConfig.Current.cooldownseconds)
        {
            count = 0;
            
            int radius = grtpConfig.Current.teleportradius ?? default(int);
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
                sapi.BroadcastMessageToAllGroups("New /GRTP coordinates generated", Vintagestory.API.Common.EnumChatType.Notification);
            
            }
            
            loaded = true;
        }

    }
    public class grtpConfig
    {
        public static grtpConfig Current { get; set; }

        public int? teleportradius { get; set; }
        public int? cooldownseconds { get; set; }

        public static grtpConfig getDefault()
        {
            var config = new grtpConfig();

            config.cooldownseconds = 900;
            config.teleportradius = 100000;

            return config;
        }
    }
}
