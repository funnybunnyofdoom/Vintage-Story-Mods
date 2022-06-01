using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ExampleMods
{ 
    public class jlj : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
        }  

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            
        }

    }

   

    class wormsjarblock : BlockBehavior
    {
        public static SimpleParticleProperties myParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(220, 220, 220,50), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());


        public wormsjarblock(Block block) : base(block)
        {
            
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            return true;
        }

        

        public override void Initialize(JsonObject properties)
        {
            
            base.Initialize(properties);
            


        }

        
    }
}
