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

	public class DBEntity : db.DB<ent.EntityId, ent.Entity>
	{
	}


	public class SysEntity : db.System<ent.EntityId, ent.Entity>
	{
		public SysEntity( res.Ref<db.SystemCfg> cfg, DBEntity db ) 
			: 
			base( cfg, db )
		{
		}
	}


	public class SvcRulesCfg : ServiceCfg
	{
		public readonly res.Ref<db.SystemCfg> SystemCfg = new res.Ref<db.SystemCfg>();
	}

	public class SvcRules : ServiceWithConfig<SvcRulesCfg>, svc.ISourceRun
	{
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
			while( true )
			{
				procMsg_block();

				switch( m_state )
				{
					case State.Running:
						m_sys.tick();
					break;
				}

				Thread.Sleep( 33 );

			}
		}

		override internal void handle( msg.Startup startup )
		{
			base.handle( startup );



			m_db = new DBEntity();

			m_sys= new SysEntity( cfg.res.SystemCfg, m_db );

			m_state = State.Running;

			m_sys.start();

			var act = new db.Act( tick );

			m_sys.next( act );

		}

		void tick()
		{
			lib.Log.debug( $"{Thread.CurrentThread.Name} Tick!" );
			var act = new db.Act( tick );
			m_sys.next( act );
		}




		public DBEntity		m_db;
		public SysEntity	m_sys;
		public State			m_state;

	}


}
