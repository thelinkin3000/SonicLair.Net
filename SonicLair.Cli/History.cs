using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonicLair.Cli
{
    public class History
    {
        private readonly List<Action> history;

        public History()
        {
            history = new List<Action>();
        }
        
        public void GoBack()
        {
            if (history.Count < 2)
            {
                return;
            }
            // Remove the one I'm in
            history.Remove(history.Last());
            // Go back
            history.Last()();
        }

        public void Push(Action action)
        {
            history.Add(action);
        }
    }
}
