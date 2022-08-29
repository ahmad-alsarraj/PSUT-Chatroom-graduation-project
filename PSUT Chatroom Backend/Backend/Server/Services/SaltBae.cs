using System.Buffers.Binary;
using Server.Db.Entities;
using System.Buffers.Binary;
using System;

namespace Server.Services;
public class SaltBae
{
    public static int SaltShakerCapacity => 8;
    Random _rand = new();
    public void SaltSteak(Span<byte> saltShaker, User? customer = null, int? steakId = null)
    {
        BinaryPrimitives.WriteInt32LittleEndian(saltShaker, customer?.Id ?? _rand.Next());
        BinaryPrimitives.WriteInt32LittleEndian(saltShaker[4..], steakId ?? _rand.Next());
    }
    public byte[] SaltSteak(User? customer = null, int? steakId = null)
    {
        var saltShaker = new byte[SaltShakerCapacity];
        SaltSteak(saltShaker, customer, steakId);
        return saltShaker;
    }
}