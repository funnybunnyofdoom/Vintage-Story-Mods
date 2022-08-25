
using Vintagestory.API.Common;

//Thanks to Craluminum for helping me to format my code mods like this

namespace extrajuice.Load
{
    class extrajuice : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("started 'Extra Juice' mod");
        }
    }
}
