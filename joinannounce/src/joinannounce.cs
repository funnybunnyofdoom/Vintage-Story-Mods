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
    List<IServerPlayer> joinedPlayers = new List<IServerPlayer>();
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }
    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        myAPI = api;
        api.Event.PlayerCreate += OnPlayerCreate;
        api.Event.PlayerNowPlaying += onNowPlaying;
    }

    private void onNowPlaying(IServerPlayer byPlayer)
    {
        if (joinedPlayers != null)
        {
            if (joinedPlayers.Contains(byPlayer))
            {
                myAPI.BroadcastMessageToAllGroups("Please welcome <font color=\"white\"><strong>" + byPlayer.PlayerName + "</strong></font> to the server!", Vintagestory.API.Common.EnumChatType.AllGroups);
                joinedPlayers.Remove(byPlayer);
            }
        }
    }

    public void OnPlayerCreate(IServerPlayer byPlayer)
    {
        joinedPlayers.Add(byPlayer);
    }

}