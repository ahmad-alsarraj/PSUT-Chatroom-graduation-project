using System;
using Server.Dto.Users;

namespace Server.Dto.Messages;

public class CreateMessageAttachmentDto
{
    public string FileName { get; set; }
    public string FileContentBase64 { get; set; }
}
