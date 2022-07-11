using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace simpleservermessage.src
{
    class simpleservermessage : ModSystem
    {
        ICoreServerAPI myAPI;
        int timevalue;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            myAPI = api;
            IPermissionManager ipm = api.Permissions;
            timevalue = 0;
            myAPI.Event.RegisterGameTickListener(CoolDown, 60000);
        }

        private void CoolDown(float obj)
        {
            System.Diagnostics.Debug.WriteLine("NOTHING");
        }

        public class ssmConfig
        {
            public static ssmConfig Current { get; set; }

            public Dictionary<String, tptinfo> tptDict { get; set; }
            public Dictionary<String, String> waitDict { get; set; }

            public List<String> messages { get; set; }

            public static tptConfig getDefault()
            {
                var config = new tptConfig();
                //BlockPos defPos = new BlockPos(0, 0, 0);
                Dictionary<String, tptinfo> tptdictionary = new Dictionary<string, tptinfo>
                {
                    { "Default",new tptinfo() }
                };
                Dictionary<String, String> waitdictionary = new Dictionary<string, string>
                {
                    { "Default","Default"}
                };
                config.tptDict = tptdictionary;
                config.waitDict = waitdictionary;
                return config;
            }
        }
        public class BPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use Simple Server Message commands
            /// </summary>
            public static string ssm = "ssm";
        }

    }
}
