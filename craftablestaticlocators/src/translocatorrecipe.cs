using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace craftablestaticlocators.src
{
    class translocatorrecipe : ModSystem
    {
        ICoreServerAPI myAPI;
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            myAPI = api;
        }

    }
}
