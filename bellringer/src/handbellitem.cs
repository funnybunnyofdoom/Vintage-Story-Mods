using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;

namespace bellringer.src
{
    public class HandBellMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("handbell", typeof(HandBellItem));
            
        }

    }

    public class HandBellItem : Item
    {
        AssetLocation sound = new AssetLocation("game", "sounds/effect/receptionbell");

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            DoRing(byEntity,slot);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            api.World.PlaySoundAt(sound, byEntity, null, false, 32);
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
        }

        public virtual void DoRing(EntityAgent byEntity,ItemSlot slot)
        {
            api.World.PlaySoundAt(sound,byEntity,null,false,32);
            this.DamageItem(byEntity.World, byEntity, slot);
        }
    }
}
