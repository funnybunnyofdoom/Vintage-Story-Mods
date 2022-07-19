using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace bunnybell.src
{
    class bunnybell : ModSystem
    {
        ICoreServerAPI sapi;
        Dictionary<string, BlockPos> location;
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell");
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            api.Event.PlayerChat += onPlayerChat;

        }
        private void onPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            string checklist = data;
            IPlayer[] playerList = sapi.World.AllOnlinePlayers;
            for (var i = 0; i < playerList.Count(); i++)
            {
                string templist = checklist;
                IPlayer templayer = playerList[i];
                if (templist.CaseInsensitiveContains(templayer.PlayerName))
                {
                    templayer.Entity.World.PlaySoundFor(sound,templayer);
                }
            }
        }
    }
}
