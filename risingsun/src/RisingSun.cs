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


public class RisingSun: ModSystem
{
    public override bool ShouldLoad(EnumAppSide side)
    {
        return side == EnumAppSide.Server;
    }
    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        api.Event.PlayerCreate += OnPlayerCreate;
    } 
        public void OnPlayerCreate(IServerPlayer byPlayer)
        {
            byPlayer.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "The sun has risen on a new player!", Vintagestory.API.Common.EnumChatType.AllGroups);
            int hour = byPlayer.Entity.World.Calendar.FullHourOfDay;
            if (hour < 8)
            {
            byPlayer.Entity.World.Calendar.Add(8 - hour);
            }else if (hour > 8)
            {
            byPlayer.Entity.World.Calendar.Add(24-hour+8);
            }
        }
  
}