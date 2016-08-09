using System;

namespace erecruit.Utils
{
    public static partial class Maybe
	{
        public class NoValueException : Exception
        {
            public NoValueException() : base("The Maybe wrapper contains no value.") { }
        }
	}
}