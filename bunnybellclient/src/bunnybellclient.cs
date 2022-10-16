using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace bunnybellclient.src
{
    class bunnybellclient : ModSystem
    {
        ICoreClientAPI capi;
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell");

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client; //Make sure this only runs client-side
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api; //asign the API
            api.Event.ChatMessage += onPlayerChat; //Register the chat message listener
        }

        private void onPlayerChat(int groupId, string message, EnumChatType chattype, string data)
        {
            int volume = 1; //this doesn't seem to work, leaving it in so I can try to adjust volume in the future
            IPlayer templayer = capi.World.Player; //Get the player
            if (templayer != null && templayer.PlayerName != null && data != null)//Make sure player is loaded and message is from a player
            {
                if (data.CaseInsensitiveContains(templayer.PlayerName)) //Check for the players name within the message
                {
                    templayer.Entity.World.PlaySoundFor(sound, templayer, true, 32, volume);//Play a sound for the player
                }
            }
        }
    }
}
