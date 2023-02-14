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
        public Dictionary<string, List<creaturedata>> creaturedeathsave = new Dictionary<string, List<creaturedata>>();

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
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-date", bdata.date), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player

            }
            if (Pstate != false)
            {
                player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:place-player", Pdata.player), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
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
            LandClaim[] lc = entity.World.Claims.Get(entity.Pos.AsBlockPos);
            if(lc == null)
            {
                return; //End function if dead creature isn't on a claim. We'll only be tracking creatures killed within a claim.
            }
            //System.Diagnostics.Debug.WriteLine(entity.FirstCodePart()); //Uncomment this to check for new projectiles to filter out
            if (entity.FirstCodePart() == "arrow" || entity.FirstCodePart() == "stone" || entity.FirstCodePart() == "thrownstone" || entity.FirstCodePart() == "magicprojectile") { return; } //Add projectiles from other mods here
            cdata.killed = entity.GetName();
            cdata.date = System.DateTime.Now;
            string killedbyname;
            cdata.pos = entity.Pos.AsBlockPos;
            
            if (damageSource != null)
            {
                if (damageSource.SourceEntity == null)
                {
                    if (damageSource.SourceBlock == null)
                    {
                        killedbyname = "Unknown";
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
            cdata.killedby = killedbyname;
           

            //System.Diagnostics.Debug.WriteLine(cdata.killed);
            //.Diagnostics.Debug.WriteLine(cdata.killedby);
            //System.Diagnostics.Debug.WriteLine(cdata.date.ToString());
            if (lc != null)
            {
                if (lc.Length > 0)
                {
                    //System.Diagnostics.Debug.WriteLine("Claim owner: " + lc[0].LastKnownOwnerName);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Not in a claim");
            }
            cdata.claimowner = lc[0].LastKnownOwnerName;
            //System.Diagnostics.Debug.WriteLine(cdata.claimowner);

            //add this new creature data to the list
            Dictionary<string, List<creaturedata>> tempdictionary = creaturedeathsave;
            
            if (tempdictionary.ContainsKey(cdata.claimowner))
            {
                List<creaturedata> tempcreaturedata;
                tempdictionary.TryGetValue(cdata.claimowner, out tempcreaturedata);
                if (tempcreaturedata == null)
                {
                    tempcreaturedata = new List<creaturedata>(); //initialize the list
                }
                tempcreaturedata.Add(cdata);
                creaturedeathsave[cdata.claimowner] = tempcreaturedata;
            }
            else
            {
                List<creaturedata> tempcreaturedata = new List<creaturedata>();
                tempcreaturedata.Add(cdata);
                creaturedeathsave[cdata.claimowner] = tempcreaturedata;
            }
            

            //Check save file DEBUG
            List<creaturedata> testcreaturedata = new List<creaturedata>();
            creaturedeathsave.TryGetValue(cdata.claimowner, out testcreaturedata);
            for (int i = 0; i < testcreaturedata.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine("name:"+ testcreaturedata[i].killed);
                System.Diagnostics.Debug.WriteLine("killed by:" + testcreaturedata[i].killedby);
                System.Diagnostics.Debug.WriteLine("Claim Owner:" + testcreaturedata[i].claimowner);
                System.Diagnostics.Debug.WriteLine("death date:" + testcreaturedata[i].date.ToString());
                System.Diagnostics.Debug.WriteLine("Position:" + testcreaturedata[i].pos.ToString());
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
            
            byte[] blockbreaksavedata = sapi.WorldManager.SaveGame.GetData("blockbreaksave");  //Get the data in bytes
            byte[] blockplacesavedata = sapi.WorldManager.SaveGame.GetData("blockplacesave");  //Get the data in bytes
            byte[] creaturedeathsavedata = sapi.WorldManager.SaveGame.GetData("creaturedeathsave");  //Get the data in bytes

            blockbreaksave = blockbreaksavedata == null ? new Dictionary<BlockPos, blockdata>() : SerializerUtil.Deserialize<Dictionary<BlockPos, blockdata>>(blockbreaksavedata); //deserialize our data and place it into blockbreaksave
            blockplacesave = blockplacesavedata == null ? new Dictionary<BlockPos, blockdata>() : SerializerUtil.Deserialize<Dictionary<BlockPos, blockdata>>(blockplacesavedata); //deserialize our data and place it into blockbreaksave
            creaturedeathsave = creaturedeathsavedata == null ? new Dictionary<string, List<creaturedata>>() : SerializerUtil.Deserialize<Dictionary<string, List<creaturedata>>>(creaturedeathsavedata); //deserialize our data and place it into blockbreaksave
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
