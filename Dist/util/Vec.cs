using System;
using System.Collections.Generic;
using System.Text;

using VecOps = MathSharp.Vector;
using Vec4 = System.Runtime.Intrinsics.Vector128<float>;
using MathSharp;
using System.Numerics;

namespace util
{
	static class Vec
	{

		public static Vec4 create( float x, float y, float z, float w )
		{
			var vec = new Vector4(
				x,
				y,
				z,
				z)
				.Load();

			return vec;
		}

		public static Vec4 create( double x, double y, double z, double w )
		{
			return create( (float)x, (float)y, (float)z, (float)w );
		}

		public static Vec4 randInBox( Vec4 min, Vec4 max )
		{
			var extent = VecOps.Subtract( max, min );

			var randMult = create( s_rand.NextDouble(), s_rand.NextDouble(), s_rand.NextDouble(), s_rand.NextDouble() );

			var normalizedPt = VecOps.Multiply( extent, randMult );

			var shiftedPt = VecOps.Add( min, normalizedPt );

			return shiftedPt;
		}

		static Random s_rand = new Random();



	}
}
