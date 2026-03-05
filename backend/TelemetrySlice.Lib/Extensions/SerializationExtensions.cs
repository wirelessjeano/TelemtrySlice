using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelemetrySlice.Lib.Extensions;

public static class SerializationExtensions
{
    public static byte[] ToByteArray(this object obj)  
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj,
            new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});
    }  
  
    public static T FromByteArray<T>(this byte[] byteArray) where T : class  
    {
        return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(byteArray))!;
    }  
}