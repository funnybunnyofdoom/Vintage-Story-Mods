using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace rocksalt.src
{
    public class rocksaltModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("rocksalt", typeof(ItemRockSalt));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.RegisterItemClass("rocksalt", typeof(ItemRockSalt));
        }
    }

    public class ItemRockSalt : Item
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "treeSeedInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();

                foreach (Block block in api.World.Blocks)
                {
                    if (block.Code == null || block.EntityClass == null) continue;
                    if (block.Fertility > 0)
                    {
                        stacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "heldhelp-plant",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            if (blockSel.Block == null) return;

            if (blockSel.Block.FirstCodePart() == "snowlayer" || blockSel.Block.FirstCodePart() == "snowblock" || blockSel.Block.FirstCodePart() == "lakeice" || blockSel.Block.FirstCodePart() == "glacierice" || blockSel.Block.FirstCodePart() == "stonepath" || blockSel.Block.FirstCodePart() == "roadblock" || blockSel.Block.FirstCodePart() == "roadblockcobblestone" || blockSel.Block.FirstCodePart() == "roadblockgravel" || blockSel.Block.FirstCodePart() == "roadblockbrick" || blockSel.Block.FirstCodePart() == "roadblockwood")
            {
                IPlayer byPlayer = null;
                Boolean Success = false;
                if (byEntity is EntityPlayer)
                {
                    byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                }

                blockSel = blockSel.Clone();

                

                if ((byEntity as EntityPlayer) == null || ((byEntity as EntityPlayer).Player as IClientPlayer) == null) return;

                ((byEntity as EntityPlayer).Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                if (byPlayer == null) return;
                if (byPlayer.WorldData == null) return;

                string firstpart = api.World.BlockAccessor.GetBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z).FirstCodePart(); //use this for the if statements

                var newBlockId = api.World.GetBlock(new AssetLocation("rocksalt:rocksaltblock")).BlockId;

                if (api.World.BlockAccessor.GetBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, BlockLayersAccess.Fluid).FirstCodePart() == "lakeice")
                {
                    //Lake ice might have water underneath. We shouldn't place our rocksalt block in this case, I think
                    api.World.BlockAccessor.SetBlock(0, blockSel.Position, BlockLayersAccess.Fluid);
                    if (api.World.BlockAccessor.GetBlock(blockSel.Position.Up()).FirstCodePart() == "snowlayer")
                    {
                        api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                    }

                    Success = true; //This triggers our sound

                }
                else if (api.World.BlockAccessor.GetBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z).FirstCodePart() == "stonepath")
                {
                    if (api.World.BlockAccessor.GetBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z).FirstCodePart(1) == "snow")
                    {
                        api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("stonepath-free")).BlockId, blockSel.Position);
                        api.World.BlockAccessor.SetBlock(newBlockId, blockSel.Position.UpCopy());

                        Success = true; //This triggers our sound

                    } 

                }
                else if (firstpart == "roadblockcobblestone" || firstpart == "roadblockgravel" || firstpart == "roadblockwood" || firstpart == "roadblockbrick" || firstpart == "roadblock")
                {
                    Block block = api.World.BlockAccessor.GetBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    string code = "roadworks:"+block.FirstCodePart()+"-free-" + block.FirstCodePart(2);
                    Block gb = api.World.GetBlock(new AssetLocation(code));
                    if (gb == null)
                    {                        
                        return;
                    }
                    api.World.BlockAccessor.SetBlock(gb.BlockId, blockSel.Position);
                    api.World.BlockAccessor.SetBlock(newBlockId, blockSel.Position.UpCopy());

                    Success = true; // This triggers our sound
                }
                else
                {
                    
                    if (api.World.BlockAccessor.GetBlock(blockSel.Position).FirstCodePart() == "snowlayer")
                    {
                        //api.World.BlockAccessor.SetBlock(newBlockId, blockSel.Position);
                        api.World.BlockAccessor.SetBlock(newBlockId, blockSel.Position);

                        Success = true; //This triggers our sound
                    }
                    else
                    {
                        api.World.BlockAccessor.SetBlock(newBlockId, blockSel.Position);
                        Success = true; //This triggers our sound and consumes salt item
                    }
                }

                if(Success == true)
                {
                    if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                    {
                        itemslot.TakeOut(1);
                        itemslot.MarkDirty();
                        byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/stonecrush"), blockSel.Position.X + 0.5f, blockSel.Position.Y, blockSel.Position.Z + 0.5f, byPlayer);
                    }
                }

            }
            else
            {
                return;
            }


            handHandling = EnumHandHandling.PreventDefault;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
