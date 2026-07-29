namespace OptionalNetworking.Hashing
{
    // Based on https://github.com/jitbit/MurmurHash.net/blob/master/MurmurHash.cs and the original MurmurHash2 implementation.
    internal static class MurmurHash2
    {
        private const ulong seed = 0xc58f1a7a;
        private const ulong m = 0xc6a4a7935bd1e995;
        private const int r = 47;

        public static ulong Hash(string data)
        {
            return Hash(System.Text.Encoding.UTF8.GetBytes(data));
        }

        public static ulong Hash(byte[] data)
        {
            return Hash(data, seed);
        }

        public static ulong Hash(byte[] data, ulong seed)
        {
            int len = data.Length;
            if (len == 0)
                return 0;

            ulong h = seed ^ ((ulong)len * m);
            int index = 0;
            while (len >= 8)
            {
                ulong k = (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16 | data[index++] << 24 |
                    data[index++] << 32 | data[index++] << 40 | data[index++] << 48 | data[index++] << 56);

                k *= m; 
                k ^= k >> r; 
                k *= m;
    
                h ^= k;
                h *= m;
                len -= 8;
            }

            switch(len)
            {
                case 7:
                    h ^= (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16 | data[index++] << 24 |
                    data[index++] << 32 | data[index++] << 40 | data[index] << 48);
                    h *= m;
                    break;
                case 6:
                    h ^= (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16 | data[index++] << 24 |
                    data[index++] << 32 | data[index++] << 40);
                    h *= m;
                    break;
                case 5:
                    h ^= (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16 | data[index++] << 24 |
                    data[index++] << 32);
                    h *= m;
                    break;
                case 4:
                    h ^= (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16 | data[index++] << 24);
                    h *= m;
                    break;
                case 3:
                    h ^= (ulong)(data[index++] | data[index++] << 8 | data[index++] << 16);
                    h *= m;
                    break;
                case 2:
                    h ^= (ulong)(data[index++] | data[index++] << 8);
                    h *= m;
                    break;
                case 1:
                    h ^= (ulong)(data[index++]);
                    h *= m;
                    break;
            };

            h ^= h >> r;
            h *= m;
            h ^= h >> r;

            return h;
        } 
    }
}
