using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class QrCodeUnitTests
   {
      [TestMethod]
      /*
       * Generating a QR code from a non-empty string returns a non-empty square matrix.
      */
      public void Case01_Generate_ReturnsSquareMatrix()
      {
         // Given / When
         bool[,] matrix = QrCode.Generate("https://github.com/YassinLokhat/Upsilon.Apps.Passkey");

         // Then
         _ = matrix.GetLength(0).Should().BeGreaterThan(0);
         _ = matrix.GetLength(0).Should().Be(matrix.GetLength(1));
      }

      [TestMethod]
      /*
       * The matrix dimension must respect the QR standard : 17 + 4 * version, and
       * the exposed matrix must be that exact size.
      */
      public void Case02_Dimension_MatchesVersion()
      {
         // Given / When
         QrCode qrCode = new("Some data to encode", ErrorCorrection.M);

         // Then
         _ = qrCode.QRCodeVersion.Should().BeInRange(1, 40);
         _ = qrCode.QRCodeDimension.Should().Be(17 + (4 * qrCode.QRCodeVersion));
         _ = qrCode.QRCodeMatrix.GetLength(0).Should().Be(qrCode.QRCodeDimension);
         _ = qrCode.QRCodeMatrix.GetLength(1).Should().Be(qrCode.QRCodeDimension);
      }

      [TestMethod]
      /*
       * The top-left finder pattern must be present : its outer corner is dark and
       * the separator right after the 7-module pattern is light.
      */
      public void Case03_FinderPattern_TopLeft()
      {
         // Given / When
         QrCode qrCode = new("finder pattern check", ErrorCorrection.H);

         // Then
         _ = qrCode.QRCodeMatrix[0, 0].Should().BeTrue();
         _ = qrCode.QRCodeMatrix[0, 6].Should().BeTrue();
         _ = qrCode.QRCodeMatrix[0, 7].Should().BeFalse();
      }

      [TestMethod]
      /*
       * A null or empty string data segment is rejected.
      */
      public void Case04_Constructor_BlankString()
      {
         // Given / When
         Action fromNull = new(() => _ = new QrCode((string)null, ErrorCorrection.M));
         Action fromEmpty = new(() => _ = new QrCode(string.Empty, ErrorCorrection.M));

         // Then
         _ = fromNull.Should().Throw<ArgumentException>();
         _ = fromEmpty.Should().Throw<ArgumentException>();
      }

      [TestMethod]
      /*
       * A null or empty byte data segment is rejected.
      */
      public void Case05_Constructor_EmptyBytes()
      {
         // Given / When
         Action fromNull = new(() => _ = new QrCode((byte[])null, ErrorCorrection.M));
         Action fromEmpty = new(() => _ = new QrCode(Array.Empty<byte>(), ErrorCorrection.M));

         // Then
         _ = fromNull.Should().Throw<ArgumentException>();
         _ = fromEmpty.Should().Throw<ArgumentException>();
      }

      [TestMethod]
      /*
       * An out-of-range error correction level is rejected.
      */
      public void Case06_InvalidErrorCorrection()
      {
         // Given / When
         Action act = new(() => _ = QrCode.Generate("data", (ErrorCorrection)99));

         // Then
         _ = act.Should().Throw<ArgumentException>();
      }

      [TestMethod]
      /*
       * The ECI assignment value must stay within the -1..999999 range.
      */
      public void Case07_EciAssignValueRange()
      {
         // Given
         QrCode qrCode = new("eci", ErrorCorrection.M);

         // When
         Action tooLow = new(() => qrCode.ECIAssignValue = -2);
         Action tooHigh = new(() => qrCode.ECIAssignValue = 1_000_000);
         Action valid = new(() => qrCode.ECIAssignValue = 123456);

         // Then
         _ = tooLow.Should().Throw<ArgumentException>();
         _ = tooHigh.Should().Throw<ArgumentException>();
         _ = valid.Should().NotThrow();
      }

      [TestMethod]
      /*
       * Encoding the same data with the same settings must be deterministic.
      */
      public void Case08_Generation_IsDeterministic()
      {
         // Given
         const string data = "deterministic payload 12345";

         // When
         bool[,] first = QrCode.Generate(data, ErrorCorrection.Q);
         bool[,] second = QrCode.Generate(data, ErrorCorrection.Q);

         // Then
         _ = first.GetLength(0).Should().Be(second.GetLength(0));

         for (int row = 0; row < first.GetLength(0); row++)
         {
            for (int col = 0; col < first.GetLength(1); col++)
            {
               _ = first[row, col].Should().Be(second[row, col]);
            }
         }
      }

      [TestMethod]
      /*
       * More data requires a bigger QR code (higher version, larger matrix).
      */
      public void Case09_MoreData_LargerMatrix()
      {
         // Given
         QrCode small = new("Hi", ErrorCorrection.H);
         QrCode large = new(new string('A', 200), ErrorCorrection.H);

         // Then
         _ = large.QRCodeVersion.Should().BeGreaterThan(small.QRCodeVersion);
         _ = large.QRCodeDimension.Should().BeGreaterThan(small.QRCodeDimension);
      }
   }
}
