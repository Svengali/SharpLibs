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

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace svc
{



	public class SvcRulesCfg : ServiceCfg
	{
		public readonly res.Ref<db.SystemCfg> SystemCfg = new res.Ref<db.SystemCfg>();
	}

	public partial class SvcRules : ServiceWithConfig<SvcRulesCfg>, svc.ISourceRun
	{
		// TODO DUPE code refactor to somewhere shared.
		public enum State
		{
			Invalid,
			Early,
			StartingUp,
			Running,
			ShuttingDown,
		}

		public SvcRules( res.Ref<SvcRulesCfg> _cfg )
			:
			base( _cfg )
		{
			m_state = State.Early;
		}

		public void run()
		{


			var currentTime = DateTime.Now;

			while( true )
			{
				switch( m_state )
				{
					case State.Running:
					m_sys.tick();

					var snap = m_db.getSnapshot();

					break;
				}

				procMsg_block();

				var spentTime = DateTime.Now;

				var delta = spentTime - currentTime;

				var deltaMS = delta.TotalMilliseconds;

				var pause = Math.Max( 0, 33.0 - deltaMS );

				var pauseInt = (int)pause;

				Thread.Sleep( pauseInt );

				if( pauseInt == 0 )
				{
					lib.Log.warn( $"Long frame {delta.TotalMilliseconds}" );
				}

				currentTime = DateTime.Now;

			}
		}



		override internal void handle( msg.Startup startup )
		{
			base.handle( startup );



			m_db = new ent.DB();

			m_sys= new ent.Sys( cfg.res.SystemCfg, m_db );

			m_state = State.Running;

			m_sys.start();


			var timed = db.Act.create( timedTick );
			m_sys.future( timed, 60.0, 0.0 );


			var act = db.Act.create( frameTick );
			m_sys.next( act );

			/*
			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );

			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );

			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );

			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );
			m_sys.next( act );
			//*/
		}



		void frameTick()
		{
			//lib.Log.debug( $"{Thread.CurrentThread.Name} Frame Tick!" );
			var act = db.Act.create( frameTick );
			m_sys.next( act );

			m_sys.addTimedActions();
		}

		DateTime m_lastTick = DateTime.Now;

		void timedTick()
		{
			var ts = DateTime.Now - m_lastTick;
			m_lastTick = DateTime.Now;

			lib.Log.debug( $"{Thread.CurrentThread.Name} Timed Tick! {ts.TotalMilliseconds}" );
			var act = db.Act.create( timedTick );
			m_sys.future( act, 60.0, 0.0 );
		}




		// @@@@ Test for passing in FormattableString s.
		void show( FormattableString format )
		{

		}



		public ent.DB		m_db;
		public ent.Sys	m_sys;
		public State		m_state;

		//public static ImmutableDictionary<EntityId, Entity> m_snapshot = ImmutableDictionary<EntityId, Entity>.Empty;

	}


}
