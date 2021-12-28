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

        public BitMap(int size)
        {
            Length = size;
            _map = new byte[(size + 1) / BIT_TO_BYTE];
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
    }
}
