using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;


namespace roadworks.src
{
    public class blockRampMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("blockRamp",typeof(blockRamp));

        }

    }

    internal class blockRamp : Block
    {

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return false;
            }

            if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                BlockFacing[] horVer = SuggestedHVOrientation(byPlayer, blockSel);
                

                BlockPos secondPos = blockSel.Position.AddCopy(horVer[0]);
                BlockPos thirdPos = secondPos.AddCopy(horVer[0]);
                BlockPos fourthPos = thirdPos.AddCopy(horVer[0]);

                BlockSelection secondBlockSel = new BlockSelection() { Position = secondPos, Face = BlockFacing.UP };
                BlockSelection thirdBlockSel = new BlockSelection() { Position = thirdPos, Face = BlockFacing.UP };
                BlockSelection fourthBlockSel = new BlockSelection() { Position = fourthPos, Face = BlockFacing.UP };
                if (!CanPlaceBlock(world, byPlayer, secondBlockSel, ref failureCode) || !CanPlaceBlock(world, byPlayer, thirdBlockSel, ref failureCode) || !CanPlaceBlock(world, byPlayer, fourthBlockSel, ref failureCode)) return false;

                string code = horVer[0].Opposite.Code;

                string roadType = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Block.LastCodePart();

                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("1", code, roadType));//hardcoded asphalt for testing
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

                AssetLocation secondCode = CodeWithParts("2", code, roadType);
                orientedBlock = world.BlockAccessor.GetBlock(secondCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, secondBlockSel, itemstack);
               

                AssetLocation thirdCode = CodeWithParts("3", code, roadType);
                orientedBlock = world.BlockAccessor.GetBlock(thirdCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, thirdBlockSel, itemstack);

                AssetLocation fourthCode = CodeWithParts("4", code, roadType);
                orientedBlock = world.BlockAccessor.GetBlock(fourthCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, fourthBlockSel, itemstack);
                return true;
            }
            return false;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            string rampPosition = LastCodePart(2);

            BlockFacing facing = BlockFacing.FromCode(LastCodePart(1)).Opposite;
            if (LastCodePart(2) == "1")
            {
                Block firstBlock = world.BlockAccessor.GetBlock(pos);
                BlockPos secondPos = pos.AddCopy(facing);
                Block secondBlock = world.BlockAccessor.GetBlock(secondPos);
                BlockPos thirdPos = secondPos.AddCopy(facing);
                Block thirdBlock = world.BlockAccessor.GetBlock(thirdPos);
                BlockPos fourthPos = thirdPos.AddCopy(facing);
                Block fourthBlock = world.BlockAccessor.GetBlock(fourthPos);
                if (firstBlock is blockRamp && firstBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                }
                if (secondBlock is blockRamp && secondBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, secondPos);
                }
                if (thirdBlock is blockRamp && thirdBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, thirdPos);
                }
                if (fourthBlock is blockRamp && fourthBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, fourthPos);
                }

                base.OnBlockRemoved(world, pos);
                base.OnBlockRemoved(world, secondPos);
                base.OnBlockRemoved(world, thirdPos);
                base.OnBlockRemoved(world, fourthPos);
            }else if (LastCodePart(2) == "2")
            {
                Block firstBlock = world.BlockAccessor.GetBlock(pos);
                BlockPos secondPos = pos.AddCopy(facing);
                Block secondBlock = world.BlockAccessor.GetBlock(secondPos);
                BlockPos thirdPos = secondPos.AddCopy(facing);
                Block thirdBlock = world.BlockAccessor.GetBlock(thirdPos);
                BlockPos fourthPos = pos.AddCopy(facing.Opposite);
                Block fourthBlock = world.BlockAccessor.GetBlock(fourthPos);
                if (firstBlock is blockRamp && firstBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                }
                if (secondBlock is blockRamp && secondBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, secondPos);
                }
                if (thirdBlock is blockRamp && thirdBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, thirdPos);
                }
                if (fourthBlock is blockRamp && fourthBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, fourthPos);
                }

                base.OnBlockRemoved(world, pos);
                base.OnBlockRemoved(world, secondPos);
                base.OnBlockRemoved(world, thirdPos);
                base.OnBlockRemoved(world, fourthPos);
            }else if (LastCodePart(2) == "3")
            {
                Block firstBlock = world.BlockAccessor.GetBlock(pos);
                BlockPos secondPos = pos.AddCopy(facing);
                Block secondBlock = world.BlockAccessor.GetBlock(secondPos);
                BlockPos thirdPos = pos.AddCopy(facing.Opposite);
                Block thirdBlock = world.BlockAccessor.GetBlock(thirdPos);
                BlockPos fourthPos = thirdPos.AddCopy(facing.Opposite);
                Block fourthBlock = world.BlockAccessor.GetBlock(fourthPos);
                if (firstBlock is blockRamp && firstBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                }
                if (secondBlock is blockRamp && secondBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, secondPos);
                }
                if (thirdBlock is blockRamp && thirdBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, thirdPos);
                }
                if (fourthBlock is blockRamp && fourthBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, fourthPos);
                }

                base.OnBlockRemoved(world, pos);
                base.OnBlockRemoved(world, secondPos);
                base.OnBlockRemoved(world, thirdPos);
                base.OnBlockRemoved(world, fourthPos);
            }else if (LastCodePart(2) == "4")
            {
                facing = facing.Opposite;
                Block firstBlock = world.BlockAccessor.GetBlock(pos);
                BlockPos secondPos = pos.AddCopy(facing);
                Block secondBlock = world.BlockAccessor.GetBlock(secondPos);
                BlockPos thirdPos = secondPos.AddCopy(facing);
                Block thirdBlock = world.BlockAccessor.GetBlock(thirdPos);
                BlockPos fourthPos = thirdPos.AddCopy(facing);
                Block fourthBlock = world.BlockAccessor.GetBlock(fourthPos);
                if (firstBlock is blockRamp && firstBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                }
                if (secondBlock is blockRamp && secondBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, secondPos);
                }
                if (thirdBlock is blockRamp && thirdBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, thirdPos);
                }
                if (fourthBlock is blockRamp && fourthBlock.LastCodePart(2) != rampPosition)
                {
                    world.BlockAccessor.SetBlock(0, fourthPos);
                }

                base.OnBlockRemoved(world, pos);
                base.OnBlockRemoved(world, secondPos);
                base.OnBlockRemoved(world, thirdPos);
                base.OnBlockRemoved(world, fourthPos);
            }
        }
        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            string roadtype = world.BlockAccessor.GetBlock(pos).LastCodePart();
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("1", "north", roadtype))) };
        }
    }
}
