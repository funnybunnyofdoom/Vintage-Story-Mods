using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace legendarymobs.src
{
    class legendaryshovel : Item
    {

            public void destroyBlocks(IWorldAccessor world, BlockPos min, BlockPos max, IPlayer player, EnumBlockMaterial material, EnumBlockMaterial material2, EnumBlockMaterial material3)
            {
                BlockPos tempPos = new BlockPos();
                for (int x = min.X; x <= max.X; x++)
                {
                    for (int y = min.Y; y <= max.Y; y++)
                    {
                        for (int z = min.Z; z <= max.Z; z++)
                        {
                            tempPos.Set(x, y, z);
                        if (world.BlockAccessor.GetBlock(tempPos).BlockMaterial == material || world.BlockAccessor.GetBlock(tempPos).BlockMaterial == material2 || world.BlockAccessor.GetBlock(tempPos).BlockMaterial == material3)
                        {
                            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                                world.BlockAccessor.SetBlock(0, tempPos);
                            else
                                world.BlockAccessor.BreakBlock(tempPos, player);
                        }
                        }
                    }
                }
            }

            public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
            {
                if (base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel))
                {
                EnumBlockMaterial material = byEntity.World.GetBlock(new AssetLocation("game:soil-medium-none")).BlockMaterial; //Dirt
                EnumBlockMaterial material2 = byEntity.World.GetBlock(new AssetLocation("game:gravel-granite")).BlockMaterial; //Gravel
                EnumBlockMaterial material3 = byEntity.World.GetBlock(new AssetLocation("game:snowblock")).BlockMaterial; //Snow
                if (byEntity is EntityPlayer && (blockSel.Block.BlockMaterial == material || blockSel.Block.BlockMaterial == material2 || blockSel.Block.BlockMaterial == material3))
                    {
                        IPlayer player = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                        switch (blockSel.Face.Axis)
                        {
                            case EnumAxis.X:
                                destroyBlocks(world, blockSel.Position.AddCopy(0, -1, -1), blockSel.Position.AddCopy(0, 1, 1), player, material, material2, material3);
                                break;
                            case EnumAxis.Y:
                                destroyBlocks(world, blockSel.Position.AddCopy(-1, 0, -1), blockSel.Position.AddCopy(1, 0, 1), player, material, material2, material3);
                                break;
                            case EnumAxis.Z:
                                destroyBlocks(world, blockSel.Position.AddCopy(-1, -1, 0), blockSel.Position.AddCopy(1, 1, 0), player, material, material2, material3);
                                break;
                        }
                    }
                    return true;
                }
                return false;
            }

    }
}
