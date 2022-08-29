using System.Collections.Generic;

namespace Server.Dto
{
    public class ErrorDto
    {
        public string Description { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}