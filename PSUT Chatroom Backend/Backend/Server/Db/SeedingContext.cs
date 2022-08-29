using Server.Db.Entities;
using System;
using System.Collections.Generic;
namespace Server.Db;
public class SeedingContext
{
    public IServiceProvider ServiceProvider { get; init; }
    public List<Conversation> Conversations { get; } = new();
    public List<Course> Courses { get; } = new();
    public List<DirectConversationMember> DirectConversationMembers { get; } = new();
    public List<Group> Groups { get; } = new();
    public List<GroupMember> GroupsMembers { get; } = new();
    public List<Message> Messages { get; } = new();
    public List<MessageDeliveryInfo> MessagesDeliveryInfo { get; } = new();
    public List<Ping> Pings { get; } = new();
    public List<Section> Sections { get; } = new();
    public List<User> Users { get; } = new();
}