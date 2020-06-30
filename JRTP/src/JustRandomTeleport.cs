using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
//FunnyBunnyofDOOM@gmail.com
//https://www.doomlandgaming.com

public class JustRandomTeleport : ModSystem
{
    EntityPlayer GEntity;
    int count = 0;
    long LID,CID;
    ICoreServerAPI myAPI;

    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api); //Register the server api to "api"
        IPermissionManager ipm = api.Permissions;
        ipm.RegisterPrivilege("rtp", "Random Teleport");
        ipm.AddPrivilegeToGroup("suplayer", BPrivilege.rtp);

        api.RegisterCommand("rtp", "Randomly Teleports the player", "",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                EntityPlayer byEntity = player.Entity; //Get the player
                GEntity = player.Entity;
                IWorldManagerAPI world = api.WorldManager;
                if (count == 0)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location", Vintagestory.API.Common.EnumChatType.Notification);
                    var randx = byEntity.World.Rand.Next(400000, 600000);//Using a hard coded 100000x in each direction until I can do the math
                    var randz = byEntity.World.Rand.Next(400000, 600000);
                    byEntity.Properties.FallDamage = false;
                    byEntity.TeleportTo(randx, 199, randz);
                    count = 1;
                    myAPI = api;
                    LID = api.Event.RegisterGameTickListener(OnTick, 1000); // register the falling tick listener
                    CID = api.Event.RegisterGameTickListener(CoolDown, 4500); // register the cooldown tick listener
                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait a short while before trying again.", Vintagestory.API.Common.EnumChatType.Notification);
                }
            }, BPrivilege.rtp);
    }

    private void CoolDown(float ct)
    {
        if (count >= 10)
        {
            count = 0;
            myAPI.Event.UnregisterGameTickListener(LID);
        }
        else
        {
            count = count + 1;
        }
    }
    private void OnTick(float dt)
    {     
        if (GEntity.OnGround || GEntity.FeetInLiquid)
        {
            GEntity.Properties.FallDamage = true;
            ICoreServerAPI api = myAPI;
            myAPI.Event.UnregisterGameTickListener(LID);
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
