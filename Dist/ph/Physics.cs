using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;

using Vec = MathSharp.Vector;
using Vec4 = System.Runtime.Intrinsics.Vector128<float>;


namespace phys
{

public struct Pos
{
	public Vec4 pos;
}

class Physics
{




	public void test()
	{
		Vec4 v1 = Vec4.Zero;
		Vec4 v2 = Vec4.Zero;

		var v3 = Vec.Add( v1, v2 );

		var pos = new Pos { pos = Vector128.Create( 0.0f, 0.0f, 0.0f, 0.0f ) };




	}






}





















}
