using System;
using System.Threading;

namespace svc
{







	public class SvcDatabaseCfg : ServiceCfg
	{
		public readonly res.Ref<db.SystemCfg> SystemCfg = new res.Ref<db.SystemCfg>();
	}




	public class SvcDatabase<TCfg> : ServiceWithConfig<TCfg>, svc.ISourceRun
		where TCfg : SvcDatabaseCfg
	{

		public enum DBState
		{
			Invalid,
			Early,
			StartingUp,
			Running,
			ShuttingDown,
		}



		public SvcDatabase( res.Ref<TCfg> _cfg )
			:
			base( _cfg )
		{
			m_dbState = DBState.Early;
		}

		public void run()
		{


			var currentTime = DateTime.Now;

			while( true )
			{
				switch( m_dbState )
				{
					case DBState.Running:
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

			m_sys = new ent.Sys( cfg.res.SystemCfg, m_db );

			m_sys.start();

			m_dbState = DBState.Running;

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


		internal virtual void frameTick()
		{
			//lib.Log.debug( $"{Thread.CurrentThread.Name} Frame Tick!" );
			var act = db.Act.create( frameTick );
			m_sys.next( act );

			m_sys.addTimedActions();
		}



		internal ent.DB   m_db;
		internal ent.Sys  m_sys;
		internal DBState  m_dbState;


	}
}