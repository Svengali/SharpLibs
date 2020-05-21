using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace net
{


	public interface Versioned
	{
		ulong Version { get; }
	}







	public class View
	{

	}

	public class View<TView> : View
	{

	}


	public interface WriteCompressed
	{

	}






	/*

	public interface IWriteRaw
	{
		void Write( ulong value );
		void Write( uint value );
		void Write( ushort value );
		void Write( string value );
		void Write( float value );
		void Write( sbyte value );
		void Write( long value );
		void Write( int value );
		void Write( short value );
		void Write( decimal value );
		void Write( char[] chars, int index, int count );
		void Write( char[] chars );
		void Write( byte[] buffer, int index, int count );
		void Write( byte[] buffer );
		void Write( byte value );
		void Write( bool value );
		void Write( double value );
		void Write( char ch );
	}

	public interface IWriteSelf<T>
	{
		void Write( string name, T obj );
	}

	public interface IWrite
	{
		void Write( string name, ulong value );
		void Write( string name, uint value );
		void Write( string name, ushort value );
		void Write( string name, string value );
		void Write( string name, float value );
		void Write( string name, sbyte value );
		void Write( string name, long value );
		void Write( string name, int value );
		void Write( string name, short value );
		void Write( string name, decimal value );
		void Write( string name, char[] chars, int index, int count );
		void Write( string name, char[] chars );
		void Write( string name, byte[] buffer, int index, int count );
		void Write( string name, byte[] buffer );
		void Write( string name, byte value );
		void Write( string name, bool value );
		void Write( string name, double value );
		void Write( string name, char ch );
		void Write<T>( string name, T obj ) where T : IWriteSelf<T>;
	}


	public class WriterFwd : IWrite
	{
		public WriterFwd( IWriteRaw raw )
		{
			m_raw = raw;
		}

		public void Write<T>( string name, T obj ) where T : IWriteSelf<T>
		{
			obj.Write( name, obj );
		}

		#region POD Writes
		public void Write( string name, ulong value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, uint value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, ushort value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, string value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, float value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, sbyte value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, long value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, int value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, short value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, decimal value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, char[] chars, int index, int count )
		{
			m_raw.Write( chars, index, count );
		}

		public void Write( string name, char[] chars )
		{
			m_raw.Write( chars );
		}

		public void Write( string name, byte[] buffer, int index, int count )
		{
			m_raw.Write( buffer, index, count );
		}

		public void Write( string name, byte[] buffer )
		{
			m_raw.Write( buffer );
		}

		public void Write( string name, byte value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, bool value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, double value )
		{
			m_raw.Write( value );
		}

		public void Write( string name, char ch )
		{
			m_raw.Write( ch );
		}

		#endregion // POD Writes

		IWriteRaw m_raw;
	}



	public class BinaryWriterRaw : System.IO.BinaryWriter, IWriteRaw
	{
		public BinaryWriterRaw( Stream stream )
			:
			base( stream )
		{

		}
	}



	public class BinaryWriter : WriterFwd, IWrite
	{
		public BinaryWriter( Stream stream )
			:
			base( new BinaryWriterRaw( stream ) )
		{

		}
	}

	*/


}
