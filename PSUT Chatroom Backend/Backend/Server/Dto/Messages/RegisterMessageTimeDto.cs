using System;

namespace Server.Dto.Messages
{
    public class RegisterMessageTimeDto
    {
        public int Id { get; set; }
        public DateTimeOffset Time { get; set; }
    }
}