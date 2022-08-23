using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using chronojigs.Core;
using Vintagestory.API.Server;

namespace chronojigs.altars
{
    class energyaltarentity : BlockEntity
    {
        ICoreServerAPI sapi;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            chronojigs.Core.chronojigs coreClass = new Core.chronojigs();
            coreClass.StartServerSide(sapi);
        }



    }


}
