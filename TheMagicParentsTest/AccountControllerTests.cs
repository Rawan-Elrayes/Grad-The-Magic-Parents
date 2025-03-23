//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using SixLabors.ImageSharp.PixelFormats;
//using SixLabors.ImageSharp;
//using System;
//using System.IO;
//using System.Threading.Tasks;
//using TheMagicParents.API.Controllers;
//using TheMagicParents.Core.DTOs;
//using TheMagicParents.Core.Interfaces;
//using TheMagicParents.Enums;
//using TheMagicParents.Models;
//using Xunit;

//namespace TheMagicParents.Tests
//{
//    public class AccountControllerTests
//    {
//        private readonly Mock<IClientRepository> _mockClientRepo;
//        private readonly Mock<IUserRepository> _mockUserRepo;
//        private readonly AccountController _controller;

//        public AccountControllerTests()
//        {
//            _mockClientRepo = new Mock<IClientRepository>();
//            _mockUserRepo = new Mock<IUserRepository>();
//            _controller = new AccountController(_mockClientRepo.Object, _mockUserRepo.Object);
//        }

//        [Fact]
//        public async Task RegisterClient_ValidModel_ReturnsOkResult()
//        {
//            // Arrange
//            var model = new ClientRegisterDTO
//            {
//                UserName = "testuser",
//                PhoneNumber = "01234567890",
//                Email = "test@example.com",
//                Password = "P@ssw0rd",
//                PersonalPhoto = CreateMockFormFile("personal.jpg"),
//                IdCardFrontPhoto = CreateMockFormFile("front.jpg"),
//                IdCardBackPhoto = CreateMockFormFile("back.jpg"),
//                CityId = 1,
//                Location = "123 Main St"
//            };

//            _mockUserRepo.Setup(repo => repo.GenerateUserNameIdFromEmailAsync(It.IsAny<string>()))
//                .ReturnsAsync("testuser");

//            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<Client>()))
//                .Returns(Task.CompletedTask);

//            var httpContext = CreateMockHttpContext();
//            _controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = httpContext
//            };

//            // Act
//            var result = await _controller.RegisterClient(model);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.Equal("Client registered successfully", okResult.Value.GetType().GetProperty("Message").GetValue(okResult.Value));
//            Assert.Equal("testuser", okResult.Value.GetType().GetProperty("UserNameId").GetValue(okResult.Value));
//        }

//        [Fact]
//        public async Task RegisterClient_InvalidModel_ReturnsBadRequest()
//        {
//            // Arrange
//            var model = new ClientRegisterDTO(); // نموذج غير صالح
//            _controller.ModelState.AddModelError("UserName", "Required");

//            // Act
//            var result = await _controller.RegisterClient(model);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            var serializableError = Assert.IsType<SerializableError>(badRequestResult.Value);
//            Assert.True(serializableError.ContainsKey("UserName"));
//        }

//        [Fact]
//        public async Task RegisterClient_ExceptionThrown_ReturnsInternalServerError()
//        {
//            // Arrange
//            var model = new ClientRegisterDTO
//            {
//                UserName = "testuser",
//                PhoneNumber = "01234567890",
//                Email = "test@example.com",
//                Password = "P@ssw0rd",
//                PersonalPhoto = CreateMockFormFile("personal.jpg"),
//                IdCardFrontPhoto = CreateMockFormFile("front.jpg"),
//                IdCardBackPhoto = CreateMockFormFile("back.jpg"),
//                CityId = 1,
//                Location = "123 Main St"
//            };

//            _mockUserRepo.Setup(repo => repo.GenerateUserNameIdFromEmailAsync(It.IsAny<string>()))
//                .ReturnsAsync("testuser");

//            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<Client>()))
//                .ThrowsAsync(new Exception("Test exception"));

//            // Act
//            var result = await _controller.RegisterClient(model);

//            // Assert
//            var statusCodeResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, statusCodeResult.StatusCode);
//            Assert.Equal("An error occurred while registering the client.", statusCodeResult.Value.GetType().GetProperty("Message").GetValue(statusCodeResult.Value));
//            Assert.Equal("Test exception", statusCodeResult.Value.GetType().GetProperty("Error").GetValue(statusCodeResult.Value));
//        }

//        [Fact]
//        public async Task RegisterClient_EmailAlreadyExists_ReturnsBadRequest()
//        {
//            // Arrange
//            var model = new ClientRegisterDTO
//            {
//                UserName = "testuser",
//                PhoneNumber = "01234567890",
//                Email = "test@example.com",
//                Password = "P@ssw0rd",
//                PersonalPhoto = CreateMockFormFile("personal.jpg"),
//                IdCardFrontPhoto = CreateMockFormFile("front.jpg"),
//                IdCardBackPhoto = CreateMockFormFile("back.jpg"),
//                CityId = 1,
//                Location = "123 Main St"
//            };

//            _mockUserRepo.Setup(repo => repo.GenerateUserNameIdFromEmailAsync(It.IsAny<string>()))
//                .ReturnsAsync("testuser");

//            _mockClientRepo.Setup(repo => repo.RegisterClientAsync(It.IsAny<Client>()))
//                .ThrowsAsync(new InvalidOperationException("Email is already registered."));

//            var httpContext = CreateMockHttpContext();
//            _controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = httpContext
//            };

//            // Act
//            var result = await _controller.RegisterClient(model);

//            // Assert
//            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
//            Assert.Equal("Email is already registered.", badRequestResult.Value.GetType().GetProperty("Message").GetValue(badRequestResult.Value));
//        }

//        private IFormFile CreateMockFormFile(string fileName)
//        {
//            var fileMock = new Mock<IFormFile>();

//            // إنشاء صورة وهمية باستخدام ImageSharp
//            using (var image = new Image<Rgba32>(100, 100)) // صورة بحجم 100x100
//            {
//                var ms = new MemoryStream();
//                image.SaveAsJpeg(ms); // حفظ الصورة بصيغة JPEG
//                ms.Position = 0;

//                fileMock.Setup(f => f.FileName).Returns(fileName);
//                fileMock.Setup(f => f.Length).Returns(ms.Length);
//                fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

//                return fileMock.Object;
//            }
//        }

//        private HttpContext CreateMockHttpContext()
//        {
//            var httpContext = new DefaultHttpContext();
//            httpContext.Request.Scheme = "http";
//            httpContext.Request.Host = new HostString("localhost:5000");
//            return httpContext;
//        }
//    }
//}