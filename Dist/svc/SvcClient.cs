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
		public enum State
		{
			Invalid,
			Early,
			StartingUp,
			Running,
			ShuttingDown,
		}


		public SvcClient( res.Ref<SvcClientCfg> _cfg )
			:
			base( _cfg )
		{
			m_state = State.Early;
		}

		public void run()
		{
			var currentTime = DateTime.Now;

			while( true )
			{
				switch( m_state )
				{
					case State.Running:

						if( m_window?.IsOpen ?? false )
						{
							m_window.Clear();

							m_window.DispatchEvents();

							/* Circle testing
							for( var i = 0; i < 1800; ++i )
							{
								var x = (float)m_rand.NextDouble() * 1200.0f;
								var y = (float)m_rand.NextDouble() * 1000.0f;
								var r = (float)m_rand.NextDouble() * 80.0f;

								var circle = new SFML.Graphics.CircleShape( r )
								{
									Position = new SFML.System.Vector2f( x, y ),
									OutlineThickness = 1.0f,
									OutlineColor = SFML.Graphics.Color.Blue,
									//FillColor = SFML.Graphics.Color.Blue,
								};

								///var rect = new SFML.Graphics.RectangleShape();

								m_window.Draw( circle );

							}
							//*/


							// Finally, display the rendered frame on screen
							m_window.Display();
						}


					break;
				}

				procMsg_block();

				var spentTime = DateTime.Now;

				var delta = spentTime - currentTime;

				var deltaMS = delta.TotalMilliseconds;

				var pause = Math.Max( 0, 33.0 - deltaMS );

				var pauseInt = (int)pause;


				if( pauseInt != 0 )
				{
					Thread.Sleep( pauseInt );
				}
				else
				{
					log.warn( $"Long frame {delta.TotalMilliseconds}" );
				}

				currentTime = DateTime.Now;

			}
		}

		override internal void handle( msg.Startup startup )
		{
			base.handle( startup );

			var mode = new SFML.Window.VideoMode(1280, 1024);
			m_window = new SFML.Graphics.RenderWindow(mode, "Client");
			m_window.KeyPressed += Window_KeyPressed;

			m_state = State.Running;

		}

		private void Window_KeyPressed( object sender, SFML.Window.KeyEventArgs e )
		{
			var window = (SFML.Window.Window)sender;
			if( e.Code == SFML.Window.Keyboard.Key.Escape )
			{
				window.Close();
			}
		}


		Random m_rand = new Random();

		SFML.Graphics.RenderWindow m_window;

		public ent.DB   m_db;
		public State    m_state;




	}


}
