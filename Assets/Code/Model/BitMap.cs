using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP.Data
{
    public class BitMap
    {
        private const int BIT_TO_BYTE = 8;

        private readonly byte[] _map;

        public bool this[int index]
        {
            get
            {
                if (index >= Length) throw new IndexOutOfRangeException();
                return (_map[index / BIT_TO_BYTE] & (1 << index % BIT_TO_BYTE)) != 0;
            }
            set
            {
                if (value)
                {
                    _map[index / BIT_TO_BYTE] |= (byte)(1 << index % BIT_TO_BYTE);
                }
                else
                {
                    _map[index / BIT_TO_BYTE] &= (byte)~(1 << index % BIT_TO_BYTE);
                }
            }
        }

        public int Length { get; private set; }

        private int _compactLength => Length / BIT_TO_BYTE + 1;

        public BitMap(int size)
        {
            Length = size;
            _map = new byte[_compactLength];
        }

        public static BitMap FromStrea(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var len = reader.ReadInt32();
            var map = new BitMap(len);
            reader.Read(map._map, 0, map._map.Length);

            return map;
        }

        public void Save(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(Length);
            writer.Write(_map);
        }

        public int GetMappedCount()
        {
            int count = 0;
            for (int i = 0; i < _compactLength; i++)
            {
                int b = _map[i];
                for (int n = 0; n < BIT_TO_BYTE; n++)
                {
                    if ((b & (1 << n)) != 0)
                        count++;
                }
            }

            return count;
        }

        public static BitMap Trim(BitMap bitMap, int newLength)
        {            
            var newBitMap = new BitMap(newLength);
            Array.Copy(bitMap._map, newBitMap._map, newBitMap._compactLength);
            return newBitMap;
        }
    }
}
