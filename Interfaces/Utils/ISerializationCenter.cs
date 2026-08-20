namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   public interface ISerializationCenter
   {
      string Serialize<T>(T toSerialize) where T : notnull;

      T Deserialize<T>(string toDeserialize) where T : notnull;
   }
}
