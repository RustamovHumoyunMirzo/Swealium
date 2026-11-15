namespace Swealium.Graphics;

// Portable PBM/PGM/PPM P1-P6 support, including ASCII comments and 16-bit binary samples.
internal static class NetpbmDecoder
{
    internal static Bitmap Decode(byte[] bytes,ImageLoadOptions options)
    {
        int kind=bytes[1]-'0',position=2;
        int width=Number(),height=Number();Bitmap.ValidateSize(width,height,options.MaximumPixels);
        int maximum=kind is 1 or 4?1:Number();if(maximum<1||maximum>65535)throw new InvalidDataException("Invalid Netpbm sample range.");
        byte[] rgba=new byte[checked(width*height*4)];
        if(kind>=4)
        {
            if(position>=bytes.Length||!White(bytes[position]))throw new InvalidDataException("Missing Netpbm data separator.");
            if(bytes[position++]==13&&position<bytes.Length&&bytes[position]==10)position++;
            long expected=kind==4?((long)width+7)/8*height:(long)width*height*(kind==6?3:1)*(maximum>255?2:1);
            if(expected>bytes.Length-position)throw new InvalidDataException("Truncated Netpbm pixels.");
        }
        for(int y=0;y<height;y++)
        {
            int row=position;
            for(int x=0;x<width;x++)
            {
                int i=(y*width+x)*4;byte r,g,b;
                if(kind==4)r=g=b=(bytes[row+(x>>3)]&(128>>(x&7)))==0?(byte)255:(byte)0;
                else if(kind==1){int sample=Number();if(sample>1)throw new InvalidDataException("Invalid PBM pixel.");r=g=b=sample==0?(byte)255:(byte)0;}
                else if(kind is 3 or 6){r=Sample();g=Sample();b=Sample();}
                else r=g=b=Sample();
                rgba[i]=r;rgba[i+1]=g;rgba[i+2]=b;rgba[i+3]=255;
            }
            if(kind==4)position+=checked((width+7)/8);
        }
        return Bitmap.FromPixelsCore(width,height,rgba,options.MaximumPixels);
        int Number()
        {
            while(position<bytes.Length)
            {
                if(White(bytes[position])){position++;continue;}
                if(bytes[position]=='#'){while(position<bytes.Length&&bytes[position]!=10)position++;continue;}break;
            }
            int start=position;long value=0;
            while(position<bytes.Length&&bytes[position] is >=(byte)'0' and <=(byte)'9')
            {value=value*10+bytes[position++]-'0';if(value>int.MaxValue)throw new InvalidDataException("Netpbm integer overflow.");}
            if(start==position)throw new InvalidDataException("Missing Netpbm header/sample value.");return (int)value;
        }
        byte Sample()
        {
            int value=kind<4?Number():maximum>255?(bytes[position++]<<8)|bytes[position++]:bytes[position++];
            if(value>maximum)throw new InvalidDataException("Netpbm sample exceeds its range.");return (byte)(((long)value*255+maximum/2)/maximum);
        }
    }
    internal static bool White(byte value)=>value is 9 or 10 or 11 or 12 or 13 or 32;
}
