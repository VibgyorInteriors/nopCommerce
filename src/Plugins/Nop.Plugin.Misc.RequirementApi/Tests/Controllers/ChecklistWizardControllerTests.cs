using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nop.Core;
using Nop.Plugin.Misc.RequirementApi.Controllers;
using Nop.Plugin.Misc.RequirementApi.Domain;
using Nop.Plugin.Misc.RequirementApi.Models.DTO;
using Nop.Plugin.Misc.RequirementApi.Services;
using NUnit.Framework;

namespace Nop.Plugin.Misc.RequirementApi.Tests.Controllers;

[TestFixture]
public class ChecklistWizardControllerTests
{
    #region Fields

    private Mock<ChecklistWizardService> _mockChecklistWizardService;
    private ChecklistWizardController _controller;

    #endregion

    #region SetUp

    [SetUp]
    public void Setup()
    {
        var mockRequirementService = new Mock<RequirementService>(Mock.Of<IRepository<Requirement>>());
        _mockChecklistWizardService = new Mock<ChecklistWizardService>(mockRequirementService.Object);
        _controller = new ChecklistWizardController(_mockChecklistWizardService.Object);
    }

    #endregion

    #region CreateRequirement Tests

    [Test]
    public async Task CreateRequirement_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "john@example.com",
            Status = "Draft"
        };

        _mockChecklistWizardService
            .Setup(x => x.CreateRequirementAsync(It.IsAny<ChecklistWizardCreateDTO>()))
            .ReturnsAsync(new ChecklistWizardDTO { Id = 1, CreatedOnUtc = DateTime.UtcNow, Status = "Draft" });

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<JsonResult>();
        _mockChecklistWizardService.Verify(x => x.CreateRequirementAsync(It.IsAny<ChecklistWizardCreateDTO>()), Times.Once);
    }

    [Test]
    public async Task CreateRequirement_SetsDefaultStatus_WhenStatusNotProvided()
    {
        // Arrange
        var request = new ChecklistWizardCreateDTO
        {
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "john@example.com"
        };

        _mockChecklistWizardService
            .Setup(x => x.CreateRequirementAsync(It.IsAny<ChecklistWizardCreateDTO>()))
            .ReturnsAsync(new ChecklistWizardDTO { Id = 1, CreatedOnUtc = DateTime.UtcNow, Status = "Draft" });

        // Act
        var result = await _controller.Create(request);

        // Assert
        result.Should().BeOfType<JsonResult>();
        _mockChecklistWizardService.Verify(x => x.CreateRequirementAsync(It.IsAny<ChecklistWizardCreateDTO>()), Times.Once);
    }

    #endregion

    #region GetRequirementById Tests

    [Test]
    public async Task GetRequirementById_ReturnsSuccess_WhenRequirementExists()
    {
        // Arrange
        var requirement = new Requirement
        {
            Id = 1,
            PrimaryName = "John Doe",
            MobileNumber = "1234567890",
            Email = "john@example.com",
            Status = "Draft",
            CreatedOnUtc = DateTime.UtcNow
        };

        _mockChecklistWizardService
            .Setup(x => x.GetRequirementByIdAsync(1))
            .ReturnsAsync(new ChecklistWizardDTO 
            { 
                Id = 1, 
                PrimaryName = "John Doe",
                MobileNumber = "1234567890",
                Email = "john@example.com",
                Status = "Draft",
                CreatedOnUtc = DateTime.UtcNow
            });

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = result as JsonResult;
        var value = jsonResult.Value as dynamic;
        Assert.That(value.success, Is.True);
        Assert.That(value.data.id, Is.EqualTo(1));
    }

    #endregion

    #region GetAllRequirements Tests

    [Test]
    public async Task GetAllRequirements_ReturnsSuccess_WithPagination()
    {
        // Arrange

        _mockChecklistWizardService
            .Setup(x => x.GetAllRequirementsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedChecklistWizardDTO
            {
                Data = new List<ChecklistWizardSummaryDTO>
                {
                    new ChecklistWizardSummaryDTO { Id = 1, PrimaryName = "User 1", Email = "user1@example.com", CreatedOnUtc = DateTime.UtcNow },
                    new ChecklistWizardSummaryDTO { Id = 2, PrimaryName = "User 2", Email = "user2@example.com", CreatedOnUtc = DateTime.UtcNow }
                },
                TotalCount = 2,
                PageIndex = 0,
                PageSize = 10
            });

        // Act
        var result = await _controller.GetAll(0, 10);

        // Assert
        result.Should().BeOfType<JsonResult>();
        var jsonResult = result as JsonResult;
        var value = jsonResult.Value as dynamic;
        Assert.That(value.success, Is.True);
        Assert.That(value.totalCount, Is.EqualTo(2));
    }

    #endregion

    #region UpdateRequirementById Tests

    [Test]
    public async Task UpdateRequirementById_ReturnsSuccess_WhenUpdateIsValid()
    {
        // Arrange
        var request = new ChecklistWizardCreateDTO
        {
            PrimaryName = "Updated Name",
            Email = "updated@example.com"
        };

        _mockChecklistWizardService
            .Setup(x => x.UpdateRequirementAsync(1, It.IsAny<ChecklistWizardCreateDTO>()))
            .ReturnsAsync(new ChecklistWizardDTO 
            { 
                Id = 1, 
                PrimaryName = "Updated Name",
                Email = "updated@example.com",
                UpdatedOnUtc = DateTime.UtcNow
            });

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        result.Should().BeOfType<JsonResult>();
        _mockChecklistWizardService.Verify(x => x.UpdateRequirementAsync(1, It.IsAny<ChecklistWizardCreateDTO>()), Times.Once);
    }

    [Test]
    public async Task UpdateRequirementById_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var request = new ChecklistWizardCreateDTO
        {
            PrimaryName = "Updated Name"
            // Only updating PrimaryName, other fields should remain unchanged
        };

        _mockChecklistWizardService
            .Setup(x => x.UpdateRequirementAsync(1, It.IsAny<ChecklistWizardCreateDTO>()))
            .ReturnsAsync(new ChecklistWizardDTO 
            { 
                Id = 1, 
                PrimaryName = "Updated Name",
                MobileNumber = "1111111111",
                Email = "original@example.com",
                UpdatedOnUtc = DateTime.UtcNow
            });

        // Act
        await _controller.Update(1, request);

        // Assert
        _mockChecklistWizardService.Verify(x => x.UpdateRequirementAsync(1, It.IsAny<ChecklistWizardCreateDTO>()), Times.Once);
    }

    #endregion

    #region DeleteRequirementById Tests

    [Test]
    public async Task DeleteRequirementById_ReturnsSuccess_WhenDeletionIsSuccessful()
    {
        // Arrange

        _mockChecklistWizardService
            .Setup(x => x.DeleteRequirementAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<JsonResult>();
        _mockChecklistWizardService.Verify(x => x.DeleteRequirementAsync(1), Times.Once);
    }

    #endregion
}

