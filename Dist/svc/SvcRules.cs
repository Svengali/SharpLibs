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

	public class SvcRulesCfg : ServiceCfg
	{
	}

	public class SvcRules : ServiceWithConfig<SvcRulesCfg>, svc.ISourceRun
	{


		public SvcRules( res.Ref<SvcRulesCfg> _cfg )
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
