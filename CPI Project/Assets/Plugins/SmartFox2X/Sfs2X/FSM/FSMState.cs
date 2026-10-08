#if UNITY_WEBGL
using System.Collections.Generic;

namespace Sfs2X.FSM
{
	public class FSMState
	{
		private int stateName;

		private Dictionary<int, int> transitions = new Dictionary<int, int>();

		public void SetStateName(int newStateName)
		{
			stateName = newStateName;
		}

		public int GetStateName()
		{
			return stateName;
		}

		public void AddTransition(int transition, int outputState)
		{
			transitions[transition] = outputState;
		}

		public int ApplyTransition(int transition)
		{
			int result = stateName;
			if (transitions.ContainsKey(transition))
			{
				result = transitions[transition];
			}
			return result;
		}
	}
}
#else
using System.Collections.Generic;

namespace Sfs2X.FSM
{
    public class FSMState
    {
        private int stateName;

        private Dictionary<int, int> transitions = new Dictionary<int, int>();

        public void SetStateName(int newStateName)
        {
            stateName = newStateName;
        }

        public int GetStateName()
        {
            return stateName;
        }

        public void AddTransition(int transition, int outputState)
        {
            transitions[transition] = outputState;
        }

        public int ApplyTransition(int transition)
        {
            int result = stateName;
            if (transitions.ContainsKey(transition))
            {
                result = transitions[transition];
            }
            return result;
        }
    }
}
#endif