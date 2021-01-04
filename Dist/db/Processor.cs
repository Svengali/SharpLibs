using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Optional.Unsafe;

namespace db
{
	public enum State
	{
		Invalid,
		Prestartup,
		Active,
		Waiting,
		Stopped,
	}


	public class Processor<TID, T> where T : IID<TID>
	{


		public DB<TID, T> DB { get; private set; }

		public System<TID, T> Sys { get; private set; }

		public State State => m_state;

		public SemaphoreSlim Semaphore { get; private set; } = new SemaphoreSlim( 1 );
		public int Processed => m_processed;

		public Act DebugCurrentAct => m_debugCurrentAct;

		public Processor( DB<TID, T> db, System<TID, T> sys )
		{
			DB = db;
			Sys= sys;
			m_state = State.Prestartup;
		}

		public void run()
		{
			m_state = State.Active;


			while( Sys.Running )
			{
				tick();
			}

			m_state = State.Stopped;
		}

		public void tick()
		{
			var actOpt = Sys.getNextAct();

			if( !actOpt.HasValue )
			{
				//lib.Log.trace( $"{Thread.CurrentThread.Name} Processed {m_processed} acts" );

				m_state = State.Waiting;
				Semaphore.Wait();

				m_state = State.Active;

				m_processed = 0;

				return;
			}

			var act = actOpt.ValueOrDefault();

			m_debugCurrentAct = act;

			// @@@ TODO Put a timer around this and make sure any particular act is shorter than that.  Probably 1ms and 5ms.  

			act.Fn();

			++m_processed;

		}

		public void kick()
		{
			Semaphore.Release();
		}

		volatile State m_state;
		int m_processed = 0;
		//volatile string ProcessingDebug = "";

		Act m_debugCurrentAct = null;








	}














}
