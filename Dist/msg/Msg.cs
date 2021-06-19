using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using svc;

namespace msg
{

	#region Core
	public interface IMsg<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{
	}


	public record RMsg()
	{

	}



	public record RMsgTest( [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
	{

	}



	public class Msg : IMsg<svc.Service<Msg>, Msg>
	{
		string FilePath = "";
		string MemberName = "";
		int    Line = -1;

		public Msg( [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
		{
			FilePath = callerFilePath;
			MemberName = callerMemberName;
			Line = callerLineNumber;
		}
	}

	public struct Answer<TSource, TMsg>
	{
		public readonly RTAddress From;
		public readonly TMsg Msg;

		public Answer( RTAddress from, TMsg msg )
		{
			From = from;
			Msg = msg;
		}
	}

	public struct MsgContext<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{
		//using TAnswer = Answer<TSource, TMsg>;

		public RTAddress From;
		public TMsg Msg;

		public Func<TSource, bool> Fn;

		//This allows our asker to wait until the other service has returned an answer
		public EventWaitHandle Wait;
		public Answer<TSource, TMsg> Response;
		//public List<Task<Answer<TSource, TMsg>>> task;

		static public MsgContext<TSource, TMsg> send( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			return new MsgContext<TSource, TMsg>( from, msg, fn, false );
		}

		static public MsgContext<TSource, TMsg> ask( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			return new MsgContext<TSource, TMsg>( from, msg, fn, true );
		}

		public MsgContext( RTAddress from, TMsg msg, Func<TSource, bool> fn, bool isAsk )
		{
			From = from;
			Msg = msg;
			Fn = fn;
			Wait = isAsk ? new EventWaitHandle( false, EventResetMode.AutoReset ) : null;
			Response = default;
			//task = new List<Task<Answer<TSource, TMsg>>>();
		}
	}
	#endregion //Core

	#region Address

	/*
	public class Address<TSource, TMsg> where TSource : class, svc.ISource<TSource, TMsg>
	{
		virtual public bool pass( TSource svc )
		{
			return false;
		}

		virtual public void deliver( TMsg msg ) 
		{
		}

		virtual public Task<msg.Answer<TSource, TMsg>> deliverAsk( TMsg msg )
		{
			return null;
		}
	}


	public class AddressAll<TSource, TMsg> : Address<TSource, TMsg> where TSource : class, svc.ISource<TSource, TMsg>
	{
		override public bool pass( TSource svc )
		{
			return true;
		}
	}


	public class AddressType<T, TSource, TMsg> : Address<TSource, TMsg> where TSource : class, svc.ISource<TSource, TMsg>
	{
		override public bool pass( TSource svc )
		{
			return svc.GetType().Equals( typeof( T ) ) || svc.GetType().IsSubclassOf( typeof( T ) );
		}
	}

	public class AddressService<TMsg> : Address<svc.Service<TMsg>, TMsg>
		where TMsg : msg.IMsg<Service<TMsg>, TMsg>    
	{
		svc.SourceId id;
		public AddressService( svc.SourceId _id )
		{
			id = _id;
		}

		override public bool pass( svc.Service<TMsg> svc )
		{
			return svc.id == id;
		}
	}



	public class AddressRoundRobin<TSource, TMsg> : Address<TSource, TMsg> where TSource : class, svc.ISource<TSource, TMsg>
	{
		static long s_count = 0;

		public AddressRoundRobin( Address<TSource, TMsg> address )
		{
			m_address = address;
		}

		override public bool pass( TSource svc )
		{
			bool passes = m_address.pass( svc );

			if( passes )
				m_svcs.Add( svc );

			return false;
		}


		override public void deliver( TMsg msg )
		{
			if( m_svcs.Count > 0 )
			{
				int index = (int)( ++s_count % m_svcs.Count );

				m_svcs[index].deliver( msg );
			}
			else
			{
			}
		}

		override public Task<msg.Answer<TSource, TMsg>> deliverAsk( TMsg msg )
		{
			if( m_svcs.Count > 0 )
			{
				int index = (int)( ++s_count % m_svcs.Count );

				return m_svcs[index].deliverAsk( msg );
			}

			return null;
		}

		Address<TSource, TMsg> m_address;

		private List<TSource> m_svcs = new List<TSource>( 8 );
	}
	*/
	#endregion

	public class DelaySend : Msg
	{
		public RTAddress address;
		public Msg msg;
	}

	public class Ping : Msg
	{
		public svc.RTAddress address;
	}

	public class Startup : Msg
	{
		public Startup( [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0 )
			:
			base( callerFilePath, callerMemberName, callerLineNumber )
		{
		}

		public svc.RTAddress from;
	}

	public class Ready : Msg
	{
		public svc.RTAddress address;
	}

	public class StartService : Msg
	{
		public string type;
		public string configPath;
	}


	public class Server<TSource, TMsg> : msg.Msg, IMsg<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{
		/*
		public svc.Ref<svc.Service> sender { get; private set; }
		public Filter filter { get; private set; }

		public string caller { get; private set; }

		public Server()
		{
			filter = FilterAll.filter;
		}

		public Server( Filter _filter ) { filter = _filter; }

		public void setSender_fromService( svc.Service _sender )
		{
			sender = new svc.Ref<svc.Service>( _sender );
		}

		public void setCaller_fromService( string callerFilePath = "", string callerMemberName = "", int callerLineNumber = 0 )
		{
			caller = String.Format( $"{callerFilePath}: {callerLineNumber}: in {callerMemberName}" );
		}

		private void setCaller()
		{

		}
		*/
		//Address<TSource, TMsg> IMsg<TSource, TMsg>.address { get; }
	}









}
