using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace bunnybellclient.src
{
    class bunnybellclient : ModSystem
    {
        ICoreClientAPI capi;
        List<AssetLocation> soundList = new List<AssetLocation>() //Create a list of sounds for the user to choose from. You can just append to this list to add more sounds. 
        {
            new AssetLocation("game", "sounds/effect/receptionbell"),
            new AssetLocation("game", "sounds/effect/anvilhit1"),
            new AssetLocation("game", "sounds/effect/cashregister"),
            new AssetLocation("game", "sounds/effect/clothrip"),
            new AssetLocation("game", "sounds/effect/crusher-impact1"),
            new AssetLocation("game", "sounds/effect/deepbell"),
            new AssetLocation("game", "sounds/effect/latch"),
            new AssetLocation("game", "sounds/effect/squish2"),
            new AssetLocation("game", "sounds/effect/stonecrush"),
            new AssetLocation("game", "sounds/effect/woodswitch"),
            new AssetLocation("game", "sounds/creature/beesting"),
            new AssetLocation("game", "sounds/creature/wolf/pup-bark"),
            new AssetLocation("game", "sounds/creature/raccoon/hurt"),
            new AssetLocation("game", "sounds/creature/pig/hurt"),
            new AssetLocation("game", "sounds/creature/chicken/rooster-call")

        };
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
            api.Event.PlayerJoin += onPlayerJoined; //Event Listener for when a player logs in
            api.Event.PlayerLeave += onPlayerLogout; //Event Listener for when a players logs out
            api.Event.OnEntityDeath += onPlayerDeath; // Event listener for when a player dies

            try
            {
                var Config = api.LoadModConfig<bbConfig>("bbconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    bbConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    bbConfig.Current = bbConfig.getDefault();
                }
            }
            catch
            {
                bbConfig.Current = bbConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {

                if (bbConfig.Current.soundsettings == null)
                    bbConfig.Current.soundsettings = bbConfig.getDefault().soundsettings;

                api.StoreModConfig(bbConfig.Current, "bbclient.json");
            }
        }

        private void onPlayerDeath(Entity entity, DamageSource damageSource)
        {
            throw new NotImplementedException();
        }

        private void onPlayerLogout(IClientPlayer byPlayer)
        {
            throw new NotImplementedException();
        }

        private void onPlayerJoined(IClientPlayer byPlayer)
        {
            throw new NotImplementedException();
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
        public class soundSettings
        {
            //public string user;
            public AssetLocation mention;
            public AssetLocation login;
            public AssetLocation logout;
            public AssetLocation death;
            public AssetLocation PVPdeath;

            public bool enablemention;
            public bool enablelogin;
            public bool enablelogout;
            public bool enabledeath;
            public bool enablePVPdeath;
        }

        public class bbConfig
        {
            public static bbConfig Current { get; set; }

            public soundSettings soundsettings = new soundSettings();

            public static bbConfig getDefault()
            {
                var config = new bbConfig();

                soundSettings tempsettings = new soundSettings();
                tempsettings.mention = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.login = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.logout = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.death = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.PVPdeath = new AssetLocation("game", "sounds/effect/receptionbell");
                tempsettings.enabledeath = true;
                tempsettings.enablelogin = true;
                tempsettings.enablelogout = true;
                tempsettings.enablemention = true;
                tempsettings.enablePVPdeath = true;

                config.soundsettings = tempsettings;

                return config;
            }
        }
    }
}
