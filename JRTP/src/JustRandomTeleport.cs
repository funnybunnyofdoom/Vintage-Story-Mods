using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
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
    }

  

    private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        if (randx / myAPI.WorldManager.ChunkSize == chunkCoord.X & (randz / myAPI.WorldManager.ChunkSize == chunkCoord.Y))
        {
            Splayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location.", Vintagestory.API.Common.EnumChatType.Notification);
            int height = myAPI.World.BlockAccessor.GetRainMapHeightAt(randx,randz);
            GEntity.TeleportTo(randx, height+1, randz);
        }

    }

    private void cmd_rtp(IServerPlayer player, int groupId, CmdArgs args)
            {
                ICoreServerAPI api = myAPI; //get the server api
                Splayer = player;
                GEntity = player.Entity; //assign the entity to global variable
                IWorldManagerAPI world = api.WorldManager;             
                if (count == 0)
                {                   
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait while destination chunks are loaded.", Vintagestory.API.Common.EnumChatType.Notification);
                    randx = GEntity.World.Rand.Next(400000, 600000);//Using a hard coded 100000x in each direction until I can do the math
                    randz = GEntity.World.Rand.Next(400000, 600000);
                    world.LoadChunkColumn(randx / myAPI.WorldManager.ChunkSize, randz / myAPI.WorldManager.ChunkSize);   
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait a short while before trying again.", Vintagestory.API.Common.EnumChatType.Notification);
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
