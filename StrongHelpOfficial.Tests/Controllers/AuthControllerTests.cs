using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StrongHelpOfficial.Controllers;
using StrongHelpOfficial.Models;
using Xunit;

namespace StrongHelpOfficial.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ILogger<HomeController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ISession> _mockSession;

    public AuthControllerTests()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockSession = new Mock<ISession>();

        _mockHttpContext.Setup(x => x.Session).Returns(_mockSession.Object);
        
        _controller = new AuthController(_mockLogger.Object, _mockConfiguration.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };
    }

    [Fact]
    public void Selection_WithValidSession_ReturnsView()
    {
        byte[] roleNameBytes = System.Text.Encoding.UTF8.GetBytes("Admin");
        _mockSession.Setup(x => x.TryGetValue("RoleName", out roleNameBytes)).Returns(true);

        var result = _controller.Selection();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Selection_WithoutSession_RedirectsToLogin()
    {
        byte[] emptyBytes = Array.Empty<byte>();
        _mockSession.Setup(x => x.TryGetValue("RoleName", out emptyBytes)).Returns(false);

        var result = _controller.Selection();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
    }

    [Fact]
    public void SwitchToEmployee_WithValidSession_RedirectsToLoanerDashboard()
    {
        byte[] roleNameBytes = System.Text.Encoding.UTF8.GetBytes("Employee");
        _mockSession.Setup(x => x.TryGetValue("RoleName", out roleNameBytes)).Returns(true);

        var result = _controller.SwitchToEmployee();

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("LoanerDashboard", redirectResult.ControllerName);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewWithErrorModel()
    {
        var result = _controller.Error();
        
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ErrorViewModel>(viewResult.Model);
    }
}
