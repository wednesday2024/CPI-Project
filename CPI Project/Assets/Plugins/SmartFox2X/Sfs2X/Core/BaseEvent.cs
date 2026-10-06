using System.Collections.Generic;

namespace Sfs2X.Core
{
	public class BaseEvent
	{
		protected Dictionary<string, object> arguments;

		protected string type;

		protected object target;

		public string Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		public IDictionary<string, object> Params
		{
			get
			{
				return arguments;
			}
			set
			{
				arguments = value as Dictionary<string, object>;
			}
		}

		public object Target
		{
			get
			{
				return target;
			}
			set
			{
				target = value;
			}
		}

		public override string ToString()
		{
			return type + " [ " + ((target != null) ? target.ToString() : "null") + "]";
		}

		public BaseEvent Clone()
		{
			return new BaseEvent(type, arguments);
		}

		public BaseEvent(string type)
		{
			Type = type;
			if (arguments == null)
			{
				arguments = new Dictionary<string, object>();
			}
		}

		public BaseEvent(string type, Dictionary<string, object> args)
		{
			Type = type;
			arguments = args;
			if (arguments == null)
			{
				arguments = new Dictionary<string, object>();
			}
		}
	}
}
