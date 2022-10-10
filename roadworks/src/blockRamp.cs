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

                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("1", code, "asphalt"));//hardcoded asphalt for testing
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

                AssetLocation secondCode = CodeWithParts("2", code,"asphalt");
                orientedBlock = world.BlockAccessor.GetBlock(secondCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, secondBlockSel, itemstack);
               

                AssetLocation thirdCode = CodeWithParts("3", code, "asphalt");
                orientedBlock = world.BlockAccessor.GetBlock(thirdCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, thirdBlockSel, itemstack);

                AssetLocation fourthCode = CodeWithParts("4", code, "asphalt");
                orientedBlock = world.BlockAccessor.GetBlock(fourthCode);
                orientedBlock.DoPlaceBlock(world, byPlayer, fourthBlockSel, itemstack);
                return true;
            }
            return false;
        }
    }
}
