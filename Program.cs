namespace Disassembler
{
    class Program
    {
        const byte OPCODE_MASK = 0B_11111100;
        const byte OPCODE_MOV = 0B_100010;
        const byte MOD_MASK = 0B_11000000;
        const byte MOD_REGTOREG = 0B_11;
        const byte REG_MASK = 0B_00111000;
        const byte RM_MASK = 0B_00000111;
        const byte REG_0 = 0B_000;
        const byte REG_1 = 0B_001;
        const byte REG_2 = 0B_010;
        const byte REG_3 = 0B_011;
        const byte REG_4 = 0B_100;
        const byte REG_5 = 0B_101;
        const byte REG_6 = 0B_110;
        const byte REG_7 = 0B_111;
        static readonly string[][] registerNames = new string[8][]
        {
            new string[2]{"al", "ax"},
            new string[2]{"cl", "cx"},
            new string[2]{"dl", "dx"},
            new string[2]{"bl", "bx"},
            new string[2]{"ah", "sp"},
            new string[2]{"ch", "bp"},
            new string[2]{"dh", "si"},
            new string[2]{"bh", "di"}
        };

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of parameters.");
                return;
            }

            BinaryReader binReader = new BinaryReader(File.Open(args[0], FileMode.Open));
            byte[] buffer = new byte[2];
            StreamWriter file = new StreamWriter("result" + args[0].Substring(args[0].IndexOf('_')) + ".asm");

            file.WriteLine("bits 16");
            file.WriteLine("");

            while (binReader.Read(buffer, 0, 1) > 0)
            {
                string instruction = "";
                if (CompareBits(buffer[0], OPCODE_MOV, OPCODE_MASK, 6))
                {
                    instruction += "mov ";

                    var wBit = GetBit(buffer[0], 0) ? 1 : 0;
                    var dBit = GetBit(buffer[0], 1);

                    binReader.Read(buffer, 1, 1);

                    if (!CompareBits(buffer[1], MOD_REGTOREG, MOD_MASK, 2)) return;

                    var reg1 = DecodeRegister(buffer[1], REG_MASK, wBit);
                    var reg2 = DecodeRegister(buffer[1], RM_MASK, wBit);

                    instruction += dBit ? reg1 + ", " + reg2 : reg2 + ", " + reg1;

                    file.WriteLine(instruction);
                }
            }

            file.Close();
        }

        private static string DecodeRegister(byte data, byte mask, int wide)
        {
            byte shiftedByte = 0;

            if (mask == REG_MASK)
                shiftedByte = ApplyMaskAndShiftBy(data, mask, 3);
            else if (mask == RM_MASK)
                shiftedByte = ApplyMask(data, mask);

            return registerNames[shiftedByte][wide];
        }

        static bool CompareBits(byte b1, byte b2, byte mask, int n)
        {
            byte shiftedByte = ApplyMaskAndShiftHigher(b1, mask, n);

            return b2.Equals(shiftedByte);
        }

        private static byte ApplyMaskAndShiftBy(byte data, byte mask, int n)
        {
            return (byte)(ApplyMask(data, mask) >> n);
        }

        private static byte ApplyMaskAndShiftHigher(byte data, byte mask, int n)
        {
            return (byte)(ApplyMask(data, mask) >> 8 - n);
        }

        private static byte ApplyMask(byte data, byte mask)
        {
            return (byte)(data & mask);
        }

        public static bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
    }
}