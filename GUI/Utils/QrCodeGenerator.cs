using System.IO;
using System.Text;

namespace Upsilon.Apps.Passkey.GUI.Utils
{
   // Énumération pour les modes d'encodage
   public enum QRMode
   {
      Numeric = 1,
      Alphanumeric = 2,
      Byte = 4,
      Kanji = 8
   }

   // Énumération pour les niveaux de correction d'erreur
   public enum ErrorCorrectionLevel
   {
      L = 0, // ~7% de correction
      M = 1, // ~15% de correction
      Q = 2, // ~25% de correction
      H = 3  // ~30% de correction
   }

   public class QRCode
   {
      private bool[,] _modules;
      private readonly int _size;
      private readonly string _data;
      private readonly int _version;
      private readonly ErrorCorrectionLevel _ecLevel;
      private readonly QRMode _mode;

      public QRCode(string data, ErrorCorrectionLevel ecLevel = ErrorCorrectionLevel.M, int? forceVersion = null)
      {
         _data = data;
         _ecLevel = ecLevel;
         _modules = new bool[_size, _size];

         // Déterminer le meilleur mode d'encodage
         _mode = _determineMode(data);

         // Déterminer la version minimale nécessaire
         _version = forceVersion ?? _determineMinVersion(data, _mode, ecLevel);

         if (_version < 1 || _version > 40)
            throw new ArgumentException("Version QR Code doit être entre 1 et 40");

         _size = (_version * 4) + 17;
         _generate();
      }

      private static QRMode _determineMode(string data)
      {
         if (string.IsNullOrEmpty(data)) return QRMode.Byte;

         // Vérifier mode numérique
         if (data.All(c => c >= '0' && c <= '9'))
            return QRMode.Numeric;

         // Vérifier mode alphanumérique
         const string alphanumChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
         if (data.All(c => alphanumChars.Contains(c)))
            return QRMode.Alphanumeric;

         // Vérifier mode Kanji (basique)
         return data.All(c => (c >= 0x8140 && c <= 0x9FFC) || (c >= 0xE040 && c <= 0xEBBF)) ? QRMode.Kanji : QRMode.Byte;
      }

      private static int _determineMinVersion(string data, QRMode mode, ErrorCorrectionLevel ecLevel)
      {
         for (int v = 1; v <= 40; v++)
         {
            int capacity = _getCapacity(v, ecLevel);
            int required = _getRequiredCapacity(data, mode, v);

            if (capacity >= required)
               return v;
         }

         throw new ArgumentException("Données trop longues pour un QR Code");
      }

      private static int _getRequiredCapacity(string data, QRMode mode, int version)
      {
         int bits = 0;

         // Mode indicator
         bits += 4;

         // Character count indicator
         bits += _getCharCountBits(mode, version);

         // Data
         switch (mode)
         {
            case QRMode.Numeric:
               bits += data.Length / 3 * 10;
               if (data.Length % 3 == 2) bits += 7;
               if (data.Length % 3 == 1) bits += 4;
               break;
            case QRMode.Alphanumeric:
               bits += data.Length / 2 * 11;
               if (data.Length % 2 == 1) bits += 6;
               break;
            case QRMode.Byte:
               bits += data.Length * 8;
               break;
            case QRMode.Kanji:
               bits += data.Length * 13;
               break;
         }

         return (bits + 7) / 8; // Convertir en bytes
      }

      private static int _getCharCountBits(QRMode mode, int version)
      {
         return version <= 9
            ? mode switch
            {
               QRMode.Numeric => 10,
               QRMode.Alphanumeric => 9,
               QRMode.Byte => 8,
               QRMode.Kanji => 8,
               _ => 8
            }
            : version <= 26
            ? mode switch
            {
               QRMode.Numeric => 12,
               QRMode.Alphanumeric => 11,
               QRMode.Byte => 16,
               QRMode.Kanji => 10,
               _ => 16
            }
            : mode switch
            {
               QRMode.Numeric => 14,
               QRMode.Alphanumeric => 13,
               QRMode.Byte => 16,
               QRMode.Kanji => 12,
               _ => 16
            };
      }

      private void _generate()
      {
         _modules = new bool[_size, _size];

         // Ajouter les patterns de fonction
         _addFinderPatterns();
         _addSeparators();
         _addAlignmentPatterns();
         _addTimingPatterns();
         _addDarkModule();
         _reserveFormatAreas();

         // Encoder les données
         byte[] dataBytes = _encodeData();
         byte[] codewords = _addErrorCorrection(dataBytes);
         _placeCodewords(codewords);

         // Appliquer le meilleur masque
         int bestMask = _findBestMask();
         _applyMask(bestMask);
         _addFormatInfo(bestMask);
      }

      private void _addFinderPatterns()
      {
         int[][] positions = [[0, 0], [_size - 7, 0], [0, _size - 7]];

         foreach (int[] pos in positions)
         {
            for (int dy = 0; dy < 7; dy++)
            {
               for (int dx = 0; dx < 7; dx++)
               {
                  int x = pos[1] + dx;
                  int y = pos[0] + dy;
                  bool dark = dx == 0 || dx == 6 || dy == 0 || dy == 6 ||
                             (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4);
                  _modules[y, x] = dark;
               }
            }
         }
      }

      private void _addSeparators()
      {
         // Séparateurs autour des finder patterns (blancs)
         for (int i = 0; i < 8; i++)
         {
            _modules[7, i] = false;
            _modules[i, 7] = false;
            _modules[7, _size - 8 + i] = false;
            _modules[i, _size - 8] = false;
            _modules[_size - 8, i] = false;
            _modules[_size - 8 + i, 7] = false;
         }
      }

      private void _addAlignmentPatterns()
      {
         if (_version == 1) return;

         int[] positions = _getAlignmentPatternPositions(_version);

         foreach (int y in positions)
         {
            foreach (int x in positions)
            {
               // Ne pas placer sur les finder patterns
               if ((x == 6 && y == 6) ||
                   (x == 6 && y == _size - 7) ||
                   (x == _size - 7 && y == 6))
                  continue;

               for (int dy = -2; dy <= 2; dy++)
               {
                  for (int dx = -2; dx <= 2; dx++)
                  {
                     bool dark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                     _modules[y + dy, x + dx] = dark;
                  }
               }
            }
         }
      }

      private int[] _getAlignmentPatternPositions(int version)
      {
         if (version == 1) return [];

         int numAlign = (version / 7) + 2;
         int step = version == 32 ? 26 : ((version * 4) + (numAlign * 2) + 1) / ((numAlign * 2) - 2) * 2;

         List<int> result = [];
         for (int i = 0, pos = _size - 7; i < numAlign; i++, pos -= step)
         {
            result.Insert(0, pos);
         }
         result[0] = 6;

         return [.. result];
      }

      private void _addTimingPatterns()
      {
         for (int i = 8; i < _size - 8; i++)
         {
            if (!_isAlignmentPattern(6, i))
               _modules[6, i] = i % 2 == 0;
            if (!_isAlignmentPattern(i, 6))
               _modules[i, 6] = i % 2 == 0;
         }
      }

      private void _addDarkModule()
      {
         _modules[(4 * _version) + 9, 8] = true;
      }

      private static void _reserveFormatAreas()
      {
         // Les zones de format seront remplies après le masquage
      }

      private byte[] _encodeData()
      {
         List<bool> bits = [];

         // Mode indicator
         int modeValue = (int)_mode;
         for (int i = 3; i >= 0; i--)
            bits.Add(((modeValue >> i) & 1) == 1);

         // Character count indicator
         int charCountBits = _getCharCountBits(_mode, _version);
         int charCount = _mode == QRMode.Kanji ? _data.Length : _data.Length;
         for (int i = charCountBits - 1; i >= 0; i--)
            bits.Add(((charCount >> i) & 1) == 1);

         // Encode data selon le mode
         switch (_mode)
         {
            case QRMode.Numeric:
               _encodeNumeric(bits);
               break;
            case QRMode.Alphanumeric:
               _encodeAlphanumeric(bits);
               break;
            case QRMode.Byte:
               _encodeByte(bits);
               break;
            case QRMode.Kanji:
               _encodeKanji(bits);
               break;
         }

         // Terminateur
         int capacity = _getCapacity(_version, _ecLevel) * 8;
         for (int i = 0; i < Math.Min(4, capacity - bits.Count); i++)
            bits.Add(false);

         // Padding pour aligner sur un byte
         while (bits.Count % 8 != 0)
            bits.Add(false);

         // Padding bytes
         byte[] padBytes = [0xEC, 0x11];
         int padIndex = 0;
         while (bits.Count < capacity)
         {
            byte pad = padBytes[padIndex % 2];
            for (int i = 7; i >= 0; i--)
               bits.Add(((pad >> i) & 1) == 1);
            padIndex++;
         }

         // Convertir en bytes
         byte[] bytes = new byte[bits.Count / 8];
         for (int i = 0; i < bytes.Length; i++)
         {
            for (int j = 0; j < 8; j++)
            {
               if (bits[(i * 8) + j])
                  bytes[i] |= (byte)(1 << (7 - j));
            }
         }

         return bytes;
      }

      private void _encodeNumeric(List<bool> bits)
      {
         for (int i = 0; i < _data.Length; i += 3)
         {
            int count = Math.Min(3, _data.Length - i);
            int value = int.Parse(_data.Substring(i, count));
            int bitCount = count == 3 ? 10 : (count == 2 ? 7 : 4);

            for (int j = bitCount - 1; j >= 0; j--)
               bits.Add(((value >> j) & 1) == 1);
         }
      }

      private void _encodeAlphanumeric(List<bool> bits)
      {
         const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

         for (int i = 0; i < _data.Length; i += 2)
         {
            if (i + 1 < _data.Length)
            {
               int value = (charset.IndexOf(_data[i]) * 45) + charset.IndexOf(_data[i + 1]);
               for (int j = 10; j >= 0; j--)
                  bits.Add(((value >> j) & 1) == 1);
            }
            else
            {
               int value = charset.IndexOf(_data[i]);
               for (int j = 5; j >= 0; j--)
                  bits.Add(((value >> j) & 1) == 1);
            }
         }
      }

      private void _encodeByte(List<bool> bits)
      {
         byte[] bytes = Encoding.UTF8.GetBytes(_data);
         foreach (byte b in bytes)
         {
            for (int i = 7; i >= 0; i--)
               bits.Add(((b >> i) & 1) == 1);
         }
      }

      private void _encodeKanji(List<bool> bits)
      {
         byte[] bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(_data);
         for (int i = 0; i < bytes.Length; i += 2)
         {
            int value = (bytes[i] << 8) | bytes[i + 1];

            if (value >= 0x8140 && value <= 0x9FFC)
               value -= 0x8140;
            else if (value >= 0xE040 && value <= 0xEBBF)
               value -= 0xC140;

            value = ((value >> 8) * 0xC0) + (value & 0xFF);

            for (int j = 12; j >= 0; j--)
               bits.Add(((value >> j) & 1) == 1);
         }
      }

      private static int _getCapacity(int version, ErrorCorrectionLevel ecLevel)
      {
         // Table de capacités (data codewords) pour chaque version et niveau EC
         int[,] capacities = new int[41, 4];

         // Version 1-10
         int[,] cap1_10 = {
                {0,0,0,0}, {19,16,13,9}, {34,28,22,16}, {55,44,34,26}, {80,64,48,36},
                {108,86,62,46}, {136,108,76,60}, {156,124,88,66}, {194,154,110,86}, {232,182,132,100}, {274,216,154,122}
            };

         // Copier les données
         for (int v = 0; v <= 10; v++)
            for (int e = 0; e < 4; e++)
               capacities[v, e] = cap1_10[v, e];

         // Versions 11-40 (valeurs approximatives)
         for (int v = 11; v <= 40; v++)
         {
            int base_cap = 274 + ((v - 10) * 50);
            capacities[v, 0] = base_cap;
            capacities[v, 1] = (int)(base_cap * 0.79);
            capacities[v, 2] = (int)(base_cap * 0.56);
            capacities[v, 3] = (int)(base_cap * 0.44);
         }

         return capacities[version, (int)ecLevel];
      }

      private byte[] _addErrorCorrection(byte[] data)
      {
         List<RSBlock> rsBlocks = _getRSBlocks(_version, _ecLevel);
         int totalDataBytes = data.Length;
         int totalECBytes = rsBlocks.Sum(b => b.ecCount * b.blockCount);

         List<byte> result = [];
         int dataOffset = 0;

         // Créer les blocs de données et calculer les codes de correction
         List<byte[]> dataBlocks = [];
         List<byte[]> ecBlocks = [];

         foreach (RSBlock rsBlock in rsBlocks)
         {
            for (int i = 0; i < rsBlock.blockCount; i++)
            {
               byte[] blockData = new byte[rsBlock.dataCount];
               Array.Copy(data, dataOffset, blockData, 0, rsBlock.dataCount);
               dataOffset += rsBlock.dataCount;

               byte[] blockEC = ReedSolomon.Encode(blockData, rsBlock.ecCount);

               dataBlocks.Add(blockData);
               ecBlocks.Add(blockEC);
            }
         }

         // Interleaver les blocs de données
         int maxDataCount = dataBlocks.Max(b => b.Length);
         for (int i = 0; i < maxDataCount; i++)
         {
            foreach (byte[] block in dataBlocks)
            {
               if (i < block.Length)
                  result.Add(block[i]);
            }
         }

         // Interleaver les blocs de correction
         int maxECCount = ecBlocks.Max(b => b.Length);
         for (int i = 0; i < maxECCount; i++)
         {
            foreach (byte[] block in ecBlocks)
            {
               if (i < block.Length)
                  result.Add(block[i]);
            }
         }

         return [.. result];
      }

      private static List<RSBlock> _getRSBlocks(int version, ErrorCorrectionLevel ecLevel)
      {
         // Informations sur les blocs Reed-Solomon
         List<RSBlock> blocks = [];

         // Table simplifiée pour versions 1-10
         if (version == 1)
         {
            if (ecLevel == ErrorCorrectionLevel.L) blocks.Add(new RSBlock(1, 19, 7));
            else if (ecLevel == ErrorCorrectionLevel.M) blocks.Add(new RSBlock(1, 16, 10));
            else if (ecLevel == ErrorCorrectionLevel.Q) blocks.Add(new RSBlock(1, 13, 13));
            else blocks.Add(new RSBlock(1, 9, 17));
         }
         else if (version == 2)
         {
            if (ecLevel == ErrorCorrectionLevel.L) blocks.Add(new RSBlock(1, 34, 10));
            else if (ecLevel == ErrorCorrectionLevel.M) blocks.Add(new RSBlock(1, 28, 16));
            else if (ecLevel == ErrorCorrectionLevel.Q) blocks.Add(new RSBlock(1, 22, 22));
            else blocks.Add(new RSBlock(1, 16, 28));
         }
         else if (version <= 10)
         {
            // Approximation pour les autres versions
            int dataCount = _getCapacity(version, ecLevel);
            int totalBytes = ((((version * 4) + 17) * ((version * 4) + 17)) - 225 - (version > 1 ? 25 : 0)) / 8;
            int ecCount = totalBytes - dataCount;
            blocks.Add(new RSBlock(1, dataCount, ecCount));
         }
         else
         {
            // Pour versions 11-40
            int dataCount = _getCapacity(version, ecLevel);
            int totalBytes = ((((version * 4) + 17) * ((version * 4) + 17)) - 225 - (25 * ((version / 7) + 2))) / 8;
            int ecCount = Math.Max(1, totalBytes - dataCount);

            // Diviser en blocs si nécessaire
            int numBlocks = (version / 10) + 1;
            int dataPerBlock = dataCount / numBlocks;
            int ecPerBlock = ecCount / numBlocks;
            blocks.Add(new RSBlock(numBlocks, dataPerBlock, ecPerBlock));
         }

         return blocks;
      }

      private void _placeCodewords(byte[] codewords)
      {
         int bitIndex = 0;
         bool upward = true;

         for (int right = _size - 1; right >= 1; right -= 2)
         {
            if (right == 6) right = 5;

            for (int vert = 0; vert < _size; vert++)
            {
               int y = upward ? _size - 1 - vert : vert;

               for (int j = 0; j < 2; j++)
               {
                  int x = right - j;

                  if (!_isFunction(y, x) && bitIndex < codewords.Length * 8)
                  {
                     bool bit = ((codewords[bitIndex / 8] >> (7 - (bitIndex % 8))) & 1) == 1;
                     _modules[y, x] = bit;
                     bitIndex++;
                  }
               }
            }

            upward = !upward;
         }
      }

      private bool _isFunction(int y, int x)
      {
         // Finder patterns
         if ((x < 9 && y < 9) || (x >= _size - 8 && y < 9) || (x < 9 && y >= _size - 8))
            return true;

         // Timing patterns
         if (x == 6 || y == 6)
            return true;

         // Alignment patterns
         if (_isAlignmentPattern(y, x))
            return true;

         // Format et version info
         return y == 8 || x == 8;
      }

      private bool _isAlignmentPattern(int y, int x)
      {
         if (_version == 1) return false;

         int[] positions = _getAlignmentPatternPositions(_version);
         foreach (int py in positions)
         {
            foreach (int px in positions)
            {
               if (Math.Abs(y - py) <= 2 && Math.Abs(x - px) <= 2)
               {
                  if (!((px == 6 && py == 6) || (px == 6 && py == _size - 7) || (px == _size - 7 && py == 6)))
                     return true;
               }
            }
         }
         return false;
      }

      private int _findBestMask()
      {
         int bestMask = 0;
         int bestPenalty = int.MaxValue;

         for (int mask = 0; mask < 8; mask++)
         {
            _applyMask(mask);
            int penalty = _calculatePenalty();
            _applyMask(mask); // Retirer le masque

            if (penalty < bestPenalty)
            {
               bestPenalty = penalty;
               bestMask = mask;
            }
         }

         return bestMask;
      }

      private int _calculatePenalty()
      {
         int penalty = 0;

         // Règle 1: modules adjacents de même couleur
         for (int y = 0; y < _size; y++)
         {
            int runColor = _modules[y, 0] ? 1 : 0;
            int runLength = 1;

            for (int x = 1; x < _size; x++)
            {
               if ((_modules[y, x] ? 1 : 0) == runColor)
               {
                  runLength++;
               }
               else
               {
                  if (runLength >= 5)
                     penalty += runLength - 2;
                  runColor = _modules[y, x] ? 1 : 0;
                  runLength = 1;
               }
            }
            if (runLength >= 5)
               penalty += runLength - 2;
         }

         // Même chose pour les colonnes
         for (int x = 0; x < _size; x++)
         {
            int runColor = _modules[0, x] ? 1 : 0;
            int runLength = 1;

            for (int y = 1; y < _size; y++)
            {
               if ((_modules[y, x] ? 1 : 0) == runColor)
               {
                  runLength++;
               }
               else
               {
                  if (runLength >= 5)
                     penalty += runLength - 2;
                  runColor = _modules[y, x] ? 1 : 0;
                  runLength = 1;
               }
            }
            if (runLength >= 5)
               penalty += runLength - 2;
         }

         // Règle 2: blocs 2x2
         for (int y = 0; y < _size - 1; y++)
         {
            for (int x = 0; x < _size - 1; x++)
            {
               bool color = _modules[y, x];
               if (_modules[y, x + 1] == color &&
                   _modules[y + 1, x] == color &&
                   _modules[y + 1, x + 1] == color)
               {
                  penalty += 3;
               }
            }
         }

         // Règle 3 & 4: patterns simplifiés
         penalty += _size * _size / 10;

         return penalty;
      }

      private void _applyMask(int pattern)
      {
         for (int y = 0; y < _size; y++)
         {
            for (int x = 0; x < _size; x++)
            {
               if (!_isFunction(y, x))
               {
                  bool mask = _getMaskPattern(pattern, y, x);
                  if (mask) _modules[y, x] = !_modules[y, x];
               }
            }
         }
      }

      private static bool _getMaskPattern(int pattern, int y, int x)
      {
         return pattern switch
         {
            0 => (y + x) % 2 == 0,
            1 => y % 2 == 0,
            2 => x % 3 == 0,
            3 => (y + x) % 3 == 0,
            4 => ((y / 2) + (x / 3)) % 2 == 0,
            5 => (y * x % 2) + (y * x % 3) == 0,
            6 => ((y * x % 2) + (y * x % 3)) % 2 == 0,
            7 => (((y + x) % 2) + (y * x % 3)) % 2 == 0,
            _ => false
         };
      }

      private void _addFormatInfo(int mask)
      {
         int formatInfo = ((int)_ecLevel << 3) | mask;
         int bch = _calculateBCH(formatInfo, 0x537); // Polynôme BCH pour format
         formatInfo = (formatInfo << 10) | bch;
         formatInfo ^= 0x5412; // Masque XOR

         // Placer les bits de format
         for (int i = 0; i < 15; i++)
         {
            bool bit = ((formatInfo >> i) & 1) == 1;

            if (i < 6)
            {
               _modules[8, i] = bit;
            }
            else if (i < 8)
            {
               _modules[8, i + 1] = bit;
            }
            else if (i < 9)
            {
               _modules[7, 8] = bit;
            }
            else
            {
               _modules[14 - i, 8] = bit;
            }

            // Copie miroir
            if (i < 8)
            {
               _modules[_size - 1 - i, 8] = bit;
            }
            else
            {
               _modules[8, _size - 15 + i] = bit;
            }
         }
      }

      private static int _calculateBCH(int value, int poly)
      {
         int msb = 0;
         for (int i = 0; i < 32; i++)
         {
            if ((poly >> i) != 0)
               msb = i;
         }

         value <<= msb;

         while (_getMSB(value) >= msb)
         {
            value ^= poly << (_getMSB(value) - msb);
         }

         return value;
      }

      private static int _getMSB(int value)
      {
         for (int i = 31; i >= 0; i--)
         {
            if ((value >> i) != 0)
               return i;
         }
         return -1;
      }

      // Méthodes d'export
      public string ToAscii(int scale = 2)
      {
         StringBuilder sb = new();

         for (int i = 0; i < scale; i++)
            _ = sb.AppendLine(new string(' ', (_size + 8) * scale));

         for (int y = 0; y < _size; y++)
         {
            for (int sy = 0; sy < scale; sy++)
            {
               _ = sb.Append(new string(' ', 4 * scale));

               for (int x = 0; x < _size; x++)
               {
                  char pixel = _modules[y, x] ? '█' : ' ';
                  _ = sb.Append(new string(pixel, scale));
               }

               _ = sb.AppendLine(new string(' ', 4 * scale));
            }
         }

         for (int i = 0; i < scale; i++)
            _ = sb.AppendLine(new string(' ', (_size + 8) * scale));

         return sb.ToString();
      }

      public string ToSVG(int moduleSize = 10)
      {
         int quietZone = 4;
         int totalSize = (_size + (quietZone * 2)) * moduleSize;

         StringBuilder sb = new();
         _ = sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 {totalSize} {totalSize}\" stroke=\"none\">");
         _ = sb.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

         for (int y = 0; y < _size; y++)
         {
            for (int x = 0; x < _size; x++)
            {
               if (_modules[y, x])
               {
                  int px = (x + quietZone) * moduleSize;
                  int py = (y + quietZone) * moduleSize;
                  _ = sb.AppendLine($"<rect x=\"{px}\" y=\"{py}\" width=\"{moduleSize}\" height=\"{moduleSize}\" fill=\"black\"/>");
               }
            }
         }

         _ = sb.AppendLine("</svg>");
         return sb.ToString();
      }

      public void SaveAsPNG(string filepath, int moduleSize = 10)
      {
         // Export en PBM (format d'image simple) puis conversion conceptuelle
         int quietZone = 4;
         int totalSize = (_size + (quietZone * 2)) * moduleSize;

         // Créer un bitmap simple en mémoire
         byte[] bitmap = new byte[totalSize * totalSize];

         for (int y = 0; y < _size; y++)
         {
            for (int x = 0; x < _size; x++)
            {
               byte color = (byte)(_modules[y, x] ? 0 : 255);

               for (int dy = 0; dy < moduleSize; dy++)
               {
                  for (int dx = 0; dx < moduleSize; dx++)
                  {
                     int px = ((x + quietZone) * moduleSize) + dx;
                     int py = ((y + quietZone) * moduleSize) + dy;
                     bitmap[(py * totalSize) + px] = color;
                  }
               }
            }
         }

         // Écrire au format PBM (Portable Bitmap - format ASCII simple)
         using (StreamWriter writer = new(filepath.Replace(".png", ".pbm")))
         {
            writer.WriteLine("P1");
            writer.WriteLine($"{totalSize} {totalSize}");

            for (int i = 0; i < bitmap.Length; i++)
            {
               writer.Write(bitmap[i] == 0 ? "1 " : "0 ");
               if ((i + 1) % totalSize == 0)
                  writer.WriteLine();
            }
         }

         Console.WriteLine($"QR Code sauvegardé au format PBM: {filepath.Replace(".png", ".pbm")}");
         Console.WriteLine("Note: Pour convertir en PNG, utilisez ImageMagick: convert file.pbm file.png");
      }

      public void SaveAsSVG(string filepath, int moduleSize = 10)
      {
         File.WriteAllText(filepath, ToSVG(moduleSize));
         Console.WriteLine($"QR Code sauvegardé: {filepath}");
      }

      // Propriétés publiques
      public bool[,] GetModules() => _modules;
      public int GetSize() => _size;
      public int GetVersion() => _version;
      public QRMode GetMode() => _mode;
      public ErrorCorrectionLevel GetErrorCorrectionLevel() => _ecLevel;
   }

   // Classe helper pour les blocs Reed-Solomon
   internal class RSBlock(int blockCount, int dataCount, int ecCount)
   {
      public int blockCount = blockCount;
      public int dataCount = dataCount;
      public int ecCount = ecCount;
   }

   // Implémentation Reed-Solomon
   internal static class ReedSolomon
   {
      private static readonly int[] _logTable = new int[256];
      private static readonly int[] _expTable = new int[256];

      static ReedSolomon()
      {
         // Initialiser les tables de logarithme et exponentielle pour GF(256)
         int x = 1;
         for (int i = 0; i < 255; i++)
         {
            _expTable[i] = x;
            _logTable[x] = i;
            x <<= 1;
            if (x > 255)
               x ^= 0x11D; // Polynôme primitif x^8 + x^4 + x^3 + x^2 + 1
         }
         _expTable[255] = _expTable[0];
      }

      public static byte[] Encode(byte[] data, int ecCount)
      {
         byte[] generator = _getGenerator(ecCount);
         byte[] result = new byte[data.Length + ecCount];
         Array.Copy(data, result, data.Length);

         for (int i = 0; i < data.Length; i++)
         {
            byte coef = result[i];
            if (coef != 0)
            {
               for (int j = 0; j < generator.Length; j++)
               {
                  result[i + j] ^= _gFMultiply(generator[j], coef);
               }
            }
         }

         byte[] ec = new byte[ecCount];
         Array.Copy(result, data.Length, ec, 0, ecCount);
         return ec;
      }

      private static byte[] _getGenerator(int degree)
      {
         byte[] result = [1];

         for (int i = 0; i < degree; i++)
         {
            byte[] temp = new byte[result.Length + 1];
            for (int j = 0; j < result.Length; j++)
            {
               temp[j] ^= result[j];
               temp[j + 1] ^= _gFMultiply(result[j], (byte)_expTable[i]);
            }
            result = temp;
         }

         return result;
      }

      private static byte _gFMultiply(byte a, byte b)
      {
         return a == 0 || b == 0 ? (byte)0 : (byte)_expTable[(_logTable[a] + _logTable[b]) % 255];
      }
   }
}