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



	public class MachineCfg : lib.Config
	{
	}

	public class Machine : ServiceWithConfig<MachineCfg>, svc.ISourceRun
	{


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

			if( address != ping.address && !m_otherServices.Contains(ping.address) )
			{
				lib.Log.debug( $"{(uint)id & 0xffff:X4}  PING adding service {ping.address}" );
				m_otherServices = m_otherServices.Add( ping.address );
			}

			sendPing( address );
		}


		void handle( msg.Startup startup )
		{
			lib.Log.debug( $"{(uint)id & 0xffff:X4}  got Startup from" );

			var address = new RTAddress( s_mgr.Id, id );
			var ready = new msg.Ready{ address = address };

			s_mgr.send( address, ready, (svc) => {
				return true;
			});
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
