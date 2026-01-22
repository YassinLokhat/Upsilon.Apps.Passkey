using System.Text;

namespace Upsilon.Apps.Passkey.Core.Utils;

public enum ErrorCorrection
{
   L,
   M,
   Q,
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
         field = value is >= (-1) and <= 999999 ? value : throw new ArgumentException("ECI Assignment Value must be 0-999999 or -1 for none");
      }
   } = -1;

   public static bool[,] Generate(string data, ErrorCorrection errorCorrection = ErrorCorrection.H)
   {
      return new QrCode(data, errorCorrection).QRCodeMatrix;
   }

   public QrCode(string stringDataSegment, ErrorCorrection errorCorrection)
   {
      if (string.IsNullOrEmpty(stringDataSegment)) throw new ArgumentException("String data segment is null or missing");
      ErrorCorrection = errorCorrection;
      _ = _encode([Encoding.UTF8.GetBytes(stringDataSegment)]);
   }

   public QrCode(byte[] singleDataSeg, ErrorCorrection errorCorrection)
   {
      if (singleDataSeg is null || singleDataSeg.Length == 0) throw new ArgumentException("Single data segment argument is null or empty");
      ErrorCorrection = errorCorrection;
      _ = _encode([singleDataSeg]);
   }

   private bool[,] _encode(byte[][] DataSegArray)
   {
      if (DataSegArray == null || DataSegArray.Length == 0)
         throw new ArgumentException("Data segments argument is null or empty");
      QRCodeVersion = 0;
      QRCodeDimension = 0;
      int num = 0;
      for (int index = 0; index < DataSegArray.Length; ++index)
      {
         byte[] dataSeg = DataSegArray[index];
         if (dataSeg == null)
            DataSegArray[index] = [];
         else
            num += dataSeg.Length;
      }
      if (num == 0)
         throw new ArgumentException("There is no data to encode.");
      _dataSegArray = DataSegArray;
      _initialization();
      _encodeData();
      _calculateErrorCorrection();
      _interleaveBlocks();
      _buildBaseMatrix();
      _loadMatrixWithData();
      _selectBestMask();
      _addFormatInformation();
      QRCodeMatrix = new bool[QRCodeDimension, QRCodeDimension];
      for (int index1 = 0; index1 < QRCodeDimension; ++index1)
      {
         for (int index2 = 0; index2 < QRCodeDimension; ++index2)
         {
            if ((_resultMatrix[index1, index2] & 1) != 0)
               QRCodeMatrix[index1, index2] = true;
         }
      }
      return QRCodeMatrix;
   }

   private void _initialization()
   {
      _encodingSegMode = new EncodingMode[_dataSegArray.Length];
      _encodedDataBits = 0;
      if (ECIAssignValue >= 0)
         _encodedDataBits = ECIAssignValue > sbyte.MaxValue ? (ECIAssignValue > 16383 /*0x3FFF*/ ? 28 : 20) : 12;
      for (int index1 = 0; index1 < _dataSegArray.Length; ++index1)
      {
         byte[] dataSeg = _dataSegArray[index1];
         int length = dataSeg.Length;
         EncodingMode encodingMode = EncodingMode.Numeric;
         for (int index2 = 0; index2 < length; ++index2)
         {
            int num = EncodingTable[dataSeg[index2]];
            if (num >= 10)
            {
               if (num < 45)
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
         int num1 = 4;
         switch (encodingMode)
         {
            case EncodingMode.Numeric:
               num1 += 10 * (length / 3);
               if (length % 3 == 1)
               {
                  num1 += 4;
                  break;
               }
               if (length % 3 == 2)
               {
                  num1 += 7;
                  break;
               }
               break;
            case EncodingMode.AlphaNumeric:
               num1 += 11 * (length / 2);
               if ((length & 1) != 0)
               {
                  num1 += 6;
                  break;
               }
               break;
            case EncodingMode.Byte:
               num1 += 8 * length;
               break;
         }
         _encodingSegMode[index1] = encodingMode;
         _encodedDataBits += num1;
      }
      int num2 = 0;
      for (QRCodeVersion = 1; QRCodeVersion <= 40; ++QRCodeVersion)
      {
         QRCodeDimension = 17 + (4 * QRCodeVersion);
         _setDataCodewordsLength();
         num2 = 0;
         for (int index = 0; index < _encodingSegMode.Length; ++index)
            num2 += _dataLengthBits(_encodingSegMode[index]);
         if (_encodedDataBits + num2 <= _maxDataBits)
            break;
      }
      if (QRCodeVersion > 40)
         throw new ApplicationException("Input data string is too long");
      _encodedDataBits += num2;
   }

   private void _encodeData()
   {
      _codewordsArray = new byte[_maxCodewords];
      _codewordsPtr = 0;
      _bitBuffer = 0U;
      _bitBufferLen = 0;
      if (ECIAssignValue >= 0)
      {
         _saveBitsToCodewordsArray(7, 4);
         if (ECIAssignValue <= sbyte.MaxValue)
            _saveBitsToCodewordsArray(ECIAssignValue, 8);
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
      for (int index1 = 0; index1 < _dataSegArray.Length; ++index1)
      {
         byte[] dataSeg = _dataSegArray[index1];
         int length = dataSeg.Length;
         _saveBitsToCodewordsArray((int)_encodingSegMode[index1], 4);
         _saveBitsToCodewordsArray(length, _dataLengthBits(_encodingSegMode[index1]));
         switch (_encodingSegMode[index1])
         {
            case EncodingMode.Numeric:
               int index2 = length / 3 * 3;
               for (int index3 = 0; index3 < index2; index3 += 3)
                  _saveBitsToCodewordsArray((100 * EncodingTable[dataSeg[index3]]) + (10 * EncodingTable[dataSeg[index3 + 1]]) + EncodingTable[dataSeg[index3 + 2]], 10);
               if (length - index2 == 1)
               {
                  _saveBitsToCodewordsArray(EncodingTable[dataSeg[index2]], 4);
                  break;
               }
               if (length - index2 == 2)
               {
                  _saveBitsToCodewordsArray((10 * EncodingTable[dataSeg[index2]]) + EncodingTable[dataSeg[index2 + 1]], 7);
                  break;
               }
               break;
            case EncodingMode.AlphaNumeric:
               int index4 = length / 2 * 2;
               for (int index5 = 0; index5 < index4; index5 += 2)
                  _saveBitsToCodewordsArray((45 * EncodingTable[dataSeg[index5]]) + EncodingTable[dataSeg[index5 + 1]], 11);
               if (length - index4 == 1)
               {
                  _saveBitsToCodewordsArray(EncodingTable[dataSeg[index4]], 6);
                  break;
               }
               break;
            case EncodingMode.Byte:
               for (int index6 = 0; index6 < length; ++index6)
                  _saveBitsToCodewordsArray(dataSeg[index6], 8);
               break;
         }
      }
      if (_encodedDataBits < _maxDataBits)
         _saveBitsToCodewordsArray(0, _maxDataBits - _encodedDataBits < 4 ? _maxDataBits - _encodedDataBits : 4);
      if (_bitBufferLen > 0)
         _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);
      int num = _maxDataCodewords - _codewordsPtr;
      for (int index = 0; index < num; ++index)
         _codewordsArray[_codewordsPtr + index] = (index & 1) == 0 ? (byte)236 : (byte)17;
   }

   private void _saveBitsToCodewordsArray(int Data, int Bits)
   {
      _bitBuffer |= (uint)(Data << (32 /*0x20*/ - _bitBufferLen - Bits));
      for (_bitBufferLen += Bits; _bitBufferLen >= 8; _bitBufferLen -= 8)
      {
         _codewordsArray[_codewordsPtr++] = (byte)(_bitBuffer >> 24);
         _bitBuffer <<= 8;
      }
   }

   private void _calculateErrorCorrection()
   {
      byte[] gen = GenArray[_errCorrCodewords - 7] ?? [];
      byte[] numArray = new byte[Math.Max(_dataCodewordsGroup1, _dataCodewordsGroup2) + _errCorrCodewords];
      int num1 = _dataCodewordsGroup1;
      int PolyLength = num1 + _errCorrCodewords;
      int sourceIndex = 0;
      int maxDataCodewords = _maxDataCodewords;
      int num2 = _blocksGroup1 + _blocksGroup2;
      for (int index = 0; index < num2; ++index)
      {
         if (index == _blocksGroup1)
         {
            num1 = _dataCodewordsGroup2;
            PolyLength = num1 + _errCorrCodewords;
         }
         Array.Copy(_codewordsArray, sourceIndex, numArray, 0, num1);
         Array.Clear(numArray, num1, _errCorrCodewords);
         sourceIndex += num1;
         _polynominalDivision(numArray, PolyLength, gen, _errCorrCodewords);
         Array.Copy(numArray, num1, _codewordsArray, maxDataCodewords, _errCorrCodewords);
         maxDataCodewords += _errCorrCodewords;
      }
   }

   private static void _polynominalDivision(
     byte[] Polynomial,
     int PolyLength,
     byte[] _generator,
     int ErrCorrCodewords)
   {
      int num1 = PolyLength - ErrCorrCodewords;
      for (int index1 = 0; index1 < num1; ++index1)
      {
         if (Polynomial[index1] != 0)
         {
            int num2 = IntToExp[Polynomial[index1]];
            for (int index2 = 0; index2 < ErrCorrCodewords; ++index2)
               Polynomial[index1 + 1 + index2] = (byte)(Polynomial[index1 + 1 + index2] ^ (uint)ExpToInt[_generator[index2] + num2]);
         }
      }
   }

   private void _interleaveBlocks()
   {
      byte[] numArray1 = new byte[_maxCodewords];
      int length = _blocksGroup1 + _blocksGroup2;
      int[] numArray2 = new int[length];
      for (int index = 1; index < length; ++index)
         numArray2[index] = numArray2[index - 1] + (index <= _blocksGroup1 ? _dataCodewordsGroup1 : _dataCodewordsGroup2);
      int num = _dataCodewordsGroup1 * length;
      int index1 = 0;
      int index2;
      for (index2 = 0; index2 < num; ++index2)
      {
         numArray1[index2] = _codewordsArray[numArray2[index1]];
         ++numArray2[index1];
         ++index1;
         if (index1 == length)
            index1 = 0;
      }
      if (_dataCodewordsGroup2 > _dataCodewordsGroup1)
      {
         int maxDataCodewords = _maxDataCodewords;
         int blocksGroup1 = _blocksGroup1;
         for (; index2 < maxDataCodewords; ++index2)
         {
            numArray1[index2] = _codewordsArray[numArray2[blocksGroup1]];
            ++numArray2[blocksGroup1];
            ++blocksGroup1;
            if (blocksGroup1 == length)
               blocksGroup1 = _blocksGroup1;
         }
      }
      numArray2[0] = _maxDataCodewords;
      for (int index3 = 1; index3 < length; ++index3)
         numArray2[index3] = numArray2[index3 - 1] + _errCorrCodewords;
      int maxCodewords = _maxCodewords;
      int index4 = 0;
      for (; index2 < maxCodewords; ++index2)
      {
         numArray1[index2] = _codewordsArray[numArray2[index4]];
         ++numArray2[index4];
         ++index4;
         if (index4 == length)
            index4 = 0;
      }
      _codewordsArray = numArray1;
   }

   private void _loadMatrixWithData()
   {
      int num1 = 0;
      int num2 = 8 * _maxCodewords;
      int index1 = QRCodeDimension - 1;
      int index2 = QRCodeDimension - 1;
      int num3 = 0;
      while (true)
      {
         if ((_baseMatrix[index1, index2] & 2) == 0)
         {
            if ((_codewordsArray[num1 >> 3] & (1 << (7 - (num1 & 7)))) != 0)
               _baseMatrix[index1, index2] = 1;
            if (++num1 == num2)
               break;
         }
         else if (index2 == 6)
            --index2;
         switch (num3)
         {
            case 0:
               --index2;
               num3 = 1;
               continue;
            case 1:
               ++index2;
               --index1;
               if (index1 >= 0)
               {
                  num3 = 0;
                  continue;
               }
               index2 -= 2;
               index1 = 0;
               num3 = 2;
               continue;
            case 2:
               --index2;
               num3 = 3;
               continue;
            case 3:
               ++index2;
               ++index1;
               if (index1 < QRCodeDimension)
               {
                  num3 = 2;
                  continue;
               }
               index2 -= 2;
               index1 = QRCodeDimension - 1;
               num3 = 0;
               continue;
            default:
               continue;
         }
      }
   }

   private void _selectBestMask()
   {
      int num1 = int.MaxValue;
      _maskCode = 0;
      for (int Mask = 0; Mask < 8; ++Mask)
      {
         _applyMask(Mask);
         int num2 = _evaluationCondition1();
         if (num2 < num1)
         {
            int num3 = num2 + _evaluationCondition2();
            if (num3 < num1)
            {
               int num4 = num3 + _evaluationCondition3();
               if (num4 < num1)
               {
                  int num5 = num4 + _evaluationCondition4();
                  if (num5 < num1)
                  {
                     _resultMatrix = _maskMatrix;
                     _maskMatrix = new byte[0, 0];
                     num1 = num5;
                     _maskCode = Mask;
                  }
               }
            }
         }
      }
   }

   private int _evaluationCondition1()
   {
      int num1 = 0;
      for (int index1 = 0; index1 < QRCodeDimension; ++index1)
      {
         int num2 = 1;
         for (int index2 = 1; index2 < QRCodeDimension; ++index2)
         {
            if (((_maskMatrix[index1, index2 - 1] ^ _maskMatrix[index1, index2]) & 1) != 0)
            {
               if (num2 >= 5)
                  num1 += num2 - 2;
               num2 = 0;
            }
            ++num2;
         }
         if (num2 >= 5)
            num1 += num2 - 2;
      }
      for (int index3 = 0; index3 < QRCodeDimension; ++index3)
      {
         int num3 = 1;
         for (int index4 = 1; index4 < QRCodeDimension; ++index4)
         {
            if (((_maskMatrix[index4 - 1, index3] ^ _maskMatrix[index4, index3]) & 1) != 0)
            {
               if (num3 >= 5)
                  num1 += num3 - 2;
               num3 = 0;
            }
            ++num3;
         }
         if (num3 >= 5)
            num1 += num3 - 2;
      }
      return num1;
   }

   private int _evaluationCondition2()
   {
      int num = 0;
      for (int index1 = 1; index1 < QRCodeDimension; ++index1)
      {
         for (int index2 = 1; index2 < QRCodeDimension; ++index2)
         {
            if ((_maskMatrix[index1 - 1, index2 - 1] & _maskMatrix[index1 - 1, index2] & _maskMatrix[index1, index2 - 1] & _maskMatrix[index1, index2] & 1) != 0)
               num += 3;
            else if (((_maskMatrix[index1 - 1, index2 - 1] | _maskMatrix[index1 - 1, index2] | _maskMatrix[index1, index2 - 1] | _maskMatrix[index1, index2]) & 1) == 0)
               num += 3;
         }
      }
      return num;
   }

   private int _evaluationCondition3()
   {
      int num1 = 0;
      for (int Row = 0; Row < QRCodeDimension; ++Row)
      {
         int num2 = 0;
         for (int Col = 0; Col < QRCodeDimension; ++Col)
         {
            if ((_maskMatrix[Row, Col] & 1) != 0)
            {
               if (Col - num2 >= 4)
               {
                  if (num2 >= 7 && _testHorizontalDarkLight(Row, num2 - 7))
                     num1 += 40;
                  if (QRCodeDimension - Col >= 7 && _testHorizontalDarkLight(Row, Col))
                  {
                     num1 += 40;
                     Col += 6;
                  }
               }
               num2 = Col + 1;
            }
         }
         if (QRCodeDimension - num2 >= 4 && num2 >= 7 && _testHorizontalDarkLight(Row, num2 - 7))
            num1 += 40;
      }
      for (int Col = 0; Col < QRCodeDimension; ++Col)
      {
         int num3 = 0;
         for (int Row = 0; Row < QRCodeDimension; ++Row)
         {
            if ((_maskMatrix[Row, Col] & 1) != 0)
            {
               if (Row - num3 >= 4)
               {
                  if (num3 >= 7 && _testVerticalDarkLight(num3 - 7, Col))
                     num1 += 40;
                  if (QRCodeDimension - Row >= 7 && _testVerticalDarkLight(Row, Col))
                  {
                     num1 += 40;
                     Row += 6;
                  }
               }
               num3 = Row + 1;
            }
         }
         if (QRCodeDimension - num3 >= 4 && num3 >= 7 && _testVerticalDarkLight(num3 - 7, Col))
            num1 += 40;
      }
      return num1;
   }

   private int _evaluationCondition4()
   {
      int num1 = 0;
      for (int index1 = 0; index1 < QRCodeDimension; ++index1)
      {
         for (int index2 = 0; index2 < QRCodeDimension; ++index2)
         {
            if ((_maskMatrix[index1, index2] & 1) != 0)
               ++num1;
         }
      }
      double num2 = num1 / (double)(QRCodeDimension * QRCodeDimension);
      return num2 > 0.55 ? (int)(20.0 * (num2 - 0.5)) * 10 : num2 < 0.45 ? (int)(20.0 * (0.5 - num2)) * 10 : 0;
   }

   private bool _testHorizontalDarkLight(int Row, int Col)
   {
      return (_maskMatrix[Row, Col] & ~_maskMatrix[Row, Col + 1] & _maskMatrix[Row, Col + 2] & _maskMatrix[Row, Col + 3] & _maskMatrix[Row, Col + 4] & ~_maskMatrix[Row, Col + 5] & _maskMatrix[Row, Col + 6] & 1) != 0;
   }

   private bool _testVerticalDarkLight(int Row, int Col)
   {
      return (_maskMatrix[Row, Col] & ~_maskMatrix[Row + 1, Col] & _maskMatrix[Row + 2, Col] & _maskMatrix[Row + 3, Col] & _maskMatrix[Row + 4, Col] & ~_maskMatrix[Row + 5, Col] & _maskMatrix[Row + 6, Col] & 1) != 0;
   }

   private void _addFormatInformation()
   {
      if (QRCodeVersion >= 7)
      {
         int num1 = QRCodeDimension - 11;
         int versionCode = VersionCodeArray[QRCodeVersion - 7];
         int num2 = 1;
         for (int index1 = 0; index1 < 6; ++index1)
         {
            for (int index2 = 0; index2 < 3; ++index2)
            {
               _resultMatrix[index1, num1 + index2] = (versionCode & num2) != 0 ? (byte)7 : (byte)6;
               num2 <<= 1;
            }
         }
         int num3 = 1;
         for (int index3 = 0; index3 < 6; ++index3)
         {
            for (int index4 = 0; index4 < 3; ++index4)
            {
               _resultMatrix[num1 + index4, index3] = (versionCode & num3) != 0 ? (byte)7 : (byte)6;
               num3 <<= 1;
            }
         }
      }
      int num4 = 0;
      switch (ErrorCorrection)
      {
         case ErrorCorrection.L:
            num4 = 8;
            break;
         case ErrorCorrection.Q:
            num4 = 24;
            break;
         case ErrorCorrection.H:
            num4 = 16 /*0x10*/;
            break;
      }
      int formatInfo = FormatInfoArray[num4 + _maskCode];
      int num5 = 1;
      for (int index5 = 0; index5 < 15; ++index5)
      {
         int num6 = (formatInfo & num5) != 0 ? 7 : 6;
         num5 <<= 1;
         _resultMatrix[FormatInfoOne[index5, 0], FormatInfoOne[index5, 1]] = (byte)num6;
         int index6 = FormatInfoTwo[index5, 0];
         if (index6 < 0)
            index6 += QRCodeDimension;
         int index7 = FormatInfoTwo[index5, 1];
         if (index7 < 0)
            index7 += QRCodeDimension;
         _resultMatrix[index6, index7] = (byte)num6;
      }
   }

   private int _dataLengthBits(EncodingMode EncodingMode)
   {
      switch (EncodingMode)
      {
         case EncodingMode.Numeric:
            if (QRCodeVersion < 10)
               return 10;
            return QRCodeVersion >= 27 ? 14 : 12;
         case EncodingMode.AlphaNumeric:
            if (QRCodeVersion < 10)
               return 9;
            return QRCodeVersion >= 27 ? 13 : 11;
         case EncodingMode.Byte:
            return QRCodeVersion >= 10 ? 16 /*0x10*/ : 8;
         default:
            throw new ApplicationException("Encoding mode error");
      }
   }

   private void _setDataCodewordsLength()
   {
      int index = (int)(((QRCodeVersion - 1) * 4) + ErrorCorrection);
      _blocksGroup1 = ECBlockInfo[index, 0];
      _dataCodewordsGroup1 = ECBlockInfo[index, 1];
      _blocksGroup2 = ECBlockInfo[index, 2];
      _dataCodewordsGroup2 = ECBlockInfo[index, 3];
      _maxDataCodewords = (_blocksGroup1 * _dataCodewordsGroup1) + (_blocksGroup2 * _dataCodewordsGroup2);
      _maxDataBits = 8 * _maxDataCodewords;
      _maxCodewords = MaxCodewordsArray[QRCodeVersion];
      _errCorrCodewords = (_maxCodewords - _maxDataCodewords) / (_blocksGroup1 + _blocksGroup2);
   }

   private void _buildBaseMatrix()
   {
      _baseMatrix = new byte[QRCodeDimension + 5, QRCodeDimension + 5];
      for (int index1 = 0; index1 < 9; ++index1)
      {
         for (int index2 = 0; index2 < 9; ++index2)
            _baseMatrix[index1, index2] = FinderPatternTopLeft[index1, index2];
      }
      int num1 = QRCodeDimension - 8;
      for (int index3 = 0; index3 < 9; ++index3)
      {
         for (int index4 = 0; index4 < 8; ++index4)
            _baseMatrix[index3, num1 + index4] = FinderPatternTopRight[index3, index4];
      }
      for (int index5 = 0; index5 < 8; ++index5)
      {
         for (int index6 = 0; index6 < 9; ++index6)
            _baseMatrix[num1 + index5, index6] = FinderPatternBottomLeft[index5, index6];
      }
      for (int index = 8; index < QRCodeDimension - 8; ++index)
         _baseMatrix[index, 6] = _baseMatrix[6, index] = (index & 1) == 0 ? (byte)7 : (byte)6;
      if (QRCodeVersion > 1)
      {
         byte[] alignmentPosition = AlignmentPositionArray[QRCodeVersion] ?? [];
         int length = alignmentPosition.Length;
         for (int index7 = 0; index7 < length; ++index7)
         {
            for (int index8 = 0; index8 < length; ++index8)
            {
               if ((index8 != 0 || index7 != 0) && (index8 != length - 1 || index7 != 0) && (index8 != 0 || index7 != length - 1))
               {
                  int num2 = alignmentPosition[index7];
                  int num3 = alignmentPosition[index8];
                  for (int index9 = -2; index9 < 3; ++index9)
                  {
                     for (int index10 = -2; index10 < 3; ++index10)
                        _baseMatrix[num2 + index9, num3 + index10] = AlignmentPattern[index9 + 2, index10 + 2];
                  }
               }
            }
         }
      }
      if (QRCodeVersion < 7)
         return;
      int num4 = QRCodeDimension - 11;
      for (int index11 = 0; index11 < 6; ++index11)
      {
         for (int index12 = 0; index12 < 3; ++index12)
            _baseMatrix[index11, num4 + index12] = 2;
      }
      for (int index13 = 0; index13 < 6; ++index13)
      {
         for (int index14 = 0; index14 < 3; ++index14)
            _baseMatrix[num4 + index14, index13] = 2;
      }
   }

   private void _applyMask(int Mask)
   {
      _maskMatrix = (byte[,])_baseMatrix.Clone();
      switch (Mask)
      {
         case 0:
            _applyMask0();
            break;
         case 1:
            _applyMask1();
            break;
         case 2:
            _applyMask2();
            break;
         case 3:
            _applyMask3();
            break;
         case 4:
            _applyMask4();
            break;
         case 5:
            _applyMask5();
            break;
         case 6:
            _applyMask6();
            break;
         case 7:
            _applyMask7();
            break;
      }
   }

   private void _applyMask0()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 2)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 2)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 1] ^= 1;
         }
      }
   }

   private void _applyMask1()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 2)
      {
         for (int index2 = 0; index2 < QRCodeDimension; ++index2)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
         }
      }
   }

   private void _applyMask2()
   {
      for (int index1 = 0; index1 < QRCodeDimension; ++index1)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 3)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
         }
      }
   }

   private void _applyMask3()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 3)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 3)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 1] ^= 1;
         }
      }
   }

   private void _applyMask4()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 4)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
            if ((_maskMatrix[index1, index2 + 1] & 2) == 0)
               _maskMatrix[index1, index2 + 1] ^= 1;
            if ((_maskMatrix[index1, index2 + 2] & 2) == 0)
               _maskMatrix[index1, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 1, index2] & 2) == 0)
               _maskMatrix[index1 + 1, index2] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 5] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 5] ^= 1;
         }
      }
   }

   private void _applyMask5()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 6)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
         {
            for (int index3 = 0; index3 < 6; ++index3)
            {
               if ((_maskMatrix[index1, index2 + index3] & 2) == 0)
                  _maskMatrix[index1, index2 + index3] ^= 1;
            }
            for (int index4 = 1; index4 < 6; ++index4)
            {
               if ((_maskMatrix[index1 + index4, index2] & 2) == 0)
                  _maskMatrix[index1 + index4, index2] ^= 1;
            }
            if ((_maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 3] ^= 1;
         }
      }
   }

   private void _applyMask6()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 6)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
         {
            for (int index3 = 0; index3 < 6; ++index3)
            {
               if ((_maskMatrix[index1, index2 + index3] & 2) == 0)
                  _maskMatrix[index1, index2 + index3] ^= 1;
            }
            for (int index4 = 1; index4 < 6; ++index4)
            {
               if ((_maskMatrix[index1 + index4, index2] & 2) == 0)
                  _maskMatrix[index1 + index4, index2] ^= 1;
            }
            if ((_maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 5] ^= 1;
            if ((_maskMatrix[index1 + 5, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 5, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 5, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 5, index2 + 5] ^= 1;
         }
      }
   }

   private void _applyMask7()
   {
      for (int index1 = 0; index1 < QRCodeDimension; index1 += 6)
      {
         for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
         {
            if ((_maskMatrix[index1, index2] & 2) == 0)
               _maskMatrix[index1, index2] ^= 1;
            if ((_maskMatrix[index1, index2 + 2] & 2) == 0)
               _maskMatrix[index1, index2 + 2] ^= 1;
            if ((_maskMatrix[index1, index2 + 4] & 2) == 0)
               _maskMatrix[index1, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 1, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 1, index2 + 5] ^= 1;
            if ((_maskMatrix[index1 + 2, index2] & 2) == 0)
               _maskMatrix[index1 + 2, index2] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 4] ^= 1;
            if ((_maskMatrix[index1 + 2, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 2, index2 + 5] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 3] ^= 1;
            if ((_maskMatrix[index1 + 3, index2 + 5] & 2) == 0)
               _maskMatrix[index1 + 3, index2 + 5] ^= 1;
            if ((_maskMatrix[index1 + 4, index2] & 2) == 0)
               _maskMatrix[index1 + 4, index2] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 4, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 4, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 5, index2 + 1] & 2) == 0)
               _maskMatrix[index1 + 5, index2 + 1] ^= 1;
            if ((_maskMatrix[index1 + 5, index2 + 2] & 2) == 0)
               _maskMatrix[index1 + 5, index2 + 2] ^= 1;
            if ((_maskMatrix[index1 + 5, index2 + 3] & 2) == 0)
               _maskMatrix[index1 + 5, index2 + 3] ^= 1;
         }
      }
   }
}
