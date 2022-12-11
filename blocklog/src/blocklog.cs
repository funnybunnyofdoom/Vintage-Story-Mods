using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.Essentials;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using ProtoBuf;

namespace blocklog.src
{
    class BlockLog : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side) //What side should this load
        {
            return side == EnumAppSide.Server; //load on the server side
        }

        ICoreServerAPI sapi; //Initialize our server API
        public Dictionary<BlockPos, blockdata> blockbreaksave = new Dictionary<BlockPos, blockdata>();
        public Dictionary<BlockPos, blockdata> blockplacesave = new Dictionary<BlockPos, blockdata>();
        public Dictionary<string, creaturedata[]> creaturedeathsave = new Dictionary<string, creaturedata[]>();

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api; //Assign our API to the global API for use later in the file
            
            //event listeners
            api.Event.BreakBlock += onBreakBlock; //detect each block broken this calls onBreakBlock function
            api.Event.DidPlaceBlock += onPlaceBlock; //Detect each time a block is placed
            api.Event.OnEntityDeath += onEntityDeath; //Detect each time an entity dies

            api.Event.SaveGameLoaded += OnSaveGameLoading; // Load our data each game load
            api.Event.GameWorldSave += OnSaveGameSaving; // Save our data each game save

            //register commands
            api.RegisterCommand("blocklog", "Tracks block broken/placed", "", cmd_blocklog, Privilege.controlserver); //Register the /blocklog command
        }

        //Command functions
        private void cmd_blocklog(IServerPlayer player, int groupId, CmdArgs args)
        {
            if (player.CurrentBlockSelection == null) return;
            BlockPos selection = player.CurrentBlockSelection.Position.UpCopy(1); //Get the block above the selection
            BlockPos placeselection = player.CurrentBlockSelection.Position;
            blockdata bdata; //initiliaze the variable to hold our class
            bool state = blockbreaksave.TryGetValue(selection,out bdata); //Try to get the value from blockbreaksave
            blockdata Pdata; //initiliaze the variable to hold our class
            bool Pstate = blockplacesave.TryGetValue(placeselection, out Pdata); //Try to get the value from blockplacesave
            if (state != false)
            {
                
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-player", bdata.player), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-block", bdata.block), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-location", (selection.X - (sapi.WorldManager.MapSizeX / 2)).ToString(), (selection.Z - (sapi.WorldManager.MapSizeZ / 2)).ToString(), (selection.Y).ToString()), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                /*System.Diagnostics.Debug.WriteLine("selection x: "+selection.X);
                System.Diagnostics.Debug.WriteLine("mapsize x: " + sapi.WorldManager.MapSizeX);
                System.Diagnostics.Debug.WriteLine("mapsize x /2: " + sapi.WorldManager.MapSizeX / 2);
                System.Diagnostics.Debug.WriteLine(selection.X+"-"+(sapi.WorldManager.MapSizeX / 2));
                System.Diagnostics.Debug.WriteLine((selection.X - (sapi.WorldManager.MapSizeX / 2)).ToString());

                System.Diagnostics.Debug.WriteLine("selection z: " + player.CurrentBlockSelection.Position.Z);
                System.Diagnostics.Debug.WriteLine("mapsize z: " + sapi.WorldManager.MapSizeZ);
                System.Diagnostics.Debug.WriteLine("mapsize z /2: " + sapi.WorldManager.MapSizeZ / 2);
                System.Diagnostics.Debug.WriteLine(selection.Z + "-" + (sapi.WorldManager.MapSizeZ / 2));
                System.Diagnostics.Debug.WriteLine((selection.Z - (sapi.WorldManager.MapSizeZ / 2)).ToString());*/
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-date", bdata.date), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player

            }
            if (Pstate != false)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:place-player", Pdata.player), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:place-block", Pdata.block), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                //player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:place-location", (placeselection.X - (sapi.WorldManager.MapSizeX / 2)).ToString(), (placeselection.Z - (sapi.WorldManager.MapSizeZ / 2)).ToString(), placeselection.Y.ToString()), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:place-date", Pdata.date), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
            }
                

        }

        //Event Listener Functions
        private void onBreakBlock(IServerPlayer byPlayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling) //This function is called by our event listener when a block breaks
        {
            blockdata bdata = new blockdata(); //Initialize our blockdata class to hold our player and block info
            BlockPos pos = blockSel.Position; //get the pos of the block
            string position = pos.ToString(); //Convert the pos into a readable string
            bdata.player = byPlayer.PlayerName; //Get the player's name
            bdata.block = sapi.World.GetBlock(blockSel.Block.BlockId).GetPlacedBlockName(sapi.World, pos); //Get the block name
            bdata.date = sapi.World.Calendar.PrettyDate(); //get the date
            if (blockbreaksave.ContainsKey(pos)) //check if this pos already has an entry
            {
                blockbreaksave.Remove(pos);//remove the old pos from the dictionary
            }
            blockbreaksave.Add(pos, bdata);  //Add the data to our dictionary to be saved
        }

        private void onPlaceBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            blockdata bdata = new blockdata(); //Initialize our blockdata class to hold our player and block info
            BlockPos pos = blockSel.Position; //get the pos of the block
            string position = pos.ToString();
            bdata.player = byPlayer.PlayerName;
            bdata.date = sapi.World.Calendar.PrettyDate();
            if (blockplacesave.ContainsKey(pos)) //check if this pos already has an entry
            {
                blockplacesave.Remove(pos);//remove the old pos from the dictionary
            }
            blockplacesave.Add(pos, bdata);  //Add the data to our dictionary to be saved
        }

        private void onEntityDeath(Entity entity, DamageSource damageSource)
        {
            
            creaturedata cdata = new creaturedata();
            
            //System.Diagnostics.Debug.WriteLine(entity.FirstCodePart()); //Uncomment this to check for new projectiles to filter out
            if (entity.FirstCodePart() == "arrow" || entity.FirstCodePart() == "stone" || entity.FirstCodePart() == "thrownstone" || entity.FirstCodePart() == "magicprojectile") { return; } //Add projectiles from other mods here
            string killedname = entity.GetName();
            DateTime date = System.DateTime.Now;
            string killedbyname;
            BlockPos deathposition = entity.Pos.AsBlockPos;
            LandClaim[] lc = entity.World.Claims.Get(entity.Pos.AsBlockPos);
            if (damageSource != null)
            {
                if (damageSource.SourceEntity == null)
                {
                    if (damageSource.SourceBlock == null)
                    {
                        return;
                    }
                    else
                    {
                        killedbyname = damageSource.SourceBlock.GetPlacedBlockName(entity.World, damageSource.SourcePos.AsBlockPos);
                    }
                }
                else
                {
                    killedbyname = damageSource.SourceEntity.GetName();
                }
            }
            else
            {
                killedbyname = "Killed by a block";
            }
            
           

            System.Diagnostics.Debug.WriteLine(killedname);
            System.Diagnostics.Debug.WriteLine(killedbyname);
            System.Diagnostics.Debug.WriteLine(date.ToString());
            if (lc != null)
            {
                if (lc.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Claim owner: " + lc[0].LastKnownOwnerName);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Not in a claim");
            }


        }
        private void OnSaveGameSaving() //This function is called when the game saves
        {
            sapi.WorldManager.SaveGame.StoreData("blockbreaksave", SerializerUtil.Serialize(blockbreaksave)); //Serialize and store our block break dictionary
            sapi.WorldManager.SaveGame.StoreData("blockplacesave", SerializerUtil.Serialize(blockplacesave)); //Serialize and store our block place dictionary
            sapi.WorldManager.SaveGame.StoreData("creaturedeathsave", SerializerUtil.Serialize(creaturedeathsave)); //Serialize and store our block place dictionary
        }

        private void OnSaveGameLoading() //This is called when the game loads
        {
            //Dictionary<BlockPos,blockdata> blockbreaksavedata = sapi.WorldManager.SaveGame.GetData<Dictionary<BlockPos,blockdata>>("blockbreaksave");  //Get the data in bytes

            byte[] blockbreaksavedata = sapi.WorldManager.SaveGame.GetData("blockbreaksave");  //Get the data in bytes
            byte[] blockplacesavedata = sapi.WorldManager.SaveGame.GetData("blockplacesave");  //Get the data in bytes
            byte[] creaturedeathsavedata = sapi.WorldManager.SaveGame.GetData("creaturedeathsave");  //Get the data in bytes

            blockbreaksave = blockbreaksavedata == null ? new Dictionary<BlockPos, blockdata>() : SerializerUtil.Deserialize<Dictionary<BlockPos, blockdata>>(blockbreaksavedata); //deserialize our data and place it into blockbreaksave
            blockplacesave = blockplacesavedata == null ? new Dictionary<BlockPos, blockdata>() : SerializerUtil.Deserialize<Dictionary<BlockPos, blockdata>>(blockplacesavedata); //deserialize our data and place it into blockbreaksave
            creaturedeathsave = creaturedeathsavedata == null ? new Dictionary<string, creaturedata[]>() : SerializerUtil.Deserialize<Dictionary<string, creaturedata[]>>(creaturedeathsavedata); //deserialize our data and place it into blockbreaksave
        }

        //classes//

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class blockdata//This class holds our broken/placed block information
        {
            public string player;//Player who did the action
            public string block; //Block the action was done to
            public string date; //Date the block was broken
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class creaturedata//This class holds our killed creature data
        {
            public string killed;//Entity that got killed
            public string killedby; //thing that killed the entity
            public DateTime date; //Date the entity was killed
            public string claimowner; //Owner of the claim this creature was killed in
            public BlockPos pos; //Position of the death
        }

    }

}
