using FluentAssertions;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;
using Moq;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. GetByIdAsync - löytyy
    // 2. GetByIdAsync - ei löydy
    // 3. GetAllAsync - palauttaa listan
    // 4. UpdateAsync - onnistuu
    // 5. UpdateAsync - käyttäjää ei löydy
    // 6. DeleteAsync - onnistuu
    // 7. DeleteAsync - käyttäjää ei löydy
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsUserDto()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        User user = new User("Matti", "Meikäläinen", "matti@example.com");
        user.Id = id;
        user.CreatedAt = DateTime.UtcNow.AddDays(-1);
        user.UpdatedAt = DateTime.UtcNow;

        _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(user);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.FirstName.Should().Be("Matti");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsListOfUserDtos()
    {
        // Arrange
        var users = new[] {
            new User("Matti","Meikäläinen","matti@example.com") { Id = Guid.NewGuid() },
            new User("Maija","Virtanen","maija@example.com") { Id = Guid.NewGuid() }
        };

        _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Email).Should().Contain(new[] { "matti@example.com", "maija@example.com" });
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsDto()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        User user = new User("Matti", "Meikäläinen", "matti@example.com");
        user.Id = id;

        UpdateUserDto dto = new UpdateUserDto { FirstName = "Maija", LastName = "Virtanen", Email = "maija@example.com" };

        _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(user);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Maija");
        result.Email.Should().Be("maija@example.com");
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => u.Id == id)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        UpdateUserDto dto = new UpdateUserDto { FirstName = "Maija", LastName = "Virtanen", Email = "maija@example.com" };
        _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateAsync(id, dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesAndReturnsTrue()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _mockRepository.Setup(x => x.ExistsAsync(id)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.DeleteAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _mockRepository.Setup(x => x.ExistsAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}