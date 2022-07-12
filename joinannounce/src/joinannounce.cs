using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;


public class joinannounce : ModSystem
{
    ICoreServerAPI myAPI;
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }
    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        myAPI = api;
        api.Event.PlayerCreate += OnPlayerCreate;
    }
    public void OnPlayerCreate(IServerPlayer byPlayer)
    {
        myAPI.BroadcastMessageToAllGroups("Please welcome <font color=\"white\"><strong>" + byPlayer.PlayerName + "</strong></font> to the server!", Vintagestory.API.Common.EnumChatType.AllGroups);
    }

}