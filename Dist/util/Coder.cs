using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ArithmeticCoder
{

	public class coder
	{
		#region private members

		#region private members used by the coder/decoder methods

		/*
		 * These four variables define the current state of the arithmetic
		 * coder/decoder.  They are assumed to be 16 bits long.  Note that
		 * by declaring them as short ints, they will actually be 16 bits
		 * on most 80X86 and 680X0 machines, as well as VAXen.
		 */
		private ushort code;  /* The present input code value       */
		private ushort low;   /* Start of the current code range    */
		private ushort high;  /* End of the current code range      */
		private ulong underflow_bits; /* Number of underflow bits pending   */

		#endregion

		#region members used by the bit I/O methods        

		private System.IO.MemoryStream output_buffer; /* This is the output i/o buffer    */

		private System.IO.MemoryStream input_buffer;  /* This is the input i/o buffer     */


		private byte current_output_byte;        /* Pointer to current output byte */

		private byte current_input_byte;        /* Pointer to current input byte   */

		private byte output_mask;                /* During output, this byte    */
		/* contains the mask that is   */
		/* applied to the output byte  */
		/* if the output bit is a 1    */
		private long input_bytes_left;           /* During input, these three   */
		private ushort input_bits_left;          /* variables keep track of the */
		/* input state.                */
		/* Note that there is          */
		/* a possibility the decoder   */
		/* can legitimately ask for    */
		/* more bits even after the    */
		/* entire stream has been      */
		/* sucked dry.                 */

		#endregion

		#endregion

		#region struct - symbol coding chars and probabilties

		/*
		 * A symbol can either be represented as an int, or as a pair of
		 * counts on a scale.  This structures gives a standard way of
		 * defining it as a pair of counts.
		 */

		/*
		 * This struct is used by the caller to initialize the coding/decoding alphabet
		 * and the statistical probalilities of encountering a given symbol. It represents the 
		 * statistical model of the input data.
		 */
		public struct Symbol
		{
			#region public members

			public char c;
			public ushort low;
			public ushort high;

			#endregion

			#region constructor           

			public Symbol( char ch, ushort lval, ushort hval )
			{
				c = ch;
				low = lval;
				high = hval;
			}

			#endregion

			#region properties

			public ushort scale
			{
				get
				{
					return coder.scale;
				}
			}

			#endregion
		}

		#endregion

		/*
		 * This is a the probability table for the symbol set used.
		 * Each symbols has a low and high range,
		 * and a fixed total count.
		 */
		#region character lookup table

		/*
		 * The scale is the always equal to the count of codes in the list                                    
		 */
		private static ushort scale = 0;

		/*
		 * This list represents the entire aplhabet used by the coder 
		 * to encode and decode symbols
		 */
		private static List<Symbol> codes;

		#endregion

		#region constructor

		public coder( List<Symbol> cds )
		{
			coder.codes = new List<Symbol>();
			foreach( Symbol s in cds )
				coder.codes.Add( s );
		}

		#endregion

		#region properties

		public List<Symbol> Characters
		{
			get
			{
				return coder.codes;
			}
		}

		public ushort Scale
		{
			get
			{
				return coder.scale;
			}
			set
			{
				coder.scale = value;
			}
		}

		#endregion

		#region methods

		#region compress/inflate functions

		/*
		 * This is the compress routine.  It shows the basic algorithm for
		 * the compression programs used in this article.  First, an input
		 * characters is loaded.  The modeling routines are called to
		 * convert the character to a symbol, which has a high, low and
		 * range.  Finally, the arithmetic coder module is called to
		 * output the symbols to the bit stream.
		 */
		public System.IO.MemoryStream compress( string input )
		{
			int i;
			char c;
			Symbol s = new Symbol();

			initialize_output_bitstream();
			initialize_arithmetic_encoder();
			for( i = 0; i < input.ToCharArray().Length; i++ )
			{
				c = input.ToCharArray()[i];
				convert_int_to_symbol( c, ref s );
				encode_symbol( s );
				if( c == '\0' )
					break;
			}
			flush_arithmetic_encoder();
			flush_output_bitstream();
			return output_buffer;
		}

		/*
		 * This expansion routine demonstrates the basic algorithm used for
		 * decompression in this article.  It first goes to the modeling
		 * module and gets the scale for the current context.  (Note that
		 * the scale is fixed here, since this is not an adaptive model).
		 * It then asks the arithmetic decoder to give a high and low
		 * value for the current input number scaled to match the current
		 * range.  Finally, it asks the modeling unit to convert the
		 * high and low values to a symbol.
		 */
		public string expand( System.IO.MemoryStream compressed_data, int size )
		{
			char c;
			ushort count;
			string retval = "";

			Symbol s = new Symbol();

			initialize_arithmetic_decoder( ref compressed_data );
			for( int i = 0; ; )
			{
				count = get_current_count( s );
				c = convert_symbol_to_int( count, ref s );
				retval += c.ToString();
				if( ++i == size )
					break;
				remove_symbol_from_stream( s );

			}
			return retval;
		}

		/*
		 * This routine is called to convert a character read in from
		 * the text input stream to a low, high, range SYMBOL.  This is
		 * part of the modeling function.  In this case, all that needs
		 * to be done is to find the character in the probabilities table
		 * and then retrieve the low and high values for that symbol.
		 */
		private void convert_int_to_symbol( char c, ref Symbol s )
		{
			int i;
			i = 0;
			for(; ; )
			{
				if( c == codes[i].c )
				{
					s.c = c;
					s.low = codes[i].low;
					s.high = codes[i].high;
					return;
				}
				if( i == ( codes.Count - 1 ) )
					error_exit( "Trying to encode a char not in the table" );
				i++;
			}
		}

		/*
		 * This modeling function is called to convert a symbol value
		 * consisting of a low, high, and range value into a text character
		 * that can be sent to a file.  It does this by finding the symbol
		 * in the probability table that straddles the current range.
		 */
		private char convert_symbol_to_int( ushort count, ref Symbol s )
		{
			int i;

			i = 0;
			for(; ; )
			{
				if( count >= codes[i].low && count < codes[i].high )
				{
					s.low = codes[i].low;
					s.high = codes[i].high;
					return ( codes[i].c );
				}
				if( i == ( codes.Count - 1 ) )
					error_exit( "Failure to decode character" );
				i++;
			}
		}

		/*
		 * A generic error routine.
		 */
		private void error_exit( string message )
		{
			throw new Exception( message );
		}

		#endregion

		#region coder/decoder methods

		/*
		 * This region contains the code needed to accomplish arithmetic
		 * coding of a symbol.  All the routines in this module need
		 * to know in order to accomplish coding is what the probabilities
		 * and scales of the symbol counts are.  This information is
		 * generally passed in a SYMBOL structure.
		 * 
		 * This code was first published by Ian H. Witten, Radford M. Neal,
		 * and John G. Cleary in "Communications of the ACM" in June 1987,
		 * and has been re-implmented in c# here.
		 */

		/*
		 * This routine must be called to initialize the encoding process.
		 * The high register is initialized to all 1s, and it is assumed that
		 * it has an infinite string of 1s to be shifted into the lower bit
		 * positions when needed.
		 */
		private void initialize_arithmetic_encoder()
		{
			low = 0;
			high = 0xffff;
			underflow_bits = 0;
		}

		/*
		 * This routine is called to encode a symbol.  The symbol is passed
		 * in the symbol structure as a low count, a high count, and a range,
		 * instead of the more conventional probability ranges.  The encoding
		 * process takes two steps.  First, the values of high and low are
		 * updated to take into account the range restriction created by the
		 * new symbol.  Then, as many bits as possible are shifted out to
		 * the output stream.  Finally, high and low are stable again and
		 * the routine returns.
		 */
		private void encode_symbol( Symbol s )
		{
			long range;
			/*
			 * These three lines rescale high and low for the new symbol.
			 */
			range = (long)( high - low ) + 1;

			high = Convert.ToUInt16( low + ( ( range * s.high ) / s.scale - 1 ) );

			low = Convert.ToUInt16( low + ( ( range * s.low ) / s.scale ) );
			/*
			 * This loop turns out new bits until high and low are far enough
			 * apart to have stabilized.
			 */
			for(; ; )
			{
				/*
				 * If this test passes, it means that the MSDigits match, and can
				 * be sent to the output stream.
				 */
				if( ( high & 0x8000 ) == ( low & 0x8000 ) )
				{
					output_bit( high & 0x8000 );
					while( underflow_bits > 0 )
					{
						output_bit( ~high & 0x8000 );
						underflow_bits--;
					}
				}
				/*
				 * If this test passes, the numbers are in danger of underflow, because
				 * the MSDigits don't match, and the 2nd digits are just one apart.
				 */
				else if( Convert.ToBoolean( low & 0x4000 ) && !Convert.ToBoolean( high & 0x4000 ) )
				{
					underflow_bits += 1;
					low &= 0x3fff;
					high |= 0x4000;
				}
				else
					return;
				low <<= 1;
				high <<= 1;
				high |= 1;
			}
		}

		/*
		 * At the end of the encoding process, there are still significant
		 * bits left in the high and low registers.  We output two bits,
		 * plus as many underflow bits as are necessary.
		 */
		private void flush_arithmetic_encoder()
		{
			output_bit( low & 0x4000 );
			underflow_bits++;
			while( underflow_bits-- > 0 )
				output_bit( ~low & 0x4000 );
		}

		/*
		 * When decoding, this routine is called to figure out which symbol
		 * is presently waiting to be decoded.  This routine expects to get
		 * the current model scale in the s.scale parameter, and it returns
		 * a count that corresponds to the present floating point code:
		 * 
		 * code = count / s.scale
		 */
		private ushort get_current_count( Symbol s )
		{
			long range;
			ushort count;
			range = (long)( high - low ) + 1;
			count = (ushort)( ( ( (long)( code - low ) + 1 ) * s.scale - 1 ) / range );
			return ( count );
		}

		/*
		 * This routine is called to initialize the state of the arithmetic
		 * decoder.  This involves initializing the high and low registers
		 * to their conventional starting values, plus reading the first
		 * 16 bits from the input stream into the code value.
		 */
		private void initialize_arithmetic_decoder( ref System.IO.MemoryStream stream )
		{
			int i;

			initialize_input_bitstream( ref stream );

			code = 0;
			for( i = 0; i < 16; i++ )
			{
				code <<= 1;
				code += input_bit();
			}
			low = 0;
			high = 0xffff;
		}

		/*
		 * Just figuring out what the present symbol is doesn't remove
		 * it from the input bit stream.  After the character has been
		 * decoded, this routine has to be called to remove it from the
		 * input stream.
		 */
		private void remove_symbol_from_stream( Symbol s )
		{
			long range;

			/*
			 * First, the range is expanded to account for the symbol removal.
			 */
			range = (long)( high - low ) + 1;
			high = Convert.ToUInt16( low + (ushort)( ( range * s.high ) / s.scale - 1 ) );
			low = Convert.ToUInt16( low + (ushort)( ( range * s.low ) / s.scale ) );
			/*
			 * Next, any possible bits are shipped out.
			 */
			for(; ; )
			{
				/*
				 * If the MSDigits match, the bits will be shifted out.
				 */
				if( ( high & 0x8000 ) == ( low & 0x8000 ) )
				{
				}
				/*
				 * Else, if underflow is threatining, shift out the 2nd MSDigit.
				 */
				else if( ( low & 0x4000 ) == 0x4000 && ( high & 0x4000 ) == 0 )
				{
					code ^= 0x4000;
					low &= 0x3fff;
					high |= 0x4000;
				}
				/*
				 * Otherwise, nothing can be shifted out, so I return.
				 */
				else
					return;
				low <<= 1;
				high <<= 1;
				high |= 1;
				code <<= 1;
				code += input_bit();
			}
		}

		#endregion

		#region bit I/O methods

		/*
		 * 
		 * This region contains a set of bit oriented i/o routines
		 * used for arithmetic data compression.  The important fact to
		 * know about these is that the first bit is stored in the msb of
		 * the first byte of the output, like you might expect.
		 * 
		 * 
		 * Both input and output maintain a local buffer so that they only
		 * have to do block reads and writes.  This is done in spite of the
		 * fact that C standard I/O does the same thing.  If these
		 * routines are ever ported to assembly language the buffering
		 * will come in handy.
		 */

		/*
		 * This routine is called once to initialze the output bitstream.
		 * All it has to do is set up the current_byte pointer, clear out
		 * all the bits in my current output byte, and set the output mask
		 * so it will set the proper bit next time a bit is output.
		 */
		private void initialize_output_bitstream()
		{
			output_buffer = new MemoryStream();
			current_output_byte = 0;
			output_mask = 0x80;
		}

		/*
		 * The output bit routine just has to set a bit in the current byte
		 * if requested to.  After that, it updates the mask.  If the mask
		 * shows that the current byte is filled up, it is time to go to the
		 * next character in the buffer.  If the next character is past the
		 * end of the buffer, it is time to flush the buffer.
		 */
		private void output_bit( int bit )
		{
			if( Convert.ToBoolean( bit ) )
				current_output_byte |= output_mask;
			output_mask >>= 1;
			if( output_mask == 0 )
			{
				output_mask = 0x80;
				output_buffer.WriteByte( current_output_byte );
				current_output_byte = 0;
			}
		}

		/*
		 * When the encoding is done, there will still be some bits and
		 * sitting in the output buffer waiting to be sent out.  This routine
		 * is called to clean things up at that point.
		 */
		private void flush_output_bitstream()
		{
			output_buffer.WriteByte( current_output_byte );
		}

		/*
		 * Bit oriented input is set up so that the next time the input_bit
		 * routine is called, it will trigger the read of a new block.  That
		 * is why input_bits_left is set to 0.
		 */
		private void initialize_input_bitstream( ref System.IO.MemoryStream stream_to_decode )
		{
			input_buffer = stream_to_decode;
			input_bits_left = 0;
			input_bytes_left = stream_to_decode.Length;
			current_input_byte = 0;
			stream_to_decode.Seek( 0, SeekOrigin.Begin );
		}

		/*
		 * This routine reads bits in from a stream.  The bits are all sitting
		 * in a buffer, and this code pulls them out, one at a time.  When the
		 * buffer has been emptied, that triggers a new stream read, and the
		 * pointers are reset.  This routine is set up to allow for dummy
		 * bits/bytes to be read in after the end of of the stream is reached.
		 * All be zero(0) valued bits however. This is because we have to keep 
		 * feeding bits into the pipeline to be decoded so that the old stuff 
		 * that is 16 bits upstream can be pushed out.                                               
		 */
		private ushort input_bit()
		{
			if( input_bits_left == 0 )
			{
				if( input_bytes_left > 0 )
					current_input_byte = Convert.ToByte( input_buffer.ReadByte() );
				else
					return 0;
				input_bytes_left--;
				input_bits_left = 8;
			}
			input_bits_left--;
			return Convert.ToUInt16( ( current_input_byte >> input_bits_left ) & 1 );
		}

		/*
		 * When monitoring compression ratios, we need to know how many
		 * bytes have been output so far.  This routine takes care of telling
		 * how many bytes have been output, including pending bytes that
		 * haven't actually been written out.
		 */
		private ulong bit_output()
		{
			ulong total;

			total = Convert.ToUInt32( output_buffer.Position );
			total += underflow_bits / 8;
			return ( total );
		}

		/*
		 * When monitoring compression ratios, we need to know how many bits
		 * have been read in so far.  This routine tells how many bytes have
		 * been read in, excluding bytes that are pending in the buffer.
		 */
		private long bit_input()
		{
			return ( input_buffer.Position - input_bytes_left + 1 );
		}

		#endregion

		#endregion
	}
}