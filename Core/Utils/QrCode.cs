using System.Text;

namespace Upsilon.Apps.Passkey.Core.Utils;

/// <summary>
/// QR error-correction level (ISO/IEC 18004). The GUI uses <see cref="H"/> so a
/// partially obscured on-screen code still scans.
/// </summary>
public enum ErrorCorrection
{
   /// <summary>~7% recovery.</summary>
   L,
   /// <summary>~15% recovery.</summary>
   M,
   /// <summary>~25% recovery.</summary>
   Q,
   /// <summary>~30% recovery (GUI default).</summary>
   H,
}

internal enum EncodingMode
{
   Terminator,
   Numeric,
   AlphaNumeric,
   Append,
   Byte,
   FNC1First,
   Unknown6,
   ECI,
   Kanji,
   FNC1Second,
   Unknown10,
   Unknown11,
   Unknown12,
   Unknown13,
   Unknown14,
   Unknown15,
}
#pragma warning disable CA1814, CA1819
/// <summary>
/// In-process QR encoder (ISO/IEC 18004), no NuGet. Used to put identifiers and
/// passwords on screen; it does not talk to the network.
/// </summary>
public class QrCode
{
   private byte[][] _dataSegArray = [];
   private int _encodedDataBits;
   private int _maxCodewords;
   private int _maxDataCodewords;
   private int _maxDataBits;
   private int _errCorrCodewords;
   private int _blocksGroup1;
   private int _dataCodewordsGroup1;
   private int _blocksGroup2;
   private int _dataCodewordsGroup2;
   private int _maskCode;
   private EncodingMode[] _encodingSegMode = [];
   private byte[] _codewordsArray = [];
   private int _codewordsPtr;
   private uint _bitBuffer;
   private int _bitBufferLen;
   private byte[,] _baseMatrix = new byte[0, 0];
   private byte[,] _maskMatrix = new byte[0, 0];
   private byte[,] _resultMatrix = new byte[0, 0];
   internal static readonly byte[]?[] AlignmentPositionArray = [null, null, [6, 18], [6, 22], [6, 26], [6, 30], [6, 34], [6, 22, 38], [6, 24, 42], [6, 26, 46], [6, 28, 50], [6, 30, 54], [6, 32/*0x20*/, 58], [6, 34, 62], [6, 26, 46, 66], [6, 26, 48/*0x30*/, 70], [6, 26, 50, 74], [6, 30, 54, 78], [6, 30, 56, 82], [6, 30, 58, 86], [6, 34, 62, 90], [6, 28, 50, 72, 94], [6, 26, 50, 74, 98], [6, 30, 54, 78, 102], [6, 28, 54, 80/*0x50*/, 106], [6, 32/*0x20*/, 58, 84, 110], [6, 30, 58, 86, 114], [6, 34, 62, 90, 118], [6, 26, 50, 74, 98, 122], [6, 30, 54, 78, 102, 126], [6, 26, 52, 78, 104, 130], [6, 30, 56, 82, 108, 134], [6, 34, 60, 86, 112/*0x70*/, 138], [6, 30, 58, 86, 114, 142], [6, 34, 62, 90, 118, 146], [6, 30, 54, 78, 102, 126, 150], [6, 24, 50, 76, 102, 128/*0x80*/, 154], [6, 28, 54, 80/*0x50*/, 106, 132, 158], [6, 32/*0x20*/, 58, 84, 110, 136, 162], [6, 26, 54, 82, 110, 138, 166], [6, 30, 58, 86, 114, 142, 170]];
   internal static readonly int[] MaxCodewordsArray = [0, 26, 44, 70, 100, 134, 172, 196, 242, 292, 346, 404, 466, 532, 581, 655, 733, 815, 901, 991, 1085, 1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185, 2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706];
   internal static readonly byte[] EncodingTable = [45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 36, 45, 45, 45, 37, 38, 45, 45, 45, 45, 39, 40, 45, 41, 42, 43, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 44, 45, 45, 45, 45, 45, 45, 10, 11, 12, 13, 14, 15, 16/*0x10*/, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31/*0x1F*/, 32/*0x20*/, 33, 34, 35, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45];
   internal const int BLOCKS_GROUP1 = 0;
   internal const int DATA_CODEWORDS_GROUP1 = 1;
   internal const int BLOCKS_GROUP2 = 2;
   internal const int DATA_CODEWORDS_GROUP2 = 3;
   internal static readonly byte[,] ECBlockInfo = new byte[160/*0xA0*/, 4] { { 1, 19, 0, 0 }, { 1, 16/*0x10*/, 0, 0 }, { 1, 13, 0, 0 }, { 1, 9, 0, 0 }, { 1, 34, 0, 0 }, { 1, 28, 0, 0 }, { 1, 22, 0, 0 }, { 1, 16/*0x10*/, 0, 0 }, { 1, 55, 0, 0 }, { 1, 44, 0, 0 }, { 2, 17, 0, 0 }, { 2, 13, 0, 0 }, { 1, 80/*0x50*/, 0, 0 }, { 2, 32/*0x20*/, 0, 0 }, { 2, 24, 0, 0 }, { 4, 9, 0, 0 }, { 1, 108, 0, 0 }, { 2, 43, 0, 0 }, { 2, 15, 2, 16/*0x10*/}, { 2, 11, 2, 12 }, { 2, 68, 0, 0 }, { 4, 27, 0, 0 }, { 4, 19, 0, 0 }, { 4, 15, 0, 0 }, { 2, 78, 0, 0 }, { 4, 31/*0x1F*/, 0, 0 }, { 2, 14, 4, 15 }, { 4, 13, 1, 14 }, { 2, 97, 0, 0 }, { 2, 38, 2, 39 }, { 4, 18, 2, 19 }, { 4, 14, 2, 15 }, { 2, 116, 0, 0 }, { 3, 36, 2, 37 }, { 4, 16/*0x10*/, 4, 17 }, { 4, 12, 4, 13 }, { 2, 68, 2, 69 }, { 4, 43, 1, 44 }, { 6, 19, 2, 20 }, { 6, 15, 2, 16/*0x10*/}, { 4, 81, 0, 0 }, { 1, 50, 4, 51 }, { 4, 22, 4, 23 }, { 3, 12, 8, 13 }, { 2, 92, 2, 93 }, { 6, 36, 2, 37 }, { 4, 20, 6, 21 }, { 7, 14, 4, 15 }, { 4, 107, 0, 0 }, { 8, 37, 1, 38 }, { 8, 20, 4, 21 }, { 12, 11, 4, 12 }, { 3, 115, 1, 116 }, { 4, 40, 5, 41 }, { 11, 16/*0x10*/, 5, 17 }, { 11, 12, 5, 13 }, { 5, 87, 1, 88 }, { 5, 41, 5, 42 }, { 5, 24, 7, 25 }, { 11, 12, 7, 13 }, { 5, 98, 1, 99 }, { 7, 45, 3, 46 }, { 15, 19, 2, 20 }, { 3, 15, 13, 16/*0x10*/}, { 1, 107, 5, 108 }, { 10, 46, 1, 47 }, { 1, 22, 15, 23 }, { 2, 14, 17, 15 }, { 5, 120, 1, 121 }, { 9, 43, 4, 44 }, { 17, 22, 1, 23 }, { 2, 14, 19, 15 }, { 3, 113, 4, 114 }, { 3, 44, 11, 45 }, { 17, 21, 4, 22 }, { 9, 13, 16/*0x10*/, 14 }, { 3, 107, 5, 108 }, { 3, 41, 13, 42 }, { 15, 24, 5, 25 }, { 15, 15, 10, 16/*0x10*/}, { 4, 116, 4, 117 }, { 17, 42, 0, 0 }, { 17, 22, 6, 23 }, { 19, 16/*0x10*/, 6, 17 }, { 2, 111, 7, 112/*0x70*/}, { 17, 46, 0, 0 }, { 7, 24, 16/*0x10*/, 25 }, { 34, 13, 0, 0 }, { 4, 121, 5, 122 }, { 4, 47, 14, 48/*0x30*/}, { 11, 24, 14, 25 }, { 16/*0x10*/, 15, 14, 16/*0x10*/}, { 6, 117, 4, 118 }, { 6, 45, 14, 46 }, { 11, 24, 16/*0x10*/, 25 }, { 30, 16/*0x10*/, 2, 17 }, { 8, 106, 4, 107 }, { 8, 47, 13, 48/*0x30*/}, { 7, 24, 22, 25 }, { 22, 15, 13, 16/*0x10*/}, { 10, 114, 2, 115 }, { 19, 46, 4, 47 }, { 28, 22, 6, 23 }, { 33, 16/*0x10*/, 4, 17 }, { 8, 122, 4, 123 }, { 22, 45, 3, 46 }, { 8, 23, 26, 24 }, { 12, 15, 28, 16/*0x10*/}, { 3, 117, 10, 118 }, { 3, 45, 23, 46 }, { 4, 24, 31/*0x1F*/, 25 }, { 11, 15, 31/*0x1F*/, 16/*0x10*/}, { 7, 116, 7, 117 }, { 21, 45, 7, 46 }, { 1, 23, 37, 24 }, { 19, 15, 26, 16/*0x10*/}, { 5, 115, 10, 116 }, { 19, 47, 10, 48/*0x30*/}, { 15, 24, 25, 25 }, { 23, 15, 25, 16/*0x10*/}, { 13, 115, 3, 116 }, { 2, 46, 29, 47 }, { 42, 24, 1, 25 }, { 23, 15, 28, 16/*0x10*/}, { 17, 115, 0, 0 }, { 10, 46, 23, 47 }, { 10, 24, 35, 25 }, { 19, 15, 35, 16/*0x10*/}, { 17, 115, 1, 116 }, { 14, 46, 21, 47 }, { 29, 24, 19, 25 }, { 11, 15, 46, 16/*0x10*/}, { 13, 115, 6, 116 }, { 14, 46, 23, 47 }, { 44, 24, 7, 25 }, { 59, 16/*0x10*/, 1, 17 }, { 12, 121, 7, 122 }, { 12, 47, 26, 48/*0x30*/}, { 39, 24, 14, 25 }, { 22, 15, 41, 16/*0x10*/}, { 6, 121, 14, 122 }, { 6, 47, 34, 48/*0x30*/}, { 46, 24, 10, 25 }, { 2, 15, 64/*0x40*/, 16/*0x10*/}, { 17, 122, 4, 123 }, { 29, 46, 14, 47 }, { 49, 24, 10, 25 }, { 24, 15, 46, 16/*0x10*/}, { 4, 122, 18, 123 }, { 13, 46, 32/*0x20*/, 47 }, { 48/*0x30*/, 24, 14, 25 }, { 42, 15, 32/*0x20*/, 16/*0x10*/}, { 20, 117, 4, 118 }, { 40, 47, 7, 48/*0x30*/}, { 43, 24, 22, 25 }, { 10, 15, 67, 16/*0x10*/}, { 19, 118, 6, 119 }, { 18, 47, 31/*0x1F*/, 48/*0x30*/}, { 34, 24, 34, 25 }, { 20, 15, 61, 16/*0x10*/} };
   private static readonly byte[] _generator7 = [87, 229, 146, 149, 238, 102, 21];
   private static readonly byte[] _generator10 = [251, 67, 46, 61, 118, 70, 64 /*0x40*/, 94, 32 /*0x20*/, 45];
   private static readonly byte[] _generator13 = [74, 152, 176 /*0xB0*/, 100, 86, 100, 106, 104, 130, 218, 206, 140, 78];
   private static readonly byte[] _generator15 = [8, 183, 61, 91, 202, 37, 51, 58, 58, 237, 140, 124, 5, 99, 105];
   private static readonly byte[] _generator16 = [120, 104, 107, 109, 102, 161, 76, 3, 91, 191, 147, 169, 182, 194, 225, 120];
   private static readonly byte[] _generator17 = [43, 139, 206, 78, 43, 239, 123, 206, 214, 147, 24, 99, 150, 39, 243, 163, 136];
   private static readonly byte[] _generator18 = [215, 234, 158, 94, 184, 97, 118, 170, 79, 187, 152, 148, 252, 179, 5, 98, 96 /*0x60*/, 153];
   private static readonly byte[] _generator20 = [17, 60, 79, 50, 61, 163, 26, 187, 202, 180, 221, 225, 83, 239, 156, 164, 212, 212, 188, 190];
   private static readonly byte[] _generator22 = [210, 171, 247, 242, 93, 230, 14, 109, 221, 53, 200, 74, 8, 172, 98, 80 /*0x50*/, 219, 134, 160 /*0xA0*/, 105, 165, 231];
   private static readonly byte[] _generator24 = [229, 121, 135, 48 /*0x30*/, 211, 117, 251, 126, 159, 180, 169, 152, 192 /*0xC0*/, 226, 228, 218, 111, 0, 117, 232, 87, 96 /*0x60*/, 227, 21];
   private static readonly byte[] _generator26 = [173, 125, 158, 2, 103, 182, 118, 17, 145, 201, 111, 28, 165, 53, 161, 21, 245, 142, 13, 102, 48 /*0x30*/, 227, 153, 145, 218, 70];
   private static readonly byte[] _generator28 = [168, 223, 200, 104, 224 /*0xE0*/, 234, 108, 180, 110, 190, 195, 147, 205, 27, 232, 201, 21, 43, 245, 87, 42, 195, 212, 119, 242, 37, 9, 123];
   private static readonly byte[] _generator30 = [41, 173, 145, 152, 216, 31 /*0x1F*/, 179, 182, 50, 48 /*0x30*/, 110, 86, 239, 96 /*0x60*/, 222, 125, 42, 173, 226, 193, 224 /*0xE0*/, 130, 156, 37, 251, 216, 238, 40, 192 /*0xC0*/, 180];
   private static readonly byte[] _generator32 = [10, 6, 106, 190, 249, 167, 4, 67, 209, 138, 138, 32 /*0x20*/, 242, 123, 89, 27, 120, 185, 80 /*0x50*/, 156, 38, 60, 171, 60, 28, 222, 80 /*0x50*/, 52, 254, 185, 220, 241];
   private static readonly byte[] _generator34 = [111, 77, 146, 94, 26, 21, 108, 19, 105, 94, 113, 193, 86, 140, 163, 125, 58, 158, 229, 239, 218, 103, 56, 70, 114, 61, 183, 129, 167, 13, 98, 62, 129, 51];
   private static readonly byte[] _generator36 = [200, 183, 98, 16 /*0x10*/, 172, 31 /*0x1F*/, 246, 234, 60, 152, 115, 0, 167, 152, 113, 248, 238, 107, 18, 63 /*0x3F*/, 218, 37, 87, 210, 105, 177, 120, 74, 121, 196, 117, 251, 113, 233, 30, 120];
   private static readonly byte[] _generator40 = [59, 116, 79, 161, 252, 98, 128 /*0x80*/, 205, 128 /*0x80*/, 161, 247, 57, 163, 56, 235, 106, 53, 26, 187, 174, 226, 104, 170, 7, 175, 35, 181, 114, 88, 41, 47, 163, 125, 134, 72, 20, 232, 53, 35, 15];
   private static readonly byte[] _generator42 = [250, 103, 221, 230, 25, 18, 137, 231, 0, 3, 58, 242, 221, 191, 110, 84, 230, 8, 188, 106, 96 /*0x60*/, 147, 15, 131, 139, 34, 101, 223, 39, 101, 213, 199, 237, 254, 201, 123, 171, 162, 194, 117, 50, 96 /*0x60*/    ];
   private static readonly byte[] _generator44 = [190, 7, 61, 121, 71, 246, 69, 55, 168, 188, 89, 243, 191, 25, 72, 123, 9, 145, 14, 247, 1, 238, 44, 78, 143, 62, 224 /*0xE0*/, 126, 118, 114, 68, 163, 52, 194, 217, 147, 204, 169, 37, 130, 113, 102, 73, 181];
   private static readonly byte[] _generator46 = [112 /*0x70*/, 94, 88, 112 /*0x70*/, 253, 224 /*0xE0*/, 202, 115, 187, 99, 89, 5, 54, 113, 129, 44, 58, 16 /*0x10*/, 135, 216, 169, 211, 36, 1, 4, 96 /*0x60*/, 60, 241, 73, 104, 234, 8, 249, 245, 119, 174, 52, 25, 157, 224 /*0xE0*/, 43, 202, 223, 19, 82, 15];
   private static readonly byte[] _generator48 = [228, 25, 196, 130, 211, 146, 60, 24, 251, 90, 39, 102, 240 /*0xF0*/, 61, 178, 63 /*0x3F*/, 46, 123, 115, 18, 221, 111, 135, 160 /*0xA0*/, 182, 205, 107, 206, 95, 150, 120, 184, 91, 21, 247, 156, 140, 238, 191, 11, 94, 227, 84, 50, 163, 39, 34, 108];
   private static readonly byte[] _generator50 = [232, 125, 157, 161, 164, 9, 118, 46, 209, 99, 203, 193, 35, 3, 209, 111, 195, 242, 203, 225, 46, 13, 32 /*0x20*/, 160 /*0xA0*/, 126, 209, 130, 160 /*0xA0*/, 242, 215, 242, 75, 77, 42, 189, 32 /*0x20*/, 113, 65, 124, 69, 228, 114, 235, 175, 124, 170, 215, 232, 133, 205];
   private static readonly byte[] _generator52 = [116, 50, 86, 186, 50, 220, 251, 89, 192 /*0xC0*/, 46, 86, 127 /*0x7F*/, 124, 19, 184, 233, 151, 215, 22, 14, 59, 145, 37, 242, 203, 134, 254, 89, 190, 94, 59, 65, 124, 113, 100, 233, 235, 121, 22, 76, 86, 97, 39, 242, 200, 220, 101, 33, 239, 254, 116, 51];
   private static readonly byte[] _generator54 = [183, 26, 201, 84, 210, 221, 113, 21, 46, 65, 45, 50, 238, 184, 249, 225, 102, 58, 209, 218, 109, 165, 26, 95, 184, 192 /*0xC0*/, 52, 245, 35, 254, 238, 175, 172, 79, 123, 25, 122, 43, 120, 108, 215, 80 /*0x50*/, 128 /*0x80*/, 201, 235, 8, 153, 59, 101, 31 /*0x1F*/, 198, 76, 31 /*0x1F*/, 156];
   private static readonly byte[] _generator56 = [106, 120, 107, 157, 164, 216, 112 /*0x70*/, 116, 2, 91, 248, 163, 36, 201, 202, 229, 6, 144 /*0x90*/, 254, 155, 135, 208 /*0xD0*/, 170, 209, 12, 139, 127 /*0x7F*/, 142, 182, 249, 177, 174, 190, 28, 10, 85, 239, 184, 101, 124, 152, 206, 96 /*0x60*/, 23, 163, 61, 27, 196, 247, 151, 154, 202, 207, 20, 61, 10];
   private static readonly byte[] _generator58 = [82, 116, 26, 247, 66, 27, 62, 107, 252, 182, 200, 185, 235, 55, 251, 242, 210, 144 /*0x90*/, 154, 237, 176 /*0xB0*/, 141, 192 /*0xC0*/, 248, 152, 249, 206, 85, 253, 142, 65, 165, 125, 23, 24, 30, 122, 240 /*0xF0*/, 214, 6, 129, 218, 29, 145, 127 /*0x7F*/, 134, 206, 245, 117, 29, 41, 63 /*0x3F*/, 159, 142, 233, 125, 148, 123];
   private static readonly byte[] _generator60 = [107, 140, 26, 12, 9, 141, 243, 197, 226, 197, 219, 45, 211, 101, 219, 120, 28, 181, 127 /*0x7F*/, 6, 100, 247, 2, 205, 198, 57, 115, 219, 101, 109, 160 /*0xA0*/, 82, 37, 38, 238, 49, 160 /*0xA0*/, 209, 121, 86, 11, 124, 30, 181, 84, 25, 194, 87, 65, 102, 190, 220, 70, 27, 209, 16 /*0x10*/, 89, 7, 33, 240 /*0xF0*/    ];
   private static readonly byte[] _generator62 = [65, 202, 113, 98, 71, 223, 248, 118, 214, 94, 0, 122, 37, 23, 2, 228, 58, 121, 7, 105, 135, 78, 243, 118, 70, 76, 223, 89, 72, 50, 70, 111, 194, 17, 212, 126, 181, 35, 221, 117, 235, 11, 229, 149, 147, 123, 213, 40, 115, 6, 200, 100, 26, 246, 182, 218, 127 /*0x7F*/, 215, 36, 186, 110, 106];
   private static readonly byte[] _generator64 = [45, 51, 175, 9, 7, 158, 159, 49, 68, 119, 92, 123, 177, 204, 187, 254, 200, 78, 141, 149, 119, 26, 127 /*0x7F*/, 53, 160 /*0xA0*/, 93, 199, 212, 29, 24, 145, 156, 208 /*0xD0*/, 150, 218, 209, 4, 216, 91, 47, 184, 146, 47, 140, 195, 195, 125, 242, 238, 63 /*0x3F*/, 99, 108, 140, 230, 242, 31 /*0x1F*/, 204, 11, 178, 243, 217, 156, 213, 231];
   private static readonly byte[] _generator66 = [5, 118, 222, 180, 136, 136, 162, 51, 46, 117, 13, 215, 81, 17, 139, 247, 197, 171, 95, 173, 65, 137, 178, 68, 111, 95, 101, 41, 72, 214, 169, 197, 95, 7, 44, 154, 77, 111, 236, 40, 121, 143, 63 /*0x3F*/, 87, 80 /*0x50*/, 253, 240 /*0xF0*/, 126, 217, 77, 34, 232, 106, 50, 168, 82, 76, 146, 67, 106, 171, 25, 132, 93, 45, 105];
   private static readonly byte[] _generator68 = [247, 159, 223, 33, 224 /*0xE0*/, 93, 77, 70, 90, 160 /*0xA0*/, 32 /*0x20*/, 254, 43, 150, 84, 101, 190, 205, 133, 52, 60, 202, 165, 220, 203, 151, 93, 84, 15, 84, 253, 173, 160 /*0xA0*/, 89, 227, 52, 199, 97, 95, 231, 52, 177, 41, 125, 137, 241, 166, 225, 118, 2, 54, 32 /*0x20*/, 82, 215, 175, 198, 43, 238, 235, 27, 101, 184, 127 /*0x7F*/, 3, 5, 8, 163, 238];
   internal static readonly byte[]?[] GenArray = [_generator7, null, null, _generator10, null, null, _generator13, null, _generator15, _generator16, _generator17, _generator18, null, _generator20, null, _generator22, null, _generator24, null, _generator26, null, _generator28, null, _generator30, null, _generator32, null, _generator34, null, _generator36, null, null, null, _generator40, null, _generator42, null, _generator44, null, _generator46, null, _generator48, null, _generator50, null, _generator52, null, _generator54, null, _generator56, null, _generator58, null, _generator60, null, _generator62, null, _generator64, null, _generator66, null, _generator68];
   internal static readonly byte[] ExpToInt = [1, 2, 4, 8, 16 /*0x10*/, 32 /*0x20*/, 64 /*0x40*/, 128 /*0x80*/, 29, 58, 116, 232, 205, 135, 19, 38, 76, 152, 45, 90, 180, 117, 234, 201, 143, 3, 6, 12, 24, 48 /*0x30*/, 96 /*0x60*/, 192 /*0xC0*/, 157, 39, 78, 156, 37, 74, 148, 53, 106, 212, 181, 119, 238, 193, 159, 35, 70, 140, 5, 10, 20, 40, 80 /*0x50*/, 160 /*0xA0*/, 93, 186, 105, 210, 185, 111, 222, 161, 95, 190, 97, 194, 153, 47, 94, 188, 101, 202, 137, 15, 30, 60, 120, 240 /*0xF0*/, 253, 231, 211, 187, 107, 214, 177, 127 /*0x7F*/, 254, 225, 223, 163, 91, 182, 113, 226, 217, 175, 67, 134, 17, 34, 68, 136, 13, 26, 52, 104, 208 /*0xD0*/, 189, 103, 206, 129, 31 /*0x1F*/, 62, 124, 248, 237, 199, 147, 59, 118, 236, 197, 151, 51, 102, 204, 133, 23, 46, 92, 184, 109, 218, 169, 79, 158, 33, 66, 132, 21, 42, 84, 168, 77, 154, 41, 82, 164, 85, 170, 73, 146, 57, 114, 228, 213, 183, 115, 230, 209, 191, 99, 198, 145, 63 /*0x3F*/, 126, 252, 229, 215, 179, 123, 246, 241, byte.MaxValue, 227, 219, 171, 75, 150, 49, 98, 196, 149, 55, 110, 220, 165, 87, 174, 65, 130, 25, 50, 100, 200, 141, 7, 14, 28, 56, 112 /*0x70*/, 224 /*0xE0*/, 221, 167, 83, 166, 81, 162, 89, 178, 121, 242, 249, 239, 195, 155, 43, 86, 172, 69, 138, 9, 18, 36, 72, 144 /*0x90*/, 61, 122, 244, 245, 247, 243, 251, 235, 203, 139, 11, 22, 44, 88, 176 /*0xB0*/, 125, 250, 233, 207, 131, 27, 54, 108, 216, 173, 71, 142, 1, 2, 4, 8, 16 /*0x10*/, 32 /*0x20*/, 64 /*0x40*/, 128 /*0x80*/, 29, 58, 116, 232, 205, 135, 19, 38, 76, 152, 45, 90, 180, 117, 234, 201, 143, 3, 6, 12, 24, 48 /*0x30*/, 96 /*0x60*/, 192 /*0xC0*/, 157, 39, 78, 156, 37, 74, 148, 53, 106, 212, 181, 119, 238, 193, 159, 35, 70, 140, 5, 10, 20, 40, 80 /*0x50*/, 160 /*0xA0*/, 93, 186, 105, 210, 185, 111, 222, 161, 95, 190, 97, 194, 153, 47, 94, 188, 101, 202, 137, 15, 30, 60, 120, 240 /*0xF0*/, 253, 231, 211, 187, 107, 214, 177, 127 /*0x7F*/, 254, 225, 223, 163, 91, 182, 113, 226, 217, 175, 67, 134, 17, 34, 68, 136, 13, 26, 52, 104, 208 /*0xD0*/, 189, 103, 206, 129, 31 /*0x1F*/, 62, 124, 248, 237, 199, 147, 59, 118, 236, 197, 151, 51, 102, 204, 133, 23, 46, 92, 184, 109, 218, 169, 79, 158, 33, 66, 132, 21, 42, 84, 168, 77, 154, 41, 82, 164, 85, 170, 73, 146, 57, 114, 228, 213, 183, 115, 230, 209, 191, 99, 198, 145, 63 /*0x3F*/, 126, 252, 229, 215, 179, 123, 246, 241, byte.MaxValue, 227, 219, 171, 75, 150, 49, 98, 196, 149, 55, 110, 220, 165, 87, 174, 65, 130, 25, 50, 100, 200, 141, 7, 14, 28, 56, 112 /*0x70*/, 224 /*0xE0*/, 221, 167, 83, 166, 81, 162, 89, 178, 121, 242, 249, 239, 195, 155, 43, 86, 172, 69, 138, 9, 18, 36, 72, 144 /*0x90*/, 61, 122, 244, 245, 247, 243, 251, 235, 203, 139, 11, 22, 44, 88, 176 /*0xB0*/, 125, 250, 233, 207, 131, 27, 54, 108, 216, 173, 71, 142, 1];
   internal static readonly byte[] IntToExp = [0, 0, 1, 25, 2, 50, 26, 198, 3, 223, 51, 238, 27, 104, 199, 75, 4, 100, 224 /*0xE0*/, 14, 52, 141, 239, 129, 28, 193, 105, 248, 200, 8, 76, 113, 5, 138, 101, 47, 225, 36, 15, 33, 53, 147, 142, 218, 240 /*0xF0*/, 18, 130, 69, 29, 181, 194, 125, 106, 39, 249, 185, 201, 154, 9, 120, 77, 228, 114, 166, 6, 191, 139, 98, 102, 221, 48 /*0x30*/, 253, 226, 152, 37, 179, 16 /*0x10*/, 145, 34, 136, 54, 208 /*0xD0*/, 148, 206, 143, 150, 219, 189, 241, 210, 19, 92, 131, 56, 70, 64 /*0x40*/, 30, 66, 182, 163, 195, 72, 126, 110, 107, 58, 40, 84, 250, 133, 186, 61, 202, 94, 155, 159, 10, 21, 121, 43, 78, 212, 229, 172, 115, 243, 167, 87, 7, 112 /*0x70*/, 192 /*0xC0*/, 247, 140, 128 /*0x80*/, 99, 13, 103, 74, 222, 237, 49, 197, 254, 24, 227, 165, 153, 119, 38, 184, 180, 124, 17, 68, 146, 217, 35, 32 /*0x20*/, 137, 46, 55, 63 /*0x3F*/, 209, 91, 149, 188, 207, 205, 144 /*0x90*/, 135, 151, 178, 220, 252, 190, 97, 242, 86, 211, 171, 20, 42, 93, 158, 132, 60, 57, 83, 71, 109, 65, 162, 31 /*0x1F*/, 45, 67, 216, 183, 123, 164, 118, 196, 23, 73, 236, 127 /*0x7F*/, 12, 111, 246, 108, 161, 59, 82, 41, 157, 85, 170, 251, 96 /*0x60*/, 134, 177, 187, 204, 62, 90, 203, 89, 95, 176 /*0xB0*/, 156, 169, 160 /*0xA0*/, 81, 11, 245, 22, 235, 122, 117, 44, 215, 79, 174, 213, 233, 230, 231, 173, 232, 116, 214, 244, 234, 168, 80 /*0x50*/, 88, 175];
   internal static readonly int[] FormatInfoArray = [21522, 20773, 24188, 23371, 17913, 16590, 20375, 19104, 30660, 29427, 32170, 30877, 26159, 25368, 27713, 26998, 5769, 5054, 7399, 6608, 1890, 597, 3340, 2107, 13663, 12392, 16177, 14854, 9396, 8579, 11994, 11245];
   internal static readonly int[,] FormatInfoOne = new int[15, 2] { { 0, 8 }, { 1, 8 }, { 2, 8 }, { 3, 8 }, { 4, 8 }, { 5, 8 }, { 7, 8 }, { 8, 8 }, { 8, 7 }, { 8, 5 }, { 8, 4 }, { 8, 3 }, { 8, 2 }, { 8, 1 }, { 8, 0 } };
   internal static readonly int[,] FormatInfoTwo = new int[15, 2] { { 8, -1 }, { 8, -2 }, { 8, -3 }, { 8, -4 }, { 8, -5 }, { 8, -6 }, { 8, -7 }, { 8, -8 }, { -7, 8 }, { -6, 8 }, { -5, 8 }, { -4, 8 }, { -3, 8 }, { -2, 8 }, { -1, 8 } };
   internal static readonly int[] VersionCodeArray = [31892, 34236, 39577, 42195, 48118, 51042, 55367, 58893, 63784, 68472, 70749, 76311, 79154, 84390, 87683, 92361, 96236, 102084, 102881, 110507, 110734, 117786, 119615, 126325, 127568, 133589, 136944, 141498, 145311, 150283, 152622, 158308, 161089, 167017];
   internal const byte White = 0;
   internal const byte Black = 1;
   internal const byte NonData = 2;
   internal const byte Fixed = 4;
   internal const byte DataWhite = 0;
   internal const byte DataBlack = 1;
   internal const byte FormatWhite = 2;
   internal const byte FormatBlack = 3;
   internal const byte FixedWhite = 6;
   internal const byte FixedBlack = 7;
   internal static readonly byte[,] FinderPatternTopLeft = new byte[9, 9] { { 7, 7, 7, 7, 7, 7, 7, 6, 2 }, { 7, 6, 6, 6, 6, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 6, 6, 6, 6, 7, 6, 2 }, { 7, 7, 7, 7, 7, 7, 7, 6, 2 }, { 6, 6, 6, 6, 6, 6, 6, 6, 2 }, { 2, 2, 2, 2, 2, 2, 2, 2, 2 } };
   internal static readonly byte[,] FinderPatternTopRight = new byte[9, 8] { { 6, 7, 7, 7, 7, 7, 7, 7 }, { 6, 7, 6, 6, 6, 6, 6, 7 }, { 6, 7, 6, 7, 7, 7, 6, 7 }, { 6, 7, 6, 7, 7, 7, 6, 7 }, { 6, 7, 6, 7, 7, 7, 6, 7 }, { 6, 7, 6, 6, 6, 6, 6, 7 }, { 6, 7, 7, 7, 7, 7, 7, 7 }, { 6, 6, 6, 6, 6, 6, 6, 6 }, { 2, 2, 2, 2, 2, 2, 2, 2 } };
   internal static readonly byte[,] FinderPatternBottomLeft = new byte[8, 9] { { 6, 6, 6, 6, 6, 6, 6, 6, 7 }, { 7, 7, 7, 7, 7, 7, 7, 6, 2 }, { 7, 6, 6, 6, 6, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 7, 7, 7, 6, 7, 6, 2 }, { 7, 6, 6, 6, 6, 6, 7, 6, 2 }, { 7, 7, 7, 7, 7, 7, 7, 6, 2 } };
   internal static readonly byte[,] AlignmentPattern = new byte[5, 5] { { 7, 7, 7, 7, 7 }, { 7, 6, 6, 6, 7 }, { 7, 6, 7, 6, 7 }, { 7, 6, 6, 6, 7 }, { 7, 7, 7, 7, 7 } };

   public bool[,] QRCodeMatrix { get; private set; } = new bool[0, 0];

   public int QRCodeVersion { get; private set; }

   public int QRCodeDimension { get; private set; }

   public ErrorCorrection ErrorCorrection
   {
      get;
      set
      {
         field = value is >= ErrorCorrection.L and <= ErrorCorrection.H ? value : throw new ArgumentException("Error correction is invalid. Must be L, M, Q or H. Default is M");
      }
   } = ErrorCorrection.H;

   public int ECIAssignValue
   {
      get;
      set
      {
         field = value is >= -1 and <= 999999 ? value : throw new ArgumentException("ECI Assignment Value must be 0-999999 or -1 for none");
      }
   } = -1;

   /// <summary>
   /// Encodes <paramref name="data"/> as a module matrix
   /// (<see langword="true"/> = dark). Default correction is <see cref="ErrorCorrection.H"/>.
   /// </summary>
   public static bool[,] Generate(string data, ErrorCorrection errorCorrection = ErrorCorrection.H)
   {
      return new QrCode(data, errorCorrection).QRCodeMatrix;
   }

   public QrCode(string stringDataSegment, ErrorCorrection errorCorrection)
   {
      if (string.IsNullOrEmpty(stringDataSegment))
      {
         throw new ArgumentException("String data segment is null or missing");
      }

      ErrorCorrection = errorCorrection;
      _ = _encode([Encoding.UTF8.GetBytes(stringDataSegment)]);
   }

   public QrCode(byte[] singleDataSeg, ErrorCorrection errorCorrection)
   {
      if (singleDataSeg is null || singleDataSeg.Length == 0)
      {
         throw new ArgumentException("Single data segment argument is null or empty");
      }

      ErrorCorrection = errorCorrection;
      _ = _encode([singleDataSeg]);
   }

   private bool[,] _encode(byte[][] dataSegments)
   {
      if (dataSegments == null || dataSegments.Length == 0)
      {
         throw new ArgumentException("Data segments argument is null or empty");
      }

      QRCodeVersion = 0;
      QRCodeDimension = 0;

      int totalDataLength = 0;
      for (int segmentIndex = 0; segmentIndex < dataSegments.Length; ++segmentIndex)
      {
         byte[] segment = dataSegments[segmentIndex];
         if (segment == null)
         {
            dataSegments[segmentIndex] = [];
         }
         else
         {
            totalDataLength += segment.Length;
         }
      }

      if (totalDataLength == 0)
      {
         throw new ArgumentException("There is no data to encode.");
      }

      _dataSegArray = dataSegments;

      _initialization();
      _encodeData();
      _calculateErrorCorrection();
      _interleaveBlocks();
      _buildBaseMatrix();
      _loadMatrixWithData();
      _selectBestMask();
      _addFormatInformation();

      QRCodeMatrix = new bool[QRCodeDimension, QRCodeDimension];
      for (int row = 0; row < QRCodeDimension; ++row)
      {
         for (int col = 0; col < QRCodeDimension; ++col)
         {
            if ((_resultMatrix[row, col] & 1) != 0)
            {
               QRCodeMatrix[row, col] = true;
            }
         }
      }

      return QRCodeMatrix;
   }

   private void _initialization()
   {
      _encodingSegMode = new EncodingMode[_dataSegArray.Length];
      _encodedDataBits = 0;

      if (ECIAssignValue >= 0)
      {
         _encodedDataBits = ECIAssignValue > sbyte.MaxValue ? (ECIAssignValue > 16383 /*0x3FFF*/ ? 28 : 20) : 12;
      }

      for (int segmentIndex = 0; segmentIndex < _dataSegArray.Length; ++segmentIndex)
      {
         byte[] segment = _dataSegArray[segmentIndex];
         int segmentLength = segment.Length;

         EncodingMode encodingMode = EncodingMode.Numeric;
         for (int i = 0; i < segmentLength; ++i)
         {
            int encodedValue = EncodingTable[segment[i]];
            if (encodedValue >= 10)
            {
               if (encodedValue < 45)
               {
                  encodingMode = EncodingMode.AlphaNumeric;
               }
               else
               {
                  encodingMode = EncodingMode.Byte;
                  break;
               }
            }
         }

         int segmentBits = 4;
         switch (encodingMode)
         {
            case EncodingMode.Numeric:
               segmentBits += 10 * (segmentLength / 3);
               if (segmentLength % 3 == 1)
               {
                  segmentBits += 4;
               }
               else if (segmentLength % 3 == 2)
               {
                  segmentBits += 7;
               }

               break;
            case EncodingMode.AlphaNumeric:
               segmentBits += 11 * (segmentLength / 2);
               if ((segmentLength & 1) != 0)
               {
                  segmentBits += 6;
               }

               break;
            case EncodingMode.Byte:
               segmentBits += 8 * segmentLength;
               break;
         }

         _encodingSegMode[segmentIndex] = encodingMode;
         _encodedDataBits += segmentBits;
      }

      int characterCountBits = 0;
      for (QRCodeVersion = 1; QRCodeVersion <= 40; ++QRCodeVersion)
      {
         QRCodeDimension = 17 + (4 * QRCodeVersion);
         _setDataCodewordsLength();

         characterCountBits = 0;
         for (int segmentIndex = 0; segmentIndex < _encodingSegMode.Length; ++segmentIndex)
         {
            characterCountBits += _dataLengthBits(_encodingSegMode[segmentIndex]);
         }

         if (_encodedDataBits + characterCountBits <= _maxDataBits)
         {
            break;
         }
      }

      if (QRCodeVersion > 40)
      {
         throw new InvalidOperationException("Input data string is too long");
      }

      _encodedDataBits += characterCountBits;
   }

   private void _encodeData()
   {
      _codewordsArray = new byte[_maxCodewords];
      _codewordsPtr = 0;
      _bitBuffer = 0U;
      _bitBufferLen = 0;

      if (ECIAssignValue >= 0)
      {
         _saveBitsToCodewordsArray((int)EncodingMode.ECI, 4);
         if (ECIAssignValue <= sbyte.MaxValue)
         {
            _saveBitsToCodewordsArray(ECIAssignValue, 8);
         }
         else if (ECIAssignValue <= 16383 /*0x3FFF*/)
         {
            _saveBitsToCodewordsArray((ECIAssignValue >> 8) | 128 /*0x80*/, 8);
            _saveBitsToCodewordsArray(ECIAssignValue & byte.MaxValue, 8);
         }
         else
         {
            _saveBitsToCodewordsArray((ECIAssignValue >> 16) /*0x10*/ | 192 /*0xC0*/, 8);
            _saveBitsToCodewordsArray((ECIAssignValue >> 8) & byte.MaxValue, 8);
            _saveBitsToCodewordsArray(ECIAssignValue & byte.MaxValue, 8);
         }
      }

      for (int segmentIndex = 0; segmentIndex < _dataSegArray.Length; ++segmentIndex)
      {
         byte[] segment = _dataSegArray[segmentIndex];
         int segmentLength = segment.Length;
         EncodingMode mode = _encodingSegMode[segmentIndex];

         _saveBitsToCodewordsArray((int)mode, 4);
         _saveBitsToCodewordsArray(segmentLength, _dataLengthBits(mode));

         switch (mode)
         {
            case EncodingMode.Numeric:
               int numericTripleEnd = segmentLength / 3 * 3;
               for (int i = 0; i < numericTripleEnd; i += 3)
               {
                  _saveBitsToCodewordsArray(
                     (100 * EncodingTable[segment[i]]) + (10 * EncodingTable[segment[i + 1]]) + EncodingTable[segment[i + 2]], 10);
               }

               if (segmentLength - numericTripleEnd == 1)
               {
                  _saveBitsToCodewordsArray(EncodingTable[segment[numericTripleEnd]], 4);
               }
               else if (segmentLength - numericTripleEnd == 2)
               {
                  _saveBitsToCodewordsArray(
                     (10 * EncodingTable[segment[numericTripleEnd]]) + EncodingTable[segment[numericTripleEnd + 1]], 7);
               }

               break;
            case EncodingMode.AlphaNumeric:
               int alphaPairEnd = segmentLength / 2 * 2;
               for (int i = 0; i < alphaPairEnd; i += 2)
               {
                  _saveBitsToCodewordsArray(
                     (45 * EncodingTable[segment[i]]) + EncodingTable[segment[i + 1]], 11);
               }

               if (segmentLength - alphaPairEnd == 1)
               {
                  _saveBitsToCodewordsArray(EncodingTable[segment[alphaPairEnd]], 6);
               }

               break;
            case EncodingMode.Byte:
               for (int i = 0; i < segmentLength; ++i)
               {
                  _saveBitsToCodewordsArray(segment[i], 8);
               }

               break;
         }
      }

      if (_encodedDataBits < _maxDataBits)
      {
         _saveBitsToCodewordsArray(0, _maxDataBits - _encodedDataBits < 4 ? _maxDataBits - _encodedDataBits : 4);
      }

      if (_bitBufferLen > 0)
      {
         _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);
      }

      int padCount = _maxDataCodewords - _codewordsPtr;
      for (int i = 0; i < padCount; ++i)
      {
         _codewordsArray[_codewordsPtr + i] = (i & 1) == 0 ? (byte)236 : (byte)17;
      }
   }

   private void _saveBitsToCodewordsArray(int data, int bitCount)
   {
      _bitBuffer |= (uint)(data << (32 /*0x20*/ - _bitBufferLen - bitCount));
      for (_bitBufferLen += bitCount; _bitBufferLen >= 8; _bitBufferLen -= 8)
      {
         _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);
         _bitBuffer <<= 8;
      }
   }

   private void _calculateErrorCorrection()
   {
      byte[] generator = GenArray[_errCorrCodewords - 7] ?? [];
      byte[] block = new byte[Math.Max(_dataCodewordsGroup1, _dataCodewordsGroup2) + _errCorrCodewords];

      int blockDataLength = _dataCodewordsGroup1;
      int polynomialLength = blockDataLength + _errCorrCodewords;
      int readIndex = 0;
      int writeIndex = _maxDataCodewords;
      int totalBlocks = _blocksGroup1 + _blocksGroup2;

      for (int blockIndex = 0; blockIndex < totalBlocks; ++blockIndex)
      {
         if (blockIndex == _blocksGroup1)
         {
            blockDataLength = _dataCodewordsGroup2;
            polynomialLength = blockDataLength + _errCorrCodewords;
         }

         Array.Copy(_codewordsArray, readIndex, block, 0, blockDataLength);
         Array.Clear(block, blockDataLength, _errCorrCodewords);
         readIndex += blockDataLength;

         _polynomialDivision(block, polynomialLength, generator, _errCorrCodewords);

         Array.Copy(block, blockDataLength, _codewordsArray, writeIndex, _errCorrCodewords);
         writeIndex += _errCorrCodewords;
      }
   }

   private static void _polynomialDivision(
     byte[] polynomial,
     int polynomialLength,
     byte[] generator,
     int errorCorrectionCodewords)
   {
      int dataLength = polynomialLength - errorCorrectionCodewords;
      for (int i = 0; i < dataLength; ++i)
      {
         if (polynomial[i] != 0)
         {
            int leadTermExp = IntToExp[polynomial[i]];
            for (int j = 0; j < errorCorrectionCodewords; ++j)
            {
               polynomial[i + 1 + j] = (byte)(polynomial[i + 1 + j] ^ (uint)ExpToInt[generator[j] + leadTermExp]);
            }
         }
      }
   }

   private void _interleaveBlocks()
   {
      byte[] interleaved = new byte[_maxCodewords];
      int totalBlocks = _blocksGroup1 + _blocksGroup2;

      int[] blockCursor = new int[totalBlocks];
      for (int blockIndex = 1; blockIndex < totalBlocks; ++blockIndex)
      {
         blockCursor[blockIndex] = blockCursor[blockIndex - 1]
            + (blockIndex <= _blocksGroup1 ? _dataCodewordsGroup1 : _dataCodewordsGroup2);
      }

      int outputIndex = 0;

      int sharedDataCount = _dataCodewordsGroup1 * totalBlocks;
      int currentBlock = 0;
      for (; outputIndex < sharedDataCount; ++outputIndex)
      {
         interleaved[outputIndex] = _codewordsArray[blockCursor[currentBlock]];
         ++blockCursor[currentBlock];
         ++currentBlock;
         if (currentBlock == totalBlocks)
         {
            currentBlock = 0;
         }
      }

      if (_dataCodewordsGroup2 > _dataCodewordsGroup1)
      {
         int currentGroup2Block = _blocksGroup1;
         for (; outputIndex < _maxDataCodewords; ++outputIndex)
         {
            interleaved[outputIndex] = _codewordsArray[blockCursor[currentGroup2Block]];
            ++blockCursor[currentGroup2Block];
            ++currentGroup2Block;
            if (currentGroup2Block == totalBlocks)
            {
               currentGroup2Block = _blocksGroup1;
            }
         }
      }

      blockCursor[0] = _maxDataCodewords;
      for (int blockIndex = 1; blockIndex < totalBlocks; ++blockIndex)
      {
         blockCursor[blockIndex] = blockCursor[blockIndex - 1] + _errCorrCodewords;
      }

      int currentEcBlock = 0;
      for (; outputIndex < _maxCodewords; ++outputIndex)
      {
         interleaved[outputIndex] = _codewordsArray[blockCursor[currentEcBlock]];
         ++blockCursor[currentEcBlock];
         ++currentEcBlock;
         if (currentEcBlock == totalBlocks)
         {
            currentEcBlock = 0;
         }
      }

      _codewordsArray = interleaved;
   }

   private void _loadMatrixWithData()
   {
      int bitIndex = 0;
      int totalBits = 8 * _maxCodewords;
      int row = QRCodeDimension - 1;
      int col = QRCodeDimension - 1;

      int phase = 0;

      while (true)
      {
         if ((_baseMatrix[row, col] & 2) == 0)
         {
            if ((_codewordsArray[bitIndex >> 3] & (1 << (7 - (bitIndex & 7)))) != 0)
            {
               _baseMatrix[row, col] = 1;
            }

            if (++bitIndex == totalBits)
            {
               break;
            }
         }
         else if (col == 6)
         {
            --col;
         }

         switch (phase)
         {
            case 0:
               --col;
               phase = 1;
               continue;
            case 1:
               ++col;
               --row;
               if (row >= 0)
               {
                  phase = 0;
                  continue;
               }
               col -= 2;
               row = 0;
               phase = 2;
               continue;
            case 2:
               --col;
               phase = 3;
               continue;
            case 3:
               ++col;
               ++row;
               if (row < QRCodeDimension)
               {
                  phase = 2;
                  continue;
               }
               col -= 2;
               row = QRCodeDimension - 1;
               phase = 0;
               continue;
            default:
               continue;
         }
      }
   }

   private void _selectBestMask()
   {
      int bestPenalty = int.MaxValue;
      _maskCode = 0;

      for (int maskPattern = 0; maskPattern < 8; ++maskPattern)
      {
         _applyMask(maskPattern);

         int penalty = _penaltyAdjacentModules()
            + _penaltyBlocks()
            + _penaltyFinderLikePatterns()
            + _penaltyDarkProportion();

         if (penalty < bestPenalty)
         {
            bestPenalty = penalty;
            _maskCode = maskPattern;
            _resultMatrix = _maskMatrix;
         }
      }
   }

   private int _penaltyAdjacentModules()
   {
      int penalty = 0;

      for (int row = 0; row < QRCodeDimension; ++row)
      {
         int runLength = 1;
         for (int col = 1; col < QRCodeDimension; ++col)
         {
            if (((_maskMatrix[row, col - 1] ^ _maskMatrix[row, col]) & 1) != 0)
            {
               if (runLength >= 5)
               {
                  penalty += runLength - 2;
               }

               runLength = 0;
            }
            ++runLength;
         }
         if (runLength >= 5)
         {
            penalty += runLength - 2;
         }
      }

      for (int col = 0; col < QRCodeDimension; ++col)
      {
         int runLength = 1;
         for (int row = 1; row < QRCodeDimension; ++row)
         {
            if (((_maskMatrix[row - 1, col] ^ _maskMatrix[row, col]) & 1) != 0)
            {
               if (runLength >= 5)
               {
                  penalty += runLength - 2;
               }

               runLength = 0;
            }
            ++runLength;
         }
         if (runLength >= 5)
         {
            penalty += runLength - 2;
         }
      }

      return penalty;
   }

   private int _penaltyBlocks()
   {
      int penalty = 0;
      for (int row = 1; row < QRCodeDimension; ++row)
      {
         for (int col = 1; col < QRCodeDimension; ++col)
         {
            int topLeft = _maskMatrix[row - 1, col - 1];
            int topRight = _maskMatrix[row - 1, col];
            int bottomLeft = _maskMatrix[row, col - 1];
            int bottomRight = _maskMatrix[row, col];

            if ((topLeft & topRight & bottomLeft & bottomRight & 1) != 0)
            {
               penalty += 3;
            }
            else if (((topLeft | topRight | bottomLeft | bottomRight) & 1) == 0)
            {
               penalty += 3;
            }
         }
      }
      return penalty;
   }

   private int _penaltyFinderLikePatterns()
   {
      int penalty = 0;

      for (int row = 0; row < QRCodeDimension; ++row)
      {
         int afterLastDark = 0;
         for (int col = 0; col < QRCodeDimension; ++col)
         {
            if ((_maskMatrix[row, col] & 1) != 0)
            {
               if (col - afterLastDark >= 4)
               {
                  if (afterLastDark >= 7 && _matchesFinderPatternHorizontally(row, afterLastDark - 7))
                  {
                     penalty += 40;
                  }

                  if (QRCodeDimension - col >= 7 && _matchesFinderPatternHorizontally(row, col))
                  {
                     penalty += 40;
                     col += 6;
                  }
               }
               afterLastDark = col + 1;
            }
         }
         if (QRCodeDimension - afterLastDark >= 4 && afterLastDark >= 7 && _matchesFinderPatternHorizontally(row, afterLastDark - 7))
         {
            penalty += 40;
         }
      }

      for (int col = 0; col < QRCodeDimension; ++col)
      {
         int afterLastDark = 0;
         for (int row = 0; row < QRCodeDimension; ++row)
         {
            if ((_maskMatrix[row, col] & 1) != 0)
            {
               if (row - afterLastDark >= 4)
               {
                  if (afterLastDark >= 7 && _matchesFinderPatternVertically(afterLastDark - 7, col))
                  {
                     penalty += 40;
                  }

                  if (QRCodeDimension - row >= 7 && _matchesFinderPatternVertically(row, col))
                  {
                     penalty += 40;
                     row += 6;
                  }
               }
               afterLastDark = row + 1;
            }
         }
         if (QRCodeDimension - afterLastDark >= 4 && afterLastDark >= 7 && _matchesFinderPatternVertically(afterLastDark - 7, col))
         {
            penalty += 40;
         }
      }

      return penalty;
   }

   private int _penaltyDarkProportion()
   {
      int darkCount = 0;
      for (int row = 0; row < QRCodeDimension; ++row)
      {
         for (int col = 0; col < QRCodeDimension; ++col)
         {
            if ((_maskMatrix[row, col] & 1) != 0)
            {
               ++darkCount;
            }
         }
      }

      double darkRatio = (double)darkCount / Math.Pow(QRCodeDimension, 2);
      return darkRatio > 0.55
         ? (int)(20.0 * (darkRatio - 0.5)) * 10
         : darkRatio < 0.45
            ? (int)(20.0 * (0.5 - darkRatio)) * 10
            : 0;
   }

   private bool _matchesFinderPatternHorizontally(int row, int col)
   {
      return (_maskMatrix[row, col] & ~_maskMatrix[row, col + 1] & _maskMatrix[row, col + 2] & _maskMatrix[row, col + 3] & _maskMatrix[row, col + 4] & ~_maskMatrix[row, col + 5] & _maskMatrix[row, col + 6] & 1) != 0;
   }

   private bool _matchesFinderPatternVertically(int row, int col)
   {
      return (_maskMatrix[row, col] & ~_maskMatrix[row + 1, col] & _maskMatrix[row + 2, col] & _maskMatrix[row + 3, col] & _maskMatrix[row + 4, col] & ~_maskMatrix[row + 5, col] & _maskMatrix[row + 6, col] & 1) != 0;
   }

   private void _addFormatInformation()
   {
      if (QRCodeVersion >= 7)
      {
         int versionBlockCol = QRCodeDimension - 11;
         int versionCode = VersionCodeArray[QRCodeVersion - 7];

         int bit = 1;
         for (int row = 0; row < 6; ++row)
         {
            for (int col = 0; col < 3; ++col)
            {
               _resultMatrix[row, versionBlockCol + col] = (versionCode & bit) != 0 ? FixedBlack : FixedWhite;
               bit <<= 1;
            }
         }

         bit = 1;
         for (int col = 0; col < 6; ++col)
         {
            for (int row = 0; row < 3; ++row)
            {
               _resultMatrix[versionBlockCol + row, col] = (versionCode & bit) != 0 ? FixedBlack : FixedWhite;
               bit <<= 1;
            }
         }
      }

      int formatEcIndex = ErrorCorrection switch
      {
         ErrorCorrection.L => 8,
         ErrorCorrection.Q => 24,
         ErrorCorrection.H => 16 /*0x10*/,
         _ => 0,
      };
      int formatInfo = FormatInfoArray[formatEcIndex + _maskCode];

      int formatBit = 1;
      for (int i = 0; i < 15; ++i)
      {
         byte moduleValue = (formatInfo & formatBit) != 0 ? FixedBlack : FixedWhite;
         formatBit <<= 1;

         _resultMatrix[FormatInfoOne[i, 0], FormatInfoOne[i, 1]] = moduleValue;

         int row = FormatInfoTwo[i, 0];
         if (row < 0)
         {
            row += QRCodeDimension;
         }

         int col = FormatInfoTwo[i, 1];
         if (col < 0)
         {
            col += QRCodeDimension;
         }

         _resultMatrix[row, col] = moduleValue;
      }
   }

   private int _dataLengthBits(EncodingMode encodingMode)
   {
      switch (encodingMode)
      {
         case EncodingMode.Numeric:
            if (QRCodeVersion < 10)
            {
               return 10;
            }

            return QRCodeVersion >= 27 ? 14 : 12;
         case EncodingMode.AlphaNumeric:
            if (QRCodeVersion < 10)
            {
               return 9;
            }

            return QRCodeVersion >= 27 ? 13 : 11;
         case EncodingMode.Byte:
            return QRCodeVersion >= 10 ? 16 /*0x10*/ : 8;
         default:
            throw new InvalidOperationException("Encoding mode error");
      }
   }

   private void _setDataCodewordsLength()
   {
      int ecBlockRow = (int)(((QRCodeVersion - 1) * 4) + ErrorCorrection);
      _blocksGroup1 = ECBlockInfo[ecBlockRow, BLOCKS_GROUP1];
      _dataCodewordsGroup1 = ECBlockInfo[ecBlockRow, DATA_CODEWORDS_GROUP1];
      _blocksGroup2 = ECBlockInfo[ecBlockRow, BLOCKS_GROUP2];
      _dataCodewordsGroup2 = ECBlockInfo[ecBlockRow, DATA_CODEWORDS_GROUP2];
      _maxDataCodewords = (_blocksGroup1 * _dataCodewordsGroup1) + (_blocksGroup2 * _dataCodewordsGroup2);
      _maxDataBits = 8 * _maxDataCodewords;
      _maxCodewords = MaxCodewordsArray[QRCodeVersion];
      _errCorrCodewords = (_maxCodewords - _maxDataCodewords) / (_blocksGroup1 + _blocksGroup2);
   }

   private void _buildBaseMatrix()
   {
      _baseMatrix = new byte[QRCodeDimension, QRCodeDimension];

      for (int row = 0; row < 9; ++row)
      {
         for (int col = 0; col < 9; ++col)
         {
            _baseMatrix[row, col] = FinderPatternTopLeft[row, col];
         }
      }

      int farCorner = QRCodeDimension - 8;
      for (int row = 0; row < 9; ++row)
      {
         for (int col = 0; col < 8; ++col)
         {
            _baseMatrix[row, farCorner + col] = FinderPatternTopRight[row, col];
         }
      }

      for (int row = 0; row < 8; ++row)
      {
         for (int col = 0; col < 9; ++col)
         {
            _baseMatrix[farCorner + row, col] = FinderPatternBottomLeft[row, col];
         }
      }

      for (int i = 8; i < QRCodeDimension - 8; ++i)
      {
         _baseMatrix[i, 6] = _baseMatrix[6, i] = (i & 1) == 0 ? FixedBlack : FixedWhite;
      }

      if (QRCodeVersion > 1)
      {
         byte[] alignmentPositions = AlignmentPositionArray[QRCodeVersion] ?? [];
         int positionCount = alignmentPositions.Length;
         for (int rowPos = 0; rowPos < positionCount; ++rowPos)
         {
            for (int colPos = 0; colPos < positionCount; ++colPos)
            {
               bool overlapsFinder =
                  (colPos == 0 && rowPos == 0)
                  || (colPos == positionCount - 1 && rowPos == 0)
                  || (colPos == 0 && rowPos == positionCount - 1);
               if (overlapsFinder)
               {
                  continue;
               }

               int centerRow = alignmentPositions[rowPos];
               int centerCol = alignmentPositions[colPos];
               for (int dRow = -2; dRow < 3; ++dRow)
               {
                  for (int dCol = -2; dCol < 3; ++dCol)
                  {
                     _baseMatrix[centerRow + dRow, centerCol + dCol] = AlignmentPattern[dRow + 2, dCol + 2];
                  }
               }
            }
         }
      }

      if (QRCodeVersion < 7)
      {
         return;
      }

      int versionBlockCol = QRCodeDimension - 11;
      for (int row = 0; row < 6; ++row)
      {
         for (int col = 0; col < 3; ++col)
         {
            _baseMatrix[row, versionBlockCol + col] = NonData;
         }
      }

      for (int col = 0; col < 6; ++col)
      {
         for (int row = 0; row < 3; ++row)
         {
            _baseMatrix[versionBlockCol + row, col] = NonData;
         }
      }
   }

   private void _applyMask(int maskPattern)
   {
      _maskMatrix = (byte[,])_baseMatrix.Clone();

      for (int row = 0; row < QRCodeDimension; ++row)
      {
         for (int col = 0; col < QRCodeDimension; ++col)
         {
            if ((_maskMatrix[row, col] & 2) != 0)
            {
               continue;
            }

            if (_maskCondition(maskPattern, row, col))
            {
               _maskMatrix[row, col] ^= 1;
            }
         }
      }
   }

   private static bool _maskCondition(int maskPattern, int row, int col) => maskPattern switch
   {
      0 => (row + col) % 2 == 0,
      1 => row % 2 == 0,
      2 => col % 3 == 0,
      3 => (row + col) % 3 == 0,
      4 => ((row / 2) + (col / 3)) % 2 == 0,
      5 => (row * col % 2) + (row * col % 3) == 0,
      6 => ((row * col % 2) + (row * col % 3)) % 2 == 0,
      7 => (((row + col) % 2) + (row * col % 3)) % 2 == 0,
      _ => false,
   };
}
#pragma warning restore CA1814

