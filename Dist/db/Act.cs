using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace db
{
	public class Act
	{
		public Action Fn => m_act;

		public string Path { get; private set; } = "";
		public int    Line { get; private set; } = -1;
		public string Member { get; private set; } = "";

		private Act( Action act, string path = "", int line = -1, string member = "" )
		{
			m_act = act;

			Path = path;
			Line = line;
			Member = member;

			//ExtractValue( act );
		}

		static public Act create( Action act, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			return new Act( act );
		}

		public static Act create<T>( Action<T> act, T p0, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			//return new Act( act );

			return new Act( () => { act( p0 ); } );
		}


		public static void ExtractValue( Delegate lambda )
		{
			var lambdaType = lambda.GetType();

			var methodType = lambda.Method.GetType();

			//Nothing here.
			//var locals = lambda.Method.GetMethodBody().LocalVariables;

			var targetType = lambda.Target?.GetType();

			var fields = lambda.Method.DeclaringType?.GetFields
								(
										BindingFlags.NonPublic |
										BindingFlags.Instance |
										BindingFlags.Public |
										BindingFlags.Static
								);
								//.SingleOrDefault(x => x.Name == variableName);

			//return (TValue)field.GetValue( lambda.Target );
		}




		Action m_act;

	}
}
