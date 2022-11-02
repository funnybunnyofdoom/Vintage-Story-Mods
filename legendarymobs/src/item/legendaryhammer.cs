using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace legendarymobs.src
{
    class legendaryhammer : ItemHammer
    {
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1) //Called when the hammer breaks a block
        {
            if (blockSel.Block.Code.Path.StartsWith("rock")) //Check that the block begins with the prefix "rock"
            {
                Vec3d position = blockSel.Position.ToVec3d(); //Save the position of the block broken
                ItemStack item = new ItemStack(blockSel.Block); //Create an itemstack of the broken block
                item.StackSize = 1; //Make the itemstack only contain 1 block
                byEntity.World.SpawnItemEntity(item, position); //Spawn the itemstack where the block was broken
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, 0);//Return, making sure to pass 0 to disable drops from the rock
        }

    }
}
