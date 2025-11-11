using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibPostalNet
{
    internal static class StringHelper
    {
        internal static unsafe int GetLenStr(sbyte* strPoint)
        {
            int i = 0;
            for (; i < 1000; i++)
            {
                if (strPoint[i] == 0)
                    break;
            }

            return i;
        }
    }

    public partial class LibpostalNormalizeOptions
    {
        public string[] Langs
        {
            get
            {
                IList<string> _lang = new List<string>();

                unsafe
                {
                    for (int buc = 0; buc < (int)NumLanguages; buc++)
                    {
                        sbyte* pLang = Languages[buc];
                        //var k = Encoding.UTF8.GetString((byte*)pLang, 20);
                        _lang.Add(Marshal.PtrToStringAnsi((IntPtr)pLang));
                    }
                }

                return _lang.ToArray();
            }

            set
            {
                unsafe
                {
                    // Освобождаем предыдущую память, если она была выделена
                    if (Languages != null)
                    {
                        for (int i = 0; i < (int)NumLanguages; i++)
                        {
                            if (Languages[i] != null)
                            {
                                Marshal.FreeHGlobal((IntPtr)Languages[i]);
                            }
                        }
                        Marshal.FreeHGlobal((IntPtr)Languages);
                    }

                    if (value == null || value.Length == 0)
                    {
                        NumLanguages = 0;
                        Languages = null;
                        return;
                    }

                    // Устанавливаем количество языков
                    NumLanguages = (uint)value.Length;

                    // Выделяем память для массива указателей
                    Languages = (sbyte**)Marshal.AllocHGlobal(value.Length * sizeof(sbyte*));

                    for (int i = 0; i < value.Length; i++)
                    {
                        if (string.IsNullOrEmpty(value[i]))
                        {
                            Languages[i] = null;
                            continue;
                        }
                        /*
                        // Конвертируем строку в нуль-терминированную ANSI строку
                        byte[] bytes = Encoding.Default.GetBytes(value[i] + '\0');

                        // Выделяем память для строки (+1 для нуль-терминатора)
                        Languages[i] = (sbyte*)Marshal.AllocHGlobal(bytes.Length);

                        // Копируем байты в неуправляемую память
                        Marshal.Copy(bytes, 0, (IntPtr)Languages[i], bytes.Length);
                        */

                        // Преобразуем строку в неуправляемую ANSI строку
                        Languages[i] = (sbyte*)Marshal.StringToHGlobalAnsi(value[i]);
                    }
                }
            }
        }
    }

    public partial class LibpostalAddressParserResponse
    {
        public List<KeyValuePair<string, string>> Results
        {
            get
            {
                var _results = new List<KeyValuePair<string, string>>();

                unsafe
                {
                    for (int buc = 0; buc < (int)NumComponents; buc++)
                    {
                        sbyte* pLabel = Labels[buc];
                        sbyte* pComponent = Components[buc];

                        //_results.Add(new KeyValuePair<string, string>(Marshal.PtrToStringUTF8((IntPtr)pLabel), Marshal.PtrToStringUTF8((IntPtr)pComponent)));

                        int lenK = StringHelper.GetLenStr(pLabel);
                        var key = Encoding.UTF8.GetString((byte*)pLabel, lenK);

                        int lenV = StringHelper.GetLenStr(pComponent);
                        var value = Encoding.UTF8.GetString((byte*)pComponent, lenV);

                        _results.Add(new KeyValuePair<string, string>(key, value));
                    }
                }

                return _results;
            }
        }
    }
}
