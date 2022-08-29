using System;
using System.Security.Cryptography;
namespace Server.Services.FilesManagers.Cryptography;
public class ClearCryptoTransform : ICryptoTransform
{
    public bool CanReuseTransform => true;
    public bool CanTransformMultipleBlocks => true;
    public int InputBlockSize => 1;
    public int OutputBlockSize => 1;
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        inputBuffer.AsSpan(inputOffset, inputCount).CopyTo(outputBuffer.AsSpan(outputOffset));
        return inputCount;
    }
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        byte[] output = new byte[inputCount];
        inputBuffer.AsSpan(inputOffset, inputCount).CopyTo(output);
        return output;
    }
    public void Dispose() { }
}
