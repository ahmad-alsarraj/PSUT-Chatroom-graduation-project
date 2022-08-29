using System.Security.Cryptography;
namespace Server.Services.FilesManagers.Cryptography;
public class XorCryptoTransform : ICryptoTransform
{
    public bool CanReuseTransform => true;
    public bool CanTransformMultipleBlocks => true;
    public int InputBlockSize => 1;
    public int OutputBlockSize => 1;

    private readonly byte _key;
    public XorCryptoTransform(byte key) { _key = key; }
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        for (int i = 0; i < inputCount; i++)
        {
            outputBuffer[outputOffset + i] = (byte)(inputBuffer[i + inputOffset] ^ _key);
        }
        return inputCount;
    }
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        byte[] output = new byte[inputCount];
        for (int i = 0; i < inputCount; i++)
        {
            output[i] = (byte)(inputBuffer[i + inputOffset] ^ _key);
        }
        return output;
    }
    public void Dispose() { }
}
