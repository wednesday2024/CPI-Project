#if UNITY_WEBGL
#else
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Sfs2X.WebSocketSharp
{
	public class LogData
	{
		private StackFrame _caller;

		private DateTime _date;

		private LogLevel _level;

		private string _message;

		public StackFrame Caller => _caller;

		public DateTime Date => _date;

		public LogLevel Level => _level;

		public string Message => _message;

		internal LogData(LogLevel level, StackFrame caller, string message)
		{
			_level = level;
			_caller = caller;
			_message = message ?? string.Empty;
			_date = DateTime.Now;
		}

		public override string ToString()
		{
			string text = $"[{_date}]";
			string text2 = $"{_level.ToString().ToUpper(),-5}";
			MethodBase method = _caller.GetMethod();
			Type declaringType = method.DeclaringType;
			string text3 = $"{declaringType.Name}.{method.Name}";
			string[] array = _message.Replace("\r\n", "\n").TrimEnd(new char[1] { '\n' }).Split(new char[1] { '\n' });
			if (array.Length <= 1)
			{
				return $"{text} {text2} {text3} {_message}";
			}
			StringBuilder stringBuilder = new StringBuilder(64);
			stringBuilder.AppendFormat("{0} {1} {2}\n\n", text, text2, text3);
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.AppendFormat("  {0}\n", array[i]);
			}
			return stringBuilder.ToString();
		}
	}
}
#endif