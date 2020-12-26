using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

using ent;

using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace net
{
	public struct NearZero : IEquatable<NearZero>
	{
		public float v;

		public float min;
		public float max;
		public int   bits;

		public NearZero(float _v, float _max = 1.0f, float _min = 0.0f, int _bits = 8)
		{
			v = _v;
			min = _min;
			max = _max;
			bits = _bits;

			scale = max - min;
			intMax = (1 << bits) - 1;
			fintMax= (float)intMax;
			encodeMult = fintMax / scale;
			decodeMult = scale / fintMax;
		}

		#region Encodings
		public uint encodeFlat()
		{
			return encodeRaw(v);
		}

		public uint encodeSqr()
		{
			var modified = MathF.Sqrt( v );
			return encodeRaw(modified);
		}

		public void decodeFlat(uint val)
		{
			var decoded = decodeRaw(val);
			v = decoded;
		}

		public void decodSqr( uint val )
		{
			var decoded = decodeRaw(val);
			var modified = decoded * decoded;
			v = modified;
		}
		#endregion

		#region Raw
		uint encodeRaw(float value)
		{
			var clamped = Math.Clamp(value, min, max);
			var shifted = clamped - min;

			var fRange = shifted * encodeMult;

			var range = (uint)fRange;

			return range;
		}

		float decodeRaw(uint val)
		{
			var fval = (float)val;
			var scaled = fval * decodeMult;
			var shifted = scaled - min;

			return shifted;
		}
		#endregion

		#region Computed
		float scale;// = max - min;
		int intMax; // = (1 << bits) - 1;
		float fintMax; //= (float)intMax;
		float encodeMult;
		float decodeMult;
		#endregion

		#region IEquatable
		public override bool Equals( object obj )
		{
			return obj is NearZero zero && Equals( zero );
		}

		public bool Equals( NearZero other )
		{
			return v == other.v;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( v );
		}

		public static bool operator ==( NearZero left, NearZero right )
		{
			return left.Equals( right );
		}

		public static bool operator !=( NearZero left, NearZero right )
		{
			return !( left == right );
		}
		#endregion
	}

	static public class DeltaOpsEx
	{
		//*
		public static void onChanged<T>( this T old, T value )
		{
			lib.Log.debug( $"<T> has changed." );
		}

		public static void onChanged( this int old, int value )
		{
			lib.Log.debug( $"int has changed." );
		}
		//*/
	}

	public interface DeltaOps
	{
		//T op<T>( string name, T old, T value );
		//T op<T>( string name, T old, T value ) where T : class;
		//T op<T>( string name, T old, T value ) where T : OnChanged<T>;

		//T op<T>( string name, T old, T value );

		//*
		ulong op( string name, ulong old, ulong value );
		int op( string name, int old, int value );
		float op( string name, float old, float value );
		NearZero op( string name, NearZero old, NearZero value );
		object op( string name, object old, object value );
		IList<T> op<T>( string name, IList<T> old, IList<T> value );
		//IList<T> opIDelta<T>( string name, IList<T> old, IList<T> val ) where T : IDelta;
		// */

			/*
			void op( string name, ref uint value );
			void op( string name, ref ushort value );
			void op( string name, ref string value );
			void op( string name, ref float value );
			void op( string name, ref sbyte value );
			void op( string name, ref long value );
			void op( string name, ref short value );
			void op( string name, ref decimal value );
			//void op( string name, ref char[] chars, int index, int count );
			void op( string name, ref char[] chars );
			//void op( string name, ref byte[] buffer, int index, int count );
			void op( string name, ref byte[] buffer );
			void op( string name, ref byte value );
			void op( string name, ref bool value );
			void op( string name, ref double value );
			void op( string name, ref char ch );
			*/
	}

	public static class App
	{
		public static int []s_byteSizes = new int[16];


		public static void startup()
		{
			for( int i = (int)TypeCode.Boolean; i <= (int)TypeCode.Double; ++i )
			{
				int zero = 0;
				byte[] byteArray = ConvertPOD<int>( zero, (TypeCode)i );

				var byteSize = byteArray.Length;

				s_byteSizes[i] = byteSize;
			}

			MemoryStream stream = new MemoryStream();

			DOSend sendTest = new DOSend( stream );

			List<int> listOld = new List<int>();
			listOld.Add(10);
			listOld.Add(20);

			List<int> listNew = new List<int>();
			listNew.Add( 20 );
			listNew.Add( 30 );


			var listDelta = sendTest.op( "nobody", listOld, listNew );




		}

		public static byte[] ConvertPOD<T>( T val )
		{
			TypeCode code = Type.GetTypeCode( typeof(T) );

			return ConvertPOD<T>( val, code );
		}

		public static byte[] ConvertPOD<T>( T val, TypeCode code )
		{

			switch( code )
			{
				case TypeCode.Empty:
				break;

				case TypeCode.Object:
				break;

				case TypeCode.DBNull:
				break;

				case TypeCode.Boolean:
				return BitConverter.GetBytes( Convert.ToBoolean( val ) );

				case TypeCode.Char:
				return BitConverter.GetBytes( Convert.ToChar( val ) );

				case TypeCode.SByte:
				return BitConverter.GetBytes( Convert.ToSByte( val ) );

				case TypeCode.Byte:
				return BitConverter.GetBytes( Convert.ToByte( val ) );

				case TypeCode.Int16:
				return BitConverter.GetBytes( Convert.ToInt16( val ) );

				case TypeCode.UInt16:
				return BitConverter.GetBytes( Convert.ToUInt16( val ) );

				case TypeCode.Int32:
				return BitConverter.GetBytes( Convert.ToInt32( val ) );

				case TypeCode.UInt32:
				return BitConverter.GetBytes( Convert.ToUInt32( val ) );

				case TypeCode.Int64:
				return BitConverter.GetBytes( Convert.ToInt64( val ) );

				case TypeCode.UInt64:
				return BitConverter.GetBytes( Convert.ToUInt64( val ) );

				case TypeCode.Single:
				return BitConverter.GetBytes( Convert.ToSingle( val ) );

				case TypeCode.Double:
				return BitConverter.GetBytes( Convert.ToDouble( val ) );

				/*
				case TypeCode.Decimal:
				break;

				case TypeCode.DateTime:
				break;

				case TypeCode.String:
				break;
				*/
			}

			return new byte[0];
		}

		public static FormatterConverter s_conv = new FormatterConverter();


		public static object ConvertPOD<T>( byte[] buf )
		{
			//Marshal.Copy( buf, 0, ptr, buf.Length );

			/*
			Type[] args = new Type[] { typeof(T) };

			var cons = typeof(T).GetConstructor( args );

			//cons.Invoke()


			Convert.ToBoolean(buf)
			*/

			TypeCode code = Type.GetTypeCode( typeof(T) );


			switch( code )
			{
				case TypeCode.Empty:
				break;

				case TypeCode.Object:
				break;

				case TypeCode.DBNull:
				break;

				case TypeCode.Boolean:
				{
					var t = BitConverter.ToBoolean( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Char:
				{
					var t = BitConverter.ToChar( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.SByte:
				{
					var t = BitConverter.ToChar( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Byte:
				{
					var t = buf[0];
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Int16:
				{
					var t = BitConverter.ToChar( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.UInt16:
				{
					var t = BitConverter.ToUInt16( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Int32:
				{
					var t = BitConverter.ToInt32( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.UInt32:
				{
					var t = BitConverter.ToUInt32( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Int64:
				{
					var t = BitConverter.ToInt64( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.UInt64:
				{
					var t = BitConverter.ToUInt64( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Single:
				{
					var t = BitConverter.ToSingle( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				case TypeCode.Double:
				{
					var t = BitConverter.ToDouble( buf );
					return s_conv.Convert( t, typeof( T ) );
				}

				/*
				case TypeCode.Decimal:
				break;

				case TypeCode.DateTime:
				break;

				case TypeCode.String:
				break;
				*/
			}

			return new object();
		}


	}


	abstract public class DOVirtual : DeltaOps
	{

		abstract public ulong op( string name, ulong old, ulong value );

		abstract public int op( string name, int old, int value );

		abstract public float op( string name, float old, float value );

		abstract public NearZero op( string name, NearZero old, NearZero value );

		abstract public object op( string name, object old, object value );

		abstract public IList<T> op<T>( string name, IList<T> old, IList<T> val );

		//abstract public IList<T> opSpecific<T>( string name, IList<T> old, IList<T> val, Action<T> fn );


		//abstract public IList<int> opSpecific( string name, IList<int> old, IList<int> val, Action<int> fn );


		//abstract public IList<T> opIDelta<T>( string name, IList<T> old, IList<T> val ) where T : IDelta;



		ImmutableDictionary<Type, MethodInfo> m_methods = ImmutableDictionary<Type, MethodInfo>.Empty;


		public delegate T DlgOp<T>( string name, T old, T val );

		public DlgOp<T> getOp<T>()
		{
	
			if( m_methods.TryGetValue( typeof(T), out var method) )
			{
				var dlg = method.CreateDelegate(typeof( DlgOp<T> ));
				return (DlgOp<T>)dlg;
			}

			//TypeCode code = Type.GetTypeCode( typeof(T) );

			Type[] paramTypes = { typeof(string), typeof(T), typeof(T) };

			method = this.GetType().GetMethod( "op", paramTypes );

			if( method != null )
			{
				var dlg = method.CreateDelegate(typeof( DlgOp<T> ), this);

				return (DlgOp<T>)dlg;
			}

			return null;
		}

	}




	public class DOSend : DOVirtual
	{
		BinaryWriter m_writer;

		public DOSend(Stream stream)
		{
			m_writer = new BinaryWriter(stream, Encoding.UTF8, true);
		}

		override public ulong op( string name, ulong old, ulong value )
		{
			m_writer.Write( value );

			return value;
		}

		override public int op( string name, int old, int value )
		{
			m_writer.Write( value );

			return value;
		}

		override public float op( string name, float old, float value )
		{
			m_writer.Write( value );

			return value;
		}

		override public NearZero op( string name, NearZero old, NearZero value )
		{
			var enc = value.encodeSqr();

			m_writer.Write( (byte)enc );

			return value;
		}

		override public object op( string name, object old, object value )
		{
			if( !object.ReferenceEquals( old, value ) )
			{
				if( value is Obj vObj )
				{
					m_writer.Write( true );
					vObj.DeltaFull( old as Obj, this );
				}
				else
				{
					m_writer.Write( false );
					lib.Log.warn( $"The object in {name} is not serializable" );
				}
			}
			else
			{
				m_writer.Write( false );
			}

			return value;
		}

		override public IList<T> op<T>( string name, IList<T> old, IList<T> val )
		{
			if( !object.ReferenceEquals( old, val ) )
			{
				m_writer.Write( true );

				var valSet = val.ToHashSet();

				var removedList = new List<byte>();
				var changedList = new List<byte>();

				if( old != null )
				{
					for( byte iOld = 0; iOld < old.Count; ++iOld )
					{
						if( valSet.TryGetValue( old[iOld], out var oldV ) )
						{
							valSet.Remove( old[iOld] );
						}
						else
						{
							removedList.Add( iOld );
						}
					}
				}

				m_writer.Write( (byte)removedList.Count );
				foreach( var r in removedList )
				{
					m_writer.Write( r );
				}

				DlgOp<T> dlg = getOp<T>();

				m_writer.Write( (byte)valSet.Count );
				foreach( var v in valSet )
				{
					//op("test", default, v);
					//BitConverter.GetBytes(v);
					//byte[] b = App.ConvertPOD( v );
					//m_writer.Write( b );
					dlg.Invoke( "{ignore}", v, v );
				}


			}
			else
			{
				m_writer.Write( false );
			}

			return val;
		}

		/*
		override public IList<T> opSpecific<T>( string name, IList<T> old, IList<T> val, Action<T> fn )
		{
			if( !object.ReferenceEquals( old, val ) )
			{
				m_writer.Write( true );

				var valSet = val.ToHashSet();

				var removedList = new List<byte>();

				for( byte iOld = 0; iOld < val.Count; ++iOld )
				{
					if( valSet.Remove( old[iOld] ) )
					{
					}
					else
					{
						removedList.Add( iOld );
					}
				}

				m_writer.Write( (byte)removedList.Count );
				foreach( var r in removedList )
				{
					m_writer.Write( r );
				}

				m_writer.Write( (byte)valSet.Count );
				foreach( var v in valSet )
				{
					fn( v );
					//op("test", default, v);
					//BitConverter.GetBytes(v);
					byte[] b = App.ConvertPOD( v );
					m_writer.Write( b );
				}


			}
			else
			{
				m_writer.Write( false );
			}

			return val;
		}

		void Proc( float v )
		{

		}

		public IList<float> opSpecific( string name, IList<float> old, IList<float> val, Action<float> fn )
		{
			return opSpecific( name, old, val, Proc );
		}

		void Proc( int v )
		{

		}

		public IList<int> opSpecific( string name, IList<int> old, IList<int> val, Action<int> fn )
		{
			return opSpecific( name, old, val, Proc );
		}

		public IList<T> opIDelta<T>( string name, IList<T> old, IList<T> val ) where T : IDelta
		{
			if( !object.ReferenceEquals( old, val ) )
			{
				m_writer.Write( true );

				var valSet = val.ToHashSet();

				var removedList = new List<byte>();

				for( byte iOld = 0; iOld < val.Count; ++iOld )
				{
					if( valSet.Remove( old[iOld] ) )
					{
					}
					else
					{
						removedList.Add( iOld );
					}
				}

				m_writer.Write( (byte)removedList.Count );
				foreach( var r in removedList )
				{
					m_writer.Write( r );
				}

				m_writer.Write( (byte)valSet.Count );
				foreach( var v in valSet )
				{
					v.DeltaFull( null, this );
				}


			}
			else
			{
				m_writer.Write( false );
			}

			return val;
		}

		*/
	}


	public class DORecv : DeltaOps
	{

		BinaryReader m_reader;

		public DORecv( Stream stream )
		{
			m_reader = new BinaryReader( stream, Encoding.UTF8, true );
		}


		public ulong op( string name, ulong old, ulong value )
		{
			return m_reader.ReadUInt64();
		}

		public int op( string name, int old, int value )
		{
			return m_reader.ReadInt32();
		}

		public float op( string name, float old, float value )
		{
			return m_reader.ReadSingle();
		}

		public NearZero op( string name, NearZero old, NearZero value )
		{
			var enc = m_reader.ReadByte();

			value.decodSqr(enc);

			return value;
		}

		public object op( string name, object old, object value )
		{
			var objChanged = m_reader.ReadBoolean();
			if( objChanged )
			{
				if( value is Obj vObj )
				{
					vObj.DeltaFull( old as Obj, this );

					return old;
				}
			}

			return old;
		}

		// @@@@ EXPENSIVE
		public IList<T> op<T>( string name, IList<T> old, IList<T> val )
		{
			var hasChanges = m_reader.ReadBoolean();
			if( hasChanges )
			{
				var removed = m_reader.ReadByte();
				for( var i = removed; i <= 0; --i )
				{
					val.RemoveAt(i);
				}

				var added = m_reader.ReadByte();
				for( var i = added; i <= 0; --i )
				{
					var code = Type.GetTypeCode( typeof( T ) );
					var size = App.s_byteSizes[(int)code];
					byte[] buf = m_reader.ReadBytes( size );

					var v = (T)App.ConvertPOD<T>( buf );
					val.Add( v );
				}

				return val;
			}

			return val;
		}

		public IList<T> opIDelta<T>( string name, IList<T> old, IList<T> val ) where T : IDelta
		{
			var hasChanges = m_reader.ReadBoolean();
			if( hasChanges )
			{
			}

			return val;
		}

	}

	public interface IDelta
	{
		public void DeltaFull( Obj old, net.DeltaOps ops );
	}

	public partial class Obj : IDelta
	{
		public virtual void DeltaFull( Obj old, net.DeltaOps ops )
		{
		}
	}


	/*
	public interface IDOChanged
	{
		void onChanged( string name, bool modified, ulong old, ulong value );
		void onChanged( string name, bool modified, int old, int value );
		void onChanged( string name, bool modified, NearZero old, NearZero value );
		void onChanged( string name, bool modified, object old, object value );
		void onChanged<T>( string name, bool modified, IList<T> old, IList<T> value );
	}
	*/

	public class DOChanged //: DeltaOps, IDOChanged
	{
		/*
		public T op<T>( string name, T old, T value, Action<string, bool, T, T> fn )
		{
			if( !old.Equals( value ) )
			{
				fn( name, true, old, value );
			}
			else
			{
				fn( name, false, old, value );
			}

			return value;
		}

		public T opRef<T>( string name, T old, T value, Action<string, bool, T, T> fn )
		{
			if( !object.ReferenceEquals( old, value ) )
			{
				fn( name, true, old, value );
			}
			else
			{
				fn( name, false, old, value );
			}

			return value;
		}
		//*/

		/*
		public ulong op( string name, ulong old, ulong value )
		{
			return op<ulong>( name, old, value, onChanged );
		}

		public int op( string name, int old, int value )
		{
			return op<int>( name, old, value, onChanged );
		}

		public NearZero op( string name, NearZero old, NearZero value )
		{
			return op<NearZero>( name, old, value, onChanged );
		}

		public object op( string name, object old, object value )
		{
			return opRef<object>( name, old, value, onChanged );
		}

		public IList<T> op<T>( string name, IList<T> old, IList<T> value )
		{
			return opRef<IList<T>>( name, old, value, onChanged );
		}
		// */

		/*
		public virtual void onChanged( string name, bool modified, ulong old, ulong value )
		{
			throw new NotImplementedException();
		}

		public virtual void onChanged( string name, bool modified, int old, int value )
		{
			throw new NotImplementedException();
		}

		public virtual void onChanged( string name, bool modified, NearZero old, NearZero value )
		{
			throw new NotImplementedException();
		}

		public virtual void onChanged( string name, bool modified, object old, object value )
		{
			throw new NotImplementedException();
		}

		public virtual void onChanged<T>( string name, bool modified, IList<T> old, IList<T> value )
		{
			throw new NotImplementedException();
		}
		// */
	}


	/*
switch(code)
{
	case TypeCode.Empty:
	break;

	case TypeCode.Object:
	break;

	case TypeCode.DBNull:
	break;

	case TypeCode.Boolean:
	break;

	case TypeCode.Char:
	break;

	case TypeCode.SByte:
	break;

	case TypeCode.Byte:
	break;

	case TypeCode.Int16:
	break;

	case TypeCode.UInt16:
	break;

	case TypeCode.Int32:
	break;

	case TypeCode.UInt32:
	break;

	case TypeCode.Int64:
	break;

	case TypeCode.UInt64:
	break;

	case TypeCode.Single:
	break;

	case TypeCode.Double:
	break;

	case TypeCode.Decimal:
	break;

	case TypeCode.DateTime:
	break;

	case TypeCode.String:
	break;

}
	*/


}
