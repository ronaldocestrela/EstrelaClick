using EstrelaClick.Shared.SharedKernel.Nfc;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class NfcServiceTests
{
    private readonly INfcService _nfcServiceMock = Substitute.For<INfcService>();

    [Fact]
    public void WhenNfcIsNotSupported_StartListening_ShouldReturnNotSupportedFailure()
    {
        // Arrange
        _nfcServiceMock.IsNfcSupported.Returns(false);
        _nfcServiceMock.StartListening().Returns(Result.Failure(NfcErrors.NotSupported));

        // Act
        var result = _nfcServiceMock.StartListening();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Nfc.NotSupported");
    }

    [Fact]
    public void WhenNfcIsDisabled_StartListening_ShouldReturnDisabledFailure()
    {
        // Arrange
        _nfcServiceMock.IsNfcSupported.Returns(true);
        _nfcServiceMock.IsNfcEnabled.Returns(false);
        _nfcServiceMock.StartListening().Returns(Result.Failure(NfcErrors.Disabled));

        // Act
        var result = _nfcServiceMock.StartListening();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Nfc.Disabled");
    }

    [Fact]
    public void WhenNfcIsEnabled_StartListening_ShouldReturnSuccess()
    {
        // Arrange
        _nfcServiceMock.IsNfcSupported.Returns(true);
        _nfcServiceMock.IsNfcEnabled.Returns(true);
        _nfcServiceMock.StartListening().Returns(Result.Success());

        // Act
        var result = _nfcServiceMock.StartListening();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task WriteUriRecordAsync_WhenTagIsLost_ShouldReturnTagLostFailure()
    {
        // Arrange
        _nfcServiceMock.WriteUriRecordAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NfcWriteResult>.Failure(NfcErrors.TagLost)));

        // Act
        var result = await _nfcServiceMock.WriteUriRecordAsync("https://estrelaclick.com/r/xyz123");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Nfc.TagLost");
    }

    [Fact]
    public async Task WriteUriRecordAsync_WhenTagIsReadOnly_ShouldReturnTagReadOnlyFailure()
    {
        // Arrange
        _nfcServiceMock.WriteUriRecordAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NfcWriteResult>.Failure(NfcErrors.TagReadOnly)));

        // Act
        var result = await _nfcServiceMock.WriteUriRecordAsync("https://estrelaclick.com/r/xyz123");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Nfc.TagReadOnly");
    }

    [Fact]
    public async Task WriteUriRecordAsync_WhenSuccessful_ShouldReturnWrittenUidAndBytesCount()
    {
        // Arrange
        var expectedWriteResult = new NfcWriteResult("04:A1:B2:C3:D4:E5:F6", 32, IsLocked: false);
        _nfcServiceMock.WriteUriRecordAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NfcWriteResult>.Success(expectedWriteResult)));

        // Act
        var result = await _nfcServiceMock.WriteUriRecordAsync("https://estrelaclick.com/r/xyz123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Uid.Should().Be("04:A1:B2:C3:D4:E5:F6");
        result.Value.BytesWritten.Should().Be(32);
        result.Value.IsLocked.Should().BeFalse();
    }
}
