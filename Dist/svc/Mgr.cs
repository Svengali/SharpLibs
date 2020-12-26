using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Imm = System.Collections.Immutable.ImmutableInterlocked;

using msg;

namespace svc
{
	public class Mgr<TSource, TMsg>
		where TSource : class, ISource<TSource, TMsg>
		where TMsg : msg.IMsg<TSource, TMsg>
	{

		public msg.AddressAll<TSource, TMsg> AddressAll()
		{
			return new AddressAll<TSource, TMsg>();
		}



		public void start( TSource svc )
		{
			m_pendingService.Enqueue( svc );
		}

		public void send_fromService( TMsg msg )
		{
			/*
			var c = new msg.MsgContext<TSource, TMsg>();
			c.msg = msg;
			m_q.Enqueue( c );
			m_wait.Set();
			*/

			procMsg( msg );
		}

		public Task<msg.Answer<TSource, TMsg>[]> ask_fromService( TMsg m )
		{

			var c = msg.MsgContext<TSource, TMsg>.ask( m );
			m_q.Enqueue( c );
			m_wait.Set();



			var a = new Func<msg.Answer<TSource, TMsg>[]>(() =>
			{
				//var time = new lib.Timer();
				//time.Start();

				c.wait.WaitOne();
				Task<msg.Answer<TSource, TMsg>>[] tasks = c.task.ToArray();
				Task.WaitAll(tasks);

				var list = new List<msg.Answer<TSource, TMsg>>();
				for(uint i = 0; i < tasks.Length; ++i)
				{
					if(tasks[ i ].Result.source != null)
					{
						list.Add(tasks[ i ].Result);
					}
				}

				/*
				Answer<TSource, TMsg>[] arr = new Answer<TSource, TMsg>[ tasks.Length ];
				for( uint i = 0; i < tasks.Length; ++i )
				{
					arr[i] = tasks[i].Result;
				}
				*/

				//time.Stop();
				//lib.Log.info( $"{time.DurationMS} to task ask_fromService" );

				return list.ToArray();
			});

			var t = new Task<msg.Answer<TSource, TMsg>[]>(a, TaskCreationOptions.LongRunning);
			t.Start();

			return t;
		}

		public void procMsg_block( int maxMS )
		{
			processMessages();
			var early = m_wait.WaitOne(maxMS);
		}


		public void procMsg( TMsg msg )
		{

			var services = m_services;

			foreach( var p in services )
			{
				if( msg.address.pass( p.Value ) )
				{
					p.Value.deliver( msg );
				}
			}

			msg.address.deliver( msg );
		}

		public void processMessages()
		{
			if( m_qMax < m_q.Count )
			{
				lib.Log.warn( $"TSource Q hit highwater of {m_q.Count} in {GetType()}." );
				m_qMax = (uint)m_q.Count;
			}

			while( m_q.Count > 0 )
			{
				msg.MsgContext<TSource, TMsg> c;
				m_q.TryDequeue( out c );

				if( c.m != null )
				{
					if( c.wait == null )
					{
						procMsg( c.m );
					}
					else
					{
						foreach( var p in m_services )
						{
							if( c.m.address.pass( p.Value ) )
							{
								var t = p.Value.deliverAsk(c.m);
								if( t != null )
									c.task.Add( t );
							}
						}

						var tf = c.m.address.deliverAsk( c.m );
						if( tf != null )
							c.task.Add( tf );

						c.wait.Set();
						//c.response = c.task.Result;

					}
				}
			}

			while( !m_pendingService.IsEmpty )
			{
				TSource svc = null;

				m_pendingService.TryDequeue( out svc );

				if( svc != null )
				{
					lib.Log.info( $"Starting service {svc}" );

					Imm.AddOrUpdate( ref m_services, svc.id, svc, ( k, v ) => svc );

					if( svc is ISourceRun runner )
					{
						var thread = new Thread(new ThreadStart(runner.run));

						thread.Start();

						lib.Log.info( $"{svc} is starting a thread to run.", "svc" );
					}
					else
					{
						lib.Log.info( $"{svc} is NOT starting a thread to run.", "svc" );
					}
				}
			}
		}

		/*

		public void save( cm.I_Savable obj )
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

		*/

		public object load( string filename )
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

		//private lib.XmlFormatter2 m_formatter = new lib.XmlFormatter2();
		//private Dictionary<lib.Token, TSource> m_services = new Dictionary<lib.Token, TSource>();
		ImmutableDictionary<SourceId, TSource> m_services = ImmutableDictionary<svc.SourceId, TSource>.Empty;


		private ConcurrentQueue<msg.MsgContext<TSource, TMsg>> m_q = new ConcurrentQueue<msg.MsgContext<TSource, TMsg>>();
		private ConcurrentQueue<TSource> m_pendingService = new ConcurrentQueue<TSource>();
		private EventWaitHandle m_wait = new EventWaitHandle( true, EventResetMode.AutoReset );

		private uint m_qMax = 10000;

		//private ConcurrentDictionary<
	}
















}
