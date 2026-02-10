using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using Xunit;

namespace SimpleExample.Tests.API;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockService = new Mock<IUserService>();
        _controller = new UsersController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithUsers()
    {
        // Arrange
        List<UserDto> users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), FirstName = "Matti", LastName = "M", Email = "m@m.com" },
            new UserDto { Id = Guid.NewGuid(), FirstName = "Maija", LastName = "V", Email = "m@v.com" }
        };

        _mockService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        ActionResult<IEnumerable<UserDto>> result = await _controller.GetAll();

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        IEnumerable<UserDto> returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UserDto user = new UserDto { Id = userId, FirstName = "Matti", LastName = "M", Email = "m@m.com" };

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        UserDto createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        UserDto returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.FirstName.Should().Be("Matti");
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. Create - InvalidOperationException (duplicate) → 409 Conflict
    // 2. Create - ArgumentException (validation) → 400 BadRequest
    // 3. Update - onnistuu → 200 OK
    // 4. Update - käyttäjää ei löydy → 404 NotFound
    // 5. Update - ArgumentException → 400 BadRequest
    // 6. Delete - onnistuu → 204 NoContent
    // 7. Delete - käyttäjää ei löydy → 404 NotFound

    [Fact]
    public async Task Create_WhenDuplicate_ReturnsConflict()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto { FirstName = "Matti", LastName = "M", Email = "dup@ex.com" };
        _mockService.Setup(x => x.CreateAsync(createDto)).ThrowsAsync(new InvalidOperationException("duplicate"));

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_WhenInvalid_ReturnsBadRequest()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto { FirstName = "AB", LastName = "M", Email = "invalid" };
        _mockService.Setup(x => x.CreateAsync(createDto)).ThrowsAsync(new ArgumentException("validation"));

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        UpdateUserDto updateDto = new UpdateUserDto { FirstName = "Maija", LastName = "V", Email = "maija@ex.com" };
        UserDto updated = new UserDto { Id = id, FirstName = updateDto.FirstName, LastName = updateDto.LastName, Email = updateDto.Email };
        _mockService.Setup(x => x.UpdateAsync(id, updateDto)).ReturnsAsync(updated);

        // Act
        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        // Assert
        OkObjectResult ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<UserDto>();
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        UpdateUserDto updateDto = new UpdateUserDto { FirstName = "Maija", LastName = "V", Email = "maija@ex.com" };
        _mockService.Setup(x => x.UpdateAsync(id, updateDto)).ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenValidationFails_ReturnsBadRequest()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        UpdateUserDto updateDto = new UpdateUserDto { FirstName = "AB", LastName = "V", Email = "bad" };
        _mockService.Setup(x => x.UpdateAsync(id, updateDto)).ThrowsAsync(new ArgumentException("validation"));

        // Act
        ActionResult<UserDto> result = await _controller.Update(id, updateDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _mockService.Setup(x => x.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        ActionResult result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _mockService.Setup(x => x.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        ActionResult result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}