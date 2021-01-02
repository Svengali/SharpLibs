using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Diagnostics;

using Optional;

namespace db
{

	struct TimedAction
	{
		public long when;
		public Act  act;

		public TimedAction( long when, Act act )
		{
			this.when = when;
			this.act = act;
		}
	}

	public class SystemCfg : lib.Config
	{
		public readonly float Cores = 2;
	}

	public class System<TID, T> where T : IID<TID>
	{
		//public static System Current => s_system;

		public SemaphoreSlim ActsExist => m_actsExist;
		public DB<TID, T> DB { get; private set; }

		public bool Running {  get; private set; }

		public System( res.Ref<SystemCfg> cfg, DB<TID, T> db )
		{
			m_cfg = cfg;
			DB = db;

			var procCount = Environment.ProcessorCount;

			//Exact comparison
			if( m_cfg.res.Cores != 0.0f )
			{
				//If its less than 1, then use it as a multiplier
				if( m_cfg.res.Cores < 0.0f )
				{
					procCount = Environment.ProcessorCount - (int)m_cfg.res.Cores;
				}
				else if( m_cfg.res.Cores < 1.0f )
				{
					procCount = (int) ((float)Environment.ProcessorCount * m_cfg.res.Cores);
				}
				else
				{
					procCount = (int)m_cfg.res.Cores;
				}
			}

			lib.Log.info( $"Running {procCount} cores out of a total cores {Environment.ProcessorCount} via a config Cores value of {m_cfg.res.Cores}" );

			Processor<TID, T>[] procs = new Processor<TID, T>[procCount];

			for( var i = 0; i < procCount; ++i )
			{
				var proc = new Processor<TID, T>( db, this );
				
				procs[i] = proc;
			}

			m_processors = m_processors.AddRange( procs );

			Running = true;

		}


		public void forcedThisTick( Act act )
		{
			m_current.Add( act );

			m_actsExist.Release();
		}

		public void next( Act act )
		{
			m_next.Add( act );
		}

		//Most things dont need accurate next frame processing, so split them between the next frame N frames
		const double s_variance = 1.0 / 15.0;

		public void future( Act act, double future, double maxVariance = s_variance )
		{
			//m_actions.Add( act );

			var variance = m_rand.NextDouble() * maxVariance;

			var nextTime = future + variance;

			if( nextTime < 1.0 / 60.0 )
			{
				next( act );
				return;
			}

			var ts = TimeSpan.FromSeconds( nextTime );

			var tsTicks = ts.Ticks;

			// @@@ TIMING Should we use a fixed time at the front of the frame for this?
			var ticks = tsTicks + DateTime.Now.Ticks;

			var ta = new TimedAction( ticks, act );

			m_sortedFutureActions = m_sortedFutureActions.Add( ta );
		}

		public void start()
		{
			int count = 0;
			foreach( var p in m_processors )
			{
				var start = new ThreadStart( p.run );

				var th = new Thread( start );
				th.Name = $"System_{count}";

				th.Start();

				++count;
			}
		}

		public void tick()
		{
			Debug.Assert( m_current.Count == 0 );

			var current = m_current;
			m_current = m_next;
			m_next = current;

			foreach( var proc in m_processors )
			{
				Debug.Assert( proc.State == State.Waiting );

				proc.kick();
			}
		}


		public void stopRunning()
		{
			Running = false;
		}




		internal Option<Act> getNextAct()
		{
			if( m_current.TryTake( out Act res ) )
			{
				return res.Some();
			}

			m_actsExist.WaitAsync();

			return Option.None<Act>();
		}

		res.Ref<SystemCfg> m_cfg;

		SemaphoreSlim m_actsExist = new SemaphoreSlim(0);

		Random m_rand = new Random();

		ConcurrentBag<Act> m_current = new ConcurrentBag<Act>();
		ConcurrentBag<Act> m_next = new ConcurrentBag<Act>();

		ImmutableList<TimedAction> m_sortedFutureActions;


		ImmutableList<Processor<TID, T>> m_processors = ImmutableList<Processor<TID, T>>.Empty;

		//private static System s_system;
	}


}
