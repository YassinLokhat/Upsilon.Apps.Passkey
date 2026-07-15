using Upsilon.Apps.Passkey.Interfaces.Enums;

namespace Upsilon.Apps.Passkey.Interfaces.Utils
{
   /// <summary>
   /// Describes how a passkey is stretched into key material (the slow hash).
   /// These parameters are stored, unencrypted, in the database header so a file
   /// can always be reopened with the exact settings it was written with. They
   /// are not secret: tampering with them only prevents the correct key from
   /// being derived, it never weakens data that is already encrypted.
   /// </summary>
   public sealed class KdfParameters
   {
      /// <summary>
      /// The version of the stretching scheme, allowing future migrations.
      /// </summary>
      public int Version { get; set; }

      /// <summary>
      /// The key-derivation function used.
      /// </summary>
      public KdfAlgorithm Algorithm { get; set; }

      /// <summary>
      /// The number of KDF iterations (work factor).
      /// </summary>
      public int Iterations { get; set; }

      /// <summary>
      /// The length, in bytes, of the derived key material.
      /// </summary>
      public int OutputLength { get; set; }

      /// <summary>
      /// The random, per-database salt (Base64-encoded) mixed into the KDF, so that two
      /// databases stretch the same passkey into different key material. It is generated
      /// once when the database is created and then stored, unencrypted, in the header;
      /// a salt is not secret, it only has to be unique and stable for a given file.
      /// </summary>
      public string Salt { get; set; } = string.Empty;
   }
}
