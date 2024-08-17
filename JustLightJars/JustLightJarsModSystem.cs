using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System;
using System.Text;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace JustLightJars
{
    public class JustLightJarsModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityBehaviorClass("butterflyjar", typeof(BEBehaviorButterflyInJar));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("justlightjars:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("justlightjars:hello"));
        }
    }

    public class BEBehaviorButterflyInJar : BlockEntityBehavior
    {
        ModSystemControlPoints modSys;
        AnimationMetaData animData;
        ControlPoint cp;

        bool isActive;

        public BEBehaviorButterflyInJar(BlockEntity blockentity) : base(blockentity)
        {

        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            animData = properties["animData"].AsObject<AnimationMetaData>();

            var controlpointcode = AssetLocation.Create(properties["controlpointcode"].ToString(), Block.Code.Domain);

            modSys = api.ModLoader.GetModSystem<ModSystemControlPoints>();
            cp = modSys[controlpointcode];
            cp.ControlData = animData;

            animData.AnimationSpeed = isActive ? 1 : 0;
            cp.Trigger();
        }



        public void Interact(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (isActive)
            {
                StopAnimation();
            }
            else
            {
                StartAnimation();
            }
        }

        public void StartAnimation()
        {
            isActive = true;
            animData.AnimationSpeed = 1;
            cp.Trigger();
            Blockentity.MarkDirty(true);
        }

        public void StopAnimation()
        {
            isActive = false;
            animData.AnimationSpeed = 0;
            cp.Trigger();
            Blockentity.MarkDirty(true);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            isActive = tree.GetBool("isActive");

            if (Api != null && worldAccessForResolve.Side == EnumAppSide.Client)
            {
                animData.AnimationSpeed = isActive ? 1 : 0;
                cp.Trigger();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("isActive", isActive);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (Api is ICoreClientAPI capi)
            {
                if (capi.Settings.Bool["extendedDebugInfo"] == true)
                {
                    dsc.AppendLine("animspeed: " + animData.AnimationSpeed);
                }
            }
        }
    }

}
