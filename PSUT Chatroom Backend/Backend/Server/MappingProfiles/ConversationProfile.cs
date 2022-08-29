using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Entities;
using Server.Dto.Conversations;
using Server.Dto.Groups;
using Server.Dto.Messages;
using Server.Dto.Users;
using Server.Services.UserSystem;

namespace Server.MappingProfiles;
public class ConversationMetadataConverter : ITypeConverter<Conversation, ConversationMetadataDto>
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public ConversationMetadataConverter(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }
    public ConversationMetadataDto Convert(Conversation src, ConversationMetadataDto dst, ResolutionContext ctx)
    {
        dst = new();
        dst.Id = src.Id;
        dst.IsClosed = src.IsClosed;
        if (!src.IsDirect)
        {
            dst.Name = src.Group.Name;
            dst.Group = ctx.Mapper.Map<GroupMetadataDto>(src.Group);
        }
        else
        {
            var user = _httpContext.HttpContext.GetUser()!;
            dst.Name = src.Members.First(m => m.Id != user.Id).Name;
        }
        var lastMessage = _dbContext.Messages
            .Where(m => m.ConversationId == src.Id)
            .OrderByDescending(m => m.SendingTime)
            .LastOrDefault();

        dst.LastMessage = ctx.Mapper.Map<MessageMetadataDto>(lastMessage);
        return dst;
    }
}
public class ConversationConverter : ITypeConverter<Conversation, ConversationDto>
{
    public ConversationDto Convert(Conversation src, ConversationDto dst, ResolutionContext ctx)
    {
        dst = new();
        //ctx.Mapper.Map<Conversation, ConversationMetadataDto>(src, dst);
        //ohh the hacks!!
        //the above call will fail, so I'll have to copy it manually
        var dm = ctx.Mapper.Map<ConversationMetadataDto>(src);
        dst.Id = dm.Id;
        dst.IsClosed = dm.IsClosed;
        dst.Group = dm.Group;
        dst.LastMessage = dm.LastMessage;
        dst.Name = dm.Name;
        if (!src.IsDirect)
        {
            dst.Group = ctx.Mapper.Map<GroupMetadataDto>(src.Group);
            dst.Members = src.Group.Members.Select(gm => ctx.Mapper.Map<UserMetadataDto>(gm.User)).ToArray();
        }
        else
        {
            dst.Members = src.Members.Select(m => ctx.Mapper.Map<UserMetadataDto>(m)).ToArray();
        }
        return dst;
    }
}
public class ConversationProfile : Profile
{
    public ConversationProfile()
    {
        CreateMap<Conversation, ConversationDto>().ConvertUsing<ConversationConverter>();
        CreateMap<Conversation, ConversationMetadataDto>().ConvertUsing<ConversationMetadataConverter>();
    }
}
