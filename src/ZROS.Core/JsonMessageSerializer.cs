using System;
using System.Text;
using System.Text.Json;

namespace ZROS.Core
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public byte[] Serialize<T>(T message) where T : IMessage
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            string json = JsonSerializer.Serialize(message, _options);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data) where T : IMessage
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty.", nameof(data));
            string json = Encoding.UTF8.GetString(data);
            var result = JsonSerializer.Deserialize<T>(json, _options);
            if (result == null)
                throw new InvalidOperationException($"Failed to deserialize message of type {typeof(T).Name}.");
            return result;
        }
    }
}
