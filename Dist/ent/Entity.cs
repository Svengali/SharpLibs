using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Diagnostics.Tracing;
using lib.Net;
using Validation;
using Optional;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ent
{

	public enum EntityId : ulong
	{
		Invalid = 0,
	}


	public partial class Component<T>
	{

	}

	public partial class ComponentWithCfg<T, TCFG> : Component<T>
	{

	}





	public partial class Entity : db.IID<EntityId>
	{
		public Option<U> com<U>() where U : Component
		{
			/*
			var res = m_coms.GetValueOrNone(typeof(U));

			m_coms.Get

			Component resVal = res.ValueOrDefault();

			Option<U> opt = Option.Some<U>((U)resVal);

			return opt;
			*/

			//return U.None<U>();

			return Option.None<U>();
		}



	}


	public partial class ComHealth
	{
		public bool isDead()
		{
			return m_health <= 0.0f;
		}

		//[gen.NetView( new[] { typeof( net.View<Gameplay> ) } )]
		public Entity takeDamage( Entity ent, float amount )
		{
			var newHealth = m_health - amount;
			var com = with( m_healthOpt: newHealth.Some() );

			//var comList = ent.

			//var health = ent.com<ComHealth>();

			return null;
		}
	}


}

