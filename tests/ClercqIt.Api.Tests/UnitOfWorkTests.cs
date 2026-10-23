using Clercq.It.Domain.Entities;
using Clercq.It.Infrastructure.Data;
using Clercq.It.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClercqIt.Api.Tests;

/// <summary>Transactions and repository queries against the Postgres test container.</summary>
[Collection(ApiTestGroup.Name)]
public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public UnitOfWorkTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<int> CountBlogsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Blogs.CountAsync();
    }

    private static Blog NewBlog(DateTime publishDate) =>
        new(Guid.NewGuid(), publishDate, publishDate, "Title", "Short", "Long", "https://img.example.com/a.png", new[] { "tag" });

    [Fact]
    public async Task CommitTransactionAsync_AfterBegin_PersistsTheChanges()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = new UnitOfWork(db);
        await unitOfWork.BeginTransactionAsync();
        await new BlogRepository(db).AddAsync(NewBlog(DateTime.UtcNow));
        await unitOfWork.SaveChangesAsync();

        // Act
        await unitOfWork.CommitTransactionAsync();

        // Assert
        (await CountBlogsAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RollbackTransactionAsync_AfterBegin_DiscardsTheChanges()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unitOfWork = new UnitOfWork(db);
        await unitOfWork.BeginTransactionAsync();
        await new BlogRepository(db).AddAsync(NewBlog(DateTime.UtcNow));
        await unitOfWork.SaveChangesAsync();

        // Act
        await unitOfWork.RollbackTransactionAsync();

        // Assert
        (await CountBlogsAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CommitAndRollback_NoTransaction_DoNothing()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = new UnitOfWork(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        // Act
        var act = async () =>
        {
            await unitOfWork.CommitTransactionAsync();
            await unitOfWork.RollbackTransactionAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPublishedAsync_PastAndFutureBlogs_ReturnsOnlyThePastOnes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var published = NewBlog(DateTime.UtcNow.AddDays(-1));
        db.Blogs.AddRange(published, NewBlog(DateTime.UtcNow.AddDays(7)));
        await db.SaveChangesAsync();

        // Act
        var result = await new BlogRepository(db).GetPublishedAsync();

        // Assert
        result.Select(b => b.Id).Should().Equal(published.Id);
    }

    [Fact]
    public async Task FindAsync_Predicate_ReturnsTheMatchingEntities()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var match = NewBlog(DateTime.UtcNow);
        match.Update("Title", "Match", "Long", new[] { "tag" });
        db.Blogs.AddRange(match, NewBlog(DateTime.UtcNow));
        await db.SaveChangesAsync();

        // Act
        var result = await new BlogRepository(db).FindAsync(b => b.ShortDescription == "Match");

        // Assert
        result.Select(b => b.Id).Should().Equal(match.Id);
    }
}
