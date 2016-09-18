using System;

namespace Core.Exceptions
{
	public enum BackendExceptionType
	{
		None = 0,
		ContractPoolEmpty = 1
	}

	public class BackendException : Exception
	{
		public BackendExceptionType Type { get; private set; }

		public BackendException(BackendExceptionType type)
			: this(type, "")
		{
			Type = type;
		}

		public BackendException(BackendExceptionType type, string message)
			: base(message)
		{
			Type = type;
		}
	}
}
