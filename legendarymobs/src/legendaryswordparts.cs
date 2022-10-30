using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace legendarymobs.src
{
    class legendarymobs : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            AiTaskRegistry.Register("magicAttack", typeof(AiTaskLegendaryRanged));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            AiTaskRegistry.Register<AiTaskLegendaryRanged>("magicAttack");
        }
    }
    
   
}
