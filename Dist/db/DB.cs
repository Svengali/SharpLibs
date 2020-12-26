using System;
using System.Collections.Immutable;
using Optional;
using static Optional.OptionExtensions;
using static System.Collections.Immutable.ImmutableInterlocked;

/*
	???? Should we have an explicit transaction class/ID?  
	???? Should we split things into threaded vs action
*/

namespace db
{

	public enum CommitResults
	{
		Invalid,
		Perfect,
		Collisions,
	}

	public interface IID<TS>
	{
		TS id { get; }
	}

	public class DB<TID, T> where T : IID<TID>
	{
		//Current snapshot of the DB.
		ImmutableDictionary<TID, T> m_objs = ImmutableDictionary<TID, T>.Empty;

		//List of committed Ids based on when they were committed.
		ImmutableList<TID> m_committed = ImmutableList<TID>.Empty;

		ImmutableDictionary<TID, T> Objects => m_objs;

		// @@@@ TODO This returns an entity that can be changing.  It should be a lazy instantiated copy
		public Option<T> lookup( TID id )
		{
			if( m_objs.TryGetValue( id, out T obj ) )
			{
				return obj.Some();
			}
			else
			{
				// LOG
			}

			return obj.None();
		}

		public (Tx<TID, T>, Option<T>) checkout( TID id )
		{
			var tx = new Tx<TID, T>( m_committed.Count, m_activeTransaction, this );

			var v = lookup( id );

			v.Match( t => {
				tx.checkout( id );
			}, () => {
			} );

			return (tx, v);
		}

		public Tx<TID, T> checkout( TID id, out Option<T> tOut )
		{
			var (tx, v) = checkout(id);

			tOut = v;

			return tx;
		}

		public Tx<TID, T> checkout()
		{
			var tx = new Tx<TID, T>( m_committed.Count, m_activeTransaction, this );

			return tx;
		}

		public CommitResults commit( ref Tx<TID, T> co )
		{
			co = null;
			return commit_internal_single( co );
		}

		public ImmutableDictionary<TID, T> getSnapshot()
		{
			ImmutableDictionary<TID, T> res = m_objs;
			return res;
		}


		internal CommitResults commit_internal_single( Tx<TID, T> tx )
		{
			//var collision = false;

			//Check for previously committed things 
			var start = tx.Start;

			var curCommitted = m_committed;

			foreach( var t in tx.Checkouts )
			{
				for( int i = start; i < curCommitted.Count; ++i )
				{
					if( !t.id.Equals( curCommitted[i] ) ) { }
					else
					{
						//collision = true;
						return CommitResults.Collisions;
					}
				}
			}

			// @@@@ LOCK 
			lock( m_committed )
			{
				TID[] committed = new TID[tx.Checkouts.Count];

				for( var i = 0; i < tx.Checkouts.Count; ++i )
				{
					committed[i] = tx.Checkouts[i].id;
					m_objs = m_objs.Add( tx.Checkouts[i].id, tx.Checkouts[i] );
				}

				m_committed = m_committed.AddRange(committed);

				foreach( var v in tx.Adds )
				{
					m_objs = m_objs.Add( v.id, v );
				}

				return CommitResults.Perfect;

			}

		}



		Option<Tx<TID, T>> m_activeTransaction = Option.None<Tx<TID, T>>();

	}

	public enum TxStates
	{
		Invalid,
		Running,
		Committed,
	}


	//This only works for a single thread
	public class Tx<TID, T>: IDisposable where T : IID<TID>
	{
		internal ImmutableList<T> Checkouts => m_checkouts;
		internal TxStates State => m_state;
		internal int Start => m_start;
		internal ImmutableList<T> Adds => m_adds;

		internal Tx( int start, DB<TID, T> db )
			:
			this(start, Option.None<Tx<TID, T>>(), db)
		{
		}

		internal Tx( int start, Option<Tx<TID, T>> parentTx, DB<TID, T> db )
		{
			m_start = start;
			m_parentTx = parentTx;
			m_childTx = m_childTx.Add(this);
			m_db = db;
			m_state = TxStates.Running;
		}

		public void Dispose()
		{
			// Dispose of unmanaged resources.
			Dispose( true );
			// Suppress finalization.
			GC.SuppressFinalize( this );
		}

		public void Dispose(bool isFromDispose )
		{
			if( isFromDispose )
			{
				m_db.commit_internal_single( this );
			}
		}

	  public Option<T> checkout( TID id )
		{
			var t = m_db.lookup( id );

			t.MatchSome( t => { m_checkouts = m_checkouts.Add( t ); } );

			return t;
		}

		public void add( T obj )
		{
			m_adds = m_adds.Add(obj);
		}


		int m_start = -1;
		DB<TID, T> m_db;

		//Do we need these?  Do we need both?
		Option<Tx<TID, T>> m_parentTx;
		ImmutableList<Tx<TID, T>> m_childTx = ImmutableList<Tx<TID, T>>.Empty;

		TxStates m_state = TxStates.Invalid;
		ImmutableList<T> m_checkouts = ImmutableList<T>.Empty;

		// New objects created this pass
		ImmutableList<T> m_adds = ImmutableList<T>.Empty;
	}

}
