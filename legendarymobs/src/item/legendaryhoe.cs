﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace legendarymobs.src
{
    class legendaryhoe : Item
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "hoeInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();

                foreach (Block block in api.World.Blocks)
                {
                    if (block.Code == null) continue;

                    if (block.Code.Path.StartsWith("soil"))
                    {
                        stacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "heldhelp-till",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }



        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null) return;

            if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            BlockPos pos = blockSel.Position;
            Block block = byEntity.World.BlockAccessor.GetBlock(pos);

            byEntity.Attributes.SetInt("didtill", 0);

            if (block.Code.Path.StartsWith("soil"))
            {
                handHandling = EnumHandHandling.PreventDefault;
            }
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey) return false;

            IPlayer byPlayer = (byEntity as EntityPlayer).Player;

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();

                float rotateToTill = GameMath.Clamp(secondsUsed * 18, 0, 2f);
                float scrape = GameMath.SmoothStep(1 / 0.4f * GameMath.Clamp(secondsUsed - 0.35f, 0, 1));
                float scrapeShake = secondsUsed > 0.35f && secondsUsed < 0.75f ? (float)(GameMath.Sin(secondsUsed * 50) / 60f) : 0;

                float rotateWithReset = Math.Max(0, rotateToTill - GameMath.Clamp(24 * (secondsUsed - 0.75f), 0, 2));
                float scrapeWithReset = Math.Max(0, scrape - Math.Max(0, 20 * (secondsUsed - 0.75f)));

                tf.Origin.Set(0f, 0, 0.5f);
                tf.Rotation.Set(0, rotateWithReset * 45, 0);
                tf.Translation.Set(scrapeShake, 0, scrapeWithReset / 2);

                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }

            if (secondsUsed > 0.35f && secondsUsed < 0.87f)
            {
                Vec3d dir = new Vec3d().AheadCopy(1, 0, byEntity.SidedPos.Yaw - GameMath.PI);
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5 + dir.X, 1.03, 0.5 + dir.Z);

                pos.X -= dir.X * secondsUsed * 1 / 0.75f * 1.2f;
                pos.Z -= dir.Z * secondsUsed * 1 / 0.75f * 1.2f;

                byEntity.World.SpawnCubeParticles(blockSel.Position, pos, 0.25f, 3, 0.5f, byPlayer);
            }

            if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill") == 0 && byEntity.World.Side == EnumAppSide.Server)
            {
                byEntity.Attributes.SetInt("didtill", 1);

                
                    IPlayer player = byPlayer;
                    switch (blockSel.Face.Axis)
                    {
                        case EnumAxis.X:
                            areaTill(byEntity.World, blockSel.Position.AddCopy(0, -1, -1), blockSel.Position.AddCopy(0, 1, 1), secondsUsed, slot, byEntity, blockSel, entitySel);
                            break;
                        case EnumAxis.Y:
                            areaTill(byEntity.World, blockSel.Position.AddCopy(-1, 0, -1), blockSel.Position.AddCopy(1, 0, 1), secondsUsed, slot, byEntity, blockSel, entitySel);
                            break;
                        case EnumAxis.Z:
                            areaTill(byEntity.World, blockSel.Position.AddCopy(-1, -1, 0), blockSel.Position.AddCopy(1, 1, 0), secondsUsed, slot, byEntity, blockSel, entitySel);
                            break;
                    }
                
            }

            return secondsUsed < 1;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        public void areaTill(IWorldAccessor world, BlockPos min, BlockPos max, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            BlockPos tempPos = new BlockPos();
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int z = min.Z; z <= max.Z; z++)
                    {
                        tempPos.Set(x, y, z);

                        if (blockSel == null) return;
                        BlockPos pos = tempPos;
                        Block block = byEntity.World.BlockAccessor.GetBlock(pos);

                        if (block.Code.Path.StartsWith("soil")) {
                            string fertility = block.LastCodePart(1);
                            Block farmland = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + fertility));

                            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
                            if (farmland != null || byPlayer != null)
                            {
                                if (block.Sounds != null) byEntity.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z, null);

                                byEntity.World.BlockAccessor.SetBlock(farmland.BlockId, pos);
                                slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot);

                                if (slot.Empty)
                                {
                                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                                }

                                BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
                                if (be is BlockEntityFarmland)
                                {
                                    ((BlockEntityFarmland)be).OnCreatedFromSoil(block);
                                }

                                byEntity.World.BlockAccessor.MarkBlockDirty(pos);
                            }
                        }                        
                    }
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
