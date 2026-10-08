#if UNITY_WEBGL
#else
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Sfs2X.Util
{
	public static class ExtensionMethods
	{
		public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			asyncOp.completed += (AsyncOperation obj) =>
			{
				tcs.SetResult(null);
			};
			return ((Task)tcs.Task).GetAwaiter();
		}
	}
}
#endif