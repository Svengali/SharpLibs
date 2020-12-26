using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace db
{
	public class Act
	{
		public Action Fn => m_act;

		public Act( Action act )
		{
			m_act = act;

			ExtractValue( act );
		}

		static public Act create( Action act )
		{
			//ExtractValue( act );

			return new Act( act );
		}

		public static Act create<T>( Action<T> act, T p0 )
		{
			//ExtractValue( act );

			//return new Act( act );

			return new Act( () => { act( p0 ); } );
		}




		public static void ExtractValue( Delegate lambda )
		{
			var delType = lambda.GetType();

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
