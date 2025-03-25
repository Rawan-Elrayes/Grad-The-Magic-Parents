using Xunit;
using Moq;
using TheMagicParents.API.Controllers;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;

namespace TheMagicParents.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IClientRepository> _mockClientRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockClientRepo = new Mock<IClientRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _controller = new AccountController(_mockClientRepo.Object, _mockUserRepo.Object);
        }

        [Fact]
        public async Task GetGovernments_ReturnsOkResult_WithGovernmentsList()
        {
            // Arrange
            var mockGovernments = new List<Governorate>
            {
                new Governorate { Id = 1, Name = "Cairo" },
                new Governorate { Id = 2, Name = "Alexandria" }
            };

            _mockUserRepo.Setup(repo => repo.GetGovernmentsAsync())
                .ReturnsAsync(mockGovernments);

            // Act
            var result = await _controller.GetGovernments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedGovernments = Assert.IsType<List<Governorate>>(okResult.Value);
            Assert.Equal(2, returnedGovernments.Count);
        }

        [Fact]
        public async Task GetCitiesByGovernment_ReturnsOkResult_WithCitiesList()
        {
            // Arrange
            var governmentId = 1;
            var mockCities = new List<City>
            {
                new City { Id = 1, Name = "Nasr City", GovernorateId = governmentId },
                new City { Id = 2, Name = "Maadi", GovernorateId = governmentId }
            };

            _mockUserRepo.Setup(repo => repo.GetCitiesByGovernmentAsync(governmentId))
                .ReturnsAsync(mockCities);

            // Act
            var result = await _controller.GetCitiesByGovernment(governmentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCities = Assert.IsType<List<City>>(okResult.Value);
            Assert.Equal(2, returnedCities.Count);
            Assert.All(returnedCities, city => Assert.Equal(governmentId, city.GovernorateId));
        }

        [Fact]
        public async Task RegisterClient_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Email is required");
            var model = new ClientRegisterDTO();

            // Act
            var result = await _controller.RegisterClient(model);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterClient_ReturnsOkResult_WhenRegistrationSuccessful()
        {
            // Arrange
            var model = new ClientRegisterDTO
            {
                UserName = "testuser",
                Email = "test@example.com",
                Password = "P@ssw0rd!",
                PhoneNumber = "01234567890",
                CityId = 1,
                Location = "Test Location",
                PersonalPhoto = CreateMockFormFile("profile.jpg"),
                IdCardFrontPhoto = CreateMockFormFile("front.jpg"),
                IdCardBackPhoto = CreateMockFormFile("back.jpg")
            };

            var expectedResponse = new ClientRegisterResponse
            {
                UserName = "testuser",
                Email = "test@example.com",
                Token = "mock-token",
                Expires = DateTime.Now.AddDays(1)
            };

            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<ClientRegisterDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RegisterClient(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response<ClientRegisterResponse>>(okResult.Value);
            Assert.Equal("Client registered successfully", response.Message);
            Assert.Equal(expectedResponse.Email, response.Data.Email);
        }

        [Fact]
        public async Task RegisterClient_ReturnsBadRequest_WhenEmailExists()
        {
            // Arrange
            var model = new ClientRegisterDTO
            {
                Email = "existing@example.com"
            };

            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<ClientRegisterDTO>()))
                .ThrowsAsync(new InvalidOperationException("Email is already registered."));

            // Act
            var result = await _controller.RegisterClient(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<Response<ClientRegisterResponse>>(badRequestResult.Value);
            Assert.False(response.Status);
            Assert.Equal("Email is already registered.", response.Message);
            Assert.Contains("Email is already registered.", response.Errors);
        }

        [Fact]
        public async Task RegisterClient_ReturnsStatusCode500_WhenExceptionOccurs()
        {
            // Arrange
            var model = new ClientRegisterDTO();
            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<ClientRegisterDTO>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.RegisterClient(model);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<Response<ClientRegisterResponse>>(statusCodeResult.Value);
            Assert.False(response.Status);
            Assert.Equal("An error occurred while registering the client.", response.Message);
            Assert.Contains("Test exception", response.Errors);
        }



        private IFormFile CreateMockFormFile(string fileName)
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write("test file content");
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

            return fileMock.Object;
        }
    }
}