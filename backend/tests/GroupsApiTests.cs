using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace socialmediaapi.Tests;

public class GroupsApiTests : IAsyncLifetime
{
    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"group-tests-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    public ValueTask InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
            builder.UseEnvironment("Development");
        });
        client = CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetAvailableGroups_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/groups/available", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AddsOwnerToMyGroupsAndHidesItFromAvailable()
    {
        await RegisterAndLoginAsync(client);

        var created = await CreateGroupAsync(client, "Weekend Hangouts", "Friends", isPrivate: true);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var mine = await client.GetFromJsonAsync<GroupResponse[]>("/api/groups/mine", Ct);
        var available = await client.GetFromJsonAsync<GroupResponse[]>("/api/groups/available", Ct);

        var group = Assert.Single(mine!);
        Assert.Equal("Weekend Hangouts", group.Name);
        Assert.Equal("Friends", group.Description);
        Assert.True(group.IsPrivate);
        Assert.Equal("Owner", group.Role);
        Assert.DoesNotContain(available!, item => item.Id == group.Id);
    }

    [Fact]
    public async Task GetAvailableGroups_ListsGroupsTheUserHasNotJoined()
    {
        await RegisterAndLoginAsync(client);
        var created = await CreateGroupAsync(client, "Open Study Circle", "Anyone", isPrivate: false);
        var createdGroup = await created.Content.ReadFromJsonAsync<GroupResponse>(Ct);

        var other = CreateClient();
        await RegisterAndLoginAsync(other);

        var available = await other.GetFromJsonAsync<GroupResponse[]>("/api/groups/available", Ct);
        var mine = await other.GetFromJsonAsync<GroupResponse[]>("/api/groups/mine", Ct);

        Assert.Contains(available!, group => group.Id == createdGroup!.Id && group.Name == "Open Study Circle");
        Assert.DoesNotContain(mine!, group => group.Id == createdGroup!.Id);
        other.Dispose();
    }

    [Fact]
    public async Task Join_MovesGroupFromAvailableToMyGroups()
    {
        await RegisterAndLoginAsync(client);
        var created = await CreateGroupAsync(client, "Hiking Group", "", isPrivate: false);
        var createdGroup = await created.Content.ReadFromJsonAsync<GroupResponse>(Ct);

        var other = CreateClient();
        await RegisterAndLoginAsync(other);

        var join = await other.PostAsync($"/api/groups/{createdGroup!.Id}/members", null, Ct);
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var mine = await other.GetFromJsonAsync<GroupResponse[]>("/api/groups/mine", Ct);
        var available = await other.GetFromJsonAsync<GroupResponse[]>("/api/groups/available", Ct);

        var membership = Assert.Single(mine!, group => group.Id == createdGroup.Id);
        Assert.Equal("Member", membership.Role);
        Assert.DoesNotContain(available!, group => group.Id == createdGroup.Id);
        other.Dispose();
    }

    [Fact]
    public async Task GetPosts_ReturnsOnlyPostsInThatGroup()
    {
        await RegisterAndLoginAsync(client);
        var first = await CreateGroupAsync(client, "Weekend Hangouts", "", isPrivate: true);
        var second = await CreateGroupAsync(client, "Family Reunion", "", isPrivate: true);
        var firstGroup = await first.Content.ReadFromJsonAsync<GroupResponse>(Ct);
        var secondGroup = await second.Content.ReadFromJsonAsync<GroupResponse>(Ct);

        var posted = await client.PostAsJsonAsync(
            $"/api/groups/{firstGroup!.Id}/posts",
            new { message = "Happy birthday Sarah!" },
            Ct);
        Assert.Equal(HttpStatusCode.OK, posted.StatusCode);

        var posts = await client.GetFromJsonAsync<PostResponse[]>(
            $"/api/groups/{firstGroup.Id}/posts",
            Ct);
        var otherPosts = await client.GetFromJsonAsync<PostResponse[]>(
            $"/api/groups/{secondGroup!.Id}/posts",
            Ct);

        var post = Assert.Single(posts!);
        Assert.Equal(firstGroup.Id, post.CircleId);
        Assert.Equal("Happy birthday Sarah!", post.Message);
        Assert.Empty(otherPosts!);
    }

    [Fact]
    public async Task Leave_RemovesMembershipAndRejectsOwner()
    {
        await RegisterAndLoginAsync(client);
        var created = await CreateGroupAsync(client, "Hiking Group", "", isPrivate: false);
        var createdGroup = await created.Content.ReadFromJsonAsync<GroupResponse>(Ct);

        var ownerLeaves = await client.PostAsync($"/api/groups/{createdGroup!.Id}/leave", null, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, ownerLeaves.StatusCode);

        var other = CreateClient();
        await RegisterAndLoginAsync(other);
        var join = await other.PostAsync($"/api/groups/{createdGroup.Id}/members", null, Ct);
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var leave = await other.PostAsync($"/api/groups/{createdGroup.Id}/leave", null, Ct);
        Assert.Equal(HttpStatusCode.OK, leave.StatusCode);

        var mine = await other.GetFromJsonAsync<GroupResponse[]>("/api/groups/mine", Ct);
        Assert.DoesNotContain(mine!, group => group.Id == createdGroup.Id);
        other.Dispose();
    }

    [Fact]
    public async Task GetPosts_WhenNotMember_ReturnsForbidden()
    {
        await RegisterAndLoginAsync(client);
        var created = await CreateGroupAsync(client, "Private Chat Room", "", isPrivate: true);
        var createdGroup = await created.Content.ReadFromJsonAsync<GroupResponse>(Ct);

        var other = CreateClient();
        await RegisterAndLoginAsync(other);

        var response = await other.GetAsync($"/api/groups/{createdGroup!.Id}/posts", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        other.Dispose();
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private async Task RegisterAndLoginAsync(HttpClient http)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var registered = await http.PostAsJsonAsync("/api/auth/register", new { email, password = "password1" }, Ct);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var loggedIn = await http.PostAsJsonAsync("/api/auth/login", new { email, password = "password1" }, Ct);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
    }

    private Task<HttpResponseMessage> CreateGroupAsync(HttpClient http, string name, string description, bool isPrivate) =>
        http.PostAsJsonAsync("/api/groups", new { name, description, isPrivate }, Ct);

    private sealed record GroupResponse(
        long Id,
        string Name,
        string Description,
        string CreatedByUserId,
        DateTimeOffset CreatedAt,
        bool IsPrivate,
        string? Role);

    private sealed record PostResponse(long Id, long CircleId, string Message, string UserId, DateTimeOffset CreatedAt);
}
