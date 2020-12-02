namespace Dullahan {
    public static class BufferUtil {
        public static int ReadInt(byte[] buffer, int index) {
            return buffer[index++] | buffer[index++] << 8 | buffer[index++] << 16 | buffer[index++] << 24;
        }

        public static void WriteInt(int value, byte[] buffer, int index) {
            buffer[index++] = (byte)(0xff & value);
            buffer[index++] = (byte)(0xff & (value >> 8));
            buffer[index++] = (byte)(0xff & (value >> 16));
            buffer[index++] = (byte)(0xff & (value >> 24));
        }
    }
}
