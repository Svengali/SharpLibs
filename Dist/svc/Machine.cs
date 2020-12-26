using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace svc
{



	public struct MachineInfo
	{
		public readonly string ip;
		public readonly ushort port;

	}

	public class MachineCfg : lib.Config
	{
		public readonly ImmutableArray<MachineInfo> machines = ImmutableArray<MachineInfo>.Empty;
		public readonly MachineInfo listen;
	}

	public class Machine : ServiceWithConfig<MachineCfg>, svc.ISourceRun
	{


		public Machine( res.Ref<MachineCfg> _cfg )
			:
			base( _cfg )
		{
			//s_mgr.send_fromService(null);

			

			var ready = new msg.Ready { address = s_mgr.AddressAll(), source = id };
			s_mgr.send_fromService( ready );
		}

		public void run()
		{
			Thread.Sleep(1000);
		}

		/*
		void handle( msg.Hello hello )
		{
			lib.Log.info( $"Got hello" );
		}
		*/

		void handle( msg.Ping ping )
		{

		}

		void handle( msg.Ready ready )
		{
			lib.Log.debug( $"{id} got Ready from {ready.source}" );

			if( id != ready.source )
			{
				m_otherServices = m_otherServices.Add( ready.source );
			}
		}


		ImmutableList<svc.SourceId> m_otherServices = ImmutableList<svc.SourceId>.Empty;

	}






















}
