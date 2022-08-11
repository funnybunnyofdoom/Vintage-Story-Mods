
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace calendar.src
{
    public class Ticking : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BlockCalendar", typeof(CalendarBlockEntity));
        }
    }

    public class CalendarBlockEntity : BlockEntity
    {
        private int timer;
        ICoreAPI myapi;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, 30000);
            myapi = api;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine(Lang.Get("calendar:"+this.Block.FirstCodePart(1)) + " " + this.Block.FirstCodePart(2).ToString());
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            int dayofmonth = (myapi.World.Calendar.DayOfYear % myapi.World.Calendar.DaysPerMonth) + 1;
            if (this.Block.FirstCodePart(1) != myapi.World.Calendar.MonthName.ToString() || this.Block.FirstCodePart(2).ToInt() != dayofmonth)
            {
                string orientation = this.Block.FirstCodePart(3);

                Block block = myapi.World.GetBlock(new AssetLocation("calendar:calendar-" + myapi.World.Calendar.MonthName.ToString() + "-" + dayofmonth + "-" + orientation));
                myapi.World.BlockAccessor.SetBlock(block.BlockId, this.Pos);
            }
        }

        public void OnTick(float par)
        {
            int dayofmonth = (myapi.World.Calendar.DayOfYear % myapi.World.Calendar.DaysPerMonth)+1;
            if (this.Block.FirstCodePart(1) != myapi.World.Calendar.MonthName.ToString() || this.Block.FirstCodePart(2).ToInt() != dayofmonth)
            {
                string orientation = this.Block.FirstCodePart(3);
                
                Block block = myapi.World.GetBlock(new AssetLocation("calendar:calendar-"+ myapi.World.Calendar.MonthName.ToString() +"-"+ dayofmonth +"-"+ orientation));
                myapi.World.BlockAccessor.SetBlock(block.BlockId, this.Pos);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("timer", timer);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            timer = tree.GetInt("timer");
        }
    }
}
