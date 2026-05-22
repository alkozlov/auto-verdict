namespace AutoVerdict.Application.Checks;

public sealed class InsufficientCreditsException() : Exception("User does not have enough credits to create a check.");
