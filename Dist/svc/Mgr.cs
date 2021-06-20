using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Imm = System.Collections.Immutable.ImmutableInterlocked;

using msg;
using res;
using System.Diagnostics;

namespace svc
{

	public struct MachineInfo
	{
		public readonly string ip;
		public readonly ushort port;

	}

	public class MgrCfg : lib.Config
	{
		public readonly ImmutableArray<MachineInfo> machines = ImmutableArray<MachineInfo>.Empty;
		public readonly MachineInfo listen;
	}


	public class Mgr<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{

		public Mgr( res.Ref<MgrCfg> cfg, SourceId id )
		{
			Id = id;
			Cfg = cfg;
		}

		public void start( TSource svc )
		{
			if( svc != null )
			{
				log.info( $"Starting service {svc}" );

				Imm.AddOrUpdate( ref m_services, svc.id, svc, ( k, v ) => svc );

				if( svc is ISourceRun runner )
				{
					var thread = new Thread(new ThreadStart(runner.run));
					thread.Name = $"Service Thread";

					thread.Start();

					log.info( $"{svc} is starting a thread to run.", "svc" );
				}
				else
				{
					log.info( $"{svc} is NOT starting a thread to run.", "svc" );
				}

			}
		}

		public void send( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, fn, false );
			m_q.Enqueue( ctx );
			//m_wait.Set();
		}

		public void send( RTAddress from, RTAddress to, TMsg msg )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, (svc) => svc.id == to.Source, false );

			if( m_services.TryGetValue( to.Source, out var svc ) )
			{
				svc.deliver( from, ctx );
			}
			else
			{
				m_q.Enqueue( ctx );
			}

			//m_wait.Set();
		}

		public void broadcast( RTAddress from, TMsg msg )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, (svc) => true, false );

			foreach( var svc in m_services )
			{
				svc.Value.deliver( from, ctx );
			}

			//m_wait.Set();
		}


		public Task<msg.Answer<TSource, TMsg>[]> ask( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, fn, true );
			m_q.Enqueue( ctx );
			//m_wait.Set();

			// @@@@ PORT ASK
			return null;
		}

		public void processMessagesBlock( int maxMS )
		{
			processPendingServices();
			processMessages();
			//var early = m_wait.WaitOne( 1000 );
		}

		public void processMessages()
		{
			/*
			if( m_floatingMax < m_q.Count )
			{
				log.warn( $"TSource Q hit highwater of {m_q.Count} in {GetType()}." );
				m_floatingMax = (uint)m_q.Count;
			}
			*/

			while( !m_q.IsEmpty )
			{
				msg.MsgContext<TSource, TMsg> ctx;
				var gotOne = m_q.TryDequeue( out ctx );

				if( gotOne )
				{
					Debug.Assert( ctx.Msg != null );

					foreach( var pair in m_services )
					{
						if( ctx.Fn( pair.Value ) )
						{
							pair.Value.deliver( ctx.From, ctx );
						}
					}
				}
			}

			//m_wait.Reset();
		}

		private void processPendingServices()
		{
			while( !m_pendingService.IsEmpty )
			{
				TSource svc = null;

				var gotService = m_pendingService.TryDequeue( out svc );

				if( gotService && svc != null )
				{
					log.info( $"Starting service {svc}" );

					Imm.AddOrUpdate( ref m_services, svc.id, svc, ( k, v ) => svc );

					if( svc is ISourceRun runner )
					{
						var thread = new Thread(new ThreadStart(runner.run));
						thread.Name = $"Service Thread";

						thread.Start();

						log.info( $"{svc} is starting a thread to run.", "svc" );
					}
					else
					{
						log.info( $"{svc} is NOT starting a thread to run.", "svc" );
					}

				}
			}
		}


		public void startup()
		{
			for( var i = 0; i < 4; ++i )
			{
				var thread = new Thread( new ThreadStart( run ) );
				thread.Name = $"Mgr {i}";
				thread.Start();

				m_threads = m_threads.Add( thread );

			}
		}

		public void run()
		{
			while( true )
			{
				//Sleeps for a second per tick internally.
				//Can get interrupted if a message comes in during its sleep.
				tick();
			}
		}

		public void tick()
		{
			processMessagesBlock( 1000 );

			//m_backend.Touch("test");

		}













		/*

		public void testSave( cm.I_Savable obj )
		{
			var filename = obj.savename();

			var path = "save/" + filename + ".xml";

			if( File.Exists( path ) )
			{
				var savepath = "save/archive/"+filename+"_"+DateTime.Now.ToBinary()+".xml";

				File.Move( path, savepath );
			}

			var filestream = new FileStream( path, FileMode.CreateNew );

			var formatter = new lib.XmlFormatter2();

			formatter.Serialize( filestream, obj );

			filestream.Close();
		}

		public object testLoad( string filename )
		{
			var path = "save/" + filename + ".xml";

			if( !File.Exists( path ) )
				return false;

			var filestream = new FileStream(path, FileMode.Open);

			var formatter = new lib.XmlFormatter2();

			object obj = formatter.Deserialize(filestream);

			filestream.Close();

			return obj;
		}
		*/


		//private lib.XmlFormatter2 m_formatter = new lib.XmlFormatter2();
		//private Dictionary<lib.Token, TSource> m_services = new Dictionary<lib.Token, TSource>();

		public SourceId Id { get; }

		ImmutableList<Thread> m_threads = ImmutableList<Thread>.Empty;

		ImmutableDictionary<SourceId, TSource> m_services = ImmutableDictionary<svc.SourceId, TSource>.Empty;


		private ConcurrentQueue<msg.MsgContext<TSource, TMsg>> m_q = new ConcurrentQueue<msg.MsgContext<TSource, TMsg>>();
		private ConcurrentQueue<TSource> m_pendingService = new ConcurrentQueue<TSource>();
		private EventWaitHandle m_wait = new EventWaitHandle( true, EventResetMode.AutoReset );


		public Ref<MgrCfg> Cfg { get; }
	}
















}
