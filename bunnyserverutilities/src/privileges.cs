using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace privileges.src
{
    //Privilege Classes to hold the permission code for each set of commands
    public class APrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /grtp
        /// </summary>
        public static string grtp = "grtp";
    }
    public class BPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /spawn
        /// </summary>
        public static string spawn = "spawn";
    }
    public class CPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /home
        /// </summary>

        public static string home = "home";
    }
    public class DPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /back
        /// </summary>
        public static string back = "back";
    }

    public class EPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use Just Private Message commands
        /// </summary>
        public static string jpm = "jpm";
        public static string jpmadmin = "jpmadmin";
    }

    public class FPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use Simple Server Message commands
        /// </summary>
        public static string ssm = "ssm";
    }

    public class GPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /tpt
        /// </summary>
        public static string tpt = "tpt";
    }
    public class HPrivilege : Privilege
    {
        /// <summary>
        /// Ability to use /rtp
        /// </summary>
        public static string rtp = "rtp";
    }
}
