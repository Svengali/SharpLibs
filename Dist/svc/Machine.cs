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



	public class MachineCfg : ServiceCfg
	{
		public readonly msg.StartService[] services;
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


		override internal void handle( msg.Startup startup )
		{
			base.handle ( startup );

			var address = new RTAddress( s_mgr.Id, id );

			lib.Log.info( $"Starting up {cfg.res.services.Length} services" );
			foreach( var svc in cfg.res.services )
			{
				send( svc, address );
			}
		}


		public void handle( msg.StartService start )
		{
			Type[] types = new Type[ 1 ];
			object[] parms = new object[ 1 ];

			//types[0] = typeof( lib.Token );

			Type svcType = Type.GetType( start.type );

			if( svcType != null )
			{
				// @@@ DEPENDENCY This implies that all services are a subclass of a class that has 1 
				// generic argument that is its config type
				Type cfgType = svcType.BaseType.GenericTypeArguments[0];

				//res.Ref cfg = res.Mgr.lookup( start.configPath, cfgType );

				var refGenType = typeof(res.Ref<>);

				var refType = refGenType.MakeGenericType( cfgType );

				var cfg = Activator.CreateInstance( refType, start.configPath );

				if( cfg != null )
				{
					types[0] = cfg.GetType();

					ConstructorInfo cons = svcType.GetConstructor( types );

					try
					{
						//parms[0] = new lib.Token( start.name );
						parms[0] = cfg;

						lib.Log.info( $"Starting service {"unknown"} of type {refType.Name} using config {start.configPath}" );

						svc.Service<msg.Msg> newService = (svc.Service<msg.Msg>)cons.Invoke( parms );

						s_mgr.start( newService );



						var startupMsg = new msg.Startup {};

						var svcAddress = new RTAddress( s_mgr.Id, newService.id );

						//var delayMsg = new msg.DelaySend { address = svcAddress, msg = startupMsg };


						//s_mgr.send(  )

						send( startupMsg, svcAddress );


					}
					catch( Exception ex )
					{
						lib.Log.error( $"Exception while calling service constructor {ex}" );
					}
				}
				else
				{
					lib.Log.warn( $"Could not find service of type {start.type}" );
				}


			}
			else
			{
				lib.Log.warn( $"Could not find service of type {start.type}" );
			}
		}





	}






















}
