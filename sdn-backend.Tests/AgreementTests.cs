using SdnBackend.Models;

namespace SdnBackend.Tests;

public class AgreementTests
{
    // Helper to build a minimal valid Agreement without repeating boilerplate in every test
    private static Agreement BuildAgreement() => new Agreement
    {
        Date = new DateOnly(2025, 6, 1),
        Time = new TimeOnly(10, 0),
        Service = new Service(1, "Manicure", 30),
        Tech = new Technician(1, "Alice", "alice@example.com"),
        Client = new Client(2, "Bob", "bob@example.com"),
        Salon = new Salon(1)
    };

    [Fact]
    public void MarkPending_SetsStatusToPending()
    {
        // Arrange
        var agreement = BuildAgreement();

        // Act
        agreement.MarkPending();

        // Assert
        Assert.Equal(AppointmentStatus.pending, agreement.ApptStatus);
    }

    [Fact]
    public void MarkPending_ReturnsNonEmptyToken()
    {
        // Arrange
        var agreement = BuildAgreement();

        // Act
        var token = agreement.MarkPending();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void MarkPending_StoresHashOfReturnedToken()
    {
        // Arrange
        var agreement = BuildAgreement();

        // Act
        var token = agreement.MarkPending();
        var expectedHash = Agreement.HashToken(token);

        // Assert
        Assert.Equal(expectedHash, agreement.ConfirmTokenHash);
    }

    [Fact]
    public void MarkPending_SetsExpirationToApproximately30MinutesFromNow()
    {
        // Arrange
        var agreement = BuildAgreement();
        var before = DateTime.Now;

        // Act
        agreement.MarkPending();
        var after = DateTime.Now;

        // Assert
        Assert.NotNull(agreement.ExpireTimestamp);
        Assert.InRange(
            agreement.ExpireTimestamp.Value,
            before.AddMinutes(30),
            after.AddMinutes(30)
        );
    }

    [Fact]
    public void MarkPending_CalledTwice_GeneratesDifferentTokens()
    {
        // Arrange
        var agreement = BuildAgreement();

        // Act
        var firstToken = agreement.MarkPending();
        var secondToken = agreement.MarkPending();

        // Assert
        Assert.NotEqual(firstToken, secondToken);
    }
}
