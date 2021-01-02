using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using msg;

namespace svc
{


	public enum SourceId : ulong
	{
		Invalid	= 0,
		Local		= 1,
		Start		= 1024,
	}

	//Runtime service addresses.  Dont bother storing these.
	public struct RTAddress : IEquatable<RTAddress>
	{
		public readonly SourceId Mgr;
		public readonly SourceId Source;

		public RTAddress( SourceId mgr, SourceId source )
		{
			Mgr = mgr;
			Source = source;
		}

		public override bool Equals( object obj )
		{
			return obj is RTAddress address && Equals( address );
		}

		public bool Equals( RTAddress other )
		{
			return Mgr == other.Mgr &&
							 Source == other.Source;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( Mgr, Source );
		}

		public override string ToString()
		{
			return $"{(uint)Mgr & 0xffff:X4}:{(uint)Source & 0xffff:X4}";
		}

		public static bool operator ==( RTAddress left, RTAddress right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( RTAddress left, RTAddress right )
		{
			return !( left == right );
		}
	}



	public interface ISource<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{

		SourceId id { get; }

		void deliver( RTAddress from, msg.MsgContext<TSource, TMsg> ctx );

		Task<msg.Answer<TSource, TMsg>> deliverAsk( RTAddress from, msg.MsgContext<TSource, TMsg> ctx );
	}

	public interface ISourceRun
	{
		void run();
	}



	public partial class Base<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{
		public static Mgr<TSource, TMsg> s_mgr = null;

		public bool QueueHasMessages { get { return !m_q.IsEmpty; } }

		public static void setMgr( Mgr<TSource, TMsg> mgr )
		{
			s_mgr = mgr;
		}


		// Single threaded.  Non-reentrent
		void procMsg( int maxCount )
		{
			var args = new Type[ 1 ];
			var thisType = GetType();

			/*
			if( m_qMax < m_q.Count )
			{
				lib.Log.warn( $"Service Q hit highwater of {m_q.Count} in {GetType()}." );
				m_qMax = (uint)m_q.Count;
			}
			*/

			//maxCount = Math.Min( maxCount, m_q.Count );

			while( !m_q.IsEmpty )
			{
				msg.MsgContext<TSource, TMsg> ctx;
				
				var gotOne = m_q.TryDequeue( out ctx );

				if( gotOne )
				{
					if( ctx.Wait == null )
					{
						args[0] = ctx.Msg.GetType();
						Action<TMsg> fn = null;

						if( !m_handlingMethod.TryGetValue( args[0], out fn ) )
						{
							var mi = thisType.GetMethod("handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);

							if( mi != null )
							{
								ParameterExpression pe = Expression.Parameter(typeof(TMsg), "msgIn");

								var exConvert = Expression.Convert(pe, args[ 0 ]);

								var exParams = new Expression[ 1 ];
								exParams[0] = exConvert;

								var exThis = Expression.Constant(this);
								var exCall = Expression.Call(exThis, mi, exParams);

								fn = Expression.Lambda<Action<TMsg>>( exCall, pe ).Compile();

								m_handlingMethod[args[0]] = fn;

								lib.Log.debug( $"{GetType().Name} is handling {args[0].Name}" );
							}
							else
							{
								lib.Log.warn( $"{args[0].Name} is unhandled in {GetType().Name}" );
								m_handlingMethod[args[0]] = unhandled;
								fn = unhandled;
							}

						}

						if( fn != null )
						{
							try
							{
								//mm_params[ 0 ] = c.msg;

								fn( ctx.Msg );

								//mi.Invoke( this, mm_params );
							}
							catch( Exception e )
							{
								lib.Log.error( $"Exception while calling { ctx.Msg.GetType()}.  {e}" );
							}
						}
						else
						{
							unhandled( ctx.Msg );
						}
					}
					else
					{
						args[0] = ctx.Msg.GetType();

						Func<TMsg, object> fn;

						if( !m_handlingAsk.TryGetValue( args[0], out fn ) )
						{
							var mi = thisType.GetMethod("handleAsk", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);

							if( mi != null )
							{
								ParameterExpression pe = Expression.Parameter(typeof(TMsg), "msgIn");

								var exConvert = Expression.Convert(pe, args[ 0 ]);

								var exParams = new Expression[ 1 ];
								exParams[0] = exConvert;

								var exThis = Expression.Constant(this);
								var exCall = Expression.Call(exThis, mi, exParams);

								fn = Expression.Lambda<Func<TMsg, TSource>>( exCall, pe ).Compile();

								m_handlingAsk = m_handlingAsk.Add( args[0], fn );
							}
						}

						if( fn != null )
						{
							try
							{
								object resp = fn( ctx.Msg );

								TSource svc = source();

								ctx.Response = new msg.Answer<TSource, TMsg>( ctx.From, (TMsg)resp );

								ctx.Wait.Set();

								//retEWH(c.wait);
							}
							catch( Exception ex )
							{
								lib.Log.warn( $"Exception while calling {ctx.Msg.GetType()}.  Ex {ex}" );
							}
						}
						else
						{
							unhandled( ctx.Msg );
						}

						//time.Stop();
						//lib.Log.info( $"{time.DurationMS} to handleAsk" );
					}
				}
			}

			Thread.Sleep(0);

			if( m_q.IsEmpty )
			{
				//m_event.Reset();
			}
		}

		virtual public TSource source()
		{
			return (TSource)Convert.ChangeType( this, typeof( TSource ) );
		}

		private void unhandled( TMsg msg )
		{
		}

		public void procMsg_block( int wait )
		{
			procMsg( 1000 );
			//m_event.WaitOne( wait );
		}

		public void procMsg_block()
		{
			procMsg_block( 1000 );
		}

		public void addHandler( Type msgType, Action<TMsg> fn )
		{
			m_handlingMethod[msgType] = fn;
		}


		//This event allows us to have a very light service that mostly sleeps until it gets a message
		protected EventWaitHandle m_event = new EventWaitHandle( false, EventResetMode.ManualReset );

		protected ConcurrentQueue<msg.MsgContext<TSource, TMsg>> m_q  = new ConcurrentQueue<msg.MsgContext<TSource, TMsg>>();
		protected Dictionary<Type, Action<TMsg>> m_handlingMethod     = new Dictionary<Type, Action<TMsg>>();
		//protected Dictionary<Type, Func<TMsg, object>> m_handlingAsk = new Dictionary<Type, Func<TMsg, object>>();

		protected ImmutableDictionary<Type, Func<TMsg, object>> m_handlingAsk = ImmutableDictionary<Type, Func<TMsg, object>>.Empty;

		protected uint m_qMax = 10000;
	}

	// Handlers
	/*
	public partial class Service
	{
		public virtual void handle( svmsg.ServiceReady ready )
		{

		}

		public object handleAsk( svmsg.Ping ping )
		{
			var dt = 0UL; //(ulong)sv.Main.main.clock.ms - ping.time;

			lib.Log.info( $"Got ping {dt}" );

			return ping;
		}

	}
	*/




	public partial class Service<TMsg> : Base<Service<TMsg>, TMsg>, ISource<Service<TMsg>, TMsg>
		where TMsg : msg.IMsg<Service<TMsg>, TMsg>
	{

		//public lib.Token id { get; private set; }

		public SourceId id { get; private set; }
		//SourceId ISource<Service<TMsg>, TMsg>.id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		//public res.Ref<TSource> sref { get { return new res.Ref<TSource>( "nothing", this ); } }

		//public ImmutableList<Type> Services { get; private set; }


		public Service()
		{
			//id = _id;

			Span<byte> bytes = stackalloc byte[8];

			m_rand.NextBytes(bytes);

			id = (SourceId)BitConverter.ToUInt64( bytes );

			// @@@@ PORT
			//gatherServices();
		}


		public override Service<TMsg> source()
		{
			return (Service<TMsg>)Convert.ChangeType( this, typeof( Service<TMsg> ) );
		}

		/* @@@@ PORT 
		void gatherServices()
		{
			var iserviceType = typeof( IService );

			var allInterfaces = GetType().GetInterfaces();

			var bldServices = ImmutableList<Type>.Empty.ToBuilder();

			foreach( var iface in allInterfaces )
			{
				if( iface == iserviceType )
					continue;

				if( iserviceType.IsAssignableFrom( iface ) )
				{
					bldServices.Add( iface );
				}
			}

			Services = bldServices.ToImmutable();
		}

		public void sendTo( TMsg msg, Service<TMsg> sref, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
		{
			// @@@@ PORT fix this one, and the next few send functions.
			//msg.setSender_fromService( this );
			//msg.setCaller_fromService( callerFilePath, callerMemberName, callerLineNumber );
			sref.deliver( msg );
			//deliver( msg );

		}
		*/

		public void send( TMsg msg, RTAddress to, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
		{
			//msg.setSender_fromService( this );
			//msg.setCaller_fromService( callerFilePath, callerMemberName, callerLineNumber );
			s_mgr.send( new RTAddress( s_mgr.Id, id ), to, msg );
		}

		public void send( TMsg msg, Func<Service<TMsg>, bool> fn, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
		{
			//msg.setSender_fromService( this );
			//msg.setCaller_fromService( callerFilePath, callerMemberName, callerLineNumber );
			s_mgr.send( new RTAddress(s_mgr.Id, id), msg, fn );
		}

		public Task<msg.Answer<Service<TMsg>, TMsg>[]> ask( TMsg msg, Func<Service<TMsg>, bool> fn, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
		{
			//msg.setSender_fromService( this );
			//msg.setCaller_fromService( callerFilePath, callerMemberName, callerLineNumber );
			return s_mgr.ask( new RTAddress( s_mgr.Id, id ), msg, fn );
		}


		public void deliver( RTAddress from, msg.MsgContext<Service<TMsg>, TMsg> ctx )
		{
			m_q.Enqueue( ctx );
			//m_event.Set();
		}

		public Task<Answer<Service<TMsg>, TMsg>> deliverAsk( RTAddress from, msg.MsgContext<Service<TMsg>, TMsg> ctx )
		{
			throw new NotImplementedException();
		}

		/*
		public Task<msg.Answer<Service<TMsg>, TMsg>> deliverAsk( TMsg msg )
		{
			msg.MsgContext<Service<TMsg>, TMsg> ctx = global::msg.MsgContext<Service<TMsg>, TMsg>.ask( msg );
			m_q.Enqueue( ctx );


			var answer = new Func<msg.Answer<Service<TMsg>, TMsg>>(() =>
			{
				ctx.wait.WaitOne();
				return ctx.response;
			});

			var t = new Task<msg.Answer<Service<TMsg>, TMsg>>(answer);
			t.Start();

			return t;
		}

		void ISource<Service<TMsg>, TMsg>.deliver( TMsg msg )
		{
			throw new NotImplementedException();
		}

		Task<Answer<Service<TMsg>, TMsg>> ISource<Service<TMsg>, TMsg>.deliverAsk( TMsg msg )
		{
			throw new NotImplementedException();
		}
		*/


		//delegate void fnHandleGeneric<T>( TMsg msg, Action<T> fn ) where T : class;

		/*
		void handleGeneric<T>( TMsg msg, Action<T> fn ) where T : class
		{
			fn(msg as T);
		}
		*/


		//internal StRunning Running => new StRunning();

		Random m_rand = new Random();




	}

	public class ServiceCfg : lib.Config
	{
		public readonly float testPingSec = 0.0f;
	}


	public class ServiceWithConfig<TCfg> : Service<msg.Msg> where TCfg : ServiceCfg
	{
		public res.Ref<TCfg> cfg { get; protected set; }

		public ServiceWithConfig( res.Ref<TCfg> _cfg )
			:
			base()
		{
			cfg = _cfg;
		}

		DateTime m_startPingTest = DateTime.Now;
		DateTime m_lastLoggedPing = DateTime.Now;
		uint m_pingsRecvd = 0;

		void handle( msg.Ping ping )
		{
			++m_pingsRecvd;

			var ts = DateTime.Now - m_lastLoggedPing;

			if( ts.TotalSeconds > 1.0 )
			{
				lib.Log.debug( $"{(uint)id & 0xffff:X4} got {m_pingsRecvd} Pings in {ts.TotalSeconds} seconds" );

				m_lastLoggedPing = DateTime.Now;
				m_pingsRecvd = 0;

			}

			//lib.Log.debug( $"{(uint)id & 0xffff:X4}  got Ping from {ping.address}" );

			var address = new RTAddress( s_mgr.Id, id );

			/*
			if( address != ping.address && !m_otherServices.Contains(ping.address) )
			{
				lib.Log.debug( $"{(uint)id & 0xffff:X4}  PING adding service {ping.address}" );
				m_otherServices = m_otherServices.Add( ping.address );
			}
			*/

			var tsFullTest = DateTime.Now - m_startPingTest;

			if( tsFullTest.TotalSeconds < cfg.res.testPingSec )
			{
				sendPing( address );
			}
			else
			{
				lib.Log.info( $"Finished doing Ping tests." );
			}
		}


		virtual internal void handle( msg.Startup startup )
		{
			lib.Log.debug( $"{(uint)id & 0xffff:X4} got Startup from" );

			var address = new RTAddress( s_mgr.Id, id );
			var ready = new msg.Ready{ address = address };

			s_mgr.broadcast( address, ready );
		}


		public void handle( msg.DelaySend msg )
		{

		}


		internal void handle( msg.Ready ready )
		{
			lib.Log.debug( $"{(uint)id & 0xffff:X4}  got Ready from {ready.address}" );

			var address = new RTAddress( s_mgr.Id, id );

			if( address != ready.address && !m_otherServices.Contains( ready.address ) )
			{
				lib.Log.debug( $"{(uint)id & 0xffff:X4} READY adding service {ready.address}" );
				m_otherServices = m_otherServices.Add( ready.address );

				var readyBack = new msg.Ready{ address = address };

				var whichService = ready.address;

				send( readyBack, whichService );

				//sendPing( address );

			}
		}

		private void sendPing( RTAddress address )
		{
			var ping = new msg.Ping{ address = address };

			var whichService = m_rand.Next( m_otherServices.Count );

			s_mgr.send( address, m_otherServices[whichService], ping );
		}

		Random m_rand = new Random();
		ImmutableList<svc.RTAddress> m_otherServices = ImmutableList<svc.RTAddress>.Empty;

	}
















}
