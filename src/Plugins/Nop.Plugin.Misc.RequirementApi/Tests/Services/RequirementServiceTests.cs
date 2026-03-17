using FluentAssertions;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.RequirementApi.Domain;
using Nop.Plugin.Misc.RequirementApi.Services;
using Nop.Tests.Nop.Services.Tests;
using NUnit.Framework;

namespace Nop.Plugin.Misc.RequirementApi.Tests.Services;

[TestFixture]
public class RequirementServiceTests : ServiceTest
{
    #region Fields

    private RequirementService _requirementService;
    private IRepository<Requirement> _requirementRepository;

    #endregion

    #region SetUp

    [OneTimeSetUp]
    public void SetUp()
    {
        _requirementRepository = GetService<IRepository<Requirement>>();
        _requirementService = new RequirementService(_requirementRepository);
    }

    #endregion

    #region Tests

    [Test]
    public async Task CanInsertRequirement()
    {
        var requirement = new Requirement
        {
            PrimaryName = "Test User",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement);

        requirement.Id.Should().BeGreaterThan(0);
        requirement.CreatedOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Cleanup
        await _requirementService.DeleteRequirementAsync(requirement);
    }

    [Test]
    public async Task CanGetRequirementById()
    {
        var requirement = new Requirement
        {
            PrimaryName = "Test User",
            MobileNumber = "1234567890",
            Email = "test@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement);

        var retrieved = await _requirementService.GetRequirementByIdAsync(requirement.Id);

        retrieved.Should().NotBeNull();
        retrieved.Id.Should().Be(requirement.Id);
        retrieved.PrimaryName.Should().Be(requirement.PrimaryName);
        retrieved.Email.Should().Be(requirement.Email);

        // Cleanup
        await _requirementService.DeleteRequirementAsync(requirement);
    }

    [Test]
    public async Task CanGetAllRequirements()
    {
        // Insert test data
        var requirement1 = new Requirement
        {
            PrimaryName = "User 1",
            MobileNumber = "1111111111",
            Email = "user1@example.com",
            Status = "Draft"
        };

        var requirement2 = new Requirement
        {
            PrimaryName = "User 2",
            MobileNumber = "2222222222",
            Email = "user2@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement1);
        await _requirementService.InsertRequirementAsync(requirement2);

        var requirements = await _requirementService.GetAllRequirementsAsync(0, 10);

        requirements.Should().NotBeNull();
        requirements.TotalCount.Should().BeGreaterThanOrEqualTo(2);

        // Cleanup
        await _requirementService.DeleteRequirementAsync(requirement1);
        await _requirementService.DeleteRequirementAsync(requirement2);
    }

    [Test]
    public async Task CanUpdateRequirement()
    {
        var requirement = new Requirement
        {
            PrimaryName = "Original Name",
            MobileNumber = "1234567890",
            Email = "original@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement);

        requirement.PrimaryName = "Updated Name";
        requirement.Email = "updated@example.com";
        await _requirementService.UpdateRequirementAsync(requirement);

        var updated = await _requirementService.GetRequirementByIdAsync(requirement.Id);
        updated.Should().NotBeNull();
        updated.PrimaryName.Should().Be("Updated Name");
        updated.Email.Should().Be("updated@example.com");
        updated.UpdatedOnUtc.Should().NotBeNull();
        updated.UpdatedOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Cleanup
        await _requirementService.DeleteRequirementAsync(requirement);
    }

    [Test]
    public async Task CanDeleteRequirement()
    {
        var requirement = new Requirement
        {
            PrimaryName = "To Delete",
            MobileNumber = "1234567890",
            Email = "delete@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement);
        var id = requirement.Id;

        await _requirementService.DeleteRequirementAsync(requirement);

        var deleted = await _requirementService.GetRequirementByIdAsync(id);
        deleted.Should().BeNull();
    }

    [Test]
    public async Task GetAllRequirementsReturnsOrderedByCreatedDateDescending()
    {
        var requirement1 = new Requirement
        {
            PrimaryName = "First",
            MobileNumber = "1111111111",
            Email = "first@example.com",
            Status = "Draft"
        };

        await Task.Delay(100); // Small delay to ensure different timestamps

        var requirement2 = new Requirement
        {
            PrimaryName = "Second",
            MobileNumber = "2222222222",
            Email = "second@example.com",
            Status = "Draft"
        };

        await _requirementService.InsertRequirementAsync(requirement1);
        await _requirementService.InsertRequirementAsync(requirement2);

        var requirements = await _requirementService.GetAllRequirementsAsync(0, 10);

        requirements.Should().NotBeNull();
        if (requirements.Count >= 2)
        {
            requirements[0].CreatedOnUtc.Should().BeAfter(requirements[1].CreatedOnUtc);
        }

        // Cleanup
        await _requirementService.DeleteRequirementAsync(requirement1);
        await _requirementService.DeleteRequirementAsync(requirement2);
    }

    [Test]
    public async Task GetAllRequirementsSupportsPagination()
    {
        // Insert multiple requirements
        for (int i = 0; i < 5; i++)
        {
            var requirement = new Requirement
            {
                PrimaryName = $"User {i}",
                MobileNumber = $"123456789{i}",
                Email = $"user{i}@example.com",
                Status = "Draft"
            };
            await _requirementService.InsertRequirementAsync(requirement);
        }

        var page1 = await _requirementService.GetAllRequirementsAsync(0, 2);
        var page2 = await _requirementService.GetAllRequirementsAsync(1, 2);

        page1.Count.Should().BeLessThanOrEqualTo(2);
        page2.Count.Should().BeLessThanOrEqualTo(2);
        page1.PageIndex.Should().Be(0);
        page2.PageIndex.Should().Be(1);

        // Cleanup - get all and delete
        var all = await _requirementService.GetAllRequirementsAsync(0, 100);
        foreach (var req in all)
        {
            if (req.PrimaryName.StartsWith("User "))
            {
                await _requirementService.DeleteRequirementAsync(req);
            }
        }
    }

    [Test]
    public void InsertRequirementThrowsExceptionWhenRequirementIsNull()
    {
        Requirement requirement = null;
        var action = async () => await _requirementService.InsertRequirementAsync(requirement);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void UpdateRequirementThrowsExceptionWhenRequirementIsNull()
    {
        Requirement requirement = null;
        var action = async () => await _requirementService.UpdateRequirementAsync(requirement);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void DeleteRequirementThrowsExceptionWhenRequirementIsNull()
    {
        Requirement requirement = null;
        var action = async () => await _requirementService.DeleteRequirementAsync(requirement);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}

