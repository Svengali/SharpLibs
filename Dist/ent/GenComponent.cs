using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis.VisualBasic.Syntax;

using Optional;
using Optional.Collections;
using Optional.Unsafe;


using Vec = MathSharp.Vector;
using Vec4 = System.Runtime.Intrinsics.Vector128<float>;



namespace ent
{

	[gen.NetView( new Type[] { } )]
	public partial class Component : net.Obj
	{
		public Type Type => m_type;
		public EntityId Id => m_id;

		protected Type m_type;
		EntityId m_id;
		internal uint m_version;

		public override void DeltaFull( net.Obj old, net.DeltaOps ops )
		{
			base.DeltaFull( old, ops );

			var vCom = old as Component;

			m_id = (EntityId)ops.op( "m_id", (uint)vCom.m_id, (uint)m_id );
		}

	}


	[gen.NetView( new Type[] { } )]
	public partial class Component<T> : Component
	{

		/*
		public Component()
		{
			m_type = typeof(T);
		}
		*/

	}

	[gen.NetView( new Type[] { } )]
	public partial class ComWithCfg<T, TCFG> : Component<T>
	{

	}


	public partial interface IComHealth
	{
		public bool isDead();
	}

	[gen.NetView( new Type[] { } )]
	public partial class ComHealth : ComWithCfg<ComHealth, ComHealthCfg>
	{
		//[gen.History]
		float m_health;
		//readonly ImmutableList<float> m_health_history;

		//[gen.NetView(new [] {typeof(net.View<Gameplay>)})]
		//readonly ImmutableList<float> m_history;


		public override void DeltaFull( net.Obj old, net.DeltaOps ops )
		{
			base.DeltaFull( old, ops );

			var vCom = old as ComHealth;

			m_health = ops.op( "m_health", vCom.m_health, m_health );

		}
	}



	[gen.NetView( new Type[] { } )]
	public partial class ComPhysics : ComWithCfg<ComPhysics, ComPhysicsCfg>
	{
		public Vec4 pos;
		public Vec4 vel;
	}



}

