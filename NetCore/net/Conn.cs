using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;





namespace net
{


	public class Conn<T, TInst> : lib.Conn<T, TInst> where T : IFormatter, new()
	                                                 where TInst : lib.ISerDes<T>, new()
	{
		private bool m_loop = true;


		public Conn( Socket sock, lib.IProcess proc = null )
			:
			base( sock, proc )
		{
		}

		public void recieveThread()
		{
			Thread thread = new Thread( new ThreadStart( this.recieveLoop ) );

			thread.Name = $"Conn {Sock.RemoteEndPoint}";

			thread.Start();
		}

		private byte[] mm_recvBuf = new byte[ BufferSize ];

		private void recieveLoop()
		{
			var curThread = Thread.CurrentThread;

			log.logProps( curThread, $"Starting thread { curThread.Name} ({ curThread.ManagedThreadId})", log.LogType.Info );

			log.logProps( Sock, $"Socket {Sock.RemoteEndPoint}", log.LogType.Info );



			while( m_loop )
			{
				try
				{
					uint sizeReadCount = (uint)Stream.Read( mm_recvBuf, 0, 4 );
					uint len = BitConverter.ToUInt32( mm_recvBuf, 0 );
					uint totalAmountRead = 0;

					//log.info( "Len {0} needed to read", len );

					var memStream = new MemoryStream( BufferSize );

					while( totalAmountRead < len )
					{
						uint amountLeft = len - totalAmountRead;

						uint maxRead = amountLeft < mm_recvBuf.Length ? amountLeft : (uint)mm_recvBuf.Length;

						uint amountRead = (uint)Stream.Read( mm_recvBuf, 0, (int)maxRead );

						memStream.Write( mm_recvBuf, 0, (int)amountRead );

						totalAmountRead += amountRead;

						//*
						var debugMemStream = new MemoryStream( BufferSize );
						debugMemStream.Write( mm_recvBuf, 0, (int)amountRead );
						debugMemStream.Position = 0;

						StreamReader debugReader = new StreamReader( debugMemStream );
						string debugText = debugReader.ReadToEnd();
						//*/

					}

					try
					{
						memStream.Position = 0;

						//var str = System.Text.Encoding.Default.GetString( memStream.GetBuffer() );
						//log.info( "Received {0} in obj {1}.", totalAmountRead, str );

						var obj = recieveObject( memStream );
						if( obj != null )
						{
							recieve( obj );
						}
						else
						{
						}
					}
					catch( Exception e )
					{
						log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
					}
				}
				catch( SocketException e )
				{
					log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
					m_loop = false;
				}
				catch( IOException e )
				{
					log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
					m_loop = false;
				}

				if( !Sock.Connected )
				{
					log.info( $"Socket is no longer connected, leaving" );
					m_loop = false;
				}
			}

			log.info( $"Ending thread { curThread.Name} ({ curThread.ManagedThreadId})" );
		}


		EventWaitHandle mm_recvWait = new EventWaitHandle( true, EventResetMode.AutoReset );
		IAsyncResult mm_recvRes;
		void recieveLoop_old()
		{
			while( m_loop )
			{
				mm_recvRes = Stream.BeginRead( mm_recvBuf, 0, BufferSize, new AsyncCallback( asyncRecv ), null );
				mm_recvWait.WaitOne();
			}
		}

		void asyncRecv( IAsyncResult ar )
		{
			int packetSize = Stream.EndRead( ar );
			log.info( $"Recieved {packetSize}" );

			if( !Sock.Connected || packetSize == 0 )
			{
				var stuff = Sock.Connected?"Connected":"Disconnected";
				log.warn( $"Socket {Sock.RemoteEndPoint} had a problem. {stuff} recv {packetSize} bytes." );
				m_loop = false;
				return;
			}

			/*
			for( int i = 0; i < packetSize; ++i )
			{
				log.info( "{0}:{1}:{2}", i, mm_recvBuf[ i ], (char)mm_recvBuf[ i ] );
			}
			*/

			try
			{
				mm_recvBuf[packetSize] = 0;

				//TODO: Make a proper 'MemoryStream'
				var recv = new MemoryStream( BufferSize );
				recv.Write( mm_recvBuf, 0, packetSize );
				recv.Position = 0;

				var str = System.Text.Encoding.Default.GetString( mm_recvBuf );
				log.info( $"Received {packetSize} in obj {str}." );

				/*
				for( int i = 0; i < packetSize; ++i )
				{
					log.info( "{0,3}:{1,3}:{2}:{3,3}", i, mm_recvBuf[ i ], (char)mm_recvBuf[ i ], mm_memRecvStream.ReadByte() );
				}
				*/

				var obj = recieveObject( recv );
				if( obj != null )
				{
					recieve( obj );
				}
				else
				{
				}
			}
			catch( SocketException e )
			{
				log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
				m_loop = false;
			}
			catch( IOException e )
			{
				log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
				m_loop = false;
			}
			catch( Exception e )
			{
				log.error( $"Socket {Sock.RemoteEndPoint} had a problem.  Ex {e}." );
			}

			mm_recvWait.Set();
		}

	}



}
