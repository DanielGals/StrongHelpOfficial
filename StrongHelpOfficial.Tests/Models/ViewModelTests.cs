using StrongHelpOfficial.Models;
using Xunit;

namespace StrongHelpOfficial.Tests.Models;

public class ViewModelTests
{
    [Fact]
    public void UserInfoViewModel_DefaultValues_AreCorrect()
    {
        var model = new UserInfoViewModel();

        Assert.False(model.IsAuthenticated);
        Assert.False(model.SQLConnectionSuccess);
        Assert.False(model.EmailExists);
        Assert.False(model.EmailMatched);
        Assert.Null(model.IsActive);
    }

    [Fact]
    public void ErrorViewModel_RequestId_CanBeSet()
    {
        var model = new ErrorViewModel { RequestId = "test-123" };

        Assert.Equal("test-123", model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public void ErrorViewModel_ShowRequestId_ReturnsFalse_WhenRequestIdIsNull()
    {
        var model = new ErrorViewModel { RequestId = null };

        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public void ApplyForLoanViewModel_RequiredDocuments_CanBeInitialized()
    {
        var model = new ApplyForLoanViewModel
        {
            RequiredDocuments = new List<string> { "Document1", "Document2" }
        };

        Assert.Equal(2, model.RequiredDocuments.Count);
        Assert.Contains("Document1", model.RequiredDocuments);
    }

    [Fact]
    public void ApplyForLoanViewModel_LoanAmount_CanBeSet()
    {
        var model = new ApplyForLoanViewModel { LoanAmount = 50000 };

        Assert.Equal(50000, model.LoanAmount);
    }
}
