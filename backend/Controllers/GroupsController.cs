using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Backend.Data;
using Backend.Models;
using Backend.Requests;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly Db db;
    private readonly UserManager<User> userManager;

    public GroupsController(Db db, UserManager<User> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableGroups()
    {
        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        var groups = await db.Circles
            .Where(circle => !db.UserCircleRoles.Any(membership =>
                membership.CircleId == circle.Id && membership.UserId == user.Id))
            .OrderBy(circle => circle.Name)
            .Select(circle => new
            {
                id = circle.Id,
                name = circle.Name,
                description = circle.Description,
                createdByUserId = circle.CreatedByUserId,
                createdAt = circle.CreatedAt,
                isPrivate = circle.IsPrivate,
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyGroups()
    {
        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        var groups = await db.UserCircleRoles
            .Where(membership => membership.UserId == user.Id)
            .OrderBy(membership => membership.Circle.Name)
            .Select(membership => new
            {
                id = membership.Circle.Id,
                name = membership.Circle.Name,
                description = membership.Circle.Description,
                createdByUserId = membership.Circle.CreatedByUserId,
                createdAt = membership.Circle.CreatedAt,
                isPrivate = membership.Circle.IsPrivate,
                role = membership.Role.Name,
            })
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{id:long}/posts")]
    public async Task<IActionResult> GetPosts(long id)
    {
        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        var circle = await db.Circles.FindAsync(id);
        if (circle is null)
        {
            return NotFound(new { message = "Group not found" });
        }

        if (!await IsMember(user.Id, id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Not a member" });
        }

        var posts = await db.Posts
            .Where(post => post.CircleId == id)
            .OrderBy(post => post.Id)
            .Select(post => new
            {
                id = post.Id,
                circleId = post.CircleId,
                message = post.Message,
                userId = post.UserId,
                createdAt = post.CreatedAt,
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCircleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        var ownerRoleId = await RoleId(CircleRole.Owner);
        var circle = new Circle
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? "",
            CreatedByUserId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            IsPrivate = request.IsPrivate,
            Members =
            [
                new UserCircleRole
                {
                    UserId = user.Id,
                    RoleId = ownerRoleId,
                },
            ],
        };

        db.Circles.Add(circle);
        await db.SaveChangesAsync();

        return Ok(new
        {
            id = circle.Id,
            name = circle.Name,
            description = circle.Description,
            createdByUserId = circle.CreatedByUserId,
            createdAt = circle.CreatedAt,
            isPrivate = circle.IsPrivate,
            role = CircleRole.Owner,
        });
    }

    [HttpPost("{id:long}/members")]
    public async Task<IActionResult> Join(long id)
    {
        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        if (!await db.Circles.AnyAsync(circle => circle.Id == id))
        {
            return NotFound(new { message = "Group not found" });
        }

        if (await IsMember(user.Id, id))
        {
            return Conflict(new { message = "Already a member" });
        }

        db.UserCircleRoles.Add(new UserCircleRole
        {
            UserId = user.Id,
            CircleId = id,
            RoleId = await RoleId(CircleRole.Member),
        });
        await db.SaveChangesAsync();

        return Ok(new { circleId = id, userId = user.Id, role = CircleRole.Member });
    }

    [HttpPost("{id:long}/leave")]
    public async Task<IActionResult> Leave(long id)
    {
        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        var membership = await db.UserCircleRoles
            .Include(membership => membership.Role)
            .SingleOrDefaultAsync(membership => membership.UserId == user.Id && membership.CircleId == id);
        if (membership is null)
        {
            return NotFound(new { message = "Group not found" });
        }

        if (membership.Role.Name == CircleRole.Owner)
        {
            return BadRequest(new { message = "Owners cannot leave their circle" });
        }

        db.UserCircleRoles.Remove(membership);
        await db.SaveChangesAsync();
        return Ok(new { message = "left circle" });
    }

    [HttpPost("{id:long}/posts")]
    public async Task<IActionResult> CreatePost(long id, [FromBody] CreatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required" });
        }

        var user = await CurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Not logged in" });
        }

        if (!await db.Circles.AnyAsync(circle => circle.Id == id))
        {
            return NotFound(new { message = "Group not found" });
        }

        if (!await IsMember(user.Id, id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Not a member" });
        }

        var post = new Post
        {
            CircleId = id,
            Message = request.Message.Trim(),
            UserId = user.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        return Ok(new
        {
            id = post.Id,
            circleId = post.CircleId,
            message = post.Message,
            userId = post.UserId,
            createdAt = post.CreatedAt,
        });
    }

    private async Task<User?> CurrentUser() => await userManager.GetUserAsync(User);

    private Task<bool> IsMember(string userId, long circleId) =>
        db.UserCircleRoles.AnyAsync(membership =>
            membership.UserId == userId && membership.CircleId == circleId);

    private Task<int> RoleId(string name) =>
        db.CircleRoles.Where(role => role.Name == name).Select(role => role.Id).SingleAsync();
}
