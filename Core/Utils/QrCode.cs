using System;
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
   private ErrorCorrection _errorCorrection = ErrorCorrection.H;
  private int _eCIAssignValue = -1;
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
   private byte[,] _maskMatrix = new byte[0,0];
  private byte[,] _resultMatrix = new byte[0,0];
  internal static readonly byte[]?[] AlignmentPositionArray =
  [
    null,
    null,
    [(byte) 6, (byte) 18],
    [(byte) 6, (byte) 22],
    [(byte) 6, (byte) 26],
    [(byte) 6, (byte) 30],
    [(byte) 6, (byte) 34],
    [(byte) 6, (byte) 22, (byte) 38],
    [(byte) 6, (byte) 24, (byte) 42],
    [(byte) 6, (byte) 26, (byte) 46],
    [(byte) 6, (byte) 28, (byte) 50],
    [(byte) 6, (byte) 30, (byte) 54],
    [(byte) 6, (byte) 32 /*0x20*/, (byte) 58],
    [(byte) 6, (byte) 34, (byte) 62],
    [(byte) 6, (byte) 26, (byte) 46, (byte) 66],
    [
      (byte) 6,
      (byte) 26,
      (byte) 48 /*0x30*/,
      (byte) 70
    ],
    [(byte) 6, (byte) 26, (byte) 50, (byte) 74],
    [(byte) 6, (byte) 30, (byte) 54, (byte) 78],
    [(byte) 6, (byte) 30, (byte) 56, (byte) 82],
    [(byte) 6, (byte) 30, (byte) 58, (byte) 86],
    [(byte) 6, (byte) 34, (byte) 62, (byte) 90],
    [
      (byte) 6,
      (byte) 28,
      (byte) 50,
      (byte) 72,
      (byte) 94
    ],
    [
      (byte) 6,
      (byte) 26,
      (byte) 50,
      (byte) 74,
      (byte) 98
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 54,
      (byte) 78,
      (byte) 102
    ],
    [
      (byte) 6,
      (byte) 28,
      (byte) 54,
      (byte) 80 /*0x50*/,
      (byte) 106
    ],
    [
      (byte) 6,
      (byte) 32 /*0x20*/,
      (byte) 58,
      (byte) 84,
      (byte) 110
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 58,
      (byte) 86,
      (byte) 114
    ],
    [
      (byte) 6,
      (byte) 34,
      (byte) 62,
      (byte) 90,
      (byte) 118
    ],
    [
      (byte) 6,
      (byte) 26,
      (byte) 50,
      (byte) 74,
      (byte) 98,
      (byte) 122
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 54,
      (byte) 78,
      (byte) 102,
      (byte) 126
    ],
    [
      (byte) 6,
      (byte) 26,
      (byte) 52,
      (byte) 78,
      (byte) 104,
      (byte) 130
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 56,
      (byte) 82,
      (byte) 108,
      (byte) 134
    ],
    [
      (byte) 6,
      (byte) 34,
      (byte) 60,
      (byte) 86,
      (byte) 112 /*0x70*/,
      (byte) 138
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 58,
      (byte) 86,
      (byte) 114,
      (byte) 142
    ],
    [
      (byte) 6,
      (byte) 34,
      (byte) 62,
      (byte) 90,
      (byte) 118,
      (byte) 146
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 54,
      (byte) 78,
      (byte) 102,
      (byte) 126,
      (byte) 150
    ],
    [
      (byte) 6,
      (byte) 24,
      (byte) 50,
      (byte) 76,
      (byte) 102,
      (byte) 128 /*0x80*/,
      (byte) 154
    ],
    [
      (byte) 6,
      (byte) 28,
      (byte) 54,
      (byte) 80 /*0x50*/,
      (byte) 106,
      (byte) 132,
      (byte) 158
    ],
    [
      (byte) 6,
      (byte) 32 /*0x20*/,
      (byte) 58,
      (byte) 84,
      (byte) 110,
      (byte) 136,
      (byte) 162
    ],
    [
      (byte) 6,
      (byte) 26,
      (byte) 54,
      (byte) 82,
      (byte) 110,
      (byte) 138,
      (byte) 166
    ],
    [
      (byte) 6,
      (byte) 30,
      (byte) 58,
      (byte) 86,
      (byte) 114,
      (byte) 142,
      (byte) 170
    ]
  ];
  internal static readonly int[] MaxCodewordsArray =
  [
    0,
    26,
    44,
    70,
    100,
    134,
    172,
    196,
    242,
    292,
    346,
    404,
    466,
    532,
    581,
    655,
    733,
    815,
    901,
    991,
    1085,
    1156,
    1258,
    1364,
    1474,
    1588,
    1706,
    1828,
    1921,
    2051,
    2185,
    2323,
    2465,
    2611,
    2761,
    2876,
    3034,
    3196,
    3362,
    3532,
    3706
  ];
  internal static readonly byte[] EncodingTable =
  [
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 36,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 37,
    (byte) 38,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 39,
    (byte) 40,
    (byte) 45,
    (byte) 41,
    (byte) 42,
    (byte) 43,
    (byte) 0,
    (byte) 1,
    (byte) 2,
    (byte) 3,
    (byte) 4,
    (byte) 5,
    (byte) 6,
    (byte) 7,
    (byte) 8,
    (byte) 9,
    (byte) 44,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 10,
    (byte) 11,
    (byte) 12,
    (byte) 13,
    (byte) 14,
    (byte) 15,
    (byte) 16 /*0x10*/,
    (byte) 17,
    (byte) 18,
    (byte) 19,
    (byte) 20,
    (byte) 21,
    (byte) 22,
    (byte) 23,
    (byte) 24,
    (byte) 25,
    (byte) 26,
    (byte) 27,
    (byte) 28,
    (byte) 29,
    (byte) 30,
    (byte) 31 /*0x1F*/,
    (byte) 32 /*0x20*/,
    (byte) 33,
    (byte) 34,
    (byte) 35,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45,
    (byte) 45
  ];
  internal const int BLOCKS_GROUP1 = 0;
  internal const int DATA_CODEWORDS_GROUP1 = 1;
  internal const int BLOCKS_GROUP2 = 2;
  internal const int DATA_CODEWORDS_GROUP2 = 3;
  internal static readonly byte[,] ECBlockInfo = new byte[160 /*0xA0*/, 4]
  {
    {
      (byte) 1,
      (byte) 19,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 16 /*0x10*/,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 13,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 9,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 34,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 28,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 22,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 16 /*0x10*/,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 55,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 44,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 17,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 13,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 80 /*0x50*/,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 32 /*0x20*/,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 24,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 9,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 108,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 43,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 15,
      (byte) 2,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 2,
      (byte) 11,
      (byte) 2,
      (byte) 12
    },
    {
      (byte) 2,
      (byte) 68,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 27,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 19,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 15,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 78,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 31 /*0x1F*/,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 14,
      (byte) 4,
      (byte) 15
    },
    {
      (byte) 4,
      (byte) 13,
      (byte) 1,
      (byte) 14
    },
    {
      (byte) 2,
      (byte) 97,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 2,
      (byte) 38,
      (byte) 2,
      (byte) 39
    },
    {
      (byte) 4,
      (byte) 18,
      (byte) 2,
      (byte) 19
    },
    {
      (byte) 4,
      (byte) 14,
      (byte) 2,
      (byte) 15
    },
    {
      (byte) 2,
      (byte) 116,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 3,
      (byte) 36,
      (byte) 2,
      (byte) 37
    },
    {
      (byte) 4,
      (byte) 16 /*0x10*/,
      (byte) 4,
      (byte) 17
    },
    {
      (byte) 4,
      (byte) 12,
      (byte) 4,
      (byte) 13
    },
    {
      (byte) 2,
      (byte) 68,
      (byte) 2,
      (byte) 69
    },
    {
      (byte) 4,
      (byte) 43,
      (byte) 1,
      (byte) 44
    },
    {
      (byte) 6,
      (byte) 19,
      (byte) 2,
      (byte) 20
    },
    {
      (byte) 6,
      (byte) 15,
      (byte) 2,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 4,
      (byte) 81,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 1,
      (byte) 50,
      (byte) 4,
      (byte) 51
    },
    {
      (byte) 4,
      (byte) 22,
      (byte) 4,
      (byte) 23
    },
    {
      (byte) 3,
      (byte) 12,
      (byte) 8,
      (byte) 13
    },
    {
      (byte) 2,
      (byte) 92,
      (byte) 2,
      (byte) 93
    },
    {
      (byte) 6,
      (byte) 36,
      (byte) 2,
      (byte) 37
    },
    {
      (byte) 4,
      (byte) 20,
      (byte) 6,
      (byte) 21
    },
    {
      (byte) 7,
      (byte) 14,
      (byte) 4,
      (byte) 15
    },
    {
      (byte) 4,
      (byte) 107,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 8,
      (byte) 37,
      (byte) 1,
      (byte) 38
    },
    {
      (byte) 8,
      (byte) 20,
      (byte) 4,
      (byte) 21
    },
    {
      (byte) 12,
      (byte) 11,
      (byte) 4,
      (byte) 12
    },
    {
      (byte) 3,
      (byte) 115,
      (byte) 1,
      (byte) 116
    },
    {
      (byte) 4,
      (byte) 40,
      (byte) 5,
      (byte) 41
    },
    {
      (byte) 11,
      (byte) 16 /*0x10*/,
      (byte) 5,
      (byte) 17
    },
    {
      (byte) 11,
      (byte) 12,
      (byte) 5,
      (byte) 13
    },
    {
      (byte) 5,
      (byte) 87,
      (byte) 1,
      (byte) 88
    },
    {
      (byte) 5,
      (byte) 41,
      (byte) 5,
      (byte) 42
    },
    {
      (byte) 5,
      (byte) 24,
      (byte) 7,
      (byte) 25
    },
    {
      (byte) 11,
      (byte) 12,
      (byte) 7,
      (byte) 13
    },
    {
      (byte) 5,
      (byte) 98,
      (byte) 1,
      (byte) 99
    },
    {
      (byte) 7,
      (byte) 45,
      (byte) 3,
      (byte) 46
    },
    {
      (byte) 15,
      (byte) 19,
      (byte) 2,
      (byte) 20
    },
    {
      (byte) 3,
      (byte) 15,
      (byte) 13,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 1,
      (byte) 107,
      (byte) 5,
      (byte) 108
    },
    {
      (byte) 10,
      (byte) 46,
      (byte) 1,
      (byte) 47
    },
    {
      (byte) 1,
      (byte) 22,
      (byte) 15,
      (byte) 23
    },
    {
      (byte) 2,
      (byte) 14,
      (byte) 17,
      (byte) 15
    },
    {
      (byte) 5,
      (byte) 120,
      (byte) 1,
      (byte) 121
    },
    {
      (byte) 9,
      (byte) 43,
      (byte) 4,
      (byte) 44
    },
    {
      (byte) 17,
      (byte) 22,
      (byte) 1,
      (byte) 23
    },
    {
      (byte) 2,
      (byte) 14,
      (byte) 19,
      (byte) 15
    },
    {
      (byte) 3,
      (byte) 113,
      (byte) 4,
      (byte) 114
    },
    {
      (byte) 3,
      (byte) 44,
      (byte) 11,
      (byte) 45
    },
    {
      (byte) 17,
      (byte) 21,
      (byte) 4,
      (byte) 22
    },
    {
      (byte) 9,
      (byte) 13,
      (byte) 16 /*0x10*/,
      (byte) 14
    },
    {
      (byte) 3,
      (byte) 107,
      (byte) 5,
      (byte) 108
    },
    {
      (byte) 3,
      (byte) 41,
      (byte) 13,
      (byte) 42
    },
    {
      (byte) 15,
      (byte) 24,
      (byte) 5,
      (byte) 25
    },
    {
      (byte) 15,
      (byte) 15,
      (byte) 10,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 4,
      (byte) 116,
      (byte) 4,
      (byte) 117
    },
    {
      (byte) 17,
      (byte) 42,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 17,
      (byte) 22,
      (byte) 6,
      (byte) 23
    },
    {
      (byte) 19,
      (byte) 16 /*0x10*/,
      (byte) 6,
      (byte) 17
    },
    {
      (byte) 2,
      (byte) 111,
      (byte) 7,
      (byte) 112 /*0x70*/
    },
    {
      (byte) 17,
      (byte) 46,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 7,
      (byte) 24,
      (byte) 16 /*0x10*/,
      (byte) 25
    },
    {
      (byte) 34,
      (byte) 13,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 4,
      (byte) 121,
      (byte) 5,
      (byte) 122
    },
    {
      (byte) 4,
      (byte) 47,
      (byte) 14,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 11,
      (byte) 24,
      (byte) 14,
      (byte) 25
    },
    {
      (byte) 16 /*0x10*/,
      (byte) 15,
      (byte) 14,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 6,
      (byte) 117,
      (byte) 4,
      (byte) 118
    },
    {
      (byte) 6,
      (byte) 45,
      (byte) 14,
      (byte) 46
    },
    {
      (byte) 11,
      (byte) 24,
      (byte) 16 /*0x10*/,
      (byte) 25
    },
    {
      (byte) 30,
      (byte) 16 /*0x10*/,
      (byte) 2,
      (byte) 17
    },
    {
      (byte) 8,
      (byte) 106,
      (byte) 4,
      (byte) 107
    },
    {
      (byte) 8,
      (byte) 47,
      (byte) 13,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 7,
      (byte) 24,
      (byte) 22,
      (byte) 25
    },
    {
      (byte) 22,
      (byte) 15,
      (byte) 13,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 10,
      (byte) 114,
      (byte) 2,
      (byte) 115
    },
    {
      (byte) 19,
      (byte) 46,
      (byte) 4,
      (byte) 47
    },
    {
      (byte) 28,
      (byte) 22,
      (byte) 6,
      (byte) 23
    },
    {
      (byte) 33,
      (byte) 16 /*0x10*/,
      (byte) 4,
      (byte) 17
    },
    {
      (byte) 8,
      (byte) 122,
      (byte) 4,
      (byte) 123
    },
    {
      (byte) 22,
      (byte) 45,
      (byte) 3,
      (byte) 46
    },
    {
      (byte) 8,
      (byte) 23,
      (byte) 26,
      (byte) 24
    },
    {
      (byte) 12,
      (byte) 15,
      (byte) 28,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 3,
      (byte) 117,
      (byte) 10,
      (byte) 118
    },
    {
      (byte) 3,
      (byte) 45,
      (byte) 23,
      (byte) 46
    },
    {
      (byte) 4,
      (byte) 24,
      (byte) 31 /*0x1F*/,
      (byte) 25
    },
    {
      (byte) 11,
      (byte) 15,
      (byte) 31 /*0x1F*/,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 7,
      (byte) 116,
      (byte) 7,
      (byte) 117
    },
    {
      (byte) 21,
      (byte) 45,
      (byte) 7,
      (byte) 46
    },
    {
      (byte) 1,
      (byte) 23,
      (byte) 37,
      (byte) 24
    },
    {
      (byte) 19,
      (byte) 15,
      (byte) 26,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 5,
      (byte) 115,
      (byte) 10,
      (byte) 116
    },
    {
      (byte) 19,
      (byte) 47,
      (byte) 10,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 15,
      (byte) 24,
      (byte) 25,
      (byte) 25
    },
    {
      (byte) 23,
      (byte) 15,
      (byte) 25,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 13,
      (byte) 115,
      (byte) 3,
      (byte) 116
    },
    {
      (byte) 2,
      (byte) 46,
      (byte) 29,
      (byte) 47
    },
    {
      (byte) 42,
      (byte) 24,
      (byte) 1,
      (byte) 25
    },
    {
      (byte) 23,
      (byte) 15,
      (byte) 28,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 17,
      (byte) 115,
      (byte) 0,
      (byte) 0
    },
    {
      (byte) 10,
      (byte) 46,
      (byte) 23,
      (byte) 47
    },
    {
      (byte) 10,
      (byte) 24,
      (byte) 35,
      (byte) 25
    },
    {
      (byte) 19,
      (byte) 15,
      (byte) 35,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 17,
      (byte) 115,
      (byte) 1,
      (byte) 116
    },
    {
      (byte) 14,
      (byte) 46,
      (byte) 21,
      (byte) 47
    },
    {
      (byte) 29,
      (byte) 24,
      (byte) 19,
      (byte) 25
    },
    {
      (byte) 11,
      (byte) 15,
      (byte) 46,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 13,
      (byte) 115,
      (byte) 6,
      (byte) 116
    },
    {
      (byte) 14,
      (byte) 46,
      (byte) 23,
      (byte) 47
    },
    {
      (byte) 44,
      (byte) 24,
      (byte) 7,
      (byte) 25
    },
    {
      (byte) 59,
      (byte) 16 /*0x10*/,
      (byte) 1,
      (byte) 17
    },
    {
      (byte) 12,
      (byte) 121,
      (byte) 7,
      (byte) 122
    },
    {
      (byte) 12,
      (byte) 47,
      (byte) 26,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 39,
      (byte) 24,
      (byte) 14,
      (byte) 25
    },
    {
      (byte) 22,
      (byte) 15,
      (byte) 41,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 6,
      (byte) 121,
      (byte) 14,
      (byte) 122
    },
    {
      (byte) 6,
      (byte) 47,
      (byte) 34,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 46,
      (byte) 24,
      (byte) 10,
      (byte) 25
    },
    {
      (byte) 2,
      (byte) 15,
      (byte) 64 /*0x40*/,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 17,
      (byte) 122,
      (byte) 4,
      (byte) 123
    },
    {
      (byte) 29,
      (byte) 46,
      (byte) 14,
      (byte) 47
    },
    {
      (byte) 49,
      (byte) 24,
      (byte) 10,
      (byte) 25
    },
    {
      (byte) 24,
      (byte) 15,
      (byte) 46,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 4,
      (byte) 122,
      (byte) 18,
      (byte) 123
    },
    {
      (byte) 13,
      (byte) 46,
      (byte) 32 /*0x20*/,
      (byte) 47
    },
    {
      (byte) 48 /*0x30*/,
      (byte) 24,
      (byte) 14,
      (byte) 25
    },
    {
      (byte) 42,
      (byte) 15,
      (byte) 32 /*0x20*/,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 20,
      (byte) 117,
      (byte) 4,
      (byte) 118
    },
    {
      (byte) 40,
      (byte) 47,
      (byte) 7,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 43,
      (byte) 24,
      (byte) 22,
      (byte) 25
    },
    {
      (byte) 10,
      (byte) 15,
      (byte) 67,
      (byte) 16 /*0x10*/
    },
    {
      (byte) 19,
      (byte) 118,
      (byte) 6,
      (byte) 119
    },
    {
      (byte) 18,
      (byte) 47,
      (byte) 31 /*0x1F*/,
      (byte) 48 /*0x30*/
    },
    {
      (byte) 34,
      (byte) 24,
      (byte) 34,
      (byte) 25
    },
    {
      (byte) 20,
      (byte) 15,
      (byte) 61,
      (byte) 16 /*0x10*/
    }
  };
  private static readonly byte[] _generator7 =
  [
    (byte) 87,
    (byte) 229,
    (byte) 146,
    (byte) 149,
    (byte) 238,
    (byte) 102,
    (byte) 21
  ];
  private static readonly byte[] _generator10 =
  [
    (byte) 251,
    (byte) 67,
    (byte) 46,
    (byte) 61,
    (byte) 118,
    (byte) 70,
    (byte) 64 /*0x40*/,
    (byte) 94,
    (byte) 32 /*0x20*/,
    (byte) 45
  ];
  private static readonly byte[] _generator13 =
  [
    (byte) 74,
    (byte) 152,
    (byte) 176 /*0xB0*/,
    (byte) 100,
    (byte) 86,
    (byte) 100,
    (byte) 106,
    (byte) 104,
    (byte) 130,
    (byte) 218,
    (byte) 206,
    (byte) 140,
    (byte) 78
  ];
  private static readonly byte[] _generator15 =
  [
    (byte) 8,
    (byte) 183,
    (byte) 61,
    (byte) 91,
    (byte) 202,
    (byte) 37,
    (byte) 51,
    (byte) 58,
    (byte) 58,
    (byte) 237,
    (byte) 140,
    (byte) 124,
    (byte) 5,
    (byte) 99,
    (byte) 105
  ];
  private static readonly byte[] _generator16 =
  [
    (byte) 120,
    (byte) 104,
    (byte) 107,
    (byte) 109,
    (byte) 102,
    (byte) 161,
    (byte) 76,
    (byte) 3,
    (byte) 91,
    (byte) 191,
    (byte) 147,
    (byte) 169,
    (byte) 182,
    (byte) 194,
    (byte) 225,
    (byte) 120
  ];
  private static readonly byte[] _generator17 =
  [
    (byte) 43,
    (byte) 139,
    (byte) 206,
    (byte) 78,
    (byte) 43,
    (byte) 239,
    (byte) 123,
    (byte) 206,
    (byte) 214,
    (byte) 147,
    (byte) 24,
    (byte) 99,
    (byte) 150,
    (byte) 39,
    (byte) 243,
    (byte) 163,
    (byte) 136
  ];
  private static readonly byte[] _generator18 =
  [
    (byte) 215,
    (byte) 234,
    (byte) 158,
    (byte) 94,
    (byte) 184,
    (byte) 97,
    (byte) 118,
    (byte) 170,
    (byte) 79,
    (byte) 187,
    (byte) 152,
    (byte) 148,
    (byte) 252,
    (byte) 179,
    (byte) 5,
    (byte) 98,
    (byte) 96 /*0x60*/,
    (byte) 153
  ];
  private static readonly byte[] _generator20 =
  [
    (byte) 17,
    (byte) 60,
    (byte) 79,
    (byte) 50,
    (byte) 61,
    (byte) 163,
    (byte) 26,
    (byte) 187,
    (byte) 202,
    (byte) 180,
    (byte) 221,
    (byte) 225,
    (byte) 83,
    (byte) 239,
    (byte) 156,
    (byte) 164,
    (byte) 212,
    (byte) 212,
    (byte) 188,
    (byte) 190
  ];
  private static readonly byte[] _generator22 =
  [
    (byte) 210,
    (byte) 171,
    (byte) 247,
    (byte) 242,
    (byte) 93,
    (byte) 230,
    (byte) 14,
    (byte) 109,
    (byte) 221,
    (byte) 53,
    (byte) 200,
    (byte) 74,
    (byte) 8,
    (byte) 172,
    (byte) 98,
    (byte) 80 /*0x50*/,
    (byte) 219,
    (byte) 134,
    (byte) 160 /*0xA0*/,
    (byte) 105,
    (byte) 165,
    (byte) 231
  ];
  private static readonly byte[] _generator24 =
  [
    (byte) 229,
    (byte) 121,
    (byte) 135,
    (byte) 48 /*0x30*/,
    (byte) 211,
    (byte) 117,
    (byte) 251,
    (byte) 126,
    (byte) 159,
    (byte) 180,
    (byte) 169,
    (byte) 152,
    (byte) 192 /*0xC0*/,
    (byte) 226,
    (byte) 228,
    (byte) 218,
    (byte) 111,
    (byte) 0,
    (byte) 117,
    (byte) 232,
    (byte) 87,
    (byte) 96 /*0x60*/,
    (byte) 227,
    (byte) 21
  ];
  private static readonly byte[] _generator26 =
  [
    (byte) 173,
    (byte) 125,
    (byte) 158,
    (byte) 2,
    (byte) 103,
    (byte) 182,
    (byte) 118,
    (byte) 17,
    (byte) 145,
    (byte) 201,
    (byte) 111,
    (byte) 28,
    (byte) 165,
    (byte) 53,
    (byte) 161,
    (byte) 21,
    (byte) 245,
    (byte) 142,
    (byte) 13,
    (byte) 102,
    (byte) 48 /*0x30*/,
    (byte) 227,
    (byte) 153,
    (byte) 145,
    (byte) 218,
    (byte) 70
  ];
  private static readonly byte[] _generator28 =
  [
    (byte) 168,
    (byte) 223,
    (byte) 200,
    (byte) 104,
    (byte) 224 /*0xE0*/,
    (byte) 234,
    (byte) 108,
    (byte) 180,
    (byte) 110,
    (byte) 190,
    (byte) 195,
    (byte) 147,
    (byte) 205,
    (byte) 27,
    (byte) 232,
    (byte) 201,
    (byte) 21,
    (byte) 43,
    (byte) 245,
    (byte) 87,
    (byte) 42,
    (byte) 195,
    (byte) 212,
    (byte) 119,
    (byte) 242,
    (byte) 37,
    (byte) 9,
    (byte) 123
  ];
  private static readonly byte[] _generator30 =
  [
    (byte) 41,
    (byte) 173,
    (byte) 145,
    (byte) 152,
    (byte) 216,
    (byte) 31 /*0x1F*/,
    (byte) 179,
    (byte) 182,
    (byte) 50,
    (byte) 48 /*0x30*/,
    (byte) 110,
    (byte) 86,
    (byte) 239,
    (byte) 96 /*0x60*/,
    (byte) 222,
    (byte) 125,
    (byte) 42,
    (byte) 173,
    (byte) 226,
    (byte) 193,
    (byte) 224 /*0xE0*/,
    (byte) 130,
    (byte) 156,
    (byte) 37,
    (byte) 251,
    (byte) 216,
    (byte) 238,
    (byte) 40,
    (byte) 192 /*0xC0*/,
    (byte) 180
  ];
  private static readonly byte[] _generator32 =
  [
    (byte) 10,
    (byte) 6,
    (byte) 106,
    (byte) 190,
    (byte) 249,
    (byte) 167,
    (byte) 4,
    (byte) 67,
    (byte) 209,
    (byte) 138,
    (byte) 138,
    (byte) 32 /*0x20*/,
    (byte) 242,
    (byte) 123,
    (byte) 89,
    (byte) 27,
    (byte) 120,
    (byte) 185,
    (byte) 80 /*0x50*/,
    (byte) 156,
    (byte) 38,
    (byte) 60,
    (byte) 171,
    (byte) 60,
    (byte) 28,
    (byte) 222,
    (byte) 80 /*0x50*/,
    (byte) 52,
    (byte) 254,
    (byte) 185,
    (byte) 220,
    (byte) 241
  ];
  private static readonly byte[] _generator34 =
  [
    (byte) 111,
    (byte) 77,
    (byte) 146,
    (byte) 94,
    (byte) 26,
    (byte) 21,
    (byte) 108,
    (byte) 19,
    (byte) 105,
    (byte) 94,
    (byte) 113,
    (byte) 193,
    (byte) 86,
    (byte) 140,
    (byte) 163,
    (byte) 125,
    (byte) 58,
    (byte) 158,
    (byte) 229,
    (byte) 239,
    (byte) 218,
    (byte) 103,
    (byte) 56,
    (byte) 70,
    (byte) 114,
    (byte) 61,
    (byte) 183,
    (byte) 129,
    (byte) 167,
    (byte) 13,
    (byte) 98,
    (byte) 62,
    (byte) 129,
    (byte) 51
  ];
  private static readonly byte[] _generator36 =
  [
    (byte) 200,
    (byte) 183,
    (byte) 98,
    (byte) 16 /*0x10*/,
    (byte) 172,
    (byte) 31 /*0x1F*/,
    (byte) 246,
    (byte) 234,
    (byte) 60,
    (byte) 152,
    (byte) 115,
    (byte) 0,
    (byte) 167,
    (byte) 152,
    (byte) 113,
    (byte) 248,
    (byte) 238,
    (byte) 107,
    (byte) 18,
    (byte) 63 /*0x3F*/,
    (byte) 218,
    (byte) 37,
    (byte) 87,
    (byte) 210,
    (byte) 105,
    (byte) 177,
    (byte) 120,
    (byte) 74,
    (byte) 121,
    (byte) 196,
    (byte) 117,
    (byte) 251,
    (byte) 113,
    (byte) 233,
    (byte) 30,
    (byte) 120
  ];
  private static readonly byte[] _generator40 =
  [
    (byte) 59,
    (byte) 116,
    (byte) 79,
    (byte) 161,
    (byte) 252,
    (byte) 98,
    (byte) 128 /*0x80*/,
    (byte) 205,
    (byte) 128 /*0x80*/,
    (byte) 161,
    (byte) 247,
    (byte) 57,
    (byte) 163,
    (byte) 56,
    (byte) 235,
    (byte) 106,
    (byte) 53,
    (byte) 26,
    (byte) 187,
    (byte) 174,
    (byte) 226,
    (byte) 104,
    (byte) 170,
    (byte) 7,
    (byte) 175,
    (byte) 35,
    (byte) 181,
    (byte) 114,
    (byte) 88,
    (byte) 41,
    (byte) 47,
    (byte) 163,
    (byte) 125,
    (byte) 134,
    (byte) 72,
    (byte) 20,
    (byte) 232,
    (byte) 53,
    (byte) 35,
    (byte) 15
  ];
  private static readonly byte[] _generator42 =
  [
    (byte) 250,
    (byte) 103,
    (byte) 221,
    (byte) 230,
    (byte) 25,
    (byte) 18,
    (byte) 137,
    (byte) 231,
    (byte) 0,
    (byte) 3,
    (byte) 58,
    (byte) 242,
    (byte) 221,
    (byte) 191,
    (byte) 110,
    (byte) 84,
    (byte) 230,
    (byte) 8,
    (byte) 188,
    (byte) 106,
    (byte) 96 /*0x60*/,
    (byte) 147,
    (byte) 15,
    (byte) 131,
    (byte) 139,
    (byte) 34,
    (byte) 101,
    (byte) 223,
    (byte) 39,
    (byte) 101,
    (byte) 213,
    (byte) 199,
    (byte) 237,
    (byte) 254,
    (byte) 201,
    (byte) 123,
    (byte) 171,
    (byte) 162,
    (byte) 194,
    (byte) 117,
    (byte) 50,
    (byte) 96 /*0x60*/
  ];
  private static readonly byte[] _generator44 =
  [
    (byte) 190,
    (byte) 7,
    (byte) 61,
    (byte) 121,
    (byte) 71,
    (byte) 246,
    (byte) 69,
    (byte) 55,
    (byte) 168,
    (byte) 188,
    (byte) 89,
    (byte) 243,
    (byte) 191,
    (byte) 25,
    (byte) 72,
    (byte) 123,
    (byte) 9,
    (byte) 145,
    (byte) 14,
    (byte) 247,
    (byte) 1,
    (byte) 238,
    (byte) 44,
    (byte) 78,
    (byte) 143,
    (byte) 62,
    (byte) 224 /*0xE0*/,
    (byte) 126,
    (byte) 118,
    (byte) 114,
    (byte) 68,
    (byte) 163,
    (byte) 52,
    (byte) 194,
    (byte) 217,
    (byte) 147,
    (byte) 204,
    (byte) 169,
    (byte) 37,
    (byte) 130,
    (byte) 113,
    (byte) 102,
    (byte) 73,
    (byte) 181
  ];
  private static readonly byte[] _generator46 =
  [
    (byte) 112 /*0x70*/,
    (byte) 94,
    (byte) 88,
    (byte) 112 /*0x70*/,
    (byte) 253,
    (byte) 224 /*0xE0*/,
    (byte) 202,
    (byte) 115,
    (byte) 187,
    (byte) 99,
    (byte) 89,
    (byte) 5,
    (byte) 54,
    (byte) 113,
    (byte) 129,
    (byte) 44,
    (byte) 58,
    (byte) 16 /*0x10*/,
    (byte) 135,
    (byte) 216,
    (byte) 169,
    (byte) 211,
    (byte) 36,
    (byte) 1,
    (byte) 4,
    (byte) 96 /*0x60*/,
    (byte) 60,
    (byte) 241,
    (byte) 73,
    (byte) 104,
    (byte) 234,
    (byte) 8,
    (byte) 249,
    (byte) 245,
    (byte) 119,
    (byte) 174,
    (byte) 52,
    (byte) 25,
    (byte) 157,
    (byte) 224 /*0xE0*/,
    (byte) 43,
    (byte) 202,
    (byte) 223,
    (byte) 19,
    (byte) 82,
    (byte) 15
  ];
  private static readonly byte[] _generator48 =
  [
    (byte) 228,
    (byte) 25,
    (byte) 196,
    (byte) 130,
    (byte) 211,
    (byte) 146,
    (byte) 60,
    (byte) 24,
    (byte) 251,
    (byte) 90,
    (byte) 39,
    (byte) 102,
    (byte) 240 /*0xF0*/,
    (byte) 61,
    (byte) 178,
    (byte) 63 /*0x3F*/,
    (byte) 46,
    (byte) 123,
    (byte) 115,
    (byte) 18,
    (byte) 221,
    (byte) 111,
    (byte) 135,
    (byte) 160 /*0xA0*/,
    (byte) 182,
    (byte) 205,
    (byte) 107,
    (byte) 206,
    (byte) 95,
    (byte) 150,
    (byte) 120,
    (byte) 184,
    (byte) 91,
    (byte) 21,
    (byte) 247,
    (byte) 156,
    (byte) 140,
    (byte) 238,
    (byte) 191,
    (byte) 11,
    (byte) 94,
    (byte) 227,
    (byte) 84,
    (byte) 50,
    (byte) 163,
    (byte) 39,
    (byte) 34,
    (byte) 108
  ];
  private static readonly byte[] _generator50 =
  [
    (byte) 232,
    (byte) 125,
    (byte) 157,
    (byte) 161,
    (byte) 164,
    (byte) 9,
    (byte) 118,
    (byte) 46,
    (byte) 209,
    (byte) 99,
    (byte) 203,
    (byte) 193,
    (byte) 35,
    (byte) 3,
    (byte) 209,
    (byte) 111,
    (byte) 195,
    (byte) 242,
    (byte) 203,
    (byte) 225,
    (byte) 46,
    (byte) 13,
    (byte) 32 /*0x20*/,
    (byte) 160 /*0xA0*/,
    (byte) 126,
    (byte) 209,
    (byte) 130,
    (byte) 160 /*0xA0*/,
    (byte) 242,
    (byte) 215,
    (byte) 242,
    (byte) 75,
    (byte) 77,
    (byte) 42,
    (byte) 189,
    (byte) 32 /*0x20*/,
    (byte) 113,
    (byte) 65,
    (byte) 124,
    (byte) 69,
    (byte) 228,
    (byte) 114,
    (byte) 235,
    (byte) 175,
    (byte) 124,
    (byte) 170,
    (byte) 215,
    (byte) 232,
    (byte) 133,
    (byte) 205
  ];
  private static readonly byte[] _generator52 =
  [
    (byte) 116,
    (byte) 50,
    (byte) 86,
    (byte) 186,
    (byte) 50,
    (byte) 220,
    (byte) 251,
    (byte) 89,
    (byte) 192 /*0xC0*/,
    (byte) 46,
    (byte) 86,
    (byte) 127 /*0x7F*/,
    (byte) 124,
    (byte) 19,
    (byte) 184,
    (byte) 233,
    (byte) 151,
    (byte) 215,
    (byte) 22,
    (byte) 14,
    (byte) 59,
    (byte) 145,
    (byte) 37,
    (byte) 242,
    (byte) 203,
    (byte) 134,
    (byte) 254,
    (byte) 89,
    (byte) 190,
    (byte) 94,
    (byte) 59,
    (byte) 65,
    (byte) 124,
    (byte) 113,
    (byte) 100,
    (byte) 233,
    (byte) 235,
    (byte) 121,
    (byte) 22,
    (byte) 76,
    (byte) 86,
    (byte) 97,
    (byte) 39,
    (byte) 242,
    (byte) 200,
    (byte) 220,
    (byte) 101,
    (byte) 33,
    (byte) 239,
    (byte) 254,
    (byte) 116,
    (byte) 51
  ];
  private static readonly byte[] _generator54 =
  [
    (byte) 183,
    (byte) 26,
    (byte) 201,
    (byte) 84,
    (byte) 210,
    (byte) 221,
    (byte) 113,
    (byte) 21,
    (byte) 46,
    (byte) 65,
    (byte) 45,
    (byte) 50,
    (byte) 238,
    (byte) 184,
    (byte) 249,
    (byte) 225,
    (byte) 102,
    (byte) 58,
    (byte) 209,
    (byte) 218,
    (byte) 109,
    (byte) 165,
    (byte) 26,
    (byte) 95,
    (byte) 184,
    (byte) 192 /*0xC0*/,
    (byte) 52,
    (byte) 245,
    (byte) 35,
    (byte) 254,
    (byte) 238,
    (byte) 175,
    (byte) 172,
    (byte) 79,
    (byte) 123,
    (byte) 25,
    (byte) 122,
    (byte) 43,
    (byte) 120,
    (byte) 108,
    (byte) 215,
    (byte) 80 /*0x50*/,
    (byte) 128 /*0x80*/,
    (byte) 201,
    (byte) 235,
    (byte) 8,
    (byte) 153,
    (byte) 59,
    (byte) 101,
    (byte) 31 /*0x1F*/,
    (byte) 198,
    (byte) 76,
    (byte) 31 /*0x1F*/,
    (byte) 156
  ];
  private static readonly byte[] _generator56 =
  [
    (byte) 106,
    (byte) 120,
    (byte) 107,
    (byte) 157,
    (byte) 164,
    (byte) 216,
    (byte) 112 /*0x70*/,
    (byte) 116,
    (byte) 2,
    (byte) 91,
    (byte) 248,
    (byte) 163,
    (byte) 36,
    (byte) 201,
    (byte) 202,
    (byte) 229,
    (byte) 6,
    (byte) 144 /*0x90*/,
    (byte) 254,
    (byte) 155,
    (byte) 135,
    (byte) 208 /*0xD0*/,
    (byte) 170,
    (byte) 209,
    (byte) 12,
    (byte) 139,
    (byte) 127 /*0x7F*/,
    (byte) 142,
    (byte) 182,
    (byte) 249,
    (byte) 177,
    (byte) 174,
    (byte) 190,
    (byte) 28,
    (byte) 10,
    (byte) 85,
    (byte) 239,
    (byte) 184,
    (byte) 101,
    (byte) 124,
    (byte) 152,
    (byte) 206,
    (byte) 96 /*0x60*/,
    (byte) 23,
    (byte) 163,
    (byte) 61,
    (byte) 27,
    (byte) 196,
    (byte) 247,
    (byte) 151,
    (byte) 154,
    (byte) 202,
    (byte) 207,
    (byte) 20,
    (byte) 61,
    (byte) 10
  ];
  private static readonly byte[] _generator58 =
  [
    (byte) 82,
    (byte) 116,
    (byte) 26,
    (byte) 247,
    (byte) 66,
    (byte) 27,
    (byte) 62,
    (byte) 107,
    (byte) 252,
    (byte) 182,
    (byte) 200,
    (byte) 185,
    (byte) 235,
    (byte) 55,
    (byte) 251,
    (byte) 242,
    (byte) 210,
    (byte) 144 /*0x90*/,
    (byte) 154,
    (byte) 237,
    (byte) 176 /*0xB0*/,
    (byte) 141,
    (byte) 192 /*0xC0*/,
    (byte) 248,
    (byte) 152,
    (byte) 249,
    (byte) 206,
    (byte) 85,
    (byte) 253,
    (byte) 142,
    (byte) 65,
    (byte) 165,
    (byte) 125,
    (byte) 23,
    (byte) 24,
    (byte) 30,
    (byte) 122,
    (byte) 240 /*0xF0*/,
    (byte) 214,
    (byte) 6,
    (byte) 129,
    (byte) 218,
    (byte) 29,
    (byte) 145,
    (byte) 127 /*0x7F*/,
    (byte) 134,
    (byte) 206,
    (byte) 245,
    (byte) 117,
    (byte) 29,
    (byte) 41,
    (byte) 63 /*0x3F*/,
    (byte) 159,
    (byte) 142,
    (byte) 233,
    (byte) 125,
    (byte) 148,
    (byte) 123
  ];
  private static readonly byte[] _generator60 =
  [
    (byte) 107,
    (byte) 140,
    (byte) 26,
    (byte) 12,
    (byte) 9,
    (byte) 141,
    (byte) 243,
    (byte) 197,
    (byte) 226,
    (byte) 197,
    (byte) 219,
    (byte) 45,
    (byte) 211,
    (byte) 101,
    (byte) 219,
    (byte) 120,
    (byte) 28,
    (byte) 181,
    (byte) 127 /*0x7F*/,
    (byte) 6,
    (byte) 100,
    (byte) 247,
    (byte) 2,
    (byte) 205,
    (byte) 198,
    (byte) 57,
    (byte) 115,
    (byte) 219,
    (byte) 101,
    (byte) 109,
    (byte) 160 /*0xA0*/,
    (byte) 82,
    (byte) 37,
    (byte) 38,
    (byte) 238,
    (byte) 49,
    (byte) 160 /*0xA0*/,
    (byte) 209,
    (byte) 121,
    (byte) 86,
    (byte) 11,
    (byte) 124,
    (byte) 30,
    (byte) 181,
    (byte) 84,
    (byte) 25,
    (byte) 194,
    (byte) 87,
    (byte) 65,
    (byte) 102,
    (byte) 190,
    (byte) 220,
    (byte) 70,
    (byte) 27,
    (byte) 209,
    (byte) 16 /*0x10*/,
    (byte) 89,
    (byte) 7,
    (byte) 33,
    (byte) 240 /*0xF0*/
  ];
  private static readonly byte[] _generator62 =
  [
    (byte) 65,
    (byte) 202,
    (byte) 113,
    (byte) 98,
    (byte) 71,
    (byte) 223,
    (byte) 248,
    (byte) 118,
    (byte) 214,
    (byte) 94,
    (byte) 0,
    (byte) 122,
    (byte) 37,
    (byte) 23,
    (byte) 2,
    (byte) 228,
    (byte) 58,
    (byte) 121,
    (byte) 7,
    (byte) 105,
    (byte) 135,
    (byte) 78,
    (byte) 243,
    (byte) 118,
    (byte) 70,
    (byte) 76,
    (byte) 223,
    (byte) 89,
    (byte) 72,
    (byte) 50,
    (byte) 70,
    (byte) 111,
    (byte) 194,
    (byte) 17,
    (byte) 212,
    (byte) 126,
    (byte) 181,
    (byte) 35,
    (byte) 221,
    (byte) 117,
    (byte) 235,
    (byte) 11,
    (byte) 229,
    (byte) 149,
    (byte) 147,
    (byte) 123,
    (byte) 213,
    (byte) 40,
    (byte) 115,
    (byte) 6,
    (byte) 200,
    (byte) 100,
    (byte) 26,
    (byte) 246,
    (byte) 182,
    (byte) 218,
    (byte) 127 /*0x7F*/,
    (byte) 215,
    (byte) 36,
    (byte) 186,
    (byte) 110,
    (byte) 106
  ];
  private static readonly byte[] _generator64 =
  [
    (byte) 45,
    (byte) 51,
    (byte) 175,
    (byte) 9,
    (byte) 7,
    (byte) 158,
    (byte) 159,
    (byte) 49,
    (byte) 68,
    (byte) 119,
    (byte) 92,
    (byte) 123,
    (byte) 177,
    (byte) 204,
    (byte) 187,
    (byte) 254,
    (byte) 200,
    (byte) 78,
    (byte) 141,
    (byte) 149,
    (byte) 119,
    (byte) 26,
    (byte) 127 /*0x7F*/,
    (byte) 53,
    (byte) 160 /*0xA0*/,
    (byte) 93,
    (byte) 199,
    (byte) 212,
    (byte) 29,
    (byte) 24,
    (byte) 145,
    (byte) 156,
    (byte) 208 /*0xD0*/,
    (byte) 150,
    (byte) 218,
    (byte) 209,
    (byte) 4,
    (byte) 216,
    (byte) 91,
    (byte) 47,
    (byte) 184,
    (byte) 146,
    (byte) 47,
    (byte) 140,
    (byte) 195,
    (byte) 195,
    (byte) 125,
    (byte) 242,
    (byte) 238,
    (byte) 63 /*0x3F*/,
    (byte) 99,
    (byte) 108,
    (byte) 140,
    (byte) 230,
    (byte) 242,
    (byte) 31 /*0x1F*/,
    (byte) 204,
    (byte) 11,
    (byte) 178,
    (byte) 243,
    (byte) 217,
    (byte) 156,
    (byte) 213,
    (byte) 231
  ];
  private static readonly byte[] _generator66 =
  [
    (byte) 5,
    (byte) 118,
    (byte) 222,
    (byte) 180,
    (byte) 136,
    (byte) 136,
    (byte) 162,
    (byte) 51,
    (byte) 46,
    (byte) 117,
    (byte) 13,
    (byte) 215,
    (byte) 81,
    (byte) 17,
    (byte) 139,
    (byte) 247,
    (byte) 197,
    (byte) 171,
    (byte) 95,
    (byte) 173,
    (byte) 65,
    (byte) 137,
    (byte) 178,
    (byte) 68,
    (byte) 111,
    (byte) 95,
    (byte) 101,
    (byte) 41,
    (byte) 72,
    (byte) 214,
    (byte) 169,
    (byte) 197,
    (byte) 95,
    (byte) 7,
    (byte) 44,
    (byte) 154,
    (byte) 77,
    (byte) 111,
    (byte) 236,
    (byte) 40,
    (byte) 121,
    (byte) 143,
    (byte) 63 /*0x3F*/,
    (byte) 87,
    (byte) 80 /*0x50*/,
    (byte) 253,
    (byte) 240 /*0xF0*/,
    (byte) 126,
    (byte) 217,
    (byte) 77,
    (byte) 34,
    (byte) 232,
    (byte) 106,
    (byte) 50,
    (byte) 168,
    (byte) 82,
    (byte) 76,
    (byte) 146,
    (byte) 67,
    (byte) 106,
    (byte) 171,
    (byte) 25,
    (byte) 132,
    (byte) 93,
    (byte) 45,
    (byte) 105
  ];
  private static readonly byte[] _generator68 =
  [
    (byte) 247,
    (byte) 159,
    (byte) 223,
    (byte) 33,
    (byte) 224 /*0xE0*/,
    (byte) 93,
    (byte) 77,
    (byte) 70,
    (byte) 90,
    (byte) 160 /*0xA0*/,
    (byte) 32 /*0x20*/,
    (byte) 254,
    (byte) 43,
    (byte) 150,
    (byte) 84,
    (byte) 101,
    (byte) 190,
    (byte) 205,
    (byte) 133,
    (byte) 52,
    (byte) 60,
    (byte) 202,
    (byte) 165,
    (byte) 220,
    (byte) 203,
    (byte) 151,
    (byte) 93,
    (byte) 84,
    (byte) 15,
    (byte) 84,
    (byte) 253,
    (byte) 173,
    (byte) 160 /*0xA0*/,
    (byte) 89,
    (byte) 227,
    (byte) 52,
    (byte) 199,
    (byte) 97,
    (byte) 95,
    (byte) 231,
    (byte) 52,
    (byte) 177,
    (byte) 41,
    (byte) 125,
    (byte) 137,
    (byte) 241,
    (byte) 166,
    (byte) 225,
    (byte) 118,
    (byte) 2,
    (byte) 54,
    (byte) 32 /*0x20*/,
    (byte) 82,
    (byte) 215,
    (byte) 175,
    (byte) 198,
    (byte) 43,
    (byte) 238,
    (byte) 235,
    (byte) 27,
    (byte) 101,
    (byte) 184,
    (byte) 127 /*0x7F*/,
    (byte) 3,
    (byte) 5,
    (byte) 8,
    (byte) 163,
    (byte) 238
  ];
  internal static readonly byte[]?[] GenArray =
  [
    _generator7,
    null,
    null,
    _generator10,
    null,
    null,
    _generator13,
    null,
    _generator15,
    _generator16,
    _generator17,
    _generator18,
    null,
    _generator20,
    null,
    _generator22,
    null,
    _generator24,
    null,
    _generator26,
    null,
    _generator28,
    null,
    _generator30,
    null,
    _generator32,
    null,
    _generator34,
    null,
    _generator36,
    null,
    null,
    null,
    _generator40,
    null,
    _generator42,
    null,
    _generator44,
    null,
    _generator46,
    null,
    _generator48,
    null,
    _generator50,
    null,
    _generator52,
    null,
    _generator54,
    null,
    _generator56,
    null,
    _generator58,
    null,
    _generator60,
    null,
    _generator62,
    null,
    _generator64,
    null,
    _generator66,
    null,
    _generator68
  ];
  internal static readonly byte[] ExpToInt =
  [
    (byte) 1,
    (byte) 2,
    (byte) 4,
    (byte) 8,
    (byte) 16 /*0x10*/,
    (byte) 32 /*0x20*/,
    (byte) 64 /*0x40*/,
    (byte) 128 /*0x80*/,
    (byte) 29,
    (byte) 58,
    (byte) 116,
    (byte) 232,
    (byte) 205,
    (byte) 135,
    (byte) 19,
    (byte) 38,
    (byte) 76,
    (byte) 152,
    (byte) 45,
    (byte) 90,
    (byte) 180,
    (byte) 117,
    (byte) 234,
    (byte) 201,
    (byte) 143,
    (byte) 3,
    (byte) 6,
    (byte) 12,
    (byte) 24,
    (byte) 48 /*0x30*/,
    (byte) 96 /*0x60*/,
    (byte) 192 /*0xC0*/,
    (byte) 157,
    (byte) 39,
    (byte) 78,
    (byte) 156,
    (byte) 37,
    (byte) 74,
    (byte) 148,
    (byte) 53,
    (byte) 106,
    (byte) 212,
    (byte) 181,
    (byte) 119,
    (byte) 238,
    (byte) 193,
    (byte) 159,
    (byte) 35,
    (byte) 70,
    (byte) 140,
    (byte) 5,
    (byte) 10,
    (byte) 20,
    (byte) 40,
    (byte) 80 /*0x50*/,
    (byte) 160 /*0xA0*/,
    (byte) 93,
    (byte) 186,
    (byte) 105,
    (byte) 210,
    (byte) 185,
    (byte) 111,
    (byte) 222,
    (byte) 161,
    (byte) 95,
    (byte) 190,
    (byte) 97,
    (byte) 194,
    (byte) 153,
    (byte) 47,
    (byte) 94,
    (byte) 188,
    (byte) 101,
    (byte) 202,
    (byte) 137,
    (byte) 15,
    (byte) 30,
    (byte) 60,
    (byte) 120,
    (byte) 240 /*0xF0*/,
    (byte) 253,
    (byte) 231,
    (byte) 211,
    (byte) 187,
    (byte) 107,
    (byte) 214,
    (byte) 177,
    (byte) 127 /*0x7F*/,
    (byte) 254,
    (byte) 225,
    (byte) 223,
    (byte) 163,
    (byte) 91,
    (byte) 182,
    (byte) 113,
    (byte) 226,
    (byte) 217,
    (byte) 175,
    (byte) 67,
    (byte) 134,
    (byte) 17,
    (byte) 34,
    (byte) 68,
    (byte) 136,
    (byte) 13,
    (byte) 26,
    (byte) 52,
    (byte) 104,
    (byte) 208 /*0xD0*/,
    (byte) 189,
    (byte) 103,
    (byte) 206,
    (byte) 129,
    (byte) 31 /*0x1F*/,
    (byte) 62,
    (byte) 124,
    (byte) 248,
    (byte) 237,
    (byte) 199,
    (byte) 147,
    (byte) 59,
    (byte) 118,
    (byte) 236,
    (byte) 197,
    (byte) 151,
    (byte) 51,
    (byte) 102,
    (byte) 204,
    (byte) 133,
    (byte) 23,
    (byte) 46,
    (byte) 92,
    (byte) 184,
    (byte) 109,
    (byte) 218,
    (byte) 169,
    (byte) 79,
    (byte) 158,
    (byte) 33,
    (byte) 66,
    (byte) 132,
    (byte) 21,
    (byte) 42,
    (byte) 84,
    (byte) 168,
    (byte) 77,
    (byte) 154,
    (byte) 41,
    (byte) 82,
    (byte) 164,
    (byte) 85,
    (byte) 170,
    (byte) 73,
    (byte) 146,
    (byte) 57,
    (byte) 114,
    (byte) 228,
    (byte) 213,
    (byte) 183,
    (byte) 115,
    (byte) 230,
    (byte) 209,
    (byte) 191,
    (byte) 99,
    (byte) 198,
    (byte) 145,
    (byte) 63 /*0x3F*/,
    (byte) 126,
    (byte) 252,
    (byte) 229,
    (byte) 215,
    (byte) 179,
    (byte) 123,
    (byte) 246,
    (byte) 241,
    byte.MaxValue,
    (byte) 227,
    (byte) 219,
    (byte) 171,
    (byte) 75,
    (byte) 150,
    (byte) 49,
    (byte) 98,
    (byte) 196,
    (byte) 149,
    (byte) 55,
    (byte) 110,
    (byte) 220,
    (byte) 165,
    (byte) 87,
    (byte) 174,
    (byte) 65,
    (byte) 130,
    (byte) 25,
    (byte) 50,
    (byte) 100,
    (byte) 200,
    (byte) 141,
    (byte) 7,
    (byte) 14,
    (byte) 28,
    (byte) 56,
    (byte) 112 /*0x70*/,
    (byte) 224 /*0xE0*/,
    (byte) 221,
    (byte) 167,
    (byte) 83,
    (byte) 166,
    (byte) 81,
    (byte) 162,
    (byte) 89,
    (byte) 178,
    (byte) 121,
    (byte) 242,
    (byte) 249,
    (byte) 239,
    (byte) 195,
    (byte) 155,
    (byte) 43,
    (byte) 86,
    (byte) 172,
    (byte) 69,
    (byte) 138,
    (byte) 9,
    (byte) 18,
    (byte) 36,
    (byte) 72,
    (byte) 144 /*0x90*/,
    (byte) 61,
    (byte) 122,
    (byte) 244,
    (byte) 245,
    (byte) 247,
    (byte) 243,
    (byte) 251,
    (byte) 235,
    (byte) 203,
    (byte) 139,
    (byte) 11,
    (byte) 22,
    (byte) 44,
    (byte) 88,
    (byte) 176 /*0xB0*/,
    (byte) 125,
    (byte) 250,
    (byte) 233,
    (byte) 207,
    (byte) 131,
    (byte) 27,
    (byte) 54,
    (byte) 108,
    (byte) 216,
    (byte) 173,
    (byte) 71,
    (byte) 142,
    (byte) 1,
    (byte) 2,
    (byte) 4,
    (byte) 8,
    (byte) 16 /*0x10*/,
    (byte) 32 /*0x20*/,
    (byte) 64 /*0x40*/,
    (byte) 128 /*0x80*/,
    (byte) 29,
    (byte) 58,
    (byte) 116,
    (byte) 232,
    (byte) 205,
    (byte) 135,
    (byte) 19,
    (byte) 38,
    (byte) 76,
    (byte) 152,
    (byte) 45,
    (byte) 90,
    (byte) 180,
    (byte) 117,
    (byte) 234,
    (byte) 201,
    (byte) 143,
    (byte) 3,
    (byte) 6,
    (byte) 12,
    (byte) 24,
    (byte) 48 /*0x30*/,
    (byte) 96 /*0x60*/,
    (byte) 192 /*0xC0*/,
    (byte) 157,
    (byte) 39,
    (byte) 78,
    (byte) 156,
    (byte) 37,
    (byte) 74,
    (byte) 148,
    (byte) 53,
    (byte) 106,
    (byte) 212,
    (byte) 181,
    (byte) 119,
    (byte) 238,
    (byte) 193,
    (byte) 159,
    (byte) 35,
    (byte) 70,
    (byte) 140,
    (byte) 5,
    (byte) 10,
    (byte) 20,
    (byte) 40,
    (byte) 80 /*0x50*/,
    (byte) 160 /*0xA0*/,
    (byte) 93,
    (byte) 186,
    (byte) 105,
    (byte) 210,
    (byte) 185,
    (byte) 111,
    (byte) 222,
    (byte) 161,
    (byte) 95,
    (byte) 190,
    (byte) 97,
    (byte) 194,
    (byte) 153,
    (byte) 47,
    (byte) 94,
    (byte) 188,
    (byte) 101,
    (byte) 202,
    (byte) 137,
    (byte) 15,
    (byte) 30,
    (byte) 60,
    (byte) 120,
    (byte) 240 /*0xF0*/,
    (byte) 253,
    (byte) 231,
    (byte) 211,
    (byte) 187,
    (byte) 107,
    (byte) 214,
    (byte) 177,
    (byte) 127 /*0x7F*/,
    (byte) 254,
    (byte) 225,
    (byte) 223,
    (byte) 163,
    (byte) 91,
    (byte) 182,
    (byte) 113,
    (byte) 226,
    (byte) 217,
    (byte) 175,
    (byte) 67,
    (byte) 134,
    (byte) 17,
    (byte) 34,
    (byte) 68,
    (byte) 136,
    (byte) 13,
    (byte) 26,
    (byte) 52,
    (byte) 104,
    (byte) 208 /*0xD0*/,
    (byte) 189,
    (byte) 103,
    (byte) 206,
    (byte) 129,
    (byte) 31 /*0x1F*/,
    (byte) 62,
    (byte) 124,
    (byte) 248,
    (byte) 237,
    (byte) 199,
    (byte) 147,
    (byte) 59,
    (byte) 118,
    (byte) 236,
    (byte) 197,
    (byte) 151,
    (byte) 51,
    (byte) 102,
    (byte) 204,
    (byte) 133,
    (byte) 23,
    (byte) 46,
    (byte) 92,
    (byte) 184,
    (byte) 109,
    (byte) 218,
    (byte) 169,
    (byte) 79,
    (byte) 158,
    (byte) 33,
    (byte) 66,
    (byte) 132,
    (byte) 21,
    (byte) 42,
    (byte) 84,
    (byte) 168,
    (byte) 77,
    (byte) 154,
    (byte) 41,
    (byte) 82,
    (byte) 164,
    (byte) 85,
    (byte) 170,
    (byte) 73,
    (byte) 146,
    (byte) 57,
    (byte) 114,
    (byte) 228,
    (byte) 213,
    (byte) 183,
    (byte) 115,
    (byte) 230,
    (byte) 209,
    (byte) 191,
    (byte) 99,
    (byte) 198,
    (byte) 145,
    (byte) 63 /*0x3F*/,
    (byte) 126,
    (byte) 252,
    (byte) 229,
    (byte) 215,
    (byte) 179,
    (byte) 123,
    (byte) 246,
    (byte) 241,
    byte.MaxValue,
    (byte) 227,
    (byte) 219,
    (byte) 171,
    (byte) 75,
    (byte) 150,
    (byte) 49,
    (byte) 98,
    (byte) 196,
    (byte) 149,
    (byte) 55,
    (byte) 110,
    (byte) 220,
    (byte) 165,
    (byte) 87,
    (byte) 174,
    (byte) 65,
    (byte) 130,
    (byte) 25,
    (byte) 50,
    (byte) 100,
    (byte) 200,
    (byte) 141,
    (byte) 7,
    (byte) 14,
    (byte) 28,
    (byte) 56,
    (byte) 112 /*0x70*/,
    (byte) 224 /*0xE0*/,
    (byte) 221,
    (byte) 167,
    (byte) 83,
    (byte) 166,
    (byte) 81,
    (byte) 162,
    (byte) 89,
    (byte) 178,
    (byte) 121,
    (byte) 242,
    (byte) 249,
    (byte) 239,
    (byte) 195,
    (byte) 155,
    (byte) 43,
    (byte) 86,
    (byte) 172,
    (byte) 69,
    (byte) 138,
    (byte) 9,
    (byte) 18,
    (byte) 36,
    (byte) 72,
    (byte) 144 /*0x90*/,
    (byte) 61,
    (byte) 122,
    (byte) 244,
    (byte) 245,
    (byte) 247,
    (byte) 243,
    (byte) 251,
    (byte) 235,
    (byte) 203,
    (byte) 139,
    (byte) 11,
    (byte) 22,
    (byte) 44,
    (byte) 88,
    (byte) 176 /*0xB0*/,
    (byte) 125,
    (byte) 250,
    (byte) 233,
    (byte) 207,
    (byte) 131,
    (byte) 27,
    (byte) 54,
    (byte) 108,
    (byte) 216,
    (byte) 173,
    (byte) 71,
    (byte) 142,
    (byte) 1
  ];
  internal static readonly byte[] IntToExp =
  [
    (byte) 0,
    (byte) 0,
    (byte) 1,
    (byte) 25,
    (byte) 2,
    (byte) 50,
    (byte) 26,
    (byte) 198,
    (byte) 3,
    (byte) 223,
    (byte) 51,
    (byte) 238,
    (byte) 27,
    (byte) 104,
    (byte) 199,
    (byte) 75,
    (byte) 4,
    (byte) 100,
    (byte) 224 /*0xE0*/,
    (byte) 14,
    (byte) 52,
    (byte) 141,
    (byte) 239,
    (byte) 129,
    (byte) 28,
    (byte) 193,
    (byte) 105,
    (byte) 248,
    (byte) 200,
    (byte) 8,
    (byte) 76,
    (byte) 113,
    (byte) 5,
    (byte) 138,
    (byte) 101,
    (byte) 47,
    (byte) 225,
    (byte) 36,
    (byte) 15,
    (byte) 33,
    (byte) 53,
    (byte) 147,
    (byte) 142,
    (byte) 218,
    (byte) 240 /*0xF0*/,
    (byte) 18,
    (byte) 130,
    (byte) 69,
    (byte) 29,
    (byte) 181,
    (byte) 194,
    (byte) 125,
    (byte) 106,
    (byte) 39,
    (byte) 249,
    (byte) 185,
    (byte) 201,
    (byte) 154,
    (byte) 9,
    (byte) 120,
    (byte) 77,
    (byte) 228,
    (byte) 114,
    (byte) 166,
    (byte) 6,
    (byte) 191,
    (byte) 139,
    (byte) 98,
    (byte) 102,
    (byte) 221,
    (byte) 48 /*0x30*/,
    (byte) 253,
    (byte) 226,
    (byte) 152,
    (byte) 37,
    (byte) 179,
    (byte) 16 /*0x10*/,
    (byte) 145,
    (byte) 34,
    (byte) 136,
    (byte) 54,
    (byte) 208 /*0xD0*/,
    (byte) 148,
    (byte) 206,
    (byte) 143,
    (byte) 150,
    (byte) 219,
    (byte) 189,
    (byte) 241,
    (byte) 210,
    (byte) 19,
    (byte) 92,
    (byte) 131,
    (byte) 56,
    (byte) 70,
    (byte) 64 /*0x40*/,
    (byte) 30,
    (byte) 66,
    (byte) 182,
    (byte) 163,
    (byte) 195,
    (byte) 72,
    (byte) 126,
    (byte) 110,
    (byte) 107,
    (byte) 58,
    (byte) 40,
    (byte) 84,
    (byte) 250,
    (byte) 133,
    (byte) 186,
    (byte) 61,
    (byte) 202,
    (byte) 94,
    (byte) 155,
    (byte) 159,
    (byte) 10,
    (byte) 21,
    (byte) 121,
    (byte) 43,
    (byte) 78,
    (byte) 212,
    (byte) 229,
    (byte) 172,
    (byte) 115,
    (byte) 243,
    (byte) 167,
    (byte) 87,
    (byte) 7,
    (byte) 112 /*0x70*/,
    (byte) 192 /*0xC0*/,
    (byte) 247,
    (byte) 140,
    (byte) 128 /*0x80*/,
    (byte) 99,
    (byte) 13,
    (byte) 103,
    (byte) 74,
    (byte) 222,
    (byte) 237,
    (byte) 49,
    (byte) 197,
    (byte) 254,
    (byte) 24,
    (byte) 227,
    (byte) 165,
    (byte) 153,
    (byte) 119,
    (byte) 38,
    (byte) 184,
    (byte) 180,
    (byte) 124,
    (byte) 17,
    (byte) 68,
    (byte) 146,
    (byte) 217,
    (byte) 35,
    (byte) 32 /*0x20*/,
    (byte) 137,
    (byte) 46,
    (byte) 55,
    (byte) 63 /*0x3F*/,
    (byte) 209,
    (byte) 91,
    (byte) 149,
    (byte) 188,
    (byte) 207,
    (byte) 205,
    (byte) 144 /*0x90*/,
    (byte) 135,
    (byte) 151,
    (byte) 178,
    (byte) 220,
    (byte) 252,
    (byte) 190,
    (byte) 97,
    (byte) 242,
    (byte) 86,
    (byte) 211,
    (byte) 171,
    (byte) 20,
    (byte) 42,
    (byte) 93,
    (byte) 158,
    (byte) 132,
    (byte) 60,
    (byte) 57,
    (byte) 83,
    (byte) 71,
    (byte) 109,
    (byte) 65,
    (byte) 162,
    (byte) 31 /*0x1F*/,
    (byte) 45,
    (byte) 67,
    (byte) 216,
    (byte) 183,
    (byte) 123,
    (byte) 164,
    (byte) 118,
    (byte) 196,
    (byte) 23,
    (byte) 73,
    (byte) 236,
    (byte) 127 /*0x7F*/,
    (byte) 12,
    (byte) 111,
    (byte) 246,
    (byte) 108,
    (byte) 161,
    (byte) 59,
    (byte) 82,
    (byte) 41,
    (byte) 157,
    (byte) 85,
    (byte) 170,
    (byte) 251,
    (byte) 96 /*0x60*/,
    (byte) 134,
    (byte) 177,
    (byte) 187,
    (byte) 204,
    (byte) 62,
    (byte) 90,
    (byte) 203,
    (byte) 89,
    (byte) 95,
    (byte) 176 /*0xB0*/,
    (byte) 156,
    (byte) 169,
    (byte) 160 /*0xA0*/,
    (byte) 81,
    (byte) 11,
    (byte) 245,
    (byte) 22,
    (byte) 235,
    (byte) 122,
    (byte) 117,
    (byte) 44,
    (byte) 215,
    (byte) 79,
    (byte) 174,
    (byte) 213,
    (byte) 233,
    (byte) 230,
    (byte) 231,
    (byte) 173,
    (byte) 232,
    (byte) 116,
    (byte) 214,
    (byte) 244,
    (byte) 234,
    (byte) 168,
    (byte) 80 /*0x50*/,
    (byte) 88,
    (byte) 175
  ];
  internal static readonly int[] FormatInfoArray =
  [
    21522,
    20773,
    24188,
    23371,
    17913,
    16590,
    20375,
    19104,
    30660,
    29427,
    32170,
    30877,
    26159,
    25368,
    27713,
    26998,
    5769,
    5054,
    7399,
    6608,
    1890,
    597,
    3340,
    2107,
    13663,
    12392,
    16177,
    14854,
    9396,
    8579,
    11994,
    11245
  ];
  internal static readonly int[,] FormatInfoOne = new int[15, 2]
  {
    {
      0,
      8
    },
    {
      1,
      8
    },
    {
      2,
      8
    },
    {
      3,
      8
    },
    {
      4,
      8
    },
    {
      5,
      8
    },
    {
      7,
      8
    },
    {
      8,
      8
    },
    {
      8,
      7
    },
    {
      8,
      5
    },
    {
      8,
      4
    },
    {
      8,
      3
    },
    {
      8,
      2
    },
    {
      8,
      1
    },
    {
      8,
      0
    }
  };
  internal static readonly int[,] FormatInfoTwo = new int[15, 2]
  {
    {
      8,
      -1
    },
    {
      8,
      -2
    },
    {
      8,
      -3
    },
    {
      8,
      -4
    },
    {
      8,
      -5
    },
    {
      8,
      -6
    },
    {
      8,
      -7
    },
    {
      8,
      -8
    },
    {
      -7,
      8
    },
    {
      -6,
      8
    },
    {
      -5,
      8
    },
    {
      -4,
      8
    },
    {
      -3,
      8
    },
    {
      -2,
      8
    },
    {
      -1,
      8
    }
  };
  internal static readonly int[] VersionCodeArray =
  [
    31892,
    34236,
    39577,
    42195,
    48118,
    51042,
    55367,
    58893,
    63784,
    68472,
    70749,
    76311,
    79154,
    84390,
    87683,
    92361,
    96236,
    102084,
    102881,
    110507,
    110734,
    117786,
    119615,
    126325,
    127568,
    133589,
    136944,
    141498,
    145311,
    150283,
    152622,
    158308,
    161089,
    167017
  ];
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
  internal static readonly byte[,] FinderPatternTopLeft = new byte[9, 9]
  {
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2
    }
  };
  internal static readonly byte[,] FinderPatternTopRight = new byte[9, 8]
  {
    {
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7
    },
    {
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6
    },
    {
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2,
      (byte) 2
    }
  };
  internal static readonly byte[,] FinderPatternBottomLeft = new byte[8, 9]
  {
    {
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 2
    },
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 6,
      (byte) 2
    }
  };
  internal static readonly byte[,] AlignmentPattern = new byte[5, 5]
  {
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 7,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 7,
      (byte) 6,
      (byte) 6,
      (byte) 6,
      (byte) 7
    },
    {
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7,
      (byte) 7
    }
  };

   public bool[,] QRCodeMatrix { get; private set; } = new bool[0, 0]; 

  public int QRCodeVersion { get; private set; }

  public int QRCodeDimension { get; private set; }

  public ErrorCorrection ErrorCorrection
  {
    get => _errorCorrection;
    set
    {
      _errorCorrection = value >= ErrorCorrection.L && value <= ErrorCorrection.H ? value : throw new ArgumentException("Error correction is invalid. Must be L, M, Q or H. Default is M");
    }
  }

  public int ECIAssignValue
  {
    get => _eCIAssignValue;
    set
    {
      _eCIAssignValue = value >= -1 && value <= 999999 ? value : throw new ArgumentException("ECI Assignment Value must be 0-999999 or -1 for none");
    }
  }

   public QrCode() { }

   public QrCode(string stringDataSegment) : this()
   {
      _encode(stringDataSegment);
   }

   public QrCode(string[] stringDataSegments) : this()
   {
      _encode(stringDataSegments);
   }

   public QrCode(byte[] singleDataSeg) : this()
   {
      _encode(singleDataSeg);
   }

   public QrCode(byte[][] dataSegArray) : this()
   {
      _encode(dataSegArray);
   }

  private bool[,] _encode(string StringDataSegment)
  {
    return !string.IsNullOrEmpty(StringDataSegment) ? _encode(
    [
      Encoding.UTF8.GetBytes(StringDataSegment)
    ]) : throw new ArgumentException("String data segment is null or missing");
  }

   private bool[,] _encode(string[] StringDataSegments)
  {
    if (StringDataSegments == null || StringDataSegments.Length == 0)
      throw new ArgumentException("String data segments are null or empty");
    for (int index = 0; index < StringDataSegments.Length; ++index)
    {
      if (StringDataSegments[index] == null)
        throw new ArgumentException("One of the string data segments is null or empty");
    }
    byte[][] DataSegArray = new byte[StringDataSegments.Length][];
    for (int index = 0; index < StringDataSegments.Length; ++index)
      DataSegArray[index] = Encoding.UTF8.GetBytes(StringDataSegments[index]);
    return _encode(DataSegArray);
  }

   private bool[,] _encode(byte[] SingleDataSeg)
  {
    return SingleDataSeg != null && SingleDataSeg.Length != 0 ? _encode(
    [
      SingleDataSeg
    ]) : throw new ArgumentException("Single data segment argument is null or empty");
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
        if (((int) _resultMatrix[index1, index2] & 1) != 0)
          QRCodeMatrix[index1, index2] = true;
      }
    }
    return QRCodeMatrix;
  }

  private void _initialization()
  {
    _encodingSegMode = new EncodingMode[_dataSegArray.Length];
    _encodedDataBits = 0;
    if (_eCIAssignValue >= 0)
      _encodedDataBits = _eCIAssignValue > (int) sbyte.MaxValue ? (_eCIAssignValue > 16383 /*0x3FFF*/ ? 28 : 20) : 12;
    for (int index1 = 0; index1 < _dataSegArray.Length; ++index1)
    {
      byte[] dataSeg = _dataSegArray[index1];
      int length = dataSeg.Length;
      EncodingMode encodingMode = EncodingMode.Numeric;
      for (int index2 = 0; index2 < length; ++index2)
      {
        int num = (int) EncodingTable[(int) dataSeg[index2]];
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
      QRCodeDimension = 17 + 4 * QRCodeVersion;
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
    if (_eCIAssignValue >= 0)
    {
      _saveBitsToCodewordsArray(7, 4);
      if (_eCIAssignValue <= (int) sbyte.MaxValue)
        _saveBitsToCodewordsArray(_eCIAssignValue, 8);
      else if (_eCIAssignValue <= 16383 /*0x3FFF*/)
      {
        _saveBitsToCodewordsArray(_eCIAssignValue >> 8 | 128 /*0x80*/, 8);
        _saveBitsToCodewordsArray(_eCIAssignValue & (int) byte.MaxValue, 8);
      }
      else
      {
        _saveBitsToCodewordsArray(_eCIAssignValue >> 16 /*0x10*/ | 192 /*0xC0*/, 8);
        _saveBitsToCodewordsArray(_eCIAssignValue >> 8 & (int) byte.MaxValue, 8);
        _saveBitsToCodewordsArray(_eCIAssignValue & (int) byte.MaxValue, 8);
      }
    }
    for (int index1 = 0; index1 < _dataSegArray.Length; ++index1)
    {
      byte[] dataSeg = _dataSegArray[index1];
      int length = dataSeg.Length;
      _saveBitsToCodewordsArray((int) _encodingSegMode[index1], 4);
      _saveBitsToCodewordsArray(length, _dataLengthBits(_encodingSegMode[index1]));
      switch (_encodingSegMode[index1])
      {
        case EncodingMode.Numeric:
          int index2 = length / 3 * 3;
          for (int index3 = 0; index3 < index2; index3 += 3)
            _saveBitsToCodewordsArray(100 * (int) EncodingTable[(int) dataSeg[index3]] + 10 * (int) EncodingTable[(int) dataSeg[index3 + 1]] + (int) EncodingTable[(int) dataSeg[index3 + 2]], 10);
          if (length - index2 == 1)
          {
            _saveBitsToCodewordsArray((int) EncodingTable[(int) dataSeg[index2]], 4);
            break;
          }
          if (length - index2 == 2)
          {
            _saveBitsToCodewordsArray(10 * (int) EncodingTable[(int) dataSeg[index2]] + (int) EncodingTable[(int) dataSeg[index2 + 1]], 7);
            break;
          }
          break;
        case EncodingMode.AlphaNumeric:
          int index4 = length / 2 * 2;
          for (int index5 = 0; index5 < index4; index5 += 2)
            _saveBitsToCodewordsArray(45 * (int) EncodingTable[(int) dataSeg[index5]] + (int) EncodingTable[(int) dataSeg[index5 + 1]], 11);
          if (length - index4 == 1)
          {
            _saveBitsToCodewordsArray((int) EncodingTable[(int) dataSeg[index4]], 6);
            break;
          }
          break;
        case EncodingMode.Byte:
          for (int index6 = 0; index6 < length; ++index6)
            _saveBitsToCodewordsArray((int) dataSeg[index6], 8);
          break;
      }
    }
    if (_encodedDataBits < _maxDataBits)
      _saveBitsToCodewordsArray(0, _maxDataBits - _encodedDataBits < 4 ? _maxDataBits - _encodedDataBits : 4);
    if (_bitBufferLen > 0)
      _codewordsArray[_codewordsPtr++] = (byte) (_bitBuffer >> 24);
    int num = _maxDataCodewords - _codewordsPtr;
    for (int index = 0; index < num; ++index)
      _codewordsArray[_codewordsPtr + index] = (index & 1) == 0 ? (byte) 236 : (byte) 17;
  }

  private void _saveBitsToCodewordsArray(int Data, int Bits)
  {
    _bitBuffer |= (uint) (Data << 32 /*0x20*/ - _bitBufferLen - Bits);
    for (_bitBufferLen += Bits; _bitBufferLen >= 8; _bitBufferLen -= 8)
    {
      _codewordsArray[_codewordsPtr++] = (byte) (_bitBuffer >> 24);
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
      Array.Copy((Array) _codewordsArray, sourceIndex, (Array) numArray, 0, num1);
      Array.Clear((Array) numArray, num1, _errCorrCodewords);
      sourceIndex += num1;
      _polynominalDivision(numArray, PolyLength, gen, _errCorrCodewords);
      Array.Copy((Array) numArray, num1, (Array) _codewordsArray, maxDataCodewords, _errCorrCodewords);
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
      if (Polynomial[index1] != (byte) 0)
      {
        int num2 = (int) IntToExp[(int) Polynomial[index1]];
        for (int index2 = 0; index2 < ErrCorrCodewords; ++index2)
          Polynomial[index1 + 1 + index2] = (byte) ((uint) Polynomial[index1 + 1 + index2] ^ (uint) ExpToInt[(int) _generator[index2] + num2]);
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
      if (((int) _baseMatrix[index1, index2] & 2) == 0)
      {
        if (((int) _codewordsArray[num1 >> 3] & 1 << 7 - (num1 & 7)) != 0)
          _baseMatrix[index1, index2] = (byte) 1;
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
              _maskMatrix = new byte[0,0];
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
        if ((((int) _maskMatrix[index1, index2 - 1] ^ (int) _maskMatrix[index1, index2]) & 1) != 0)
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
        if ((((int) _maskMatrix[index4 - 1, index3] ^ (int) _maskMatrix[index4, index3]) & 1) != 0)
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
        if (((int) _maskMatrix[index1 - 1, index2 - 1] & (int) _maskMatrix[index1 - 1, index2] & (int) _maskMatrix[index1, index2 - 1] & (int) _maskMatrix[index1, index2] & 1) != 0)
          num += 3;
        else if ((((int) _maskMatrix[index1 - 1, index2 - 1] | (int) _maskMatrix[index1 - 1, index2] | (int) _maskMatrix[index1, index2 - 1] | (int) _maskMatrix[index1, index2]) & 1) == 0)
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
        if (((int) _maskMatrix[Row, Col] & 1) != 0)
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
        if (((int) _maskMatrix[Row, Col] & 1) != 0)
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
        if (((int) _maskMatrix[index1, index2] & 1) != 0)
          ++num1;
      }
    }
    double num2 = (double) num1 / (double) (QRCodeDimension * QRCodeDimension);
    if (num2 > 0.55)
      return (int) (20.0 * (num2 - 0.5)) * 10;
    return num2 < 0.45 ? (int) (20.0 * (0.5 - num2)) * 10 : 0;
  }

  private bool _testHorizontalDarkLight(int Row, int Col)
  {
    return ((int) _maskMatrix[Row, Col] & (int) ~_maskMatrix[Row, Col + 1] & (int) _maskMatrix[Row, Col + 2] & (int) _maskMatrix[Row, Col + 3] & (int) _maskMatrix[Row, Col + 4] & (int) ~_maskMatrix[Row, Col + 5] & (int) _maskMatrix[Row, Col + 6] & 1) != 0;
  }

  private bool _testVerticalDarkLight(int Row, int Col)
  {
    return ((int) _maskMatrix[Row, Col] & (int) ~_maskMatrix[Row + 1, Col] & (int) _maskMatrix[Row + 2, Col] & (int) _maskMatrix[Row + 3, Col] & (int) _maskMatrix[Row + 4, Col] & (int) ~_maskMatrix[Row + 5, Col] & (int) _maskMatrix[Row + 6, Col] & 1) != 0;
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
          _resultMatrix[index1, num1 + index2] = (versionCode & num2) != 0 ? (byte) 7 : (byte) 6;
          num2 <<= 1;
        }
      }
      int num3 = 1;
      for (int index3 = 0; index3 < 6; ++index3)
      {
        for (int index4 = 0; index4 < 3; ++index4)
        {
          _resultMatrix[num1 + index4, index3] = (versionCode & num3) != 0 ? (byte) 7 : (byte) 6;
          num3 <<= 1;
        }
      }
    }
    int num4 = 0;
    switch (_errorCorrection)
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
      _resultMatrix[FormatInfoOne[index5, 0], FormatInfoOne[index5, 1]] = (byte) num6;
      int index6 = FormatInfoTwo[index5, 0];
      if (index6 < 0)
        index6 += QRCodeDimension;
      int index7 = FormatInfoTwo[index5, 1];
      if (index7 < 0)
        index7 += QRCodeDimension;
      _resultMatrix[index6, index7] = (byte) num6;
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
    int index = (int) ((QRCodeVersion - 1) * 4 + _errorCorrection);
    _blocksGroup1 = (int) ECBlockInfo[index, 0];
    _dataCodewordsGroup1 = (int) ECBlockInfo[index, 1];
    _blocksGroup2 = (int) ECBlockInfo[index, 2];
    _dataCodewordsGroup2 = (int) ECBlockInfo[index, 3];
    _maxDataCodewords = _blocksGroup1 * _dataCodewordsGroup1 + _blocksGroup2 * _dataCodewordsGroup2;
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
      _baseMatrix[index, 6] = _baseMatrix[6, index] = (index & 1) == 0 ? (byte) 7 : (byte) 6;
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
            int num2 = (int) alignmentPosition[index7];
            int num3 = (int) alignmentPosition[index8];
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
        _baseMatrix[index11, num4 + index12] = (byte) 2;
    }
    for (int index13 = 0; index13 < 6; ++index13)
    {
      for (int index14 = 0; index14 < 3; ++index14)
        _baseMatrix[num4 + index14, index13] = (byte) 2;
    }
  }

  private void _applyMask(int Mask)
  {
    _maskMatrix = (byte[,]) _baseMatrix.Clone();
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
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 1] ^= (byte) 1;
      }
    }
  }

  private void _applyMask1()
  {
    for (int index1 = 0; index1 < QRCodeDimension; index1 += 2)
    {
      for (int index2 = 0; index2 < QRCodeDimension; ++index2)
      {
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
      }
    }
  }

  private void _applyMask2()
  {
    for (int index1 = 0; index1 < QRCodeDimension; ++index1)
    {
      for (int index2 = 0; index2 < QRCodeDimension; index2 += 3)
      {
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
      }
    }
  }

  private void _applyMask3()
  {
    for (int index1 = 0; index1 < QRCodeDimension; index1 += 3)
    {
      for (int index2 = 0; index2 < QRCodeDimension; index2 += 3)
      {
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 1] ^= (byte) 1;
      }
    }
  }

  private void _applyMask4()
  {
    for (int index1 = 0; index1 < QRCodeDimension; index1 += 4)
    {
      for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
      {
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1, index2 + 1] & 2) == 0)
          _maskMatrix[index1, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1, index2 + 2] & 2) == 0)
          _maskMatrix[index1, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2] & 2) == 0)
          _maskMatrix[index1 + 1, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 5] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 5] ^= (byte) 1;
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
          if (((int) _maskMatrix[index1, index2 + index3] & 2) == 0)
            _maskMatrix[index1, index2 + index3] ^= (byte) 1;
        }
        for (int index4 = 1; index4 < 6; ++index4)
        {
          if (((int) _maskMatrix[index1 + index4, index2] & 2) == 0)
            _maskMatrix[index1 + index4, index2] ^= (byte) 1;
        }
        if (((int) _maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 3] ^= (byte) 1;
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
          if (((int) _maskMatrix[index1, index2 + index3] & 2) == 0)
            _maskMatrix[index1, index2 + index3] ^= (byte) 1;
        }
        for (int index4 = 1; index4 < 6; ++index4)
        {
          if (((int) _maskMatrix[index1 + index4, index2] & 2) == 0)
            _maskMatrix[index1 + index4, index2] ^= (byte) 1;
        }
        if (((int) _maskMatrix[index1 + 1, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 5] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 5, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 5, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 5, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 5, index2 + 5] ^= (byte) 1;
      }
    }
  }

  private void _applyMask7()
  {
    for (int index1 = 0; index1 < QRCodeDimension; index1 += 6)
    {
      for (int index2 = 0; index2 < QRCodeDimension; index2 += 6)
      {
        if (((int) _maskMatrix[index1, index2] & 2) == 0)
          _maskMatrix[index1, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1, index2 + 2] & 2) == 0)
          _maskMatrix[index1, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1, index2 + 4] & 2) == 0)
          _maskMatrix[index1, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 1, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 1, index2 + 5] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2] & 2) == 0)
          _maskMatrix[index1 + 2, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 4] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 4] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 2, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 2, index2 + 5] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 3] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 3, index2 + 5] & 2) == 0)
          _maskMatrix[index1 + 3, index2 + 5] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2] & 2) == 0)
          _maskMatrix[index1 + 4, index2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 4, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 4, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 5, index2 + 1] & 2) == 0)
          _maskMatrix[index1 + 5, index2 + 1] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 5, index2 + 2] & 2) == 0)
          _maskMatrix[index1 + 5, index2 + 2] ^= (byte) 1;
        if (((int) _maskMatrix[index1 + 5, index2 + 3] & 2) == 0)
          _maskMatrix[index1 + 5, index2 + 3] ^= (byte) 1;
      }
    }
  }
}
