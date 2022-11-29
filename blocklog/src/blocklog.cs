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

namespace blocklog.src
{
    class BlockLog : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side) //What side should this load
        {
            return side == EnumAppSide.Server; //load on the server side
        }

        ICoreServerAPI sapi; //Initialize our server API
        Dictionary<BlockPos, blockdata> blockbreaksave;
        
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api; //Assign our API to the global API for use later in the file
            
            //event listeners
            api.Event.BreakBlock += onBreakBlock; //detect each block broken this calls onBreakBlock function
            api.Event.SaveGameLoaded += OnSaveGameLoading; // Load our data each game load
            api.Event.GameWorldSave += OnSaveGameSaving; // Save our data each game save

            //register commands
            api.RegisterCommand("blocklog", "Tracks block broken/placed", "", cmd_blocklog); //Register the /blocklog command
        }

        //Command functions
        private void cmd_blocklog(IServerPlayer player, int groupId, CmdArgs args)
        {
            BlockPos selection = player.CurrentBlockSelection.Position.Up(); //Get the block above the selection
            blockdata bdata; //initiliaze the variable to hold our class
            bool state = blockbreaksave.TryGetValue(selection,out bdata); //Try to get the value from blockbreaksave
            if (state == false) return;
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-player", bdata.player), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-block", bdata.block), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-location", (selection.X - (sapi.WorldManager.MapSizeX / 2)).ToString(), (selection.Z - (sapi.WorldManager.MapSizeZ / 2)).ToString(),selection.Y.ToString()), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player
            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, Lang.Get("blocklog:break-date", bdata.date), Vintagestory.API.Common.EnumChatType.Notification); //Display information to the player

        }

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

        private void OnSaveGameSaving() //This function is called when the game saves
        {
            sapi.WorldManager.SaveGame.StoreData("bsuBack", SerializerUtil.Serialize(blockbreaksave)); //Serialize and store our dictionary
        }

        private void OnSaveGameLoading() //This is called when the game loads
        {
            byte[] blocksavedata = sapi.WorldManager.SaveGame.GetData("blockbreaksave");  //Get the data in bytes

            blockbreaksave = blocksavedata == null ? new Dictionary<BlockPos, blockdata>() : SerializerUtil.Deserialize<Dictionary<BlockPos, blockdata>>(blocksavedata); //deserialize our data and place it into blockbreaksave
        }

        //classes//
        
        public class blockdata //This class holds our broken/placed block information
        {
            public string player; //Player who did the action
            public string block; //Block the action was done to
            public string date; //Date the block was broken
        }

    }

}
