using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Core.Utils
{
    public static class TaskExtensions
    {
		public static T RunSync<T>(this Task<T> task)
		{
			try
			{
				return Task.Run(async () => await task).Result;
			}
			catch (AggregateException ex)
			{
				if (ex.InnerExceptions.Count == 1)
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		public static void RunSync(this Task task)
		{
			try
			{
				Task.Run(async () => await task).Wait();
			}
			catch (AggregateException ex)
			{
				if (ex.InnerExceptions.Count == 1)
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		public static Task<T> Null<T>() where T : class
		{
			return Task.FromResult((T)null);
		}

		public static readonly Task Empty = Task.FromResult(0);
	}
}
