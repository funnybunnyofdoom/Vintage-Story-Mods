
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
            EnumSeason season = Api.World.Calendar.GetSeason(this.Pos); //This can get our season for the overlay
            int dayofmonth = (myapi.World.Calendar.DayOfYear % myapi.World.Calendar.DaysPerMonth) + 1;
            if (this.Block.FirstCodePart(1) != myapi.World.Calendar.MonthName.ToString() || this.Block.FirstCodePart(2).ToInt() != dayofmonth)
            {
                string material = this.Block.FirstCodePart(3);
                string orientation = this.Block.FirstCodePart(5);
                if (orientation == null) //This checks for blocks without the new 4th property
                {
                    material = "linen";
                    if (this.Block.FirstCodePart(4) == null) {
                        orientation = this.Block.FirstCodePart(3);
                    }else if (this.Block.FirstCodePart(5) == null)
                    {
                        orientation = this.Block.FirstCodePart(4);
                    }
                    
                }

                Block block = myapi.World.GetBlock(new AssetLocation("calendar:calendar-" + myapi.World.Calendar.MonthName.ToString() + "-" + dayofmonth + "-" + material + "-" + season.ToString() + "-" + orientation));
                myapi.World.BlockAccessor.SetBlock(block.BlockId, this.Pos);
            }
        }

        public void OnTick(float par)
        {
            
            EnumSeason season = Api.World.Calendar.GetSeason(this.Pos); //This can get our season for the overlay
            int dayofmonth = (myapi.World.Calendar.DayOfYear % myapi.World.Calendar.DaysPerMonth)+1;
            if (this.Block.FirstCodePart(1) != myapi.World.Calendar.MonthName.ToString() || this.Block.FirstCodePart(2).ToInt() != dayofmonth)
            {
                string material = this.Block.FirstCodePart(3);
                
                string orientation = this.Block.FirstCodePart(5);
                if (orientation == null) //This checks for blocks without the new 4th property
                {
                    material = "linen";
                    if (this.Block.FirstCodePart(4) == null)
                    {
                        orientation = this.Block.FirstCodePart(3);
                    }
                    else if (this.Block.FirstCodePart(5) == null)
                    {
                        orientation = this.Block.FirstCodePart(4);
                    }
                }
                
                Block block = myapi.World.GetBlock(new AssetLocation("calendar:calendar-"+ myapi.World.Calendar.MonthName.ToString() +"-"+ dayofmonth +"-"+ material + "-"+ season.ToString() + "-" + orientation));
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
