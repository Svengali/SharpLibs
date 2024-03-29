﻿/*
	M A I N

	The core server class.  

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using System.Reflection;

using System.IO;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using svc;

namespace sv
{


	#region Test


	class TestMsg
	{
	}

	class TestCalls
	{

		private int m_runCount = 10000000;

		public void runAllTests()
		{
			testDirect();
			testInvoke();
			testDelegate();
			testExpression();


		}


		public void testDirect()
		{
			var o = new TestMsg();

			var timer = new lib.Timer();

			timer.Start();
			for( int i = 0; i < m_runCount; ++i )
			{
				handle( o );
			}
			var endMs = timer.Current;

			log.info( $"testDirect: {endMs}" );
		}

		public void testInvoke()
		{
			var argTypes = new Type[ 1 ];
			argTypes[0] = typeof( TestMsg );

			var mi = GetType().GetMethod( "handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, argTypes, null );

			var o = new TestMsg();

			var timer = new lib.Timer();

			var args = new object[1];
			args[0] = o;

			timer.Start();
			for( int i = 0; i < m_runCount; ++i )
			{
				mi.Invoke( this, args );
			}
			var endMs = timer.Current;

			log.info( $"testInvoke: {endMs}" );
		}

		public delegate void dlgHandler( TestMsg msg );

		public void testDelegate()
		{
			/*
			var args = new Type[1];
			args[0]=typeof( TestMsg );

			var mi = GetType().GetMethod( "handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null );

			var cb = mi.CreateDelegate( typeof(dlgHandler) );

			var o = new TestMsg();

			var timer = new lib.Timer();

			timer.Start();
			for( int i = 0; i< m_runCount; ++i )
			{
				cb.DynamicInvoke( o );
			}
			var endMs = timer.CurrentMS;

			log.info( $"testDelegate: {endMs}" );
			/*/
			log.info( $"testDelegate: OFF" );
			//*/


		}

		public void testExpression()
		{
			var args = new Type[1];
			args[0] = typeof( TestMsg );

			var mi = GetType().GetMethod( "handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null );

			ParameterExpression pe = Expression.Parameter( typeof (TestMsg ), "msgIn" );

			var exConvert = Expression.Convert( pe, args[ 0 ] );

			var exParams = new Expression[ 1 ];
			exParams[0] = exConvert;

			var exThis = Expression.Constant( this );
			var exCall = Expression.Call( exThis, mi, exParams );

			var fn = Expression.Lambda<Action<TestMsg>>( exCall, pe ).Compile();

			var o = new TestMsg();

			var timer = new lib.Timer();

			timer.Start();
			for( int i = 0; i < m_runCount; ++i )
			{
				fn( o );
			}
			var endMs = timer.Current;


			log.info( $"testExpression: {endMs}" );
		}

		public void handle( TestMsg msg )
		{
		}


	}

	#endregion

	public enum ENodeType
	{
		Root,
		Leaf
	}

	[Serializable]
	public class SerializationTest
	{

		public SerializationTest()
		{
			m_strings[0] = "Str 0";
			m_strings[1] = "Str 1";

			m_comList[new lib.Token( "Token" )] = "Token";
			m_comList[new lib.Token( "Token_2" )] = "Token_2";

			m_immList = m_immList.Add( new lib.Token( "ImmToken 1" ), "ImmToken 1 is this string" );
			m_immList = m_immList.Add( new lib.Token( "ImmToken 2" ), "ImmToken 2 is this string" );
		}

		String[] m_strings = new String[ 3 ];

		Dictionary<lib.Token, string> m_comList = new Dictionary<lib.Token, string>();
		ImmutableDictionary<lib.Token, string> m_immList = ImmutableDictionary<lib.Token, string>.Empty;
	}










	[Serializable]
	public class NewService
	{
		public string type = "<unknown>";
		public string name = "<unknown>";
		public string configPath = "<unknown>";
	}

	[Serializable]
	public class ServiceOnDemand
	{
		public NewService service = new NewService();
	}

	[Serializable]
	public class RemoteMachine
	{
		public string address = "0.0.0.0";
		public int    port = 8008;
	}


	[Serializable]
	public class ServerCfg : lib.Config
	{
		/*
		public ENodeType node = ENodeType.Leaf;
		public string name = "Test";
		public string address = "0.0.0.0";
		public int    port = 8008;
		*/

		public res.Ref<svc.MachineCfg> machineCfg;
		public res.Ref<svc.MgrCfg> mgrCfg;

		public NewService[] services = { new NewService() };
		//public ServiceOnDemand[] servicesOnDemand = { new ServiceOnDemand() };

		//public RemoteMachine[] machines = { new RemoteMachine() };
	}




	public class Main
	{
		static public Main main;

		#region Logging
		/*

		//Konsole

		static Window s_fullscreenWin;
		static IConsole s_logWin;
		//*/


		static public void logOut( log.LogEvent evt )
		{
			var seperatorSymbol = "|";

			switch( evt.LogType )
			{
				case log.LogType.Trace:
					seperatorSymbol = ":";
					Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
				case log.LogType.Debug:
					seperatorSymbol = ":";
					Console.ForegroundColor = ConsoleColor.Gray;
				break;
				case log.LogType.Info:
					Console.ForegroundColor = ConsoleColor.DarkGreen;
				break;
				case log.LogType.High:
					Console.ForegroundColor = ConsoleColor.Cyan;
				break;
				case log.LogType.Warn:
					seperatorSymbol = "#";
					Console.ForegroundColor = ConsoleColor.Yellow;
				break;
				case log.LogType.Error:
					seperatorSymbol = "#";
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
				case log.LogType.Fatal:
					seperatorSymbol = "#";
					Console.ForegroundColor = ConsoleColor.Red;
					Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
			}



			char sym = log.getSymbol( evt.LogType );

			var truncatedCat = evt.Cat.Substring(0, Math.Min( 8, evt.Cat.Length ) );

			string finalLine = string.Format( "{0,-8}{1}{2} {3}", truncatedCat, sym, seperatorSymbol, evt.Msg );


			Console.WriteLine( finalLine );

			Console.ResetColor();

		}

		#endregion Logging


		public lib.Clock clock { get; private set; }

		public Main( string configPath )
		{
			main = this;

			/* Konsole Logging
			s_fullscreenWin = new Window();
			s_fullscreenWin.BackgroundColor = ConsoleColor.DarkGray;
			s_fullscreenWin.Clear( ConsoleColor.DarkGray );

			var xStart = 2;
			var yStart = 2;

			var xSize = s_fullscreenWin.WindowWidth  - xStart * 2;
			var ySize = s_fullscreenWin.WindowHeight - yStart * 2;

			s_logWin = Window.Open( xStart, yStart, xSize, ySize, "Logging" );
			//1*/

			net.App.startup();

			Process p = Process.GetCurrentProcess();

			var t = DateTime.Now;

			var date = $"{t.Year}{t.Month:D2}{t.Day:D2}_{t.Hour:D2}{t.Minute:D2}";

			string logpath = $"logs/{Environment.MachineName}_{date}_{p.Id}.log";


			log.create( logpath );

			log.addDelegate( logOut );

			log.info( $"Saving log to [{logpath}]" );


			//Log examples of each log type
			log.trace( $"Trace test" );
			log.debug( $"Debug test" );
			log.info( $"Info test" );
			log.high( $"High test" );
			log.warn( $"Warn test" );
			log.error( $"Error test" );
			log.fatal( $"Fatal test" );



			//*
			log.info( $"Command line {Environment.CommandLine}" );
			log.info( $"Current working directory {Environment.CurrentDirectory}" );

			log.info( $"Running as {( Environment.Is64BitProcess ? "64" : "32" )}bit on a {( Environment.Is64BitOperatingSystem ? "64" : "32" )}bit machine." );
			log.info( $"This machine has {Environment.ProcessorCount} processors." );

			log.info( $"Currently given {Environment.WorkingSet} memory" );
			log.info( $"System page size of {Environment.SystemPageSize}" );

			log.info( $"Running on CLR {Environment.Version}" );
			log.info( $"Running on {Environment.OSVersion}" );

			log.info( $"Running as {Environment.UserName}" );
			//*/


			/*
			var test = new TestCalls();
			test.runAllTests();
			/*/
			log.info( $"Skipping tests." );
			//*/



			//Serializer.Use( new svc.CerasSerializerForShielded() );


			res.Mgr.startup();
			lib.Config.startup( "server_config.cfg" );


			//Load configs
			log.info( $"Loading config {configPath}" );
			//m_cfg = lib.Config.load<ServerCfg>( configPath );
			m_cfg = res.Mgr.lookup<ServerCfg>( configPath );


			// @@ TODO Move to specific
			lib.Util.checkAndAddDirectory( "logs" );
			// save/static and save/dynamic are created when they dont exist in order to create the universe
			lib.Util.checkAndAddDirectory( "save/players" );


			lib.Util.checkAndAddDirectory( "save/archive/static" );
			lib.Util.checkAndAddDirectory( "save/archive/dynamic" );
			lib.Util.checkAndAddDirectory( "save/archive/players" );

			clock = new lib.Clock( 0 );

			m_svcMgr = new svc.Mgr<svc.Service<msg.Msg>, msg.Msg>( m_cfg.res.mgrCfg, svc.SourceId.Local );
			svc.Base<svc.Service<msg.Msg>, msg.Msg>.setMgr( m_svcMgr );



			//*
			foreach( NetworkInterface nic in
					NetworkInterface.GetAllNetworkInterfaces() )
			{
				if( nic.OperationalStatus == OperationalStatus.Up )
				{
					log.logProps( nic, "Network Interface (up)", log.LogType.Info, prefix: "  " );
				}
				else
				{
					log.info( "Network Interface (down)" );
					log.info( $"  {nic.Name} {nic.Description}" );
				}

				foreach( UnicastIPAddressInformation addrInfo in
						nic.GetIPProperties().UnicastAddresses )
				{
					//log.logProps( addrInfo, " Addresses", log.LogType.Info, prefix: "    " );
					log.debug( $"    {addrInfo.Address}" );
				}
			}
			//*/



			//string machineName = m_cfg.res.name; //+"/"+ep.Address.ToString() + ":" + ep.Port;

			//var machineName = new Shielded<string>(m_cfg.res.name);

			var machines = new List<IPEndPoint>(); // new Dictionary<string, IPEndPoint>();

			//machines[machineName] = new IPEndPoint( IPAddress.Any, m_cfg.res.port );
			/*
			var localEndPoint = new IPEndPoint( IPAddress.Any, m_cfg.res.port );


			foreach( var mac in m_cfg.res.machines )
			{
				//var remoteName = $"remote_{mac.address}:{mac.port.ToString()}";

				//machines[remoteName] = new IPEndPoint( IPAddress.Parse( mac.address ), mac.port );

				machines.Add( new IPEndPoint( IPAddress.Parse( mac.address ), mac.port ) );
			}
			*/

			/* Shielded
			connectToOtherMachines( machineName, localEndPoint, machines );

			var machineName_ver = machineName.SvcVersion( 0 );

			var key = $"name_{m_cfg.res.name}_{m_cfg.res.port.ToString()}";

			m_backend.Set( key, machineName_ver );

			//var test = machineVer.Value;

			Shielded.Shield.InTransaction( () => {

				machineName.Value = "Hello";


				//machineName = "Hello";

				//machineVer = machineVer.NextVersion();


			} );
			*/

			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );


			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/

			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/

			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/

			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/

			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/

			/*
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			m_machines = m_machines.Add( new svc.Machine( m_cfg.res.machineCfg ) );
			//*/


			foreach( var mac in m_machines )
			{
				m_svcMgr.start( mac );
			}


			m_svcMgr.startup();

			Thread.Sleep( 1000 );

			//*
			var startup = new msg.Startup {};

			var address = new RTAddress( m_svcMgr.Id, m_svcMgr.Id );

			foreach( var mac in m_machines )
			{
				m_svcMgr.send( address, startup, ( svc ) => svc.id == mac.id );
			}
			//*/


		}


		public void shutdown()
		{

			//var shutdown = new svmsg.Shutdown();

			//m_machine.send( shutdown );

			m_svcMgr.processMessages();
		}



		public void startup()
		{
			Thread thread = new Thread( new ThreadStart( run ) );
			thread.Name = $"Main Thread";
			thread.Start();
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
			clock.tick();

			//m_svcMgr.processMessagesBlock( 1000 );

			//m_backend.Touch("test");

			Thread.Sleep(10);
		}


		res.Ref<ServerCfg> m_cfg;

		svc.Mgr<svc.Service<msg.Msg>, msg.Msg> m_svcMgr;

		ImmutableList<svc.Machine> m_machines = ImmutableList<svc.Machine>.Empty;


	}



}
