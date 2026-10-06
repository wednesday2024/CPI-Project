using System;
using System.Collections.Generic;
using Sfs2X.Core;

namespace Sfs2X.Logging
{
	public class LoggerEvent : BaseEvent, ICloneable
	{
		private LogLevel level;

		public LoggerEvent(LogLevel level, Dictionary<string, object> parameters)
			: base(LogEventType(level), parameters)
		{
			this.level = level;
		}

		public static string LogEventType(LogLevel level)
		{
			return "LOG_" + level;
		}

		public override string ToString()
		{
			return string.Format("LoggerEvent " + type);
		}

		public new object Clone()
		{
			return new LoggerEvent(level, arguments);
		}
	}
}
