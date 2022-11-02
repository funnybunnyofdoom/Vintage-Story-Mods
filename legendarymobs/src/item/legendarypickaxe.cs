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
    class legendarypickaxe : Item
    {

            public void destroyBlocks(IWorldAccessor world, BlockPos min, BlockPos max, IPlayer player, EnumBlockMaterial material)
            {
                BlockPos tempPos = new BlockPos();
                for (int x = min.X; x <= max.X; x++)
                {
                    for (int y = min.Y; y <= max.Y; y++)
                    {
                        for (int z = min.Z; z <= max.Z; z++)
                        {
                            tempPos.Set(x, y, z);
                        if (world.BlockAccessor.GetBlock(tempPos).BlockMaterial == material)
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
                EnumBlockMaterial stoneMaterial = byEntity.World.GetBlock(6098).BlockMaterial;
                    if (byEntity is EntityPlayer && blockSel.Block.BlockMaterial == stoneMaterial)
                    {
                        IPlayer player = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                        switch (blockSel.Face.Axis)
                        {
                            case EnumAxis.X:
                                destroyBlocks(world, blockSel.Position.AddCopy(0, -1, -1), blockSel.Position.AddCopy(0, 1, 1), player, stoneMaterial);
                                break;
                            case EnumAxis.Y:
                                destroyBlocks(world, blockSel.Position.AddCopy(-1, 0, -1), blockSel.Position.AddCopy(1, 0, 1), player, stoneMaterial);
                                break;
                            case EnumAxis.Z:
                                destroyBlocks(world, blockSel.Position.AddCopy(-1, -1, 0), blockSel.Position.AddCopy(1, 1, 0), player, stoneMaterial);
                                break;
                        }
                    }
                    return true;
                }
                return false;
            }

    }
}
