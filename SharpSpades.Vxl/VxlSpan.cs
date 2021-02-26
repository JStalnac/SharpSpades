namespace SharpSpades.Vxl
{
    public record VxlSpan(byte Length, byte AirStart, byte ColorStart, byte ColorEnd, byte[] Colors);
}
