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
		public Func<CommitResults> Fn => m_act;


		public string DebugInfo { get; private set; } = "";
		public string Path { get; private set; } = "";
		public int    Line { get; private set; } = -1;
		public string Member { get; private set; } = "";

		private Act( Func<CommitResults> act, string debugInfo = "{unknown_base}", string path = "", int line = -1, string member = "" )
		{
			m_act = act;

			DebugInfo = debugInfo;
			Path = path;
			Line = line;
			Member = member;

			//ExtractValue( act );
		}

		static public Act create( Func<CommitResults> act, string debugInfo = "{unknown}", [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			return new Act( act, debugInfo, path, line, member );
		}

		public static Act create<T>( Func<T, CommitResults> act, T p0, string debugInfo = "{unknown}", [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			//return new Act( act );

			return new Act( () => { return act( p0 ); }, debugInfo, path, line, member );
		}

		// If we're not doing any commit ops we can just use these.
		static public Act create( Action act, string debugInfo = "{unknown}", [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			return new Act( () => { act(); return CommitResults.Perfect; }, debugInfo, path, line, member );
		}

		public static Act create<T>( Action<T> act, T p0, string debugInfo = "{unknown}", [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			//ExtractValue( act );

			//return new Act( act );

			return new Act( () => { act( p0 ); return CommitResults.Perfect; }, debugInfo, path, line, member );
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




		Func<CommitResults> m_act;

	}
}
