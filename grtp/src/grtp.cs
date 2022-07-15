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
            if (grtpConfig.Current.cooldownminutes == null)
                grtpConfig.Current.cooldownminutes = grtpConfig.getDefault().cooldownminutes;

            api.StoreModConfig(grtpConfig.Current, "grtpconfig.json");
        }
        count = grtpConfig.Current.cooldownminutes + 1;
        CID = api.Event.RegisterGameTickListener(CoolDown, 60000);
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
        string cmd = args.PopWord();
        switch (cmd)
        {
            case null:
                if (loaded == true)
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location. Others can join you for " + (grtpConfig.Current.cooldownminutes - count) + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);

                    player.Entity.TeleportTo(randx, height + 2, randz);

                }
                else
                {
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Chunk is loading. Please try again in a few seconds", Vintagestory.API.Common.EnumChatType.Notification);
                    sapi.WorldManager.LoadChunkColumnPriority(randx / sapi.WorldManager.ChunkSize, randz / sapi.WorldManager.ChunkSize);
                }
                break;
            case "help":
                displayhelp(player);
                break;
            case "version":
                var modinfo = Mod.Info;
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                break;
            case "cooldown":
                if (player.Role.Code == "admin")
                {
                    int? cdnum = args.PopInt();
                    if (cdnum == null | cdnum == 0)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number greater than 0 in minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else if (cdnum < 0)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a non-negative number.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        grtpConfig.Current.cooldownminutes = cdnum;
                        sapi.StoreModConfig(grtpConfig.Current, "grtpconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP cooldown has been updated to " + cdnum + " minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                break;
            case "radius":
                if (player.Role.Code == "admin")
                {
                    int? cdnum = args.PopInt();
                    if (cdnum == null | cdnum < 10 )
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number 10 or greater.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else if (cdnum < 0)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a non-negative number.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    else
                    {
                        grtpConfig.Current.teleportradius = cdnum;
                        sapi.StoreModConfig(grtpConfig.Current, "grtpconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP radius has been updated to " + cdnum + " blocks.", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                }
                break;
            case "now":
                if (player.Role.Code == "admin")
                {
                    count = grtpConfig.Current.cooldownminutes + 1;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP now issued. Please wait up to a couple minutes to take effect.", Vintagestory.API.Common.EnumChatType.Notification);
                }
                break;
        }


        
    }
    private void CoolDown(float ct)
    {
        if (count >= grtpConfig.Current.cooldownminutes)
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
                sapi.BroadcastMessageToAllGroups("New /grtp coordinates generated. New location will be available in "+grtpConfig.Current.cooldownminutes, Vintagestory.API.Common.EnumChatType.Notification);
            
            }
            
            loaded = true;
        }

    }

    private void displayhelp(IServerPlayer player)
    {
        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Group Random Teleport Commands:", Vintagestory.API.Common.EnumChatType.Notification);
        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "GRTP location updates every "+grtpConfig.Current.cooldownminutes+" minutes.", Vintagestory.API.Common.EnumChatType.Notification);
        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleport Radius: "+ grtpConfig.Current.teleportradius + " Blocks.", Vintagestory.API.Common.EnumChatType.Notification);
        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp - teleports the player to the group teleport point", Vintagestory.API.Common.EnumChatType.Notification);
        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp version - displays the current version of GRTP installed", Vintagestory.API.Common.EnumChatType.Notification);
        if (player.Role.Code == "admin")
        {
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp cooldown <i>number</i> - sets the cooldown timer", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp radius <i>number</i> - sets the teleport radius", Vintagestory.API.Common.EnumChatType.Notification);
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/grtp now - Sets the GRTP to update on the next 1 minute tick", Vintagestory.API.Common.EnumChatType.Notification);
        }
    }
    public class grtpConfig
    {
        public static grtpConfig Current { get; set; }

        public int? teleportradius { get; set; }
        public int? cooldownminutes { get; set; }

        public static grtpConfig getDefault()
        {
            var config = new grtpConfig();

            config.cooldownminutes = 60;
            config.teleportradius = 100000;

            return config;
        }
    }
}

