using BetterTogether.Bluetooth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auto_Guide
{
    public class RobotHelper
    {
        public async Task SendCommand(IBluetoothConnection _conn, Byte[] comPackage)
        {
            byte[] comPackageFinal = new byte[6 * (int)Math.Ceiling((double)comPackage.Length / 5)];
            int i, xorbyteindex;
            for (i = 0; i < comPackage.Length; i++)
            {
                xorbyteindex = (((int)i / 5) + 1) * 6 - 1;
                if (i % 5 == 0) comPackageFinal[xorbyteindex] = 0;
                comPackageFinal[(i / 5) * 6 + (i % 5)] = comPackage[i];
                comPackageFinal[xorbyteindex] ^= comPackage[i];
            }
            await _conn.WriteAsync(comPackageFinal);
        }

        public async Task<Byte[]> GetData(IBluetoothConnection _conn)
        {
            byte[] comPackage = new byte[6];
            await _conn.ReadAsync((uint)comPackage.Length, comPackage);
            return comPackage;
        }

        public async Task ClearData(IBluetoothConnection _conn, int length)
        {
            byte[] comPackage = new byte[length];
            await _conn.ReadAsync((uint)comPackage.Length, comPackage);
        }
    }
}
