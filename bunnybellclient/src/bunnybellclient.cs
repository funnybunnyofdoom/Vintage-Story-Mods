using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
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
            api.Event.OnEntityDeath += test;//onPlayerDeath; // Event listener for when a player dies
            api.Event.OnEntityDespawn += test2;
            api.RegisterCommand("bb", "Bunny Bell configuration", "[volume|mute|help|version]", cmd_bb);

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

        private void test2(Entity entity, EntityDespawnReason reason)
        {
            capi.Logger.Debug("Side: {0}", capi.Side);
        }

        private void test(Entity entity, DamageSource damageSource)
        {
            capi.Logger.Debug("Side: {0}", capi.Side);
        }

        private void cmd_bb(int groupId, CmdArgs args)
        {
            IClientPlayer player = capi.World.Player;
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "help":
                    displayhelp(player);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    capi.World.Player.ShowChatNotification("Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version);
                    break;
                case "sound":
                    var num = args.PopWord();
                    if (num == null)
                    {
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:need-number", soundList.Count));
                    }
                    else
                    {
                        int result;
                        if (int.TryParse(num, out result))
                        {
                            if (result > -1 && result <= soundList.Count() - 1)
                            {
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:play-sound", result));
                                player.Entity.World.PlaySoundFor(soundList[result], player);
                            }
                            else
                            {
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:need-number", soundList.Count - 1));
                            }
                        }
                        else
                        {
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:need-number", soundList.Count - 1));
                        }
                    }
                    break;
                case "set":
                    string action = args.PopWord();
                    int? sound = args.PopWord().ToInt();
                    if (action == null || sound == null)
                    {
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:set-sound-help-1", soundList.Count - 1));
                    }
                    else if (sound < 0 || sound > soundList.Count - 1)
                    {
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:set-sound-help-2", soundList.Count - 1));
                    }
                    else
                    {
                            soundSettings tempSettings = bbConfig.Current.soundsettings;

                            if (action == "mention")
                            {
                                tempSettings.mention = soundList[(int)sound];
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:global-sound-set", action, sound));
                            }
                            else if (action == "login")
                            {
                                tempSettings.login = soundList[(int)sound];
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:global-sound-set", action, sound));
                            }
                            else if (action == "logout")
                            {
                                tempSettings.logout = soundList[(int)sound];
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:global-sound-set", action, sound));
                            }
                            else if (action == "death")
                            {
                                tempSettings.death = soundList[(int)sound];
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:global-sound-set", action, sound));
                            }
                            else if (action == "PVPdeath")
                            {
                                tempSettings.PVPdeath = soundList[(int)sound];
                                capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:global-sound-set", action, sound));
                            }
                            capi.StoreModConfig(bbConfig.Current, "bbclient.json");
                    }
                    break;
                case "enable": //Enables the sounds 
                    string gaction = args.PopWord();
                    if (gaction == null)
                    {
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-enable"));
                    }
                    

                        if (gaction == "mention")
                        {
                            bbConfig.Current.soundsettings.enablemention = true;
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:enable-mention-global", gaction));
                        }
                        else if (gaction == "login")
                        {
                            bbConfig.Current.soundsettings.enablelogin = true;
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:enable-mention-global", gaction));
                        }
                        else if (gaction == "logout")
                        {
                            bbConfig.Current.soundsettings.enablelogout = true;
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:enable-mention-global", gaction));
                        }
                        else if (gaction == "death")
                        {
                            bbConfig.Current.soundsettings.enabledeath = true;
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:enable-mention-global", gaction));
                        }
                        else if (gaction == "PVPdeath")
                        {
                            bbConfig.Current.soundsettings.enablePVPdeath = true;
                            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:enable-mention-global", gaction));
                        }
                        capi.StoreModConfig(bbConfig.Current, "bbclient.json");
                    break;
                case "disable":
                    string daction = args.PopWord();
                    if (daction == null)
                    {
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-disable"));
                    }


                    if (daction == "mention")
                    {
                        bbConfig.Current.soundsettings.enablemention = false;
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:disable-mention-global", daction));
                    }
                    else if (daction == "login")
                    {
                        bbConfig.Current.soundsettings.enablelogin = false;
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:disable-mention-global", daction));
                    }
                    else if (daction == "logout")
                    {
                        bbConfig.Current.soundsettings.enablelogout = false;
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:disable-mention-global", daction));
                    }
                    else if (daction == "death")
                    {
                        bbConfig.Current.soundsettings.enabledeath = false;
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:disable-mention-global", daction));
                    }
                    else if (daction == "PVPdeath")
                    {
                        bbConfig.Current.soundsettings.enablePVPdeath = false;
                        capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:disable-mention-global", daction));
                    }
                    capi.StoreModConfig(bbConfig.Current, "bbclient.json");
                    break;
            }
        }

        private void onPlayerDeath(Entity entity, DamageSource damageSource)
        {
            capi.Logger.Debug("Side: {0}", capi.Side);
            System.Diagnostics.Debug.Write("An entity has died");
            if (entity.Code.FirstCodePart() == "player")
            {
                System.Diagnostics.Debug.Write("The dead entity is a player");
                string fcp;
                if (damageSource.SourceEntity == null)
                {
                    fcp = "notPlayer";
                    System.Diagnostics.Debug.Write("The killer is not a player");
                }
                else
                {
                    fcp = damageSource.SourceEntity.FirstCodePart();
                    System.Diagnostics.Debug.Write("Checking to be sure the killer was a player");
                }
                if (fcp == "player")//Check for PVP
                {
                    System.Diagnostics.Debug.Write("The killer was a player");
                    if (bbConfig.Current.soundsettings.enablePVPdeath == false) { return; }
                    System.Diagnostics.Debug.Write("PVPdeath is enabled");
                    capi.World.PlaySoundFor(bbConfig.Current.soundsettings.PVPdeath, capi.World.Player);
                }
                else
                {
                    System.Diagnostics.Debug.Write("The killer was not a player");
                    if (bbConfig.Current.soundsettings.enabledeath == false) { return; }
                    System.Diagnostics.Debug.Write("death is enabled");
                    capi.World.PlaySoundFor(bbConfig.Current.soundsettings.death, capi.World.Player);
                }
            }
        }

        private void onPlayerLogout(IClientPlayer byPlayer)
        {
            if (bbConfig.Current.soundsettings.enablelogout == true)
            {
                capi.World.PlaySoundFor(bbConfig.Current.soundsettings.logout, capi.World.Player);
            }
        }

        private void onPlayerJoined(IClientPlayer byPlayer)
        {
            if (bbConfig.Current.soundsettings.enablelogin == true)
            {
                capi.World.PlaySoundFor(bbConfig.Current.soundsettings.login, capi.World.Player);
            }
        }

        private void onPlayerChat(int groupId, string message, EnumChatType chattype, string data)
        {
            if (bbConfig.Current.soundsettings.enablemention == false) { return; }
            IPlayer templayer = capi.World.Player; //Get the player
            if (templayer != null && templayer.PlayerName != null && data != null)//Make sure player is loaded and message is from a player
            {
                if (data.CaseInsensitiveContains(templayer.PlayerName)) //Check for the players name within the message
                {
                    templayer.Entity.World.PlaySoundFor(bbConfig.Current.soundsettings.mention, templayer, true, 32);//Play a sound for the player
                }
            }
        }

        private void displayhelp(IClientPlayer player)
        {
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-title"));
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-sound", soundList.Count - 1));
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-set", soundList.Count - 1));
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-enable"));
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-disable"));
            capi.World.Player.ShowChatNotification(Lang.Get("bunnybell:help-version"));
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
