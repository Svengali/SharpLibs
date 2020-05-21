using System;
using CodeGeneration.Roslyn;
using System.Diagnostics;

namespace gen
{



	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true, Inherited = true )]
	public class NetVersionAttribute : Attribute
	{
	}



	///[Conditional("CodeGeneration")]


	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = true )]
	[CodeGenerationAttribute( typeof( NetViewGen ) )]
	[Conditional( "CodeGeneration" )]
	public class NetViewAttribute : NetVersionAttribute
	{
		public Type[] view { get; set; }

		public NetViewAttribute( Type[] views )
		{
			view = views;
		}


		/*
		public net.Views PrimaryView { get; private set; }
		//public (net.Views from, net.Views to)[] Distribution { get; private set; }

		public NetViewAttribute( net.Views primaryView )
		{
			PrimaryView = primaryView;

			/*
			var halfLength = distribution.Length / 2;

			Distribution = new (net.Views from, net.Views to)[halfLength];

			for( int i = 0; i < halfLength; ++i )
			{
				Distribution[i] = (distribution[i * 2 + 0], distribution[i * 2 + 1]);
			}
			* /
		}
		*/

	}


	/*
	[CodeGenerationAttribute(typeof(NetViewGen))]
	[Conditional("CodeGeneration")]
	*/

	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Field, Inherited = true, AllowMultiple = true )]
	public class NetDistAttribute : Attribute
	{
		public Type from { get; set; }
		public Type to { get; set; }

		public NetDistAttribute( Type p_from, Type p_to )
		{
			from = p_from;
			to = p_to;
		}


		/*
		public net.Views Set { get; private set; }
		public net.Views Get { get; private set; }

		public NetDistAttribute( net.Views set = net.Views.None, net.Views get = net.Views.None )
		{
			Set = set;
			Get = get;
		}
		*/

	}



}
