using System;
using System.Runtime.InteropServices;

namespace MERX.Features.Misc.Opus
{
    public class OpusWrapper
    {
        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_encoder_get_size(int channels);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern OpusStatusCode opus_encoder_init(IntPtr st, int fs, int channels,
            OpusApplicationType application);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string opus_get_version_string();

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_encode_float(IntPtr st, float[] pcm, int frame_size, byte[] data,
            int max_data_bytes);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_encoder_ctl(IntPtr st, OpusCtlSetRequest request, int value);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_encoder_ctl(IntPtr st, OpusCtlGetRequest request, ref int value);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_encode(IntPtr st, short[] pcm, int frame_size, byte[] data, int max_data_bytes);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_decoder_get_size(int channels);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern OpusStatusCode opus_decoder_init(IntPtr st, int fr, int channels);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_decode(IntPtr st, byte[] data, int len, short[] pcm, int frame_size,
            int decode_fec);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_decode_float(IntPtr st, byte[] data, int len, float[] pcm, int frame_size,
            int decode_fec);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_decode(IntPtr st, IntPtr data, int len, short[] pcm, int frame_size,
            int decode_fec);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_decode_float(IntPtr st, IntPtr data, int len, float[] pcm, int frame_size,
            int decode_fec);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_packet_get_bandwidth(byte[] data);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int opus_packet_get_nb_channels(byte[] data);

        [DllImport("libopus-0", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern string opus_strerror(OpusStatusCode error);

        public static IntPtr CreateEncoder(int samplingRate, int channels, OpusApplicationType application)
        {
            var intPtr = Marshal.AllocHGlobal(opus_encoder_get_size(channels));
            var statusCode = opus_encoder_init(intPtr, samplingRate, channels, application);
            try
            {
                HandleStatusCode(statusCode);
            }
            catch (Exception ex)
            {
                if (intPtr != IntPtr.Zero) Destroy(intPtr);
                throw ex;
            }

            return intPtr;
        }

        public static int Encode(IntPtr st, float[] pcm, int frameSize, byte[] data)
        {
            if (st == IntPtr.Zero) throw new ObjectDisposedException("Encoder is already disposed!");
            var num = opus_encode_float(st, pcm, frameSize, data, data.Length);
            if (num <= 0) HandleStatusCode((OpusStatusCode)num);
            return num;
        }

        public static int GetEncoderSetting(IntPtr st, OpusCtlGetRequest request)
        {
            if (st == IntPtr.Zero) throw new ObjectDisposedException("Encoder is already disposed!");
            var result = 0;
            HandleStatusCode((OpusStatusCode)opus_encoder_ctl(st, request, ref result));
            return result;
        }

        public static void SetEncoderSetting(IntPtr st, OpusCtlSetRequest request, int value)
        {
            if (st == IntPtr.Zero) throw new ObjectDisposedException("Encoder is already disposed!");
            HandleStatusCode((OpusStatusCode)opus_encoder_ctl(st, request, value));
        }

        public static IntPtr CreateDecoder(int samplingRate, int channels)
        {
            var intPtr = Marshal.AllocHGlobal(opus_decoder_get_size(channels));
            var statusCode = opus_decoder_init(intPtr, samplingRate, channels);
            try
            {
                HandleStatusCode(statusCode);
            }
            catch (Exception ex)
            {
                if (intPtr != IntPtr.Zero) Destroy(intPtr);
                throw ex;
            }

            return intPtr;
        }

        public static int Decode(IntPtr st, byte[] data, int dataLength, float[] pcm, bool fec, int channels)
        {
            if (st == IntPtr.Zero) throw new ObjectDisposedException("OpusDecoder is already disposed!");
            var decode_fec = fec ? 1 : 0;
            var frame_size = pcm.Length / channels;
            var num = data != null
                ? opus_decode_float(st, data, dataLength, pcm, frame_size, decode_fec)
                : opus_decode_float(st, IntPtr.Zero, 0, pcm, frame_size, decode_fec);
            if (num == -4) return 0;
            if (num <= 0) HandleStatusCode((OpusStatusCode)num);
            return num;
        }

        public static int GetBandwidth(byte[] data)
        {
            return opus_packet_get_bandwidth(data);
        }

        public static void HandleStatusCode(OpusStatusCode statusCode)
        {
            if (statusCode == OpusStatusCode.OK) return;
            throw new OpusException(statusCode, opus_strerror(statusCode));
        }

        public static void Destroy(IntPtr st)
        {
            Marshal.FreeHGlobal(st);
        }

        public const string DllName = "libopus-0";
    }

    public enum OpusCtlGetRequest
    {
        Application = 4001,
        Bitrate = 4003,
        MaxBandwidth = 4005,
        VBR = 4007,
        Bandwidth = 4009,
        Complexity = 4011,
        InbandFec = 4013,
        PacketLossPercentage = 4015,
        Dtx = 4017,
        VBRConstraint = 4021,
        ForceChannels = 4023,
        Signal = 4025,
        LookAhead = 4027,
        SampleRate = 4029,
        FinalRange = 4031,
        Pitch = 4033,
        Gain = 4035,
        LsbDepth = 4037
    }

    public enum OpusApplicationType
    {
        Voip = 2048,
        Audio,
        RestrictedLowDelay = 2051
    }

    public enum OpusCtlSetRequest
    {
        Application = 4000,
        Bitrate = 4002,
        MaxBandwidth = 4004,
        VBR = 4006,
        Bandwidth = 4008,
        Complexity = 4010,
        InbandFec = 4012,
        PacketLossPercentage = 4014,
        Dtx = 4016,
        VBRConstraint = 4020,
        ForceChannels = 4022,
        Signal = 4024,
        Gain = 4034,
        LsbDepth = 4036
    }

    public enum OpusStatusCode
    {
        OK,
        BadArguments = -1,
        BufferTooSmall = -2,
        InternalError = -3,
        InvalidPacket = -4,
        Unimplemented = -5,
        InvalidState = -6,
        AllocFail = -7
    }
}