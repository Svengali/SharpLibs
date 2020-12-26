using System;
using System.Threading;
using System.Runtime.CompilerServices;

//using static net.Views;

//using EntityId = lib.Id<ent.Id>;


//*
static class Ex 
{

		static T Exchange<T>( ref T target, T value ) where T : Enum
		{
			var localV = value;

			var t = Interlocked.Exchange( ref Unsafe.As<T, int>( ref target ), Unsafe.As<T, int>( ref localV ) );

			T foo = Unsafe.As<int, T>( ref t );

			return foo;
		}

		static T Increment<T>( ref T target ) where T : Enum
		{
			var t = Interlocked.Increment( ref Unsafe.As<T, int>( ref target ) );

			T foo = Unsafe.As<int, T>( ref t );

			return foo;
		}

}
// */