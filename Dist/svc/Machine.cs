using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace svc
{

		//define a class and a struct for a basic 2d particle
		public class CParticle
		{
			public Vector2 position;
			public CParticle( Vector2 pos )
			{ position = pos; }
		}

		public struct SParticle
		{
			public Vector2 position;
			public SParticle( Vector2 pos )
			{ position = pos; }
		}



	public class MachineCfg : lib.Config
	{
		public readonly float testPingSec = 0.0f;
	}

	public class Machine : ServiceWithConfig<MachineCfg>, svc.ISourceRun
	{

#if false
		//define arrays of structs vs classes
		static int size = 10000000;
		svc.SParticle[] structArray = new svc.SParticle[size];
		svc.CParticle[] classArray = new svc.CParticle[size];
		float[] floatArray = new float[size];
		float gravity = 0.9f;

		int frames = 0;
		Stopwatch stopwatch = new Stopwatch();
		long classTime = 0;
		long structTime = 0;
		long floatTime = 0;



		protected void initForTest()
		{
			//initialize arrays to size
			for( int i = 0; i < size; i++ )
			{
				structArray[i] = new svc.SParticle( new Vector2( 100, 100 ) );
				classArray[i] = new svc.CParticle( new Vector2( 100, 100 ) );
				floatArray[i] = 100f;
			}
		}

		protected void timeTest()
		{

			//periodically time gravity being applied to particles
			frames++;

			Thread.Sleep( 1000 );
			lib.Log.debug( "Tick" );
			Thread.Sleep( 1000 );
			lib.Log.debug( "Tock" );
			Thread.Sleep( 1000 );
			lib.Log.debug( "Tick" );

			//wait a bit before timing
			//if( frames == 60 * 2 )
			{
				frames = 0;

				stopwatch.Restart();
				//loop class array, updating positions
				for( int i = 0; i < size; i++ )
				{
					classArray[i].position.Y += gravity;
					if( classArray[i].position.Y > 1024 )
					{ classArray[i].position.Y = 0; }
				}
				stopwatch.Stop();
				classTime = stopwatch.ElapsedTicks;

				stopwatch.Restart();
				//loop struct array, updating positions
				for( int i = 0; i < size; i++ )
				{
					structArray[i].position.Y += gravity;
					if( structArray[i].position.Y > 1024 )
					{ structArray[i].position.Y = 0; }
				}
				stopwatch.Stop();
				structTime = stopwatch.ElapsedTicks;

				stopwatch.Restart();
				//loop float array, updating positions
				for( int i = 0; i < size; i++ )
				{
					floatArray[i] += gravity;
					if( floatArray[i] > 1024 )
					{ floatArray[i] = 0; }
				}
				stopwatch.Stop();
				floatTime = stopwatch.ElapsedTicks;

				//what took the longest? class, struct, float (works in release mode)
				lib.Log.debug( $"{(uint)id & 0xffff:X4}  class array time: {classTime:10}" );
				lib.Log.debug( $"{(uint)id & 0xffff:X4} struct array time: {structTime:10}" );
				lib.Log.debug( $"{(uint)id & 0xffff:X4}  float array time: {floatTime:10}" );
			}
		}


#endif


		public Machine( res.Ref<MachineCfg> _cfg )
			:
			base( _cfg )
		{
			//s_mgr.send_fromService(null);

			

			//var ready = new msg.Ready { /*address = s_mgr.AddressAll(),*/ source = id };
			//s_mgr.send_fromService( ready );
		}

		public void run()
		{
			while( true )
			{
				procMsg_block();
				//Thread.Sleep(1);
			}

			//Thread.Sleep(1000);
		}

		/*
		void handle( msg.Hello hello )
		{
			lib.Log.info( $"Got hello" );
		}
		*/

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


		void handle( msg.Startup startup )
		{
			lib.Log.debug( $"{(uint)id & 0xffff:X4}  got Startup from" );

			var address = new RTAddress( s_mgr.Id, id );
			var ready = new msg.Ready{ address = address };

			s_mgr.broadcast( address, ready );

			//initForTest();
			//timeTest();
		}

		void handle( msg.Ready ready )
		{
			lib.Log.debug( $"{(uint)id & 0xffff:X4}  got Ready from {ready.address}" );

			var address = new RTAddress( s_mgr.Id, id );

			if( address != ready.address && !m_otherServices.Contains( ready.address ) )
			{
				lib.Log.debug( $"{(uint)id & 0xffff:X4}  READY adding service {ready.address}" );
				m_otherServices = m_otherServices.Add( ready.address );

				sendPing( address );

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
