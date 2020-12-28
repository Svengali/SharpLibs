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
			m_pendingService.Enqueue( svc );
		}


		/*
		public msg.AddressAll<TSource, TMsg> AddressAll()
		{
			return new AddressAll<TSource, TMsg>();
		}
		*/

		public void send_fromService( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, fn, false );
			m_q.Enqueue( ctx );
			m_wait.Set();
		}

		public Task<msg.Answer<TSource, TMsg>[]> ask_fromService( RTAddress from, TMsg msg, Func<TSource, bool> fn )
		{
			var ctx = new msg.MsgContext<TSource, TMsg>( from, msg, fn, true );
			m_q.Enqueue( ctx );
			m_wait.Set();

			// @@@@ PORT ASK
			return null;
		}

		/*
		public void send_fromService( TMsg msg )
		{
			/*
			var c = new msg.MsgContext<TSource, TMsg>();
			c.msg = msg;
			m_q.Enqueue( c );
			m_wait.Set();
			* /

			procMsg( msg );
		}

		public Task<msg.Answer<TSource, TMsg>[]> ask_fromService( TMsg m )
		{

			var ctx = msg.MsgContext<TSource, TMsg>.ask( m );
			m_q.Enqueue( ctx );
			m_wait.Set();



			var answer = new Func<msg.Answer<TSource, TMsg>[]>(() =>
			{
				//var time = new lib.Timer();
				//time.Start();

				ctx.wait.WaitOne();
				Task<msg.Answer<TSource, TMsg>>[] tasks = ctx.task.ToArray();
				Task.WaitAll(tasks);

				var list = new List<msg.Answer<TSource, TMsg>>();
				for(uint i = 0; i < tasks.Length; ++i)
				{
					if(tasks[ i ].Result.source != null)
					{
						list.Add(tasks[ i ].Result);
					}
				}

				//Answer<TSource, TMsg>[] arr = new Answer<TSource, TMsg>[ tasks.Length ];
				//for( uint i = 0; i < tasks.Length; ++i )
				//{
				//	arr[i] = tasks[i].Result;
				//}

				//time.Stop();
				//lib.Log.info( $"{time.DurationMS} to task ask_fromService" );

				return list.ToArray();
			});

			var task = new Task<msg.Answer<TSource, TMsg>[]>(answer);
			task.Start();

			return task;
		}
		*/

		public void processMessagesBlock( int maxMS )
		{
			processPendingServices();
			processMessages();
			var early = m_wait.WaitOne(maxMS);
		}

		public void processMessages()
		{
			if( m_floatingMax < m_q.Count )
			{
				lib.Log.warn( $"TSource Q hit highwater of {m_q.Count} in {GetType()}." );
				m_floatingMax = (uint)m_q.Count;
			}

			while( m_q.Count > 0 )
			{
				msg.MsgContext<TSource, TMsg> ctx;
				m_q.TryDequeue( out ctx );

				Debug.Assert( ctx.Msg != null );

				foreach( var pair in m_services )
				{
					if( ctx.Fn(pair.Value) )
					{
						pair.Value.deliver( ctx.From, ctx );
					}
				}


					/*
					if( ctx.msg != null )
					{
						if( ctx.wait == null )
						{
							procMsg( ctx.msg );
						}
						else
						{
							foreach( var p in m_services )
							{
								if( ctx.msg.address.pass( p.Value ) )
								{
									var t = p.Value.deliverAsk(ctx.msg);
									if( t != null )
										ctx.task.Add( t );
								}
							}

							var tf = ctx.msg.address.deliverAsk( ctx.msg );
							if( tf != null )
								ctx.task.Add( tf );

							ctx.wait.Set();
							//c.response = c.task.Result;

						}
					}

					*/
				}
			}

		private void processPendingServices()
		{
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



		ImmutableDictionary<SourceId, TSource> m_services = ImmutableDictionary<svc.SourceId, TSource>.Empty;


		private ConcurrentQueue<msg.MsgContext<TSource, TMsg>> m_q = new ConcurrentQueue<msg.MsgContext<TSource, TMsg>>();
		private ConcurrentQueue<TSource> m_pendingService = new ConcurrentQueue<TSource>();
		private EventWaitHandle m_wait = new EventWaitHandle( true, EventResetMode.AutoReset );

		private uint m_floatingMax = 10000;

		public Ref<MgrCfg> Cfg { get; }

		//private ConcurrentDictionary<
	}
















}
