/*
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

			lib.Log.info( $"testDirect: {endMs}" );
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

			lib.Log.info( $"testInvoke: {endMs}" );
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

			lib.Log.info( $"testDelegate: {endMs}" );
			/*/
			lib.Log.info( $"testDelegate: OFF" );
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


			lib.Log.info( $"testExpression: {endMs}" );
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
		public ENodeType node = ENodeType.Leaf;
		public string name = "Test";
		public string address = "0.0.0.0";
		public int    port = 8008;

		public res.Ref<svc.MachineCfg> machineCfg;

		public NewService[] services = { new NewService() };
		public ServiceOnDemand[] servicesOnDemand = { new ServiceOnDemand() };

		public RemoteMachine[] machines = { new RemoteMachine() };
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


		static public void log( lib.LogEvent evt )
		{
			/*
			switch( evt.LogType )
			{
				case lib.LogType.Error:
				s_logWin.ForegroundColor = ConsoleColor.Red;
				break;
				case lib.LogType.Warn:
				s_logWin.ForegroundColor = ConsoleColor.Yellow;
				break;
				case lib.LogType.Info:
				s_logWin.ForegroundColor = ConsoleColor.Gray;
				break;

			}

			s_logWin.WriteLine( $"{evt.Msg}" );
	
		Invalid = 0,
		Trace = 1,
		Debug = 2,
		Info = 3,
		Warn = 4,
		Error = 5,
		Fatal = 6,
			
			
			/*/

			var seperatorSymbol = ":";

			switch( evt.LogType )
			{
				case lib.LogType.Trace:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
				case lib.LogType.Debug:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
				case lib.LogType.Info:
				seperatorSymbol = ">";
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				break;
				case lib.LogType.High:
				seperatorSymbol = ">";
				Console.ForegroundColor = ConsoleColor.Cyan;
				break;
				case lib.LogType.Warn:
				seperatorSymbol = ">";
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
				case lib.LogType.Error:
				seperatorSymbol = ">";
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
				case lib.LogType.Fatal:
				seperatorSymbol = ">";
				Console.ForegroundColor = ConsoleColor.Red;
				Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
			}



			char sym = lib.Log.getSymbol( evt.LogType );

			var truncatedCat = evt.Cat.Substring(0, Math.Min( 8, evt.Cat.Length ) );

			string finalLine = string.Format( "{0,-8}{1}{2} {3}", truncatedCat, sym, seperatorSymbol, evt.Msg );


			Console.WriteLine( finalLine );

			Console.ResetColor();
			//*/


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


			lib.Log.create( logpath );

			lib.Log.s_log.addDelegate( log );

			lib.Log.info( $"Saving log to [{logpath}]" );


			//Log examples of each log type
			lib.Log.trace( $"Trace test" );
			lib.Log.debug( $"Debug test" );
			lib.Log.info( $"Info test" );
			lib.Log.high( $"High test" );
			lib.Log.warn( $"Warn test" );
			lib.Log.error( $"Error test" );
			lib.Log.fatal( $"Fatal test" );



			//*
			lib.Log.info( $"Command line {Environment.CommandLine}" );
			lib.Log.info( $"Current working directory {Environment.CurrentDirectory}" );

			lib.Log.info( $"Running as {( Environment.Is64BitProcess ? "64" : "32" )}bit on a {( Environment.Is64BitOperatingSystem ? "64" : "32" )}bit machine." );
			lib.Log.info( $"This machine has {Environment.ProcessorCount} processors." );

			lib.Log.info( $"Currently given {Environment.WorkingSet} memory" );
			lib.Log.info( $"System page size of {Environment.SystemPageSize}" );

			lib.Log.info( $"Running on CLR {Environment.Version}" );
			lib.Log.info( $"Running on {Environment.OSVersion}" );

			lib.Log.info( $"Running as {Environment.UserName}" );
			//*/


			/*
			var test = new TestCalls();
			test.runAllTests();
			/*/
			lib.Log.info( $"Skipping tests." );
			//*/



			//Serializer.Use( new svc.CerasSerializerForShielded() );


			res.Mgr.startup();
			lib.Config.startup( "server_config.cfg" );


			// @@ TODO Move to specific
			lib.Util.checkAndAddDirectory( "logs" );
			// save/static and save/dynamic are created when they dont exist in order to create the universe
			lib.Util.checkAndAddDirectory( "save/players" );


			lib.Util.checkAndAddDirectory( "save/archive/static" );
			lib.Util.checkAndAddDirectory( "save/archive/dynamic" );
			lib.Util.checkAndAddDirectory( "save/archive/players" );

			clock = new lib.Clock( 0 );

			m_svcMgr = new svc.Mgr<svc.Service<msg.Msg>, msg.Msg>();
			svc.Base<svc.Service<msg.Msg>, msg.Msg>.setMgr(m_svcMgr);

			//Load configs
			lib.Log.info( $"Loading config {configPath}" );
			//m_cfg = lib.Config.load<ServerCfg>( configPath );
			m_cfg = res.Mgr.lookup<ServerCfg>( configPath );


			//*
			foreach( NetworkInterface nic in
					NetworkInterface.GetAllNetworkInterfaces() )
			{
				if( nic.OperationalStatus == OperationalStatus.Up )
				{
					lib.Log.logProps( nic, "Network Interface (up)", lib.LogType.Info, prefix: "  " );
				}
				else
				{
					lib.Log.info( "Network Interface (down)" );
					lib.Log.info( $"  {nic.Name} {nic.Description}" );
				}

				foreach( UnicastIPAddressInformation addrInfo in
						nic.GetIPProperties().UnicastAddresses )
				{
					//lib.Log.logProps( addrInfo, " Addresses", lib.LogType.Info, prefix: "    " );
					lib.Log.debug( $"    {addrInfo.Address}" );
				}
			}
			//*/



			//string machineName = m_cfg.res.name; //+"/"+ep.Address.ToString() + ":" + ep.Port;

			//var machineName = new Shielded<string>(m_cfg.res.name);

			var machines = new List<IPEndPoint>(); // new Dictionary<string, IPEndPoint>();

			//machines[machineName] = new IPEndPoint( IPAddress.Any, m_cfg.res.port );
			var localEndPoint = new IPEndPoint( IPAddress.Any, m_cfg.res.port );


			foreach( var mac in m_cfg.res.machines )
			{
				//var remoteName = $"remote_{mac.address}:{mac.port.ToString()}";

				//machines[remoteName] = new IPEndPoint( IPAddress.Parse( mac.address ), mac.port );

				machines.Add( new IPEndPoint( IPAddress.Parse( mac.address ), mac.port ) );
			}

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

			m_machine = new svc.Machine( m_cfg.res.machineCfg );

			m_machine2 = new svc.Machine( m_cfg.res.machineCfg );
			m_machine3 = new svc.Machine( m_cfg.res.machineCfg );




			m_svcMgr.start( m_machine );
			m_svcMgr.start( m_machine2 );
			m_svcMgr.start( m_machine3 );

			//TODO: Move these into machine startup.
			tick();
			Thread.Sleep( 1000 );

			//Now startup all the listed services
			foreach( var s in m_cfg.res.services )
			{
				/*
				var start = new svmsg.StartService( new svmsg.FilterType<svc.Machine>() )
				{
					type = s.type,
					configPath = s.configPath,
					name = s.name,
				};

				//start.type = s.type;
				//start.configPath = s.configPath;
				//start.name = s.name;

				m_machine.send( start );
				*/
			}


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
			thread.Start();
		}

		public void run()
		{
			while( true )
			{
				tick();
			}
		}

		public void tick()
		{
			clock.tick();

			m_svcMgr.procMsg_block( 1000 );

			//m_backend.Touch("test");

		}


		res.Ref<ServerCfg> m_cfg;

		svc.Mgr<svc.Service<msg.Msg>, msg.Msg> m_svcMgr;

		svc.Machine m_machine;
		svc.Machine m_machine2;
		svc.Machine m_machine3;




	}



}
