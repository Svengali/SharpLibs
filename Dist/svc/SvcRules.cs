using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Optional;

namespace svc
{



	public class SvcRulesCfg : SvcDatabaseCfg
	{
		public string savePath = "save/";


	}

	public partial class SvcRules : SvcDatabase<SvcRulesCfg>
	{
		// TODO DUPE code refactor to somewhere shared.
		/*
		public enum State
		{
			Invalid,
			Early,
			StartingUp,
			Running,
			ShuttingDown,
		}
		*/

		public SvcRules( res.Ref<SvcRulesCfg> _cfg )
			:
			base( _cfg )
		{
		}

		override internal void handle( msg.Startup startup )
		{
			base.handle( startup );

			timedTick();

			var load = db.Act.create( loadEntities );
			m_sys.next( load );
		}

		DateTime m_lastTick = DateTime.Now;

		void timedTick()
		{
			var ts = DateTime.Now - m_lastTick;
			m_lastTick = DateTime.Now;

			log.debug( $"{Thread.CurrentThread.Name} Rules Timed Tick! {ts.TotalMilliseconds}" );
			var act = db.Act.create( timedTick );
			m_sys.future( act, 10.0, 0.0 );
		}

		internal override void frameTick()
		{
			base.frameTick();
		}

		void createRandomEntity()
		{

			// TODO Generalize spawn system

			{
				var comHealth = ent.ComHealth.create(m_healthOpt: 100.0f.Some());

				var minPos = util.Vec.create( -200, -200, -200, 0 );
				var maxPos = util.Vec.create(  200,  200,  200, 0 );

				var minVel = util.Vec.create( -1, -1, 0, 0 );
				var maxVel = util.Vec.create(  1,  1,  0, 0 );


				var pos = util.Vec.randInBox( minPos, maxPos );
				var vel = util.Vec.randInBox( minVel, maxVel );

				var comPhysics= ent.ComPhysics.create( posOpt: pos.Some(), velOpt: vel.Some() );

				var nComs = ImmutableDictionary<Type, ent.Component>
				.Empty
				.Add( comHealth.GetType(), comHealth )
				.Add( comPhysics.GetType(), comPhysics );

				var ent1 = ent.Entity.create( m_comsOpt: nComs.Some() ); //, m_nzOpt: nz.Some() );

				ent.EntityId newId = ent1.id;

				using var tx = m_db.checkout();

				tx.add( ent1 );
			}


		}

		void loadEntities()
		{

		}




























		// @@@@ Test for passing in FormattableString s.
		void show( FormattableString format )
		{

		}

		//public static ImmutableDictionary<EntityId, Entity> m_snapshot = ImmutableDictionary<EntityId, Entity>.Empty;

	}


}
