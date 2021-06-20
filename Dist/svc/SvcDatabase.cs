using System;
using System.Diagnostics;
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

			var sw = new Stopwatch();

			sw.Start();

			while( true )
			{
				sw.Restart();

				procMsg_block();

				var snap = m_db.getSnapshot();

				switch( m_dbState )
				{
					case DBState.Running:
						m_sys.tick();


					break;
				}

				var tickTime = sw.Elapsed.TotalMilliseconds;

				while( sw.Elapsed.TotalMilliseconds < 33.3 )
				{
					if( sw.Elapsed.TotalMilliseconds < 25 )
					{
						Thread.Sleep( 5 );
					}
				}

				if( sw.Elapsed.TotalMilliseconds > 45 )
				{
					log.debug( $"Long loop of {sw.Elapsed.TotalMilliseconds}" );
				}

				sw.Stop();
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
			//log.debug( $"{Thread.CurrentThread.Name} Frame Tick!" );
			var act = db.Act.create( frameTick );
			m_sys.next( act );

			m_sys.addTimedActions();
		}


		//internal void processTransaction( ent )


		internal ent.DB   m_db;
		internal ent.Sys  m_sys;
		internal DBState  m_dbState;


	}
}