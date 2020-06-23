using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

public class JustRandomTeleport : ModSystem
{
	EntityPlayer GEntity;
	int count = 0;
	long LID;
	ICoreServerAPI myAPI;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api); //Register the server api to "api"
		IPermissionManager ipm = api.Permissions;
		ipm.RegisterPrivilege("rtp","Random Teleport");

		api.RegisterCommand("rtp", "Teleports the player to spawn","",
			(IServerPlayer player, int groupId, CmdArgs args) =>
			{
				EntityPlayer byEntity = player.Entity; //Get the player
				GEntity = player.Entity;
				IWorldManagerAPI world = api.WorldManager;
				if (count == 0) 
				{
					player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Teleporting to a random location", Vintagestory.API.Common.EnumChatType.Notification);
					var randx = byEntity.World.Rand.Next(400000, 600000);//Using a hard coded 100000x in each direction until I can do the math
					var randz = byEntity.World.Rand.Next(400000, 600000);
					byEntity.Properties.FallDamage = false;
					byEntity.TeleportTo(randx, 199, randz);
					count = 1;
					myAPI = api;
					LID = api.Event.RegisterGameTickListener(OnTick, 15000); // register the tick listener
				}
				else
				{
					player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please wait a short while before trying again.", Vintagestory.API.Common.EnumChatType.Notification);
				}

				
				
			}, BPrivilege.rtp);
	}

	private void OnTick(float dt)
	{
		if (count >= 5)
		{
			GEntity.Properties.FallDamage = true;
			count = 0;
			ICoreServerAPI api = myAPI;
			myAPI.Event.UnregisterGameTickListener(LID);
		}
		else
		{
			count = count + 1;
		}
		Console.WriteLine(count);
	}

	public class BPrivilege : Privilege
    {
		/// <summary>
		/// Ability to use /rtp
		/// </summary>
		public static string rtp = "rtp";
	}

	
}