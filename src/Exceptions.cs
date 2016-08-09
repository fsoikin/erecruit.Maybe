using System;

namespace erecruit.Utils
{
	public static partial class Maybe
	{
		public class NoValueException : Exception
		{
			public NoValueException() : base("The Maybe wrapper contains no value.") { }
		}

		public class ComputationErrorException : Exception
		{
			public ComputationErrorException( Error error ) : 
				base( 
					message:
						error?.Exception != null 
						? "An exception was thrown during a Maybe computation."
						: string.Join( ", ", error?.Messages ?? new string[0] ),

					innerException:
						error?.Exception ) 
			{ }
		}
	}
}