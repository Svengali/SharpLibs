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


//using static net.Views;

//using EntityId = lib.Id<ent.Id>;


namespace ent
{

	public enum Server
	{

	}



	public partial interface IEntity
	{
		EntityId id { get; }
		Option<U> com<U>() where U : Component;
	}

	static public class EntityOps
	{
		//static int s_entityId = 1024;
	}

	public class VisitEntity
	{

	}

	public struct ComList : IEquatable<ComList>
	{
		public Type type;
		public Component com;

		public ComList(Component _com)
		{
			type = _com.GetType();
			com = _com;
		}

		public override bool Equals( object obj )
		{
			return obj is ComList list && Equals( list );
		}

		public bool Equals( ComList other )
		{
			return EqualityComparer<Type>.Default.Equals( type, other.type ) &&
							EqualityComparer<Component>.Default.Equals( com, other.com );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( type, com );
		}

		public static bool operator ==( ComList left, ComList right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( ComList left, ComList right )
		{
			return !( left == right );
		}
	}

	[gen.NetView( new Type[] { } )]
	public partial class Entity : net.Obj, IEntity, db.IID<EntityId>
	{

		public EntityId id => m_id;


		/*
		public Entity()
		{
			Interlocked.Increment( ref s_entityId );

			m_id = (EntityId)s_entityId;
			s_entityId++;
		}
		*/




		public override void DeltaFull( net.Obj old, net.DeltaOps ops )
		{
			base.DeltaFull( old, ops );

			var vEntity = old as Entity;

			//m_id = ops.op( "m_id", vEntity.m_id, m_id );
			//m_test01	= ops.op( "m_test01", vEntity.m_test01, m_test01 );
			//m_com			= (Component)ops.op( "m_com", vEntity.m_com, m_com );
			//m_nz			= ops.op( "m_nz", vEntity.m_nz, m_nz );
			//m_coms		= (ImmutableArray<ComList>)ops.op( "m_coms", vEntity.m_coms, m_coms );
		}



		EntityId m_id;

		uint m_version;

		//int m_test01 = 10;

		//Component m_com;

		//net.NearZero m_nz;

		//public ImmutableArray<ComList> m_coms;


		public ImmutableDictionary<Type, ent.Component> m_coms;


	}





}
