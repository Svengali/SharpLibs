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

	public class SvcClientCfg : ServiceCfg
	{
	}

	public class SvcClient : ServiceWithConfig<SvcClientCfg>, svc.ISourceRun
	{


		public SvcClient( res.Ref<SvcClientCfg> _cfg )
			:
			base( _cfg )
		{
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

	}


}
