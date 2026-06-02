namespace StudentPortal.Models;

public class ClassMembership
{
    public int Id { get; set; }

    public int ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public int AppUserId { get; set; }

    public AppUser? AppUser { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
