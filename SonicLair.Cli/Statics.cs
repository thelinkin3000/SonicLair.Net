using SonicLair.Lib.Types.SonicLair;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicLair.Cli
{
    

    public class Statics
    {
        private static StaticsContainer _container { get; set; }

        private static StaticsContainer Get()
        {
            if(_container == null)
            {
                _container = new StaticsContainer();
            }
            return _container;
        }

        public static Account GetActiveAccount()
        {
            return Get().ActiveAccount;
        }
        public static void SetActiveAccount(Account value)
        {
            Get().ActiveAccount = value;
        }

        private class StaticsContainer
        {
            public StaticsContainer()
            {
                ActiveAccount = new Account();
            }
            public Account ActiveAccount { get; set; }
        }

    }
}
