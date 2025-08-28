namespace Common.Contracts;

public record UserRegistered(string UserId, string Phone);
public record PhoneVerified(string UserId, DateTime VerifiedAt);
public record ProfileUpdated(string UserId, string Field, string NewValue);
public record OnboardingCompleted(string UserId, DateTime CompletedAt);
