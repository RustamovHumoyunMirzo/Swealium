using System.Buffers.Binary;
namespace Swealium.Graphics;

// Extract a TTC face into standalone SFNT data. CoreText-backed decoders do not
// consistently accept collection data/index pairs, unlike FreeType/DirectWrite.
internal static class FontCollectionData
{
    private readonly record struct Table(uint Tag,int Offset,int Length);
    internal static byte[]? Extract(ReadOnlySpan<byte> source,int faceIndex)
    {
        if(source.Length<4||BinaryPrimitives.ReadUInt32BigEndian(source)!=0x74746366)
        {
            if(faceIndex!=0)throw new InvalidDataException("A standalone font has only face index zero.");
            return null;
        }
        if(source.Length<12)throw Invalid();
        uint version=Read32(source,4),count=Read32(source,8);
        if(version is not (0x00010000 or 0x00020000)||count==0||count>(source.Length-12)/4||(uint)faceIndex>=count)throw Invalid();
        uint face=Read32(source,12+faceIndex*4);if(face>source.Length-12)throw Invalid();
        int start=(int)face,n=BinaryPrimitives.ReadUInt16BigEndian(source.Slice(start+4));
        if(n==0||n>4095||n>(source.Length-start-12)/16)throw Invalid();
        var tables=new List<Table>(n);int size=12;
        for(int i=0;i<n;i++)
        {
            int record=start+12+i*16;uint tag=Read32(source,record),offset=Read32(source,record+8),length=Read32(source,record+12);
            if(offset>source.Length||length>source.Length-offset)throw Invalid();
            if(tag==0x44534947)continue; // A collection's DSIG cannot authenticate an extracted/repacked face.
            if(length>64*1024*1024)throw Invalid();
            tables.Add(new(tag,(int)offset,(int)length));
            size=checked(size+16+(((int)length+3)&~3));
            if(size>64*1024*1024)throw Invalid();
        }
        n=tables.Count;if(n==0)throw Invalid();
        var result=new byte[size];source.Slice(start,4).CopyTo(result);
        Write16(result,4,n);int power=1,selector=0;while(power*2<=n){power*=2;selector++;}
        Write16(result,6,power*16);Write16(result,8,selector);Write16(result,10,n*16-power*16);
        int destination=12+n*16,head=-1;
        for(int i=0;i<n;i++)
        {
            var table=tables[i];int record=12+i*16;source.Slice(table.Offset,table.Length).CopyTo(result.AsSpan(destination));
            if(table.Tag==0x68656164)
            {
                if(table.Length<12)throw Invalid();head=destination;Write32(result,head+8,0);
            }
            Write32(result,record,table.Tag);Write32(result,record+4,Checksum(result.AsSpan(destination,(table.Length+3)&~3)));
            Write32(result,record+8,(uint)destination);Write32(result,record+12,(uint)table.Length);destination+=(table.Length+3)&~3;
        }
        if(head>=0)Write32(result,head+8,unchecked(0xB1B0AFBAu-Checksum(result)));
        return result;
    }
    private static uint Checksum(ReadOnlySpan<byte> bytes)
    {uint sum=0;for(int i=0;i<bytes.Length;i+=4)sum=unchecked(sum+Read32(bytes,i));return sum;}
    private static uint Read32(ReadOnlySpan<byte> bytes,int at)=>BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(at,4));
    private static void Write32(Span<byte> bytes,int at,uint value)=>BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(at,4),value);
    private static void Write16(Span<byte> bytes,int at,int value)=>BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(at,2),(ushort)value);
    private static InvalidDataException Invalid()=>new("Invalid font collection data or face index.");
}
