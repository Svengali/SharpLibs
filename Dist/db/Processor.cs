using System;
using System.Collections.Generic;
using System.Text;

using Optional.Unsafe;

namespace db
{
	public enum State
	{
		Invalid,
		Prestartup,
		Running,
		Stopped,
	}


	public class Processor<TID, T> where T : IID<TID>
	{


		public DB<TID, T> DB { get; private set; }

		public System<TID, T> Sys { get; private set; }

		public State State { get; private set; } = State.Invalid;


		public Processor( DB<TID, T> db, System<TID, T> sys )
		{
			DB = db;
			Sys= sys;
			State = State.Prestartup;
		}

		public void run()
		{
			State = State.Running;

			while( Sys.Running )
			{
				tick();
			}

			State = State.Stopped;
		}

		public void tick()
		{
			var actOpt = Sys.getNextAct();

			if( !actOpt.HasValue )
			{
				lib.Log.info( $"Out of acts" );
				return;
			}

			var act = actOpt.ValueOrDefault();

			//act.
		}













	}














}
